using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillLineDamageEffect", menuName = "Loot Knights/Hero/Skill Effects/Line Damage")]
public class HeroSkillLineDamageEffect : CharacterSkillEffectDefinition
{
    private const string WavebreakerNodeId = "knight.wavebreaker";

    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float length = 3.2f;
    [SerializeField, Min(0.05f)] private float width = 0.75f;
    [SerializeField, Min(0f)] private float startOffset = 0.45f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1.45f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float wavebreakerStunDurationPerRank = 0.15f;

    [Header("Projectile Feedback")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0f)] private float projectileSpeed = 9f;
    [SerializeField] private float projectileRotationOffset;
    [SerializeField] private bool projectileColliderDealsDamage;
    [SerializeField, Min(0)] private int projectilePenetration;
    [SerializeField] private bool logProjectileDebug;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null)
        {
            LogProjectileDebug("Execute skipped because caster is null.");
            return;
        }

        if (caster.CharacterStat == null)
        {
            LogProjectileDebug($"Execute skipped because {caster.name} has no CharacterStat.");
            return;
        }

        int wavebreakerRank = SkillTreeRankResolver.GetRank(caster, WavebreakerNodeId);
        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        float effectiveLength = length + wavebreakerRank * 0.45f;
        float effectiveWidth = width + wavebreakerRank * 0.08f;
        Vector2 center = (Vector2)caster.transform.position + direction * (startOffset + effectiveLength * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        LogProjectileDebug(
            $"Execute caster={caster.name}, direction={direction}, center={center}, length={effectiveLength}, width={effectiveWidth}.");

        DamageData hitDamageData = GetHitDamageData(wavebreakerRank);
        PlayProjectileFeedback(caster, direction, effectiveLength, hitDamageData, wavebreakerRank);

        if (projectileColliderDealsDamage)
        {
            DamagedTargets.Clear();
            return;
        }

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

        int count = Physics2D.OverlapBox(center, new Vector2(effectiveLength, effectiveWidth), angle, filter, Hits);
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

            float damage = CalculateDamage(caster, wavebreakerRank);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, hitDamageData);
        }

        DamagedTargets.Clear();
    }

    private DamageData GetHitDamageData(int wavebreakerRank)
    {
        if (damageData == null || wavebreakerRank <= 0 || wavebreakerStunDurationPerRank <= 0f)
            return damageData;

        DamageData upgradedDamageData = damageData.CloneWithElement(damageData.Element);
        upgradedDamageData.CausesHitStun = true;
        upgradedDamageData.HitStunDuration = wavebreakerRank * wavebreakerStunDurationPerRank;
        upgradedDamageData.HitStunImmunityDuration = Mathf.Max(upgradedDamageData.HitStunImmunityDuration, 0.8f);
        upgradedDamageData.InterruptsAttack = true;
        return upgradedDamageData;
    }

    private static bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (target.Collider2D == null) return true;
        return hitCollider == target.Collider2D;
    }

    private float CalculateDamage(CharacterCtrl caster, int wavebreakerRank)
    {
        float multiplier = damageData != null ? damageData.Multiplier : 1f;
        multiplier += wavebreakerRank * 0.08f;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private void PlayProjectileFeedback(
        CharacterCtrl caster,
        Vector2 direction,
        float effectiveLength,
        DamageData hitDamageData,
        int wavebreakerRank)
    {
        if (projectileVfx == null)
        {
            LogProjectileDebug("Projectile VFX is not assigned.");
            return;
        }

        if (!VFXManager.HasInstance)
        {
            LogProjectileDebug("VFXManager instance is missing.");
            return;
        }

        Vector3 position = caster.transform.position + (Vector3)(direction * startOffset);
        PoolObj projectile = VFXManager.InstanceOrNull.Play(projectileVfx, position, direction);
        if (projectile == null)
        {
            LogProjectileDebug(
                $"VFXManager returned null. definition={projectileVfx.name}, prefab={(projectileVfx.Prefab != null ? projectileVfx.Prefab.name : "null")}");
            return;
        }

        if (projectileColliderDealsDamage)
        {
            HeroSkillProjectileDamage projectileDamage = projectile.GetComponent<HeroSkillProjectileDamage>();
            if (projectileDamage == null)
                projectileDamage = projectile.gameObject.AddComponent<HeroSkillProjectileDamage>();

            projectileDamage.Configure(
                caster,
                targetLayer,
                hitDamageData,
                flatBonusDamage,
                wavebreakerRank * 0.08f,
                projectilePenetration);
        }

        VFXProjectileMover mover = projectile.GetComponent<VFXProjectileMover>();
        if (mover == null)
            mover = projectile.gameObject.AddComponent<VFXProjectileMover>();

        mover.Play(direction, effectiveLength, projectileSpeed, projectileRotationOffset);

        LogProjectileDebug(
            $"Spawned {projectile.name} at {position}, direction={direction}, distance={effectiveLength}, active={projectile.gameObject.activeInHierarchy}.");
    }

    private void LogProjectileDebug(string message)
    {
        if (!logProjectileDebug)
            return;

        Debug.Log($"{nameof(HeroSkillLineDamageEffect)} ({name}): {message}", this);
    }

    private void OnValidate()
    {
        length = Mathf.Max(0.05f, length);
        width = Mathf.Max(0.05f, width);
        startOffset = Mathf.Max(0f, startOffset);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectilePenetration = Mathf.Max(0, projectilePenetration);
        wavebreakerStunDurationPerRank = Mathf.Max(0f, wavebreakerStunDurationPerRank);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
