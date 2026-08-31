using System.Collections.Generic;

public static class EquipmentLoadService
{
    public static void LoadInto(EquipmentInventory inventory, ItemDatabase itemDatabase)
    {
        if (inventory == null) return;

        inventory.EnsureDefaultSlots();
        inventory.ClearAll();

        if (itemDatabase == null) return;

        IReadOnlyList<EquipmentItemSaveData> savedItems = EquipmentSaveService.LoadItems();
        foreach (EquipmentItemSaveData savedItem in savedItems)
        {
            if (savedItem == null || string.IsNullOrWhiteSpace(savedItem.ItemId)) continue;
            if (!itemDatabase.TryGetItem(savedItem.ItemId, out ItemDefinition item)) continue;
            if (!CanEquip(item, savedItem.SlotType)) continue;

            EquipmentInstanceData instance = savedItem.HasEquipmentInstance
                ? savedItem.EquipmentInstance
                : item.CreateEquipmentInstance();

            inventory.SetItem(savedItem.SlotType, item, instance);
        }
    }

    private static bool CanEquip(ItemDefinition item, EquipmentSlotType slotType)
    {
        return item != null &&
               item.IsValid &&
               item.Category == ItemCategory.Equipment &&
               item.EquipmentSlotType == slotType;
    }
}
