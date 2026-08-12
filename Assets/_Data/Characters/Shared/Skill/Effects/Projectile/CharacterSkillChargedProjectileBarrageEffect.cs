using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillChargedProjectileBarrageEffect", menuName = "Loot Knights/Character/Skill Effects/Charged Projectile Barrage")]
public sealed class CharacterSkillChargedProjectileBarrageEffect : CharacterSkillProjectileEffect, ICharacterSkillResourceConsumer
{
    [SerializeField, Min(1)] private int maximumCharges = 5;
    [SerializeField, Min(1)] private int extraProjectilesPerCharge = 1;
    [SerializeField] private string resourceId;
    [SerializeField, Min(0.1f)] private float targetSearchRadius = 8.5f;
    [SerializeField] private LayerMask targetingLayer;
    public string ResourceId => resourceId;

    protected override int GetProjectileCount(CharacterSkillExecutionContext context)
    {
        int baseCount = base.GetProjectileCount(context);
        CharacterCtrl caster = context.Caster;
        string skillId = context.Definition != null ? context.Definition.SkillId : null;
        int effectiveMaximumCharges = maximumCharges + Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ChargeCapacity));
        int projectilesPerCharge = extraProjectilesPerCharge + Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ChargeProjectilesPerCharge));
        int charges = CharacterSkillResource.ConsumeAll(context.Caster, resourceId);
        int minimumCharges = Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.ChargeMinimum));
        int effectiveCharges = Mathf.Max(charges, minimumCharges);
        return baseCount + Mathf.Clamp(effectiveCharges, 0, effectiveMaximumCharges) * projectilesPerCharge;
    }

    protected override Vector2 GetProjectileDirection(
        CharacterSkillExecutionContext context,
        CharacterCtrl caster,
        Vector3 origin,
        Vector2 fallbackDirection)
    {
        if (context.HasManualTargetPosition)
        {
            Vector2 manualDirection = context.ManualTargetPosition - (Vector2)origin;
            return manualDirection.sqrMagnitude > 0.001f
                ? manualDirection.normalized
                : fallbackDirection;
        }

        string skillId = context.Definition != null ? context.Definition.SkillId : null;
        float effectiveSearchRadius = targetSearchRadius + SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.TargetSearchRadius);
        CharacterCtrl target = CharacterSkillTargetUtility.FindClosestTarget(
            caster,
            caster.transform.position,
            effectiveSearchRadius,
            targetingLayer);

        if (target == null)
            return fallbackDirection;

        Vector2 targetDirection = (Vector2)target.transform.position - (Vector2)origin;
        return targetDirection.sqrMagnitude > 0.001f ? targetDirection.normalized : fallbackDirection;
    }

    private void OnValidate()
    {
        maximumCharges = Mathf.Max(1, maximumCharges);
        extraProjectilesPerCharge = Mathf.Max(1, extraProjectilesPerCharge);
        targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
    }
}
