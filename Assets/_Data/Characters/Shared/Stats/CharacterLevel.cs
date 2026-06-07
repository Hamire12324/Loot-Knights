using UnityEngine;

public class CharacterLevel : CharacterAbstract
{
    [SerializeField] private bool usePlayerProgression = true;

    [Header("Attribute Points")]
    [SerializeField] private bool healByAddedMaxHealth = true;

    [SerializeField] private int currentLevel = 1;
    public int CurrentLevel => Mathf.Max(1, currentLevel);
    public PlayerLevelSnapshot CurrentSnapshot { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribePlayerLevel();
        SubscribeAttributePoints();
        RefreshFromPlayerProgression();
    }

    protected override void OnDisable()
    {
        UnsubscribePlayerLevel();
        UnsubscribeAttributePoints();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
    }

    public void RefreshFromPlayerProgression()
    {
        if (!usePlayerProgression) return;

        PlayerAttributePointStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        ApplySnapshot(PlayerExperienceStorage.Snapshot);
    }

    public void ApplySnapshot(PlayerLevelSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
        currentLevel = Mathf.Max(1, snapshot.Level);
        ApplyAllocatedStats();
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplyAllocatedStats();
    }

    public void ApplyAllocatedStats()
    {
        CharacterStat stat = characterCtrl != null ? characterCtrl.CharacterStat : null;

        if (stat == null) return;

        float previousMaxHealth = stat.MaxHealth != null
            ? stat.MaxHealth.FinalValue
            : 0f;

        stat.RemoveModifiersFromSource(this);

        foreach (StatType statType in PlayerAttributePointStorage.GetSpendableStats())
            ApplyFlatModifier(statType, PlayerAttributePointStorage.GetBonusValue(statType));

        float nextMaxHealth = stat.MaxHealth != null
            ? stat.MaxHealth.FinalValue
            : previousMaxHealth;

        if (healByAddedMaxHealth && nextMaxHealth > previousMaxHealth)
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
        if (Mathf.Approximately(amount, 0f)) return;

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

    private void SubscribeAttributePoints()
    {
        PlayerAttributePointStorage.OnPointsChanged -= HandleAttributePointsChanged;
        PlayerAttributePointStorage.OnPointsChanged += HandleAttributePointsChanged;
    }

    private void UnsubscribeAttributePoints()
    {
        PlayerAttributePointStorage.OnPointsChanged -= HandleAttributePointsChanged;
    }

    private void HandleLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        if (!usePlayerProgression) return;

        ApplySnapshot(snapshot);
    }

    private void HandleAttributePointsChanged()
    {
        if (!usePlayerProgression) return;

        ApplyAllocatedStats();
    }
}
