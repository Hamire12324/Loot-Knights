using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillArcherProjectileEffect", menuName = "Loot Knights/Hero/Skill Effects/Archer Projectile")]
public sealed class HeroSkillArcherProjectileEffect : CharacterSkillEffectDefinition
{
    private const string LongshotNodeId = "archer.longshot";
    private const string PowerShotNodeId = "archer.power_shot";
    private const string PiercingMasteryNodeId = "archer.piercing_mastery";
    private const string TrickArrowNodeId = "archer.trick_arrow";

    [Header("Projectile")]
    [SerializeField, Min(0.05f)] private float length = 5.5f;
    [SerializeField, Min(0.05f)] private float width = 0.45f;
    [SerializeField, Min(0f)] private float startOffset = 0.55f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(0.9f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float damageMultiplierPerPowerShotRank = 0.08f;
    [SerializeField, Min(0f)] private float piercingDamageMultiplierPerRank = 0.06f;
    [SerializeField, Min(0f)] private float trickArrowHitStunPerRank = 0.08f;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0f)] private float projectileSpeed = 12f;
    [SerializeField] private float projectileRotationOffset;
    [SerializeField, Min(0)] private int projectilePenetration;
    [SerializeField] private bool logProjectileDebug;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null)
        {
            LogProjectileDebug("Execute skipped because caster or stats are missing.");
            return;
        }

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        int longshotRank = SkillTreeRankResolver.GetRank(caster, LongshotNodeId);
        int powerShotRank = SkillTreeRankResolver.GetRank(caster, PowerShotNodeId);
        int piercingMasteryRank = IsPiercingShot(context.Definition)
            ? SkillTreeRankResolver.GetRank(caster, PiercingMasteryNodeId)
            : 0;
        int trickArrowRank = SkillTreeRankResolver.GetRank(caster, TrickArrowNodeId);

        float effectiveLength = length + longshotRank * 0.55f;
        float effectiveSpeed = projectileSpeed + longshotRank * 0.75f;
        DamageData hitDamageData = GetHitDamageData(trickArrowRank);

        PlayProjectile(
            caster,
            direction,
            effectiveLength,
            effectiveSpeed,
            hitDamageData,
            GetMultiplierBonus(powerShotRank, piercingMasteryRank),
            GetPenetration(piercingMasteryRank));
    }

    private DamageData GetHitDamageData(int trickArrowRank)
    {
        if (damageData == null || trickArrowRank <= 0 || trickArrowHitStunPerRank <= 0f)
            return damageData;

        DamageData upgradedDamageData = damageData.CloneWithElement(damageData.Element);
        upgradedDamageData.CausesHitStun = true;
        upgradedDamageData.HitStunDuration = Mathf.Max(upgradedDamageData.HitStunDuration, trickArrowRank * trickArrowHitStunPerRank);
        upgradedDamageData.HitStunImmunityDuration = Mathf.Max(upgradedDamageData.HitStunImmunityDuration, 0.6f);
        upgradedDamageData.InterruptsAttack = true;
        return upgradedDamageData;
    }

    private float GetMultiplierBonus(int powerShotRank, int piercingMasteryRank)
    {
        return powerShotRank * damageMultiplierPerPowerShotRank +
               piercingMasteryRank * piercingDamageMultiplierPerRank;
    }

    private int GetPenetration(int piercingMasteryRank)
    {
        return Mathf.Max(0, projectilePenetration + piercingMasteryRank);
    }

    private void PlayProjectile(
        CharacterCtrl caster,
        Vector2 direction,
        float effectiveLength,
        float effectiveSpeed,
        DamageData hitDamageData,
        float multiplierBonus,
        int penetration)
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
            LogProjectileDebug($"VFXManager returned null. definition={projectileVfx.name}.");
            return;
        }

        HeroSkillProjectileDamage projectileDamage = projectile.GetComponent<HeroSkillProjectileDamage>();
        if (projectileDamage == null)
            projectileDamage = projectile.gameObject.AddComponent<HeroSkillProjectileDamage>();

        projectileDamage.Configure(
            caster,
            targetLayer,
            hitDamageData,
            flatBonusDamage,
            multiplierBonus,
            penetration);

        VFXProjectileMover mover = projectile.GetComponent<VFXProjectileMover>();
        if (mover == null)
            mover = projectile.gameObject.AddComponent<VFXProjectileMover>();

        mover.Play(direction, effectiveLength, effectiveSpeed, projectileRotationOffset);
    }

    private static bool IsPiercingShot(CharacterSkillDefinition definition)
    {
        return definition != null && definition.SkillId == "ranger.piercing_shot";
    }

    private void LogProjectileDebug(string message)
    {
        if (!logProjectileDebug)
            return;

        Debug.Log($"{nameof(HeroSkillArcherProjectileEffect)} ({name}): {message}", this);
    }

    private void OnValidate()
    {
        length = Mathf.Max(0.05f, length);
        width = Mathf.Max(0.05f, width);
        startOffset = Mathf.Max(0f, startOffset);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectilePenetration = Mathf.Max(0, projectilePenetration);
        damageMultiplierPerPowerShotRank = Mathf.Max(0f, damageMultiplierPerPowerShotRank);
        piercingDamageMultiplierPerRank = Mathf.Max(0f, piercingDamageMultiplierPerRank);
        trickArrowHitStunPerRank = Mathf.Max(0f, trickArrowHitStunPerRank);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
