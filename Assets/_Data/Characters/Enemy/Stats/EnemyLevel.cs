using System.Collections.Generic;
using UnityEngine;

public class EnemyLevel : CharacterLevel
{
    [SerializeField] private List<EnemyLevelStatGrowth> statGrowths = new()
    {
        new EnemyLevelStatGrowth(StatType.MaxHealth, 10f),
        new EnemyLevelStatGrowth(StatType.Attack, 2f),
        new EnemyLevelStatGrowth(StatType.Armor, 0.5f)
    };

    public EnemyCtrl Enemy => characterCtrl as EnemyCtrl;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<EnemyCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no EnemyCtrl in {gameObject.name}", gameObject);
    }

    protected override StatType[] GetAllocatedStatTypes()
    {
        if (statGrowths == null || statGrowths.Count == 0)
            return System.Array.Empty<StatType>();

        List<StatType> statTypes = new();
        foreach (EnemyLevelStatGrowth growth in statGrowths)
        {
            if (growth == null || growth.StatType == StatType.None)
                continue;

            if (!statTypes.Contains(growth.StatType))
                statTypes.Add(growth.StatType);
        }

        return statTypes.ToArray();
    }

    protected override float GetAllocatedStatBonus(StatType statType)
    {
        if (statGrowths == null || statGrowths.Count == 0)
            return 0f;

        float bonus = 0f;
        int levelsAboveOne = Mathf.Max(0, CurrentLevel - 1);

        foreach (EnemyLevelStatGrowth growth in statGrowths)
        {
            if (growth == null || growth.StatType != statType)
                continue;

            bonus += growth.BonusPerLevel * levelsAboveOne;
        }

        return bonus;
    }
}

[System.Serializable]
public class EnemyLevelStatGrowth
{
    [SerializeField] private StatType statType;
    [SerializeField] private float bonusPerLevel;

    public StatType StatType => statType;
    public float BonusPerLevel => bonusPerLevel;

    public EnemyLevelStatGrowth(StatType statType, float bonusPerLevel)
    {
        this.statType = statType;
        this.bonusPerLevel = bonusPerLevel;
    }
}
