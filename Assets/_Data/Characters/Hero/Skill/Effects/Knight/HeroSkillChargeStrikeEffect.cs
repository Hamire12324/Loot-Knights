using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillChargeStrikeEffect", menuName = "Loot Knights/Hero/Skill Effects/Charge Strike")]
public class HeroSkillChargeStrikeEffect : CharacterSkillEffectDefinition
{
    private const string CleavingArcNodeId = "knight.cleaving_arc";
    private const string RallyingCryNodeId = "knight.rallying_cry";

    [Header("Movement")]
    [SerializeField, Min(0f)] private float distance = 2.4f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
    [SerializeField, Min(0f)] private float stopDistanceFromTarget = 0.75f;
    [SerializeField] private bool invincibleDuringDash = true;

    [Header("Slash Timing")]
    [SerializeField, Min(0f)] private float slashHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float attackVisualDuration = 0.75f;

    [Header("Impact Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.1f;
    [SerializeField, Range(1f, 360f)] private float angle = 80f;
    [SerializeField] private float forwardOffset = 0.55f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1.4f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float rallyingCryLifeStealPercentPerRank = 0.05f;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition impactVfx;
    [SerializeField] private SFXDefinition impactSfx;

    private static readonly Collider2D[] Hits = new Collider2D[24];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Controller == null || context.Caster == null) return;

        context.Controller.StartCoroutine(ChargeRoutine(
            context.Controller,
            context.Caster,
            context.AimDirection,
            context.Target));
    }

