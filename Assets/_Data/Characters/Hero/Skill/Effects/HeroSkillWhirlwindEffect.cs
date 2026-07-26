using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillWhirlwindEffect", menuName = "Loot Knights/Hero/Skill Effects/Whirlwind")]
public class HeroSkillWhirlwindEffectIns : CharacterSkillEffectDefinition
{
    private const string SpinningGuardNodeId = "knight.spinning_guard";
    private const string GuardianSpinNodeId = "knight.guardian_spin";

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float duration = 2.2f;
    [SerializeField, Min(0.05f)] private float tickInterval = 0.35f;

    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(0.45f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float guardianSpinLifeStealPercentPerRank = 0.04f;

    [Header("Spin Visual")]
    [SerializeField] private VFXDefinition spinVfx;
    [SerializeField, Min(0.02f)] private float spinVfxInterval = 0.12f;
    [SerializeField, Min(0f)] private float spinVfxRadius = 0.55f;
    [SerializeField, Min(1)] private int spinVfxCount = 2;
    [SerializeField] private float spinDegreesPerSecond = 540f;
    [SerializeField] private float spinRotationOffset = -90f;

    private static readonly Collider2D[] Hits = new Collider2D[32];
    private static readonly HashSet<CharacterDamReceiver> DamagedThisTick = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Controller == null || context.Caster == null) return;

        context.Controller.StartCoroutine(WhirlwindRoutine(context.Caster));
    }

    private IEnumerator WhirlwindRoutine(CharacterCtrl caster)
    {
        int spinningGuardRank = SkillTreeRankResolver.GetRank(caster, SpinningGuardNodeId);
        int guardianSpinRank = SkillTreeRankResolver.GetRank(caster, GuardianSpinNodeId);
        ApplySpinningGuardArmor(caster, spinningGuardRank);

        float elapsed = 0f;
        float damageTimer = 0f;
        float spinTimer = 0f;
        float effectiveDuration = duration + spinningGuardRank * 0.2f;
        float effectiveTickInterval = Mathf.Max(0.08f, tickInterval * (1f - guardianSpinRank * 0.08f));

        while (elapsed < effectiveDuration)
        {
            if (damageTimer <= 0f)
            {
                TickDamage(caster, spinningGuardRank, guardianSpinRank);
                damageTimer = effectiveTickInterval;
            }

            if (spinTimer <= 0f)
            {
                PlaySpinVfx(caster, elapsed);
                spinTimer = spinVfxInterval;
            }

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            damageTimer -= deltaTime;
            spinTimer -= deltaTime;
            yield return null;
        }
    }

    private void PlaySpinVfx(CharacterCtrl caster, float elapsed)
    {
        if (caster == null || spinVfx == null || !VFXManager.HasInstance)
            return;

        int count = Mathf.Max(1, spinVfxCount);
        float baseAngle = elapsed * spinDegreesPerSecond;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (360f / count) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 radialDirection = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 position = caster.transform.position + (Vector3)(radialDirection * spinVfxRadius);

            PoolObj spawned = VFXManager.InstanceOrNull.Play(spinVfx, position, radialDirection);
            if (spawned == null)
                continue;

            float tangentAngle = angle + 90f + spinRotationOffset;
            spawned.transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
        }
    }

    private void ApplySpinningGuardArmor(CharacterCtrl caster, int rank)
    {
        if (rank <= 0 || caster == null || caster.CharacterStat == null)
            return;

        StatValue armor = caster.CharacterStat.GetStat(StatType.Armor);
        if (armor == null)
            return;

        armor.AddBuffModifier(new StatModifier(StatType.Armor, ModifierType.Flat, rank * 1.5f, this, duration + rank * 0.2f));
        armor.NotifyValueChanged();
    }

    private void TickDamage(CharacterCtrl caster, int spinningGuardRank, int guardianSpinRank)
    {
        if (caster == null || caster.CharacterStat == null) return;

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

        float effectiveRadius = radius + spinningGuardRank * 0.12f + guardianSpinRank * 0.2f;
        int count = Physics2D.OverlapCircle(caster.transform.position, effectiveRadius, filter, Hits);
        DamagedThisTick.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null) continue;

            CharacterCtrl target = hit.GetComponentInParent<CharacterCtrl>();
            if (target == null || target == caster) continue;
            if (!IsTargetBodyCollider(hit, target)) continue;
            if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead) continue;
            if (!FactionManager.CanAttack(caster.Faction, target.Faction)) continue;
            if (!DamagedThisTick.Add(target.CharacterDamReceiver)) continue;

            Vector2 direction = target.transform.position - caster.transform.position;
            float damage = CalculateDamage(caster, guardianSpinRank);
            target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
            ApplyLifeSteal(caster, damage, guardianSpinRank);
        }

        DamagedThisTick.Clear();
    }

    private void ApplyLifeSteal(CharacterCtrl caster, float damage, int guardianSpinRank)
    {
        if (caster == null || guardianSpinRank <= 0 || damage <= 0f)
            return;

        float healAmount = damage * guardianSpinRank * guardianSpinLifeStealPercentPerRank;
        if (healAmount > 0f)
            caster.CharacterDamReceiver?.Heal(healAmount);
    }

    private static bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (target.Collider2D == null) return true;
        return hitCollider == target.Collider2D;
    }

    private float CalculateDamage(CharacterCtrl caster, int guardianSpinRank)
    {
        float multiplier = damageData != null ? damageData.Multiplier : 1f;
        multiplier += guardianSpinRank * 0.08f;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.05f, duration);
        tickInterval = Mathf.Max(0.05f, tickInterval);
        radius = Mathf.Max(0.05f, radius);
        spinVfxInterval = Mathf.Max(0.02f, spinVfxInterval);
        spinVfxRadius = Mathf.Max(0f, spinVfxRadius);
        spinVfxCount = Mathf.Max(1, spinVfxCount);
        guardianSpinLifeStealPercentPerRank = Mathf.Max(0f, guardianSpinLifeStealPercentPerRank);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }

}
