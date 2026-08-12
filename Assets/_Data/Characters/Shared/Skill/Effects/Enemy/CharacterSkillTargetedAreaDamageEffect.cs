using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillTargetedAreaDamageEffect", menuName = "Loot Knights/Enemy/Skill Effects/Targeted Area Damage")]
public sealed class CharacterSkillTargetedAreaDamageEffect : CharacterSkillEffectDefinition
{
    [SerializeField, Min(0.05f)] private float radius = 1.1f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private DamageData damageData = new(1.25f, false);
    [SerializeField] private VFXDefinition impactVfx;

    private readonly List<CharacterCtrl> targets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null)
            return;

        Vector2 center = context.HasManualTargetPosition
            ? context.ManualTargetPosition
            : context.Target != null
            ? context.Target.position
            : (Vector2)context.Caster.transform.position + context.AimDirection.normalized * 2f;

        targets.Clear();
        CharacterSkillTargetUtility.FindCircleTargets(context.Caster, center, radius, targetLayer, targets);
        foreach (CharacterCtrl target in targets)
            CharacterSkillDamageUtility.DealDamage(context.Caster, target, damageData);
        targets.Clear();

        PlayImpactVfx(center);
    }

    private void PlayImpactVfx(Vector2 position)
    {
        if (impactVfx == null || impactVfx.Prefab == null)
            return;

        // Do not require a VFXManager in gameplay scenes. This is a combat VFX,
        // so the pool is sufficient and guarantees it appears on the target.
        PoolManager poolManager = PoolManager.InstanceOrNull;
        if (poolManager == null)
            return;

        Vector3 spawnPosition = (Vector3)position + impactVfx.Offset;
        PoolObj vfx = poolManager.Spawn(impactVfx.Prefab, spawnPosition, Quaternion.identity);
        if (vfx != null)
            vfx.transform.localScale = impactVfx.Prefab.transform.localScale * impactVfx.EffectiveScale;
    }
}
