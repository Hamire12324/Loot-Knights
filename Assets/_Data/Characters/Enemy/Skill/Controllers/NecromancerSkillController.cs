using System.Collections;
using UnityEngine;

/// <summary>
/// Chooses the Necromancer's combat pattern: summon first when available,
/// cast magic at range, and fall back to the staff melee attack up close.
/// </summary>
public sealed class NecromancerSkillController : EnemySkillController
{
    [Header("Decision Ranges")]
    [SerializeField, Min(0f)] private float meleeRange = 1.25f;
    [SerializeField, Min(0f)] private float magicRange = 4f;

    [Header("Skill Chances")]
    [SerializeField, Range(0f, 1f)] private float summonChance = 0.4f;

    [Header("Last Stand")]
    [SerializeField, Range(0f, 1f)] private float lastStandHealthThreshold = 0.5f;
    [SerializeField, Min(0f)] private float lastStandArmorBonus = 12f;
    [SerializeField, Min(0f)] private float lastStandAttackSpeedBonus = 0.9f;
    [SerializeField, Min(0.1f)] private float lastStandMeleeRange = 0.9f;

    private bool lastStandActive;

    protected override void OnEnable()
    {
        base.OnEnable();
        lastStandActive = false;
        Enemy?.CharacterStat?.RemoveModifiersFromSource(this);
        (Enemy?.EnemyAIController as MeleeEnemyAIController)?.RestoreDefaultCombatDistances();
        StartCoroutine(SummonImmediately());
    }

    protected override void OnDisable()
    {
        Enemy?.CharacterStat?.RemoveModifiersFromSource(this);
        (Enemy?.EnemyAIController as MeleeEnemyAIController)?.RestoreDefaultCombatDistances();
        lastStandActive = false;
        base.OnDisable();
    }

    public override bool TryCastBasicAttack()
    {
        if (Enemy == null || Enemy.CharacterTargetFinder == null)
            return base.TryCastBasicAttack();

        ActivateLastStandIfNeeded();

        Transform target = Enemy.CharacterTargetFinder.CurrentTarget;
        if (target == null)
            return base.TryCastBasicAttack();

        float distance = Vector2.Distance(Enemy.transform.position, target.position);

        if (lastStandActive)
            return distance <= lastStandMeleeRange ? base.TryCastBasicAttack() : false;

        CharacterSkillRuntime summon = GetSkill(1);
        if (summon != null && summon.CanCast(this) && Random.value <= summonChance)
            return TryCast(1);

        CharacterSkillRuntime magic = GetSkill(0);
        // Never fall back to a melee hit while the target is outside melee range.
        // At range the Necromancer either casts magic or waits for its cooldown.
        if (distance > meleeRange)
        {
            if (magic != null && distance <= magicRange && magic.CanCast(this))
                return TryCast(0);

            return false;
        }

        return base.TryCastBasicAttack();
    }

    private IEnumerator SummonImmediately()
    {
        yield return null;

        if (Enemy == null || Enemy.CharacterDamReceiver == null || Enemy.CharacterDamReceiver.IsDead)
            yield break;

        CharacterSkillRuntime summon = GetSkill(1);
        if (summon != null && summon.CanCast(this))
            TryCast(1);
    }

    private void ActivateLastStandIfNeeded()
    {
        if (lastStandActive || Enemy == null || Enemy.CharacterStat == null)
            return;

        CharacterStat stats = Enemy.CharacterStat;
        float maxHealth = stats.MaxHealth != null ? stats.MaxHealth.FinalValue : 0f;
        if (maxHealth <= 0f || stats.CurrentHealth / maxHealth > lastStandHealthThreshold)
            return;

        lastStandActive = true;
        stats.Armor?.AddBuffModifier(new StatModifier(
            StatType.Armor, ModifierType.Flat, lastStandArmorBonus, this));
        stats.AttackSpeed?.AddBuffModifier(new StatModifier(
            StatType.AttackSpeed, ModifierType.PercentAdd, lastStandAttackSpeedBonus, this));
        stats.NotifyAllStatsChanged();

        MeleeEnemyAIController meleeAi = Enemy.EnemyAIController as MeleeEnemyAIController;
        meleeAi?.ConfigureCombatDistances(lastStandMeleeRange * 0.65f, lastStandMeleeRange);
    }
}