    private IEnumerator ChargeRoutine(
        CharacterSkillController controller,
        CharacterCtrl caster,
        Vector2 direction,
        Transform target)
    {
        if (caster.Rb == null) yield break;

        int cleavingArcRank = SkillTreeRankResolver.GetRank(caster, CleavingArcNodeId);
        int rallyingCryRank = SkillTreeRankResolver.GetRank(caster, RallyingCryNodeId);
        Vector2 start = caster.Rb.position;
        Vector2 end = GetDashEndPosition(start, direction, target, cleavingArcRank);
        direction = end - start;

        if (direction.sqrMagnitude <= 0.001f)
            direction = GetAimDirection(start, direction, target);

        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
        caster.CharacterMovement?.SetLookDirection(direction);

        CharacterDamReceiver receiver = caster.CharacterDamReceiver;
        bool previousInvincible = receiver != null && receiver.IsInvincible;

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(true);

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);
            caster.Rb.MovePosition(Vector2.Lerp(start, end, t));
            yield return null;
        }

        caster.Rb.MovePosition(end);

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(previousInvincible);

        FaceHorizontalDirection(caster.transform, direction);

        controller.PlaySkillAttackAnimation();

        if (slashHitDelay > 0f)
            yield return new WaitForSeconds(slashHitDelay);

        PlayImpactFeedback((Vector2)caster.transform.position + direction * forwardOffset, direction, caster.transform);
        DealImpactDamage(caster, direction, cleavingArcRank, rallyingCryRank);
        ApplyRallyingCryBuff(caster, rallyingCryRank);

        float restoreDelay = Mathf.Max(0f, attackVisualDuration - slashHitDelay);
        if (restoreDelay > 0f)
            yield return new WaitForSeconds(restoreDelay);

    }

    private void DealImpactDamage(CharacterCtrl caster, Vector2 direction, int cleavingArcRank, int rallyingCryRank)
    {
        Vector2 origin = (Vector2)caster.transform.position + direction * forwardOffset;
        int layerMask = targetLayer.value != 0
            ? targetLayer
            : caster.CharacterTargetFinder != null
                ? caster.CharacterTargetFinder.TargetLayer
                : Physics2D.DefaultRaycastLayers;

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            useTriggers = true,
            layerMask = layerMask
        };

        float effectiveRadius = radius + cleavingArcRank * 0.12f;
        int count = Physics2D.OverlapCircle(origin, effectiveRadius, filter, Hits);
        DamagedTargets.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null) continue;

            CharacterCtrl target = hit.GetComponentInParent<CharacterCtrl>();
            if (target == null || target == caster) continue;
            if (!IsTargetBodyCollider(hit, target)) continue;
            if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead) continue;
            if (!FactionManager.CanAttack(caster.Faction, target.Faction)) continue;
            if (!DamagedTargets.Add(target.CharacterDamReceiver)) continue;
            if (!IsInsideAngle(caster.transform.position, direction, target.transform.position, cleavingArcRank)) continue;

            float damage = CalculateDamage(caster, cleavingArcRank);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
            ApplyLifeSteal(caster, damage, rallyingCryRank);
        }

        DamagedTargets.Clear();
    }

    private void ApplyLifeSteal(CharacterCtrl caster, float damage, int rallyingCryRank)
    {
        if (caster == null || rallyingCryRank <= 0 || damage <= 0f)
            return;

        float healAmount = damage * rallyingCryRank * rallyingCryLifeStealPercentPerRank;
        if (healAmount > 0f)
            caster.CharacterDamReceiver?.Heal(healAmount);
    }

    private bool IsInsideAngle(Vector2 casterPosition, Vector2 aimDirection, Vector2 targetPosition, int cleavingArcRank)
    {
        float effectiveAngle = Mathf.Min(360f, angle + cleavingArcRank * 12f);
        if (effectiveAngle >= 359f) return true;

        Vector2 toTarget = targetPosition - casterPosition;
        if (toTarget.sqrMagnitude <= 0.001f) return true;

        return Vector2.Angle(aimDirection, toTarget.normalized) <= effectiveAngle * 0.5f;
    }

    private static bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (target.Collider2D == null) return true;
        return hitCollider == target.Collider2D;
    }

    private float CalculateDamage(CharacterCtrl caster, int cleavingArcRank)
    {
        float multiplier = damageData != null ? damageData.Multiplier : 1f;
        multiplier += cleavingArcRank * 0.1f;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private void ApplyRallyingCryBuff(CharacterCtrl caster, int rank)
    {
        if (rank <= 0 || caster == null || caster.CharacterStat == null)
            return;

        StatValue attackSpeed = caster.CharacterStat.GetStat(StatType.AttackSpeed);
        if (attackSpeed == null)
            return;

        attackSpeed.AddBuffModifier(new StatModifier(StatType.AttackSpeed, ModifierType.Flat, rank * 0.08f, this, 3f));
        attackSpeed.NotifyValueChanged();
    }

    private void PlayImpactFeedback(Vector3 position, Vector2 direction, Transform target)
    {
        if (impactVfx != null && VFXManager.HasInstance)
            VFXManager.InstanceOrNull.Play(impactVfx, position, direction, target);

        if (impactSfx != null)
            SFXManager.Play(impactSfx, position);
    }

    private Vector2 GetDashEndPosition(Vector2 start, Vector2 fallbackDirection, Transform target, int cleavingArcRank)
    {
        if (target == null)
            return start + GetAimDirection(start, fallbackDirection, target) * GetEffectiveDistance(cleavingArcRank);

        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - start;

        if (toTarget.sqrMagnitude <= 0.001f)
            return start;

        Vector2 targetDirection = toTarget.normalized;
        Vector2 desired = targetPosition - targetDirection * stopDistanceFromTarget;
        Vector2 dash = desired - start;

        if (dash.sqrMagnitude <= 0.001f)
            return start;

        float maxDistance = GetEffectiveDistance(cleavingArcRank);
        if (dash.magnitude > maxDistance)
            return start + dash.normalized * maxDistance;

        return desired;
    }

    private float GetEffectiveDistance(int cleavingArcRank)
    {
        return Mathf.Max(0f, distance + cleavingArcRank * 0.25f);
    }

    private static Vector2 GetAimDirection(Vector2 start, Vector2 fallbackDirection, Transform target)
    {
        if (target != null)
        {
            Vector2 toTarget = (Vector2)target.position - start;
            if (toTarget.sqrMagnitude > 0.001f)
                return toTarget.normalized;
        }

        return fallbackDirection.sqrMagnitude > 0.001f
            ? fallbackDirection.normalized
            : Vector2.down;
    }

    private static bool FaceHorizontalDirection(Transform casterTransform, Vector2 direction)
    {
        if (casterTransform == null || Mathf.Abs(direction.x) <= 0.01f)
            return false;

        Vector3 scale = casterTransform.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = direction.x >= 0f ? absX : -absX;
        casterTransform.localScale = scale;
        return true;
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        stopDistanceFromTarget = Mathf.Max(0f, stopDistanceFromTarget);
        slashHitDelay = Mathf.Max(0f, slashHitDelay);
        attackVisualDuration = Mathf.Max(0f, attackVisualDuration);

        if (damageData == null)
            damageData = new DamageData(1f, false);

        rallyingCryLifeStealPercentPerRank = Mathf.Max(0f, rallyingCryLifeStealPercentPerRank);
    }
}
