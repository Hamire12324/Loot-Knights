using UnityEngine;

public static class EquipmentUpgradeStorage
{
    public const string UpgradeKeyPrefix = "LootKnights.Equipment.Upgrade.";

    public static int GetLevel(ItemDefinition item)
    {
        if (item == null || !item.IsValid) return 0;

        int savedLevel = PlayerPrefs.GetInt(GetKey(item), 0);
        return Mathf.Clamp(savedLevel, 0, item.MaxUpgradeLevel);
    }

    public static void SetLevel(ItemDefinition item, int level)
    {
        if (item == null || !item.IsValid) return;

        int safeLevel = Mathf.Clamp(level, 0, item.MaxUpgradeLevel);
        PlayerPrefs.SetInt(GetKey(item), safeLevel);
        PlayerPrefs.Save();
    }

    public static int AddLevels(ItemDefinition item, int amount)
    {
        if (item == null || !item.IsValid) return 0;

        int safeAmount = Mathf.Max(0, amount);
        int level = Mathf.Clamp(GetLevel(item) + safeAmount, 0, item.MaxUpgradeLevel);
        SetLevel(item, level);
        return level;
    }

    public static void Delete(ItemDefinition item)
    {
        if (item == null || !item.IsValid) return;

        PlayerPrefs.DeleteKey(GetKey(item));
        PlayerPrefs.Save();
    }

    private static string GetKey(ItemDefinition item)
    {
        return UpgradeKeyPrefix + item.ItemId;
    }
}
