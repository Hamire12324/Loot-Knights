using System.Collections.Generic;
using UnityEngine;

public static class EquipmentSaveService
{
    public const string EquipmentKey = "LootKnights.Equipment.Items";

    public static IReadOnlyList<EquipmentItemSaveData> LoadItems()
    {
        EquipmentSaveData data = LoadData();
        List<EquipmentItemSaveData> items = new();

        foreach (EquipmentItemSaveData item in data.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId)) continue;

            items.Add(item.Clone());
        }

        return items;
    }

    public static void SaveItems(IEnumerable<EquipmentSlotData> slots)
    {
        EquipmentSaveData data = new();

        if (slots != null)
        {
            foreach (EquipmentSlotData slot in slots)
            {
                if (slot == null || slot.Item == null || string.IsNullOrWhiteSpace(slot.Item.ItemId)) continue;

                data.Items.Add(slot.EquipmentInstance != null && slot.EquipmentInstance.IsValid
                    ? new EquipmentItemSaveData(slot.SlotType, slot.EquipmentInstance)
                    : new EquipmentItemSaveData(slot.SlotType, slot.Item.ItemId));
            }
        }

        SaveData(data);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(GetCurrentEquipmentKey());
        PlayerPrefs.Save();
    }

    private static EquipmentSaveData LoadData()
    {
        string json = PlayerPrefs.GetString(GetLoadEquipmentKey(), string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return new EquipmentSaveData();

        EquipmentSaveData data = JsonUtility.FromJson<EquipmentSaveData>(json);
        data ??= new EquipmentSaveData();
        data.Items ??= new List<EquipmentItemSaveData>();
        RemoveInvalidItems(data);
        return data;
    }

    private static void SaveData(EquipmentSaveData data)
    {
        RemoveInvalidItems(data);
        PlayerPrefs.SetString(GetCurrentEquipmentKey(), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static void RemoveInvalidItems(EquipmentSaveData data)
    {
        data.Items.RemoveAll(item =>
            item == null ||
            string.IsNullOrWhiteSpace(item.ItemId));
    }

    private static string GetCurrentEquipmentKey() => CharacterProfileStorage.GetCurrentCharacterKey(EquipmentKey);

    private static string GetLoadEquipmentKey()
    {
        string characterKey = GetCurrentEquipmentKey();
        return !PlayerPrefs.HasKey(characterKey) && CharacterProfileStorage.IsLegacyProgressOwnedByCurrentCharacter()
            ? EquipmentKey : characterKey;
    }
}
