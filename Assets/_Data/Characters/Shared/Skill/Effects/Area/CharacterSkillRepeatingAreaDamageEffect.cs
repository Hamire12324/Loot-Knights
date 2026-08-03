using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillRepeatingAreaDamageEffect", menuName = "Loot Knights/Character/Skill Effects/Repeating Area Damage")]
public sealed class CharacterSkillRepeatingAreaDamageEffect : CharacterSkillEffectDefinition
{
    [System.Serializable]
    private struct RankScaling
    {
        [SerializeField] private string skillTreeNodeId;
        [SerializeField] private float radiusPerRank;
        [SerializeField] private float damageMultiplierPerRank;
        [SerializeField] private float durationPerRank;
        [SerializeField, Range(0f, 0.95f)] private float tickIntervalReductionPerRank;
        [SerializeField, Min(0f)] private float lifeStealPercentPerRank;
        [SerializeField, Min(0f)] private float hitStunDurationPerRank;

        public void Apply(CharacterCtrl caster, ref RuntimeSettings settings)
        {
            int rank = string.IsNullOrWhiteSpace(skillTreeNodeId)
                ? 0
                : SkillTreeRankResolver.GetRank(caster, skillTreeNodeId);

            settings.Radius += rank * radiusPerRank;
            settings.DamageMultiplierBonus += rank * damageMultiplierPerRank;
            settings.Duration += rank * durationPerRank;
            settings.TickInterval *= Mathf.Max(0.05f, 1f - rank * tickIntervalReductionPerRank);
            settings.LifeStealPercent += rank * lifeStealPercentPerRank;
            settings.HitStunDurationBonus += rank * hitStunDurationPerRank;
        }
    }

    private struct RuntimeSettings
    {
        public float Radius;
        public float Duration;
        public float TickInterval;
        public float DamageMultiplierBonus;
        public float LifeStealPercent;
        public float HitStunDurationBonus;
        public bool ContinuousHitStun;
    }

    [Header("Area")]
    [SerializeField, Min(0.05f)] private float radius = 2f;
    [SerializeField, Min(0f)] private float forwardOffset = 3f;
    [SerializeField] private bool followCaster;
    [SerializeField, Min(0.05f)] private float duration = 3f;
    [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Targeting")]
    [SerializeField] private bool placeOnClosestTarget;
    [SerializeField, Min(0.05f)] private float targetSearchRadius = 8f;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(0.35f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField] private RankScaling[] rankScalings;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition areaVfx;
    [SerializeField, Min(0.05f)] private float vfxInterval = 0.5f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        RuntimeSettings settings = new()
        {
            Radius = radius,
            Duration = duration,
            TickInterval = tickInterval
        };

        foreach (RankScaling scaling in rankScalings ?? System.Array.Empty<RankScaling>())
            scaling.Apply(context.Caster, ref settings);

        ApplySkillModifiers(context.Caster, context.Definition, ref settings);

        settings.Radius = Mathf.Max(0.05f, settings.Radius);
        settings.Duration = Mathf.Max(0.05f, settings.Duration);
        settings.TickInterval = Mathf.Max(0.05f, settings.TickInterval);
        Vector3 fixedCenter = context.Caster.transform.position + (Vector3)(direction * forwardOffset);
        if (placeOnClosestTarget)
        {
            CharacterCtrl target = CharacterSkillTargetUtility.FindClosestTarget(
                context.Caster,
                context.Caster.transform.position,
                targetSearchRadius,
                targetLayer);

            if (target != null)
            {
                Vector2 targetDirection = (Vector2)target.transform.position - (Vector2)context.Caster.transform.position;
                if (targetDirection.sqrMagnitude > 0.001f)
                    direction = targetDirection.normalized;

                fixedCenter = target.transform.position;
            }
        }

        context.Controller.StartCoroutine(DamageRoutine(context.Caster, direction, fixedCenter, settings));
    }

    private IEnumerator DamageRoutine(
        CharacterCtrl caster,
        Vector2 direction,
        Vector3 fixedCenter,
        RuntimeSettings settings)
    {
        float elapsed = 0f;
        float nextVfxTime = 0f;
        WaitForSeconds tickWait = new(settings.TickInterval);

        while (caster != null && elapsed < settings.Duration)
        {
            Vector3 center = followCaster
                ? caster.transform.position + (Vector3)(direction * forwardOffset)
                : fixedCenter;

            if (areaVfx != null && elapsed >= nextVfxTime)
            {
                CharacterSkillVfxUtility.Play(areaVfx, center, direction);
                nextVfxTime += vfxInterval;
            }

            List<CharacterCtrl> targets = new();
            CharacterSkillTargetUtility.FindCircleTargets(caster, center, settings.Radius, targetLayer, targets);
            for (int i = 0; i < targets.Count; i++)
            {
                CharacterCtrl target = targets[i];
                if (target == null || target.CharacterDamReceiver == null)
                    continue;

                float damage = CharacterSkillDamageUtility.CalculateDamage(
                    caster,
                    damageData,
                    flatBonusDamage,
                    settings.DamageMultiplierBonus);
                target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, GetDamageData(settings));

                if (settings.LifeStealPercent > 0f)
                    caster.CharacterDamReceiver?.Heal(damage * settings.LifeStealPercent);
            }

            yield return tickWait;
            elapsed += settings.TickInterval;
        }
    }

    private DamageData GetDamageData(RuntimeSettings settings)
    {
        if (settings.HitStunDurationBonus <= 0f && !settings.ContinuousHitStun)
            return damageData;

        DamageData scaledDamageData = damageData != null
            ? damageData.CloneWithElement(damageData.Element)
            : new DamageData(1f, true);

        scaledDamageData.CausesHitStun = true;
        scaledDamageData.HitStunDuration += settings.HitStunDurationBonus;
        scaledDamageData.IgnoresHitStunImmunity = settings.ContinuousHitStun;
        return scaledDamageData;
    }

    private static void ApplySkillModifiers(
        CharacterCtrl caster,
        CharacterSkillDefinition definition,
        ref RuntimeSettings settings)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
            return;

        string skillId = definition.SkillId;
        settings.Radius += SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.Radius);
        settings.Duration += SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.Duration);
        settings.DamageMultiplierBonus += SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.DamageMultiplier);
        settings.LifeStealPercent += SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.LifeStealPercent);
        settings.HitStunDurationBonus += SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.HitStunDuration);
        settings.ContinuousHitStun = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ContinuousHitStun) > 0f;

        float tickReduction = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.TickIntervalReduction);
        settings.TickInterval *= Mathf.Max(0.05f, 1f - tickReduction);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
        forwardOffset = Mathf.Max(0f, forwardOffset);
        duration = Mathf.Max(0.05f, duration);
        tickInterval = Mathf.Max(0.05f, tickInterval);
        targetSearchRadius = Mathf.Max(0.05f, targetSearchRadius);
        vfxInterval = Mathf.Max(0.05f, vfxInterval);
        damageData ??= new DamageData(0.35f, true);
    }
}
