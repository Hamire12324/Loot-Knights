using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillProjectileEffect", menuName = "Loot Knights/Character/Skill Effects/Projectile")]
public sealed class CharacterSkillProjectileEffect : CharacterSkillEffectDefinition
{
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

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField] private float multiplierBonus;

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
        CharacterCtrl caster = context.Caster;
        if (caster == null)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 side = new(-direction.y, direction.x);
        int count = Mathf.Max(1, projectileCount);
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = -spreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float sideOffset = (i - (count - 1) * 0.5f) * sideSpacing;
            Vector2 projectileDirection = angle == 0f ? direction : Rotate(direction, angle);
            Vector3 position = caster.transform.position + (Vector3)(projectileDirection * startOffset + side * sideOffset);
            SpawnProjectile(caster, position, projectileDirection);
        }
    }

    private void SpawnProjectile(CharacterCtrl caster, Vector3 position, Vector2 direction)
    {
        PoolObj projectile = CharacterSkillVfxUtility.PlayProjectile(
            projectileVfx,
            position,
            direction,
            length,
            speed,
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
            flatBonusDamage,
            multiplierBonus,
            penetration,
            returnToPoolOnFinalHit);
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

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
