using UnityEngine;

/// <summary>
/// Runs a two-phase boss pattern: summon and magic at range, then an enraged
/// melee last stand once the Necromancer reaches low health.
/// </summary>
public sealed class NecromancerSkillController : EnemySkillController
{
    [Header("Decision Ranges")]
    [SerializeField, Min(0f)] private float magicRange = 4f;

    [Header("Last Stand")]
    [SerializeField, Range(0f, 1f)] private float lastStandHealthThreshold = 0.35f;
    [SerializeField, Min(0f)] private float lastStandArmorBonus = 35f;
    [SerializeField, Min(0f)] private float lastStandAttackBonus = 55f;
    [SerializeField, Min(0f)] private float lastStandAttackSpeedBonus = 1.25f;
    [SerializeField, Min(0.1f)] private float lastStandMeleeRange = 1.1f;

    private bool lastStandActive;

    protected override void OnEnable()
    {
        base.OnEnable();
        lastStandActive = false;
        Enemy?.CharacterStat?.RemoveModifiersFromSource(this);
        (Enemy?.EnemyAIController as MeleeEnemyAIController)?.RestoreDefaultCombatDistances();
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

        // Phase 1: remain a caster. The boss must close in before it can cast,
        // but it never switches to melee while above the last-stand threshold.
        if (distance > magicRange)
            return false;

        CharacterSkillRuntime summon = GetSkill(1);
        if (summon != null && summon.CanCast(this))
            return TryCast(1);

        // Phase 1 fallback: while Summon Skeletons is cooling down, pressure
        // the hero with Falling Magic instead of using a staff attack.
        CharacterSkillRuntime magic = GetSkill(0);
        if (magic != null && magic.CanCast(this))
            return TryCast(0);

        return false;
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
        stats.Attack?.AddBuffModifier(new StatModifier(
            StatType.Attack, ModifierType.Flat, lastStandAttackBonus, this));
        stats.AttackSpeed?.AddBuffModifier(new StatModifier(
            StatType.AttackSpeed, ModifierType.PercentAdd, lastStandAttackSpeedBonus, this));
        stats.NotifyAllStatsChanged();

        MeleeEnemyAIController meleeAi = Enemy.EnemyAIController as MeleeEnemyAIController;
        meleeAi?.ConfigureCombatDistances(lastStandMeleeRange * 0.65f, lastStandMeleeRange);
    }
}
