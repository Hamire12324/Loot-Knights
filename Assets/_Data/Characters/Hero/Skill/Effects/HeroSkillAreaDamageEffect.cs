using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillAreaDamageEffect", menuName = "Loot Knights/Hero/Skill Effects/Area Damage")]
public class HeroSkillAreaDamageEffect : CharacterSkillEffectDefinition
{
    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField, Range(1f, 360f)] private float angle = 90f;
    [SerializeField] private float forwardOffset = 0.75f;
    [SerializeField] private float sideOffset;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1.5f, true);
    [SerializeField] private float flatBonusDamage;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition impactVfx;
    [SerializeField] private SFXDefinition impactSfx;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null) return;

        Vector2 origin = GetOrigin(caster.transform.position, context.AimDirection);
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

        int count = Physics2D.OverlapCircle(origin, radius, filter, Hits);
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
            if (!IsInsideAngle(caster.transform.position, context.AimDirection, target.transform.position)) continue;

            float damage = CalculateDamage(caster);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
            PlayImpactFeedback(target.transform.position, context.AimDirection, target.transform);
        }

        DamagedTargets.Clear();
    }

    private Vector2 GetOrigin(Vector2 heroPosition, Vector2 aimDirection)
    {
        Vector2 right = new(-aimDirection.y, aimDirection.x);
        return heroPosition + aimDirection * forwardOffset + right * sideOffset;
    }

    private bool IsInsideAngle(Vector2 casterPosition, Vector2 aimDirection, Vector2 targetPosition)
    {
        if (angle >= 359f) return true;

        Vector2 toTarget = targetPosition - casterPosition;
        if (toTarget.sqrMagnitude <= 0.001f) return true;

        return Vector2.Angle(aimDirection, toTarget.normalized) <= angle * 0.5f;
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

    private void PlayImpactFeedback(Vector3 position, Vector2 direction, Transform target)
    {
        if (impactVfx != null && VFXManager.HasInstance)
            VFXManager.InstanceOrNull.Play(impactVfx, position, direction, target);

        if (impactSfx != null)
            SFXManager.Play(impactSfx, position);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
