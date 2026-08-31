using System;
using System.Collections.Generic;

public static class InventorySortingService
{
    public static bool TrySortByRarityAndName(
        InventoryContainer container,
        ItemDatabase itemDatabase,
        out List<InventoryItemStack> sortedStacks)
    {
        sortedStacks = container != null ? container.ToStacks() : new List<InventoryItemStack>();
        if (sortedStacks.Count <= 1)
            return false;

        List<InventoryItemStack> previousStacks = CloneStacks(sortedStacks);
        sortedStacks.Sort((left, right) => CompareStacks(left, right, itemDatabase));

        return !StacksEqual(previousStacks, sortedStacks) || !IsCompacted(container, sortedStacks.Count);
    }

    private static int CompareStacks(InventoryItemStack left, InventoryItemStack right, ItemDatabase itemDatabase)
    {
        ItemDefinition leftItem = itemDatabase != null ? itemDatabase.GetItem(left.ItemId) : null;
        ItemDefinition rightItem = itemDatabase != null ? itemDatabase.GetItem(right.ItemId) : null;

        int rarityCompare = GetRaritySortValue(rightItem).CompareTo(GetRaritySortValue(leftItem));
        if (rarityCompare != 0) return rarityCompare;

        string leftName = leftItem != null ? leftItem.DisplayName : left.ItemId;
        string rightName = rightItem != null ? rightItem.DisplayName : right.ItemId;
        return string.CompareOrdinal(leftName, rightName);
    }

    private static int GetRaritySortValue(ItemDefinition item)
    {
        return item != null ? (int)item.Rarity : 0;
    }

    private static bool IsCompacted(InventoryContainer container, int itemStackCount)
    {
        for (int i = 0; i < container.Capacity; i++)
        {
            InventorySlotData slot = container.GetSlot(i);
            bool shouldHaveItem = i < itemStackCount;
            bool hasItem = slot != null && !slot.IsEmpty;
            if (shouldHaveItem != hasItem)
                return false;
        }

        return true;
    }

    private static List<InventoryItemStack> CloneStacks(IEnumerable<InventoryItemStack> source)
    {
        List<InventoryItemStack> stacks = new();
        foreach (InventoryItemStack stack in source)
        {
            if (stack != null)
                stacks.Add(stack.Clone());
        }

        return stacks;
    }

    private static bool StacksEqual(IReadOnlyList<InventoryItemStack> left, IReadOnlyList<InventoryItemStack> right)
    {
        if (left.Count != right.Count) return false;

        for (int i = 0; i < left.Count; i++)
        {
            InventoryItemStack leftStack = left[i];
            InventoryItemStack rightStack = right[i];
            if (leftStack.ItemId != rightStack.ItemId || leftStack.Amount != rightStack.Amount)
                return false;

            string leftInstanceId = leftStack.EquipmentInstance != null ? leftStack.EquipmentInstance.InstanceId : null;
            string rightInstanceId = rightStack.EquipmentInstance != null ? rightStack.EquipmentInstance.InstanceId : null;
            if (leftInstanceId != rightInstanceId)
                return false;
        }

        return true;
    }
}
