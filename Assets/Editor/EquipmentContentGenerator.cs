using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the game's equipment assets from the imported CaptainCatSparrow icons.
/// It is safe to run again: existing assets are updated in place.
/// </summary>
public static class EquipmentContentGenerator
{
    private const string RootFolder = "Assets/_Data/Content/Equipment";
    private const string SetFolder = RootFolder + "/Sets";
    private const string ItemFolder = RootFolder + "/Items";
    private const string DatabaseFolder = "Assets/Resources/Inventory";
    private const string DatabasePath = DatabaseFolder + "/ItemDatabase.asset";
    private const string IconRoot = "Assets/_ThirdParty/CaptainCatSparrow/ArmorAndJewelry/Icons/";

    [MenuItem("Loot Knights/Content/Create Game Equipment And Loot")]
    public static void CreateGameEquipmentAndLoot()
    {
        EnsureContentFolders();

        EquipmentSetDefinition ironwall = CreateSet(
            "ironwall", "Ironwall", "Heavy armour made for the knight who holds the line.",
            Bonus(2, "Armor +12", StatType.Armor, ModifierType.Flat, 12f),
            Bonus(4, "Max Health +18%", StatType.MaxHealth, ModifierType.PercentAdd, 18f),
            Bonus(6, "Health Regen +3", StatType.HealthRegen, ModifierType.Flat, 3f));

        EquipmentSetDefinition stormhunter = CreateSet(
            "stormhunter", "Stormhunter", "Swift leather gear for relentless critical attacks.",
            Bonus(2, "Attack Speed +10%", StatType.AttackSpeed, ModifierType.PercentAdd, 10f),
            Bonus(4, "Crit Chance +8", StatType.CritChance, ModifierType.Flat, 8f),
            Bonus(6, "Attack +15%", StatType.Attack, ModifierType.PercentAdd, 15f));

        EquipmentSetDefinition sunforged = CreateSet(
            "sunforged", "Sunforged", "Radiant royal gear for veteran dungeon delvers.",
            Bonus(2, "Max Mana +35", StatType.MaxMana, ModifierType.Flat, 35f),
            Bonus(4, "Crit Damage +25%", StatType.CritDamage, ModifierType.PercentAdd, 25f),
            Bonus(6, "Attack +18 and Armor +18",
                new StatModifierData(StatType.Attack, ModifierType.Flat, 18f),
                new StatModifierData(StatType.Armor, ModifierType.Flat, 18f)));

        List<ItemDefinition> items = new();
        items.AddRange(CreateSetItems(ironwall, ItemRarity.Uncommon, 1, new[]
        {
            Item("ironwall_helmet", "Ironwall Helm", "A reinforced helm that deflects brutal blows.", EquipmentSlotType.Helmet, "Helmets/Helmet_1.png", StatType.Armor, 7f),
            Item("ironwall_cuirass", "Ironwall Cuirass", "Layered steel built to endure the front line.", EquipmentSlotType.Armor, "BodyArmor/BodyArmor_1.png", StatType.Armor, 14f),
            Item("ironwall_gauntlets", "Ironwall Gauntlets", "Heavy gauntlets that steady every strike.", EquipmentSlotType.Gloves, "Gloves/Gloves_1.png", StatType.MaxHealth, 35f),
            Item("ironwall_greaves", "Ironwall Greaves", "Steel boots made for unyielding steps.", EquipmentSlotType.Boots, "Boots/Boots_1.png", StatType.Armor, 5f),
            Item("ironwall_pendant", "Ironwall Pendant", "A sturdy pendant worn by old guardians.", EquipmentSlotType.Necklace, "Necklaces/Necklace_1.png", StatType.MaxHealth, 30f),
            Item("ironwall_band", "Ironwall Band", "A ring bearing the mark of a fortress.", EquipmentSlotType.Ring, "Rings/Ring_1.png", StatType.Armor, 4f)
        }));
        items.AddRange(CreateSetItems(stormhunter, ItemRarity.Rare, 2, new[]
        {
            Item("stormhunter_hood", "Stormhunter Hood", "A hood woven for hunters who strike first.", EquipmentSlotType.Helmet, "Helmets/Helmet_4.png", StatType.CritChance, 3f),
            Item("stormhunter_jerkin", "Stormhunter Jerkin", "Light armour that never slows the chase.", EquipmentSlotType.Armor, "BodyArmor/BodyArmor_4.png", StatType.AttackSpeed, 6f),
            Item("stormhunter_grips", "Stormhunter Grips", "Charged grips that sharpen swift attacks.", EquipmentSlotType.Gloves, "Gloves/Gloves_4.png", StatType.Attack, 7f),
            Item("stormhunter_boots", "Stormhunter Boots", "Boots that carry the wearer on a gust.", EquipmentSlotType.Boots, "Boots/Boots_4.png", StatType.MoveSpeed, 0.45f),
            Item("stormhunter_talisman", "Stormhunter Talisman", "A talisman that hums before the storm.", EquipmentSlotType.Necklace, "Necklaces/Necklace_4.png", StatType.CritDamage, 12f),
            Item("stormhunter_loop", "Stormhunter Loop", "A lightning-fast hunter's signet.", EquipmentSlotType.Ring, "Rings/Ring_4.png", StatType.AttackSpeed, 5f)
        }));
        items.AddRange(CreateSetItems(sunforged, ItemRarity.Epic, 3, new[]
        {
            Item("sunforged_crown", "Sunforged Crown", "A crown that carries the warmth of dawn.", EquipmentSlotType.Helmet, "Helmets/Helmet_8.png", StatType.MaxMana, 25f),
            Item("sunforged_plate", "Sunforged Plate", "Radiant plate forged for champions.", EquipmentSlotType.Armor, "BodyArmor/BodyArmor_8.png", StatType.Armor, 18f),
            Item("sunforged_handguards", "Sunforged Handguards", "Golden guards that channel sacred strength.", EquipmentSlotType.Gloves, "Gloves/Gloves_8.png", StatType.Attack, 11f),
            Item("sunforged_sabatons", "Sunforged Sabatons", "Sabatons that leave a trail of embers.", EquipmentSlotType.Boots, "Boots/Boots_8.png", StatType.MoveSpeed, 0.7f),
            Item("sunforged_amulet", "Sunforged Amulet", "An amulet blessed with the first sunrise.", EquipmentSlotType.Necklace, "Necklaces/Necklace_8.png", StatType.CritDamage, 18f),
            Item("sunforged_seal", "Sunforged Seal", "A royal seal for those worthy of its light.", EquipmentSlotType.Ring, "Rings/Ring_8.png", StatType.Attack, 8f)
        }));

        AddItemsToDatabase(items);
        ApplyRecommendedStageLoot(items);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        Debug.Log("Created 18 CaptainCatSparrow equipment items, 3 complete sets, and recommended loot for every stage.");
    }

