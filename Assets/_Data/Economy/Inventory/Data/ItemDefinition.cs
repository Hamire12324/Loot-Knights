using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private ItemCategory category = ItemCategory.Auto;
    [SerializeField] private EquipmentSlotType equipmentSlotType = EquipmentSlotType.None;
    [Tooltip("Optional set this equipment belongs to. Set bonuses are applied by PlayerEquipmentManager.")]
    [SerializeField] private EquipmentSetDefinition equipmentSet;
    [SerializeField] private int maxStack = 99;
    [SerializeField] private int maxUpgradeLevel = 10;
    [SerializeField] private List<StatModifierData> equipmentModifiers = new();
    [SerializeField] private List<StatModifierData> upgradeModifiersPerLevel = new();
    [SerializeField] private int minRolledModifierCount;
    [SerializeField] private int maxRolledModifierCount;
    [SerializeField] private List<StatModifierData> rolledModifierPool = new();

    public string ItemId => itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public ItemCategory Category => category == ItemCategory.Auto
        ? (MaxStack <= 1 ? ItemCategory.Equipment : ItemCategory.Item)
        : category;
    public int MaxStack => Mathf.Max(1, maxStack);
    public bool IsValid => !string.IsNullOrWhiteSpace(itemId);
    public EquipmentSlotType EquipmentSlotType => equipmentSlotType;
    public EquipmentSetDefinition EquipmentSet => equipmentSet;
    public int MaxUpgradeLevel => Mathf.Max(0, maxUpgradeLevel);
    public IReadOnlyList<StatModifierData> EquipmentModifiers => equipmentModifiers;
    public IReadOnlyList<StatModifierData> UpgradeModifiersPerLevel => upgradeModifiersPerLevel;
    public IReadOnlyList<StatModifierData> RolledModifierPool => rolledModifierPool;
    public float RarityStatMultiplier => GetRarityStatMultiplier(rarity);

    public List<StatModifier> BuildEquipmentModifiers(int upgradeLevel)
    {
        int safeUpgradeLevel = Mathf.Clamp(upgradeLevel, 0, MaxUpgradeLevel);
        List<RuntimeModifierTotal> totals = new();

        AddModifierTotals(totals, equipmentModifiers, 1, RarityStatMultiplier);
        AddModifierTotals(totals, upgradeModifiersPerLevel, safeUpgradeLevel, RarityStatMultiplier);

        List<StatModifier> modifiers = new();
        foreach (RuntimeModifierTotal total in totals)
        {
            modifiers.Add(new StatModifier(
                total.StatType,
                total.ModifierType,
                total.Amount));
        }

        return modifiers;
    }

    public EquipmentInstanceData CreateEquipmentInstance()
    {
        if (Category != ItemCategory.Equipment)
            return null;

        return new EquipmentInstanceData(ItemId, RollModifiers());
    }

    private static void AddModifierTotals(
        List<RuntimeModifierTotal> totals,
        IReadOnlyList<StatModifierData> source,
        int multiplier,
        float statMultiplier)
    {
        if (source == null || multiplier <= 0) return;

        foreach (StatModifierData modifierData in source)
        {
            if (modifierData == null || modifierData.StatType == StatType.None) continue;

            int index = FindModifierTotalIndex(
                totals,
                modifierData.StatType,
                modifierData.ModifierType);

            if (index >= 0)
            {
                RuntimeModifierTotal total = totals[index];
                total.Amount += modifierData.Amount * multiplier * statMultiplier;
                totals[index] = total;
                continue;
            }

            totals.Add(new RuntimeModifierTotal(
                modifierData.StatType,
                modifierData.ModifierType,
                modifierData.Amount * multiplier * statMultiplier));
        }
    }

    private static int FindModifierTotalIndex(
        IReadOnlyList<RuntimeModifierTotal> totals,
        StatType statType,
        ModifierType modifierType)
    {
        for (int i = 0; i < totals.Count; i++)
        {
            if (totals[i].StatType == statType &&
                totals[i].ModifierType == modifierType)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);
        maxUpgradeLevel = Mathf.Max(0, maxUpgradeLevel);
        minRolledModifierCount = Mathf.Max(0, minRolledModifierCount);
        maxRolledModifierCount = Mathf.Max(minRolledModifierCount, maxRolledModifierCount);
        equipmentModifiers ??= new List<StatModifierData>();
        upgradeModifiersPerLevel ??= new List<StatModifierData>();
        rolledModifierPool ??= new List<StatModifierData>();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;
    }

    private List<StatModifierData> RollModifiers()
    {
        List<StatModifierData> rolledModifiers = new();

        if (rolledModifierPool == null || rolledModifierPool.Count == 0 || maxRolledModifierCount <= 0)
            return rolledModifiers;

        int rollCount = UnityEngine.Random.Range(minRolledModifierCount, maxRolledModifierCount + 1);
        if (rollCount <= 0)
            return rolledModifiers;

        List<StatModifierData> candidates = new(rolledModifierPool);

        while (rolledModifiers.Count < rollCount && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            StatModifierData selected = candidates[index];
            candidates.RemoveAt(index);

            if (selected == null || selected.StatType == StatType.None) continue;

            rolledModifiers.Add(new StatModifierData(
                selected.StatType,
                selected.ModifierType,
                selected.Amount * RarityStatMultiplier));
        }

        return rolledModifiers;
    }

    private static float GetRarityStatMultiplier(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => 1.1f,
            ItemRarity.Rare => 1.25f,
            ItemRarity.Epic => 1.5f,
            ItemRarity.Legendary => 2f,
            _ => 1f
        };
    }

    private struct RuntimeModifierTotal
    {
        public readonly StatType StatType;
        public readonly ModifierType ModifierType;
        public float Amount;

        public RuntimeModifierTotal(StatType statType, ModifierType modifierType, float amount)
        {
            StatType = statType;
            ModifierType = modifierType;
            Amount = amount;
        }
    }
}
