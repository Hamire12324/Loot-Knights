using UnityEngine;

public class CharacterLevel : CharacterAbstract
{
    [SerializeField] private int currentLevel = 1;
    public int CurrentLevel => Mathf.Max(1, currentLevel);
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
        float previousCurrentHealth = stat.CurrentHealth;

        stat.RemoveModifiersFromSource(this, updateHealthByMaxHealthDelta: false);

        foreach (StatType statType in GetAllocatedStatTypes())
            ApplyFlatModifier(statType, GetAllocatedStatBonus(statType));

        float nextMaxHealth = stat.MaxHealth != null
            ? stat.MaxHealth.FinalValue
            : previousMaxHealth;

        stat.ApplyMaxHealthDelta(previousMaxHealth, previousCurrentHealth);
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

    protected virtual StatType[] GetAllocatedStatTypes()
    {
        return System.Array.Empty<StatType>();
    }

    protected virtual float GetAllocatedStatBonus(StatType statType)
    {
        return 0f;
    }
}
