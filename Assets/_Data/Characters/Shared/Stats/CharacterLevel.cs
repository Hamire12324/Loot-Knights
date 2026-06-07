using UnityEngine;

public class CharacterLevel : CharacterAbstract
{
    [SerializeField] private bool usePlayerProgression = true;

    [Header("Level Bonuses")]
    [SerializeField] private float attackPerLevel = 2f;
    [SerializeField] private float maxHealthPerLevel = 10f;
    [SerializeField] private float armorPerLevel = 0.5f;
    [SerializeField] private bool healByAddedMaxHealth = true;

    [SerializeField] private int currentLevel = 1;
    public int CurrentLevel => Mathf.Max(1, currentLevel);
    public PlayerLevelSnapshot CurrentSnapshot { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribePlayerLevel();
        RefreshFromPlayerProgression();
    }

    protected override void OnDisable()
    {
        UnsubscribePlayerLevel();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
    }

    public void RefreshFromPlayerProgression()
    {
        if (!usePlayerProgression) return;

        ApplySnapshot(PlayerExperienceStorage.Snapshot);
    }

    public void ApplySnapshot(PlayerLevelSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
        ApplyLevel(snapshot.Level);
    }

    public void ApplyLevel(int level)
    {
        CharacterStat stat = characterCtrl != null ? characterCtrl.CharacterStat : null;

        if (stat == null) return;

        int safeLevel = Mathf.Max(1, level);
        int previousLevel = CurrentLevel;
        float previousMaxHealth = stat.MaxHealth != null
            ? stat.MaxHealth.FinalValue
            : 0f;

        currentLevel = safeLevel;

        int bonusLevels = Mathf.Max(0, safeLevel - 1);
        ApplyFlatModifier(StatType.Attack, attackPerLevel * bonusLevels);
        ApplyFlatModifier(StatType.MaxHealth, maxHealthPerLevel * bonusLevels);
        ApplyFlatModifier(StatType.Armor, armorPerLevel * bonusLevels);

        float nextMaxHealth = stat.MaxHealth != null
            ? stat.MaxHealth.FinalValue
            : previousMaxHealth;

        if (healByAddedMaxHealth && safeLevel > previousLevel && nextMaxHealth > previousMaxHealth)
            stat.SetCurrentHealth(stat.CurrentHealth + nextMaxHealth - previousMaxHealth);
        else if (stat.CurrentHealth > nextMaxHealth)
            stat.SetCurrentHealth(nextMaxHealth);

        stat.SetPreviousMaxHealth(previousMaxHealth);
        stat.NotifyAllStatsChanged();
    }

    public void ClearLevelModifiers()
    {
        characterCtrl?.CharacterStat?.RemoveModifiersFromSource(this);
    }

    private void ApplyFlatModifier(StatType statType, float amount)
    {
        CharacterStat stat = characterCtrl != null ? characterCtrl.CharacterStat : null;
        StatValue statValue = stat != null ? stat.GetStat(statType) : null;

        statValue?.AddBuffModifier(new StatModifier(
            statType,
            ModifierType.Flat,
            amount,
            this));
    }

    private void SubscribePlayerLevel()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelSnapshotChanged;
    }

    private void UnsubscribePlayerLevel()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
    }

    private void HandleLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        if (!usePlayerProgression) return;

        ApplySnapshot(snapshot);
    }
}
