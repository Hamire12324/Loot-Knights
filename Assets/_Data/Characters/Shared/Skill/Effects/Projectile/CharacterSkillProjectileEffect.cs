using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillProjectileEffect", menuName = "Loot Knights/Character/Skill Effects/Projectile")]
public class CharacterSkillProjectileEffect : CharacterSkillEffectDefinition
{
    [System.Serializable]
    private struct RankScaling
    {
        [SerializeField] private string skillTreeNodeId;
        [SerializeField] private float lengthPerRank;
        [SerializeField] private float speedPerRank;
        [SerializeField] private int penetrationPerRank;
        [SerializeField] private int projectileCountPerRank;
        [SerializeField] private float multiplierBonusPerRank;
        [SerializeField] private float flatBonusDamagePerRank;
        [SerializeField, Min(0f)] private float hitStunDurationPerRank;

        public void Apply(CharacterCtrl caster, ref RuntimeSettings settings)
        {
            int rank = string.IsNullOrWhiteSpace(skillTreeNodeId)
                ? 0
                : SkillTreeRankResolver.GetRank(caster, skillTreeNodeId);

            settings.Length += rank * lengthPerRank;
            settings.Speed += rank * speedPerRank;
            settings.Penetration += rank * penetrationPerRank;
            settings.ProjectileCount += rank * projectileCountPerRank;
            settings.MultiplierBonus += rank * multiplierBonusPerRank;
            settings.FlatBonusDamage += rank * flatBonusDamagePerRank;
            settings.HitStunDurationBonus += rank * hitStunDurationPerRank;
        }
    }

    private struct RuntimeSettings
    {
        public float Length;
        public float Speed;
        public int Penetration;
        public int ProjectileCount;
        public float MultiplierBonus;
        public float FlatBonusDamage;
        public float HitStunDurationBonus;
    }

