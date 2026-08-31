using System.Collections.Generic;
using UnityEngine;

public static class InventorySaveService
{
    public const string InventoryKey = "LootKnights.Inventory.Items";

    public static IReadOnlyList<InventoryItemStack> LoadItems()
    {
        InventorySaveData data = LoadData();
        List<InventoryItemStack> items = new();

        foreach (InventoryItemStack stack in data.Items)
        {
            if (stack == null) continue;

            items.Add(stack.Clone());
        }

        return items;
    }

    public static void SaveItems(IEnumerable<InventoryItemStack> items)
    {
        InventorySaveData data = new();

        if (items != null)
        {
            foreach (InventoryItemStack item in items)
            {
                if (item == null) continue;
                data.Items.Add(item.Clone());
            }
        }

        SaveData(data);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(GetCurrentInventoryKey());
        PlayerPrefs.Save();
    }

    private static InventorySaveData LoadData()
    {
        string json = PlayerPrefs.GetString(GetLoadInventoryKey(), string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return new InventorySaveData();

        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        data ??= new InventorySaveData();
        data.Items ??= new List<InventoryItemStack>();
        RemoveInvalidStacks(data);
        return data;
    }

    private static void SaveData(InventorySaveData data)
    {
        RemoveInvalidStacks(data);
        PlayerPrefs.SetString(GetCurrentInventoryKey(), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static void RemoveInvalidStacks(InventorySaveData data)
    {
        data.Items.RemoveAll(stack =>
            stack == null ||
            string.IsNullOrWhiteSpace(stack.ItemId) ||
            stack.Amount <= 0);
    }

    private static string GetCurrentInventoryKey() => CharacterProfileStorage.GetCurrentCharacterKey(InventoryKey);

    private static string GetLoadInventoryKey()
    {
        string characterKey = GetCurrentInventoryKey();
        return !PlayerPrefs.HasKey(characterKey) && CharacterProfileStorage.IsLegacyProgressOwnedByCurrentCharacter()
            ? InventoryKey : characterKey;
    }
}
