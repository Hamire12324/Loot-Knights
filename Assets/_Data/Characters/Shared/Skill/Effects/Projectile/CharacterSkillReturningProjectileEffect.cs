using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillReturningProjectileEffect", menuName = "Loot Knights/Character/Skill Effects/Returning Projectile")]
public sealed class CharacterSkillReturningProjectileEffect : CharacterSkillEffectDefinition
{
    [Header("Projectile")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0.05f)] private float distance = 7f;
    [SerializeField, Min(0f)] private float startOffset = 0.55f;
    [SerializeField, Min(0.05f)] private float speed = 8f;
    [SerializeField, Min(0f)] private float returnDelay = 0.5f;
    [SerializeField] private float rotationOffset;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField, Min(0.05f)] private float hitRadius = 0.45f;

    [Header("Damage")]
    [SerializeField] private DamageData outboundDamage = new(1f, true);
    [SerializeField] private DamageData returnDamage = new(1.15f, true);
    [SerializeField] private float flatBonusDamage;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        context.Controller.StartCoroutine(TravelRoutine(
            context.Caster,
            context.AimDirection,
            context.Definition != null ? context.Definition.SkillId : null,
            context.Runtime));
    }

    private IEnumerator TravelRoutine(CharacterCtrl caster, Vector2 direction, string skillId, CharacterSkillRuntime runtime)
    {
        Vector2 normalizedDirection = direction == Vector2.zero ? Vector2.down : direction.normalized;
        Vector3 start = caster.transform.position + (Vector3)(normalizedDirection * startOffset);
        float effectiveDistance = distance +
            SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ProjectileLength);
        Vector3 outwardEnd = start + (Vector3)(normalizedDirection * effectiveDistance);
        PoolObj visual = CharacterSkillVfxUtility.Play(projectileVfx, start, normalizedDirection);
        Transform mover = visual != null ? visual.transform : null;
        HashSet<CharacterCtrl> outboundHits = new();
        HashSet<CharacterCtrl> returnHits = new();
        float effectiveSpeed = speed +
            SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ProjectileSpeed);
        float effectiveHitRadius = hitRadius +
            SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.Radius);
        float damageMultiplierBonus = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ReturnDamageMultiplier);

        yield return TravelSegment(caster, mover, start, outwardEnd, outboundDamage, outboundHits, effectiveSpeed, effectiveHitRadius, damageMultiplierBonus);
        if (returnDelay > 0f)
            yield return new WaitForSeconds(returnDelay);

        DamageData effectiveReturnDamage = GetReturnDamage(caster, skillId, outboundHits.Count);
        yield return TravelSegment(caster, mover, outwardEnd, caster.transform.position, effectiveReturnDamage, returnHits, effectiveSpeed, effectiveHitRadius, damageMultiplierBonus);

        ApplyEnergyReclaim(caster, runtime, skillId);

        if (visual != null)
            visual.ReturnToPool();
    }

    private DamageData GetReturnDamage(CharacterCtrl caster, string skillId, int outboundHitCount)
    {
        float returnDamagePerHit = SkillTreeSkillModifierResolver.GetValue(
            caster,
            skillId,
            SkillModifierType.ReturnDamagePerOutboundHit);
        int maximumStacks = Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(
            caster,
            skillId,
            SkillModifierType.ReturnDamageStackCap));

        float damageMultiplierBonus = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.DamageMultiplier);
        if (returnDamage == null || (returnDamagePerHit <= 0f && damageMultiplierBonus <= 0f))
            return returnDamage;

        DamageData scaledDamage = returnDamage.CloneWithElement(returnDamage.Element);
        if (returnDamagePerHit > 0f && outboundHitCount > 0)
            scaledDamage.Multiplier += Mathf.Min(Mathf.Max(1, maximumStacks), outboundHitCount) * returnDamagePerHit;

        scaledDamage.Multiplier += damageMultiplierBonus;
        return scaledDamage;
    }

    private void ApplyEnergyReclaim(CharacterCtrl caster, CharacterSkillRuntime runtime, string skillId)
    {
        if (caster == null)
            return;

        float cooldownRefund = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.CooldownRefund);
        float manaRestorePercent = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ManaRestoreMaxPercent);
        float moveSpeedPercent = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.TemporaryMoveSpeedPercent);
        float moveSpeedDuration = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.TemporaryMoveSpeedDuration);
        if (cooldownRefund <= 0f && manaRestorePercent <= 0f && moveSpeedPercent <= 0f)
            return;

        runtime?.ReduceCooldown(cooldownRefund);

        float maxMana = caster.CharacterStat?.MaxMana != null ? caster.CharacterStat.MaxMana.FinalValue : 0f;
        caster.CharacterStat?.RestoreMana(maxMana * manaRestorePercent);

        StatValue moveSpeed = caster.CharacterStat != null ? caster.CharacterStat.GetStat(StatType.MoveSpeed) : null;
        if (moveSpeed == null || moveSpeedPercent <= 0f || moveSpeedDuration <= 0f)
            return;

        moveSpeed.AddBuffModifier(new StatModifier(
            StatType.MoveSpeed,
            ModifierType.PercentAdd,
            moveSpeedPercent,
            this,
            moveSpeedDuration));
        moveSpeed.NotifyValueChanged();
        caster.CharacterStat.NotifyAllStatsChanged();
    }

    private IEnumerator TravelSegment(
        CharacterCtrl caster,
        Transform mover,
        Vector3 start,
        Vector3 end,
        DamageData damageData,
        HashSet<CharacterCtrl> hitTargets,
        float travelSpeed,
        float radius,
        float damageMultiplierBonus)
    {
        float length = Vector3.Distance(start, end);
        float elapsed = 0f;
        float travelDuration = length / Mathf.Max(0.05f, travelSpeed);

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / travelDuration);
            Vector3 position = Vector3.Lerp(start, end, progress);
            Vector2 movementDirection = ((Vector2)(end - start)).normalized;

            if (mover != null)
            {
                mover.position = position;
                float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
                mover.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
            }

            List<CharacterCtrl> targets = new();
            CharacterSkillTargetUtility.FindCircleTargets(caster, position, radius, targetLayer, targets);
            for (int i = 0; i < targets.Count; i++)
            {
                CharacterCtrl target = targets[i];
                if (target != null && hitTargets.Add(target))
                    CharacterSkillDamageUtility.DealDamage(caster, target, damageData, flatBonusDamage, damageMultiplierBonus);
            }

            yield return null;
        }
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0.05f, distance);
        startOffset = Mathf.Max(0f, startOffset);
        speed = Mathf.Max(0.05f, speed);
        returnDelay = Mathf.Max(0f, returnDelay);
        hitRadius = Mathf.Max(0.05f, hitRadius);
        outboundDamage ??= new DamageData(1f, true);
        returnDamage ??= new DamageData(1.15f, true);
    }
}