    [Header("Projectile")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0.05f)] private float length = 5f;
    [SerializeField, Min(0f)] private float startOffset = 0.5f;
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(0f)] private float delay;
    [SerializeField] private float rotationOffset;
    [SerializeField, Min(0)] private int penetration;
    [SerializeField] private bool returnToPoolOnFinalHit = true;
    [SerializeField] private LayerMask targetLayer;

    [Header("Spread")]
    [SerializeField, Min(1)] private int projectileCount = 1;
    [SerializeField, Min(0f)] private float spreadAngle;
    [SerializeField, Min(0f)] private float sideSpacing;

    [Header("Barrage")]
    [SerializeField, Min(0f)] private float shotInterval;
    [SerializeField, Min(0f)] private float spawnOriginRadius;
    [SerializeField] private float spawnOriginAngleStep = 45f;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField] private float multiplierBonus;
    [SerializeField] private RankScaling[] rankScalings;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (delay > 0f && context.Controller != null)
        {
            context.Controller.StartCoroutine(SpawnAfterDelay(context));
            return;
        }

        SpawnProjectiles(context);
    }

    private IEnumerator SpawnAfterDelay(CharacterSkillExecutionContext context)
    {
        yield return new WaitForSeconds(delay);
        SpawnProjectiles(context);
    }

    private void SpawnProjectiles(CharacterSkillExecutionContext context)
    {
        RuntimeSettings settings = GetRuntimeSettings(context);
        int count = GetProjectileCount(context) + settings.ProjectileCount - Mathf.Max(1, projectileCount);
        count = Mathf.Max(1, count);
        if (count > 1 && shotInterval > 0f && context.Controller != null)
        {
            context.Controller.StartCoroutine(SpawnBarrage(context, count, settings));
            return;
        }

        for (int i = 0; i < count; i++)
            SpawnProjectileAtIndex(context, i, count, settings);
    }

    private RuntimeSettings GetRuntimeSettings(CharacterSkillExecutionContext context)
    {
        RuntimeSettings settings = new()
        {
            Length = length,
            Speed = speed,
            Penetration = penetration,
            ProjectileCount = Mathf.Max(1, projectileCount),
            MultiplierBonus = multiplierBonus,
            FlatBonusDamage = flatBonusDamage
        };

        foreach (RankScaling scaling in rankScalings ?? System.Array.Empty<RankScaling>())
            scaling.Apply(context.Caster, ref settings);

        string skillId = context.Definition != null ? context.Definition.SkillId : null;
        settings.Length += SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.ProjectileLength);
        settings.Speed += SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.ProjectileSpeed);
        settings.Penetration += Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.Penetration));
        settings.ProjectileCount += Mathf.RoundToInt(SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.ProjectileCount));
        settings.MultiplierBonus += SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.DamageMultiplier);
        settings.FlatBonusDamage += SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.FlatDamage);
        settings.HitStunDurationBonus += SkillTreeSkillModifierResolver.GetValue(context.Caster, skillId, SkillModifierType.HitStunDuration);

        settings.Length = Mathf.Max(0.05f, settings.Length);
        settings.Speed = Mathf.Max(0f, settings.Speed);
        settings.Penetration = Mathf.Max(0, settings.Penetration);
        settings.ProjectileCount = Mathf.Max(1, settings.ProjectileCount);
        return settings;
    }

    private IEnumerator SpawnBarrage(CharacterSkillExecutionContext context, int count, RuntimeSettings settings)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnProjectileAtIndex(context, i, count, settings);

            if (i < count - 1)
                yield return new WaitForSeconds(shotInterval);
        }
    }

    private void SpawnProjectileAtIndex(CharacterSkillExecutionContext context, int index, int count, RuntimeSettings settings)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 side = new(-direction.y, direction.x);
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = -spreadAngle * 0.5f;
        float angle = startAngle + angleStep * index;
        float sideOffset = (index - (count - 1) * 0.5f) * sideSpacing;
        Vector2 projectileDirection = angle == 0f ? direction : Rotate(direction, angle);
        Vector2 originOffset = spawnOriginRadius <= 0f
            ? Vector2.zero
            : Rotate(direction, index * spawnOriginAngleStep) * spawnOriginRadius;
        Vector3 position = caster.transform.position + (Vector3)(originOffset + projectileDirection * startOffset + side * sideOffset);
        projectileDirection = GetProjectileDirection(context, caster, position, projectileDirection);
        SpawnProjectile(caster, position, projectileDirection, settings);
    }

    protected virtual int GetProjectileCount(CharacterSkillExecutionContext context)
    {
        return Mathf.Max(1, projectileCount);
    }

    protected virtual Vector2 GetProjectileDirection(
        CharacterSkillExecutionContext context,
        CharacterCtrl caster,
        Vector3 origin,
        Vector2 fallbackDirection)
    {
        return fallbackDirection;
    }

    private void SpawnProjectile(CharacterCtrl caster, Vector3 position, Vector2 direction, RuntimeSettings settings)
    {
        PoolObj projectile = CharacterSkillVfxUtility.PlayProjectile(
            projectileVfx,
            position,
            direction,
            settings.Length,
            settings.Speed,
            rotationOffset);

        if (projectile == null)
            return;

        CharacterSkillProjectileDamage projectileDamage = projectile.GetComponent<CharacterSkillProjectileDamage>();
        if (projectileDamage == null)
            projectileDamage = projectile.gameObject.AddComponent<CharacterSkillProjectileDamage>();

        if (projectileDamage == null)
        {
            Debug.LogError($"Could not add {nameof(CharacterSkillProjectileDamage)} to projectile '{projectile.name}'.", projectile);
            return;
        }

        projectileDamage.Configure(
            caster,
            targetLayer,
            damageData,
            settings.FlatBonusDamage,
            settings.MultiplierBonus,
            settings.Penetration,
            returnToPoolOnFinalHit,
            GetDamageData(settings));
    }

    private DamageData GetDamageData(RuntimeSettings settings)
    {
        if (settings.HitStunDurationBonus <= 0f)
            return damageData;

        DamageData scaledDamageData = damageData != null
            ? damageData.CloneWithElement(damageData.Element)
            : new DamageData(1f, true);

        scaledDamageData.CausesHitStun = true;
        scaledDamageData.HitStunDuration += settings.HitStunDurationBonus;
        return scaledDamageData;
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos).normalized;
    }

    private void OnValidate()
    {
        length = Mathf.Max(0.05f, length);
        startOffset = Mathf.Max(0f, startOffset);
        speed = Mathf.Max(0f, speed);
        delay = Mathf.Max(0f, delay);
        penetration = Mathf.Max(0, penetration);
        projectileCount = Mathf.Max(1, projectileCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        sideSpacing = Mathf.Max(0f, sideSpacing);
        shotInterval = Mathf.Max(0f, shotInterval);
        spawnOriginRadius = Mathf.Max(0f, spawnOriginRadius);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
