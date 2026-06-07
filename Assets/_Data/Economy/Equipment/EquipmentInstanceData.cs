using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentInstanceData
{
    [SerializeField] private string instanceId;
    [SerializeField] private string itemId;
    [SerializeField] private int upgradeLevel;
    [SerializeField] private List<StatModifierData> rolledModifiers = new();

    public string InstanceId => instanceId;
    public string ItemId => itemId;
    public int UpgradeLevel => Mathf.Max(0, upgradeLevel);
    public IReadOnlyList<StatModifierData> RolledModifiers => rolledModifiers;
    public bool IsValid => !string.IsNullOrWhiteSpace(instanceId) && !string.IsNullOrWhiteSpace(itemId);

    public EquipmentInstanceData(string itemId, IEnumerable<StatModifierData> rolledModifiers = null)
    {
        instanceId = Guid.NewGuid().ToString("N");
        this.itemId = itemId;
        upgradeLevel = 0;
        this.rolledModifiers = CloneModifiers(rolledModifiers);
    }

    public EquipmentInstanceData(
        string instanceId,
        string itemId,
        int upgradeLevel,
        IEnumerable<StatModifierData> rolledModifiers = null)
    {
        this.instanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
        this.itemId = itemId;
        this.upgradeLevel = Mathf.Max(0, upgradeLevel);
        this.rolledModifiers = CloneModifiers(rolledModifiers);
    }

    public EquipmentInstanceData Clone()
    {
        return new EquipmentInstanceData(instanceId, itemId, upgradeLevel, rolledModifiers);
    }

    public void SetUpgradeLevel(int value, int maxUpgradeLevel)
    {
        upgradeLevel = Mathf.Clamp(value, 0, Mathf.Max(0, maxUpgradeLevel));
    }

    public void AddUpgradeLevels(int amount, int maxUpgradeLevel)
    {
        if (amount <= 0) return;

        SetUpgradeLevel(upgradeLevel + amount, maxUpgradeLevel);
    }

    public List<StatModifier> BuildModifiers(ItemDefinition item)
    {
        List<StatModifier> modifiers = item != null
            ? item.BuildEquipmentModifiers(UpgradeLevel)
            : new List<StatModifier>();

        foreach (StatModifierData modifierData in rolledModifiers)
        {
            if (modifierData == null || modifierData.StatType == StatType.None) continue;

            modifiers.Add(new StatModifier(
                modifierData.StatType,
                modifierData.ModifierType,
                modifierData.Amount));
        }

        return modifiers;
    }

    private static List<StatModifierData> CloneModifiers(IEnumerable<StatModifierData> source)
    {
        List<StatModifierData> clone = new();

        if (source == null)
            return clone;

        foreach (StatModifierData modifierData in source)
        {
            if (modifierData == null) continue;

            clone.Add(new StatModifierData(
                modifierData.StatType,
                modifierData.ModifierType,
                modifierData.Amount));
        }

        return clone;
    }
}
