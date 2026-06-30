using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillWhirlwindEffect", menuName = "Loot Knights/Hero/Skill Effects/Whirlwind")]
public class HeroSkillWhirlwindEffect : CharacterSkillEffectDefinition
{
    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float duration = 2.2f;
    [SerializeField, Min(0.05f)] private float tickInterval = 0.35f;

    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(0.45f, true);
    [SerializeField] private float flatBonusDamage;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition tickVfx;
    [SerializeField] private SFXDefinition tickSfx;

    [Header("Spin Visual")]
    [SerializeField] private VFXDefinition spinVfx;
    [SerializeField, Min(0.02f)] private float spinVfxInterval = 0.12f;
    [SerializeField, Min(0f)] private float spinVfxRadius = 0.55f;
    [SerializeField, Min(1)] private int spinVfxCount = 2;
    [SerializeField] private float spinDegreesPerSecond = 540f;
    [SerializeField] private float spinRotationOffset = -90f;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedThisTick = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Controller == null || context.Caster == null) return;

        context.Controller.StartCoroutine(WhirlwindRoutine(context.Caster));
    }

    private IEnumerator WhirlwindRoutine(CharacterCtrl caster)
    {
        float elapsed = 0f;
        float damageTimer = 0f;
        float spinTimer = 0f;

        while (elapsed < duration)
        {
            if (damageTimer <= 0f)
            {
                TickDamage(caster);
                damageTimer = tickInterval;
            }

            if (spinTimer <= 0f)
            {
                PlaySpinVfx(caster, elapsed);
                spinTimer = spinVfxInterval;
            }

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            damageTimer -= deltaTime;
            spinTimer -= deltaTime;
            yield return null;
        }
    }

    private void PlaySpinVfx(CharacterCtrl caster, float elapsed)
    {
        if (caster == null || spinVfx == null || !VFXManager.HasInstance)
            return;

        int count = Mathf.Max(1, spinVfxCount);
        float baseAngle = elapsed * spinDegreesPerSecond;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (360f / count) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 radialDirection = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 position = caster.transform.position + (Vector3)(radialDirection * spinVfxRadius);

            PoolObj spawned = VFXManager.InstanceOrNull.Play(spinVfx, position, radialDirection);
            if (spawned == null)
                continue;

            float tangentAngle = angle + 90f + spinRotationOffset;
            spawned.transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
        }
    }

    private void TickDamage(CharacterCtrl caster)
    {
        if (caster == null || caster.CharacterStat == null) return;

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

        int count = Physics2D.OverlapCircle(caster.transform.position, radius, filter, Hits);
        DamagedThisTick.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null) continue;

            CharacterCtrl target = hit.GetComponentInParent<CharacterCtrl>();
            if (target == null || target == caster) continue;
            if (!IsTargetBodyCollider(hit, target)) continue;
            if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead) continue;
            if (!FactionManager.CanAttack(caster.Faction, target.Faction)) continue;
            if (!DamagedThisTick.Add(target.CharacterDamReceiver)) continue;

            Vector2 direction = target.transform.position - caster.transform.position;
            float damage = CalculateDamage(caster);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
            PlayTickFeedback(target.transform.position, direction, target.transform);
        }

        DamagedThisTick.Clear();
    }

    private static bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (target.Collider2D == null) return true;
        return hitCollider == target.Collider2D;
    }

    private float CalculateDamage(CharacterCtrl caster)
    {
        float multiplier = damageData != null ? damageData.Multiplier : 1f;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private void PlayTickFeedback(Vector3 position, Vector2 direction, Transform target)
    {
        if (tickVfx != null && VFXManager.HasInstance)
            VFXManager.InstanceOrNull.Play(tickVfx, position, direction, target);

        if (tickSfx != null)
            SFXManager.Play(tickSfx, position);
    }
}