    private static List<ItemDefinition> CreateSetItems(EquipmentSetDefinition set, ItemRarity rarity, int tier, EquipmentItemData[] definitions)
    {
        EnsureFolder(ItemFolder + "/Sets/" + set.SetId);
        List<ItemDefinition> results = new(definitions.Length);
        foreach (EquipmentItemData definition in definitions)
            results.Add(CreateItem(definition, set, rarity, tier));
        return results;
    }

    private static EquipmentSetDefinition CreateSet(string id, string displayName, string description, params BonusData[] bonuses)
    {
        string path = SetFolder + "/" + id + ".asset";
        EquipmentSetDefinition set = AssetDatabase.LoadAssetAtPath<EquipmentSetDefinition>(path) ?? CreateAsset<EquipmentSetDefinition>(path);
        SerializedObject serialized = new(set);
        serialized.FindProperty("setId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
        SerializedProperty array = serialized.FindProperty("bonuses");
        array.arraySize = bonuses.Length;
        for (int i = 0; i < bonuses.Length; i++)
        {
            SerializedProperty bonus = array.GetArrayElementAtIndex(i);
            bonus.FindPropertyRelative("requiredPieceCount").intValue = bonuses[i].RequiredPieces;
            bonus.FindPropertyRelative("description").stringValue = bonuses[i].Description;
            WriteModifiers(bonus.FindPropertyRelative("modifiers"), bonuses[i].Modifiers);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return set;
    }

    private static ItemDefinition CreateItem(EquipmentItemData data, EquipmentSetDefinition set, ItemRarity rarity, int tier)
    {
        string path = ItemFolder + "/Sets/" + set.SetId + "/" + data.Id + ".asset";
        ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) ?? CreateAsset<ItemDefinition>(path);
        SerializedObject serialized = new(item);
        serialized.FindProperty("itemId").stringValue = data.Id;
        serialized.FindProperty("displayName").stringValue = data.Name;
        serialized.FindProperty("description").stringValue = data.Description;
        serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(IconRoot + data.IconPath);
        serialized.FindProperty("rarity").enumValueIndex = (int)rarity;
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Equipment;
        serialized.FindProperty("equipmentSlotType").enumValueIndex = (int)data.Slot;
        serialized.FindProperty("equipmentSet").objectReferenceValue = set;
        serialized.FindProperty("maxStack").intValue = 1;
        serialized.FindProperty("maxUpgradeLevel").intValue = 10;
        WriteModifiers(serialized.FindProperty("equipmentModifiers"), new[] { new StatModifierData(data.Stat, ModifierType.Flat, data.Amount) });
        WriteModifiers(serialized.FindProperty("upgradeModifiersPerLevel"), new[] { new StatModifierData(data.Stat, ModifierType.Flat, Mathf.Max(1f, data.Amount * 0.2f)) });
        serialized.FindProperty("minRolledModifierCount").intValue = tier >= 2 ? 1 : 0;
        serialized.FindProperty("maxRolledModifierCount").intValue = tier >= 3 ? 2 : 1;
        WriteModifiers(serialized.FindProperty("rolledModifierPool"), BuildRollPool(data.Slot, tier));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    private static StatModifierData[] BuildRollPool(EquipmentSlotType slot, int tier)
    {
        float bonus = tier * 2f;
        return slot switch
        {
            EquipmentSlotType.Helmet or EquipmentSlotType.Armor => new[] { new StatModifierData(StatType.Armor, ModifierType.Flat, bonus), new StatModifierData(StatType.MaxHealth, ModifierType.Flat, bonus * 10f) },
            EquipmentSlotType.Gloves => new[] { new StatModifierData(StatType.Attack, ModifierType.Flat, bonus), new StatModifierData(StatType.AttackSpeed, ModifierType.PercentAdd, bonus) },
            EquipmentSlotType.Boots => new[] { new StatModifierData(StatType.MoveSpeed, ModifierType.Flat, bonus * 0.05f), new StatModifierData(StatType.HealthRegen, ModifierType.Flat, tier) },
            _ => new[] { new StatModifierData(StatType.CritChance, ModifierType.Flat, tier), new StatModifierData(StatType.CritDamage, ModifierType.PercentAdd, bonus * 2f) }
        };
    }

    private static void ApplyRecommendedStageLoot(IReadOnlyList<ItemDefinition> items)
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:StageConfig", new[] { "Assets/Resources/Stages" });
        foreach (string guid in stageGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageConfig stage = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (stage == null) continue;
            int tier = stage.DifficultyLevel <= 5 ? 1 : stage.DifficultyLevel <= 12 ? 2 : 3;
            WriteStageDrops(stage, GetItemsForTier(items, tier));
        }
    }

    private static List<ItemDefinition> GetItemsForTier(IReadOnlyList<ItemDefinition> items, int tier)
    {
        int start = (tier - 1) * 6;
        List<ItemDefinition> tierItems = new(6);
        for (int i = start; i < start + 6 && i < items.Count; i++) tierItems.Add(items[i]);
        return tierItems;
    }

    private static void WriteStageDrops(StageConfig stage, IReadOnlyList<ItemDefinition> items)
    {
        SerializedObject serialized = new(stage);
        SerializedProperty drops = serialized.FindProperty("itemDrops");
        drops.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            SerializedProperty drop = drops.GetArrayElementAtIndex(i);
            drop.FindPropertyRelative("item").objectReferenceValue = items[i];
            drop.FindPropertyRelative("weight").floatValue = i < 4 ? 22f : 6f;
            drop.FindPropertyRelative("minAmount").intValue = 1;
            drop.FindPropertyRelative("maxAmount").intValue = 1;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddItemsToDatabase(IEnumerable<ItemDefinition> items)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath) ?? CreateAsset<ItemDatabase>(DatabasePath);
        SerializedObject serialized = new(database);
        SerializedProperty databaseItems = serialized.FindProperty("items");
        foreach (ItemDefinition item in items)
        {
            if (ContainsItem(databaseItems, item)) continue;
            int index = databaseItems.arraySize;
            databaseItems.InsertArrayElementAtIndex(index);
            databaseItems.GetArrayElementAtIndex(index).objectReferenceValue = item;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool ContainsItem(SerializedProperty items, ItemDefinition target)
    {
        for (int i = 0; i < items.arraySize; i++)
            if (items.GetArrayElementAtIndex(i).objectReferenceValue == target) return true;
        return false;
    }

    private static void WriteModifiers(SerializedProperty destination, IReadOnlyList<StatModifierData> modifiers)
    {
        destination.arraySize = modifiers.Count;
        for (int i = 0; i < modifiers.Count; i++)
        {
            SerializedProperty modifier = destination.GetArrayElementAtIndex(i);
            modifier.FindPropertyRelative("statType").enumValueIndex = (int)modifiers[i].StatType;
            modifier.FindPropertyRelative("modifierType").enumValueIndex = (int)modifiers[i].ModifierType;
            modifier.FindPropertyRelative("amount").floatValue = modifiers[i].Amount;
        }
    }

    private static void EnsureContentFolders()
    {
        EnsureFolder("Assets/_Data/Content");
        EnsureFolder(RootFolder);
        EnsureFolder(SetFolder);
        EnsureFolder(ItemFolder);
        EnsureFolder(ItemFolder + "/Sets");
        EnsureFolder(ItemFolder + "/Weapons");
        EnsureFolder("Assets/Resources/Inventory");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slashIndex = folder.LastIndexOf('/');
        AssetDatabase.CreateFolder(folder.Substring(0, slashIndex), folder.Substring(slashIndex + 1));
    }

    private static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static EquipmentItemData Item(string id, string name, string description, EquipmentSlotType slot, string iconPath, StatType stat, float amount)
    {
        return new EquipmentItemData(id, name, description, slot, iconPath, stat, amount);
    }

    private static BonusData Bonus(int pieces, string description, StatType stat, ModifierType type, float amount)
    {
        return new BonusData(pieces, description, new StatModifierData(stat, type, amount));
    }

    private static BonusData Bonus(int pieces, string description, params StatModifierData[] modifiers)
    {
        return new BonusData(pieces, description, modifiers);
    }

    private readonly struct EquipmentItemData
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public EquipmentSlotType Slot { get; }
        public string IconPath { get; }
        public StatType Stat { get; }
        public float Amount { get; }
        public EquipmentItemData(string id, string name, string description, EquipmentSlotType slot, string iconPath, StatType stat, float amount) => (Id, Name, Description, Slot, IconPath, Stat, Amount) = (id, name, description, slot, iconPath, stat, amount);
    }

    private readonly struct BonusData
    {
        public int RequiredPieces { get; }
        public string Description { get; }
        public IReadOnlyList<StatModifierData> Modifiers { get; }
        public BonusData(int requiredPieces, string description, params StatModifierData[] modifiers) => (RequiredPieces, Description, Modifiers) = (requiredPieces, description, modifiers);
    }
}
