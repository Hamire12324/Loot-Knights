using UnityEngine;

public class CharacterLevel : BaseMonoBehaviour
{
    [SerializeField] private CharacterCtrl characterCtrl;
    [SerializeField] private CharacterStat characterStat;
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
        LoadCharacterCtrl();
        LoadCharacterStat();
    }

    public void Configure(CharacterCtrl ctrl, CharacterStat stat)
    {
        if (characterCtrl == null)
            characterCtrl = ctrl;

        if (characterStat == null)
            characterStat = stat;

        RefreshFromPlayerProgression();
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
        LoadCharacterStat();

        if (characterStat == null) return;

        int safeLevel = Mathf.Max(1, level);
        int previousLevel = CurrentLevel;
        float previousMaxHealth = characterStat.MaxHealth != null
            ? characterStat.MaxHealth.FinalValue
            : 0f;

        currentLevel = safeLevel;

        int bonusLevels = Mathf.Max(0, safeLevel - 1);
        ApplyFlatModifier(StatType.Attack, attackPerLevel * bonusLevels);
        ApplyFlatModifier(StatType.MaxHealth, maxHealthPerLevel * bonusLevels);
        ApplyFlatModifier(StatType.Armor, armorPerLevel * bonusLevels);

        float nextMaxHealth = characterStat.MaxHealth != null
            ? characterStat.MaxHealth.FinalValue
            : previousMaxHealth;

        if (healByAddedMaxHealth && safeLevel > previousLevel && nextMaxHealth > previousMaxHealth)
            characterStat.SetCurrentHealth(characterStat.CurrentHealth + nextMaxHealth - previousMaxHealth);
        else if (characterStat.CurrentHealth > nextMaxHealth)
            characterStat.SetCurrentHealth(nextMaxHealth);

        characterStat.SetPreviousMaxHealth(previousMaxHealth);
        characterStat.NotifyAllStatsChanged();
    }

    public void ClearLevelModifiers()
    {
        LoadCharacterStat();
        characterStat?.RemoveModifiersFromSource(this);
    }

    private void ApplyFlatModifier(StatType statType, float amount)
    {
        CharacterStat stat = characterStat;
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

    private void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<CharacterCtrl>(true);
    }

    private void LoadCharacterStat()
    {
        if (characterStat != null) return;

        if (characterCtrl == null)
            LoadCharacterCtrl();

        if (characterCtrl != null && characterCtrl.CharacterStat != null)
        {
            characterStat = characterCtrl.CharacterStat;
            return;
        }

        characterStat = GetComponentInChildren<CharacterStat>(true);
        if (characterStat != null) return;

        characterStat = GetComponentInParent<CharacterStat>(true);
    }
}
