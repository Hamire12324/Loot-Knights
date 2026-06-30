using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillLineDamageEffect", menuName = "Loot Knights/Hero/Skill Effects/Line Damage")]
public class HeroSkillLineDamageEffect : CharacterSkillEffectDefinition
{
    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float length = 3.2f;
    [SerializeField, Min(0.05f)] private float width = 0.75f;
    [SerializeField, Min(0f)] private float startOffset = 0.45f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1.45f, true);
    [SerializeField] private float flatBonusDamage;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition impactVfx;
    [SerializeField] private SFXDefinition impactSfx;

    [Header("Projectile Feedback")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0f)] private float projectileSpeed = 9f;
    [SerializeField] private float projectileRotationOffset;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null) return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 center = (Vector2)caster.transform.position + direction * (startOffset + length * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        PlayProjectileFeedback(caster.transform.position, direction);

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

        int count = Physics2D.OverlapBox(center, new Vector2(length, width), angle, filter, Hits);
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

            float damage = CalculateDamage(caster);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
            PlayImpactFeedback(target.transform.position, direction, target.transform);
        }

        DamagedTargets.Clear();
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

    private void PlayProjectileFeedback(Vector3 origin, Vector2 direction)
    {
        if (projectileVfx == null || !VFXManager.HasInstance)
            return;

        Vector3 position = origin + (Vector3)(direction * startOffset);
        PoolObj projectile = VFXManager.InstanceOrNull.Play(projectileVfx, position, direction);
        if (projectile == null)
            return;

        VFXProjectileMover mover = projectile.GetComponent<VFXProjectileMover>();
        if (mover == null)
            mover = projectile.gameObject.AddComponent<VFXProjectileMover>();

        mover.Play(direction, length, projectileSpeed, projectileRotationOffset);
    }
}
