using System;
using UnityEngine;
public class BossEnemy : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private string displayName;

    private EnemyCtrl enemy;
    private CharacterDamReceiver damageReceiver;
    private CharacterStat characterStat;
    private string configuredDisplayName;
    public bool IsBoss { get; private set; }
    public string DisplayName => string.IsNullOrWhiteSpace(configuredDisplayName)
        ? (string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName)
        : configuredDisplayName;
    public CharacterDamReceiver DamageReceiver => damageReceiver;

    public static event Action<BossEnemy> OnBossSpawned;
    public static event Action<BossEnemy> OnBossDefeated;

    private void Awake()
    {
        enemy = GetComponent<EnemyCtrl>();
        damageReceiver = enemy != null ? enemy.CharacterDamReceiver : null;
    }

    private void OnDisable()
    {
        UnsubscribeDeath();
        RemoveBossModifiers();
    }

    public void Configure(BossEncounterConfig config)
    {
        enemy ??= GetComponent<EnemyCtrl>();
        damageReceiver ??= enemy != null ? enemy.CharacterDamReceiver : null;
        characterStat ??= enemy != null ? enemy.CharacterStat : null;
        UnsubscribeDeath();
        RemoveBossModifiers();

        IsBoss = config != null && config.Enabled;
        configuredDisplayName = IsBoss ? config.DisplayName : null;
        enemy?.SetFaction(IsBoss ? Faction.Boss : Faction.Enemy);

        if (!IsBoss)
            return;

        ApplyBossModifiers(config);
        if (damageReceiver != null)
            damageReceiver.OnDeath += HandleDeath;

        OnBossSpawned?.Invoke(this);
    }

    private void HandleDeath(CharacterDamReceiver _)
    {
        if (!IsBoss)
            return;

        OnBossDefeated?.Invoke(this);
        UnsubscribeDeath();
    }

    private void UnsubscribeDeath()
    {
        if (damageReceiver != null)
            damageReceiver.OnDeath -= HandleDeath;
    }

    private void ApplyBossModifiers(BossEncounterConfig config)
    {
        if (characterStat == null)
            return;

        AddMultiplier(characterStat.MaxHealth, config.HealthMultiplier, StatType.MaxHealth);
        AddMultiplier(characterStat.Attack, config.AttackMultiplier, StatType.Attack);
        AddMultiplier(characterStat.Armor, config.ArmorMultiplier, StatType.Armor);
        characterStat.SetCurrentHealth(characterStat.MaxHealth.FinalValue);
    }

    private void RemoveBossModifiers()
    {
        if (characterStat != null)
            characterStat.RemoveModifiersFromSource(this, false);
    }

    private void AddMultiplier(StatValue stat, float multiplier, StatType statType)
    {
        if (stat != null)
            stat.AddBuffModifier(new StatModifier(statType, ModifierType.PercentMultiply, multiplier - 1f, this));
    }
}
