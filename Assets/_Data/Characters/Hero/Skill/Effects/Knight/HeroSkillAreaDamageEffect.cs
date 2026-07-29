using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillAreaDamageEffect", menuName = "Loot Knights/Hero/Skill Effects/Area Damage")]
public class HeroSkillAreaDamageEffect : CharacterSkillEffectDefinition
{
    private const string ShieldWallNodeId = "knight.shield_wall";

    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField, Range(1f, 360f)] private float angle = 90f;
    [SerializeField] private float forwardOffset = 0.75f;
    [SerializeField] private float sideOffset;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1.5f, true);
    [SerializeField] private float flatBonusDamage;

    [Header("Shield Bash Support")]
    [SerializeField, Min(0f)] private float shieldBashHealMaxHealthPercent = 0.12f;
    [SerializeField, Min(0f)] private float shieldWallHealMaxHealthPercentPerRank = 0.04f;
    [SerializeField, Min(0f)] private float shieldWallArmorPerRank = 1.5f;
    [SerializeField, Min(0f)] private float shieldWallArmorDuration = 2.5f;

    [Header("Support Feedback")]
    [SerializeField] private VFXDefinition supportVfx;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null) return;

        int shieldWallRank = IsShieldBash(context.Definition)
            ? SkillTreeRankResolver.GetRank(caster, ShieldWallNodeId)
            : 0;
        if (IsShieldBash(context.Definition))
        {
            ApplyShieldBashSupport(caster, shieldWallRank);
            return;
        }

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

        float effectiveRadius = radius + shieldWallRank * 0.1f;
        int count = Physics2D.OverlapCircle(origin, effectiveRadius, filter, Hits);
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

            float damage = CalculateDamage(caster, shieldWallRank);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
        }

        DamagedTargets.Clear();
        ApplyShieldWallArmor(caster, shieldWallRank);
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

    private float CalculateDamage(CharacterCtrl caster, int shieldWallRank)
    {
        return CharacterSkillDamageUtility.CalculateDamage(
            caster,
            damageData,
            flatBonusDamage,
            shieldWallRank * 0.06f);
    }

    private void ApplyShieldWallArmor(CharacterCtrl caster, int rank)
    {
        if (rank <= 0 || caster == null || caster.CharacterStat == null)
            return;

        StatValue armor = caster.CharacterStat.GetStat(StatType.Armor);
        if (armor == null)
            return;

        armor.AddBuffModifier(new StatModifier(StatType.Armor, ModifierType.Flat, rank * shieldWallArmorPerRank, this, shieldWallArmorDuration));
        armor.NotifyValueChanged();
    }

    private void ApplyShieldBashSupport(CharacterCtrl caster, int shieldWallRank)
    {
        if (caster == null || caster.CharacterStat == null)
            return;

        float maxHealth = caster.CharacterStat.MaxHealth != null
            ? caster.CharacterStat.MaxHealth.FinalValue
            : 0f;
        float healPercent = shieldBashHealMaxHealthPercent + shieldWallRank * shieldWallHealMaxHealthPercentPerRank;
        float healAmount = maxHealth * healPercent;
        if (healAmount > 0f)
            caster.CharacterDamReceiver?.Heal(healAmount);

        ApplyShieldWallArmor(caster, shieldWallRank);
        PlaySupportVfx(caster.transform);
    }

    private void PlaySupportVfx(Transform anchor)
    {
        if (anchor == null || supportVfx == null || !VFXManager.HasInstance)
            return;

        VFXManager.InstanceOrNull.Play(supportVfx, anchor.position, Vector2.up, anchor);
    }

    private static bool IsShieldBash(CharacterSkillDefinition definition)
    {
        return definition != null && definition.SkillId == "hero.shield_bash";
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);

        if (damageData == null)
            damageData = new DamageData(1f, false);

        shieldBashHealMaxHealthPercent = Mathf.Max(0f, shieldBashHealMaxHealthPercent);
        shieldWallHealMaxHealthPercentPerRank = Mathf.Max(0f, shieldWallHealMaxHealthPercentPerRank);
        shieldWallArmorPerRank = Mathf.Max(0f, shieldWallArmorPerRank);
        shieldWallArmorDuration = Mathf.Max(0f, shieldWallArmorDuration);
    }
}
