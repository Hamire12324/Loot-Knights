using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryContainer
{
    private readonly List<InventorySlotData> slots = new();

    public int Capacity => slots.Count;
    public IReadOnlyList<InventorySlotData> Slots => slots;

    public InventoryContainer(int capacity)
    {
        SetCapacity(capacity);
    }

    public InventoryOperationResult SetCapacity(int capacity)
    {
        int safeCapacity = Mathf.Max(1, capacity);
        if (slots.Count == safeCapacity)
            return InventoryOperationResult.NoChange(InventoryChangeType.CapacityChanged);

        int previousCapacity = slots.Count;
        while (slots.Count < safeCapacity)
            slots.Add(new InventorySlotData());

        while (slots.Count > safeCapacity)
            slots.RemoveAt(slots.Count - 1);

        int changedCount = Mathf.Max(previousCapacity, safeCapacity);
        return InventoryOperationResult.Succeeded(
            InventoryChangeType.CapacityChanged,
            GetSlotIndexes(changedCount));
    }

    public InventorySlotData GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    public InventoryOperationResult AddItem(ItemDefinition item, int amount)
    {
        if (item == null || !item.IsValid)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidItem, requestedAmount: amount);

        if (amount <= 0)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidAmount);

        if (item.Category == ItemCategory.Equipment)
            return AddEquipmentItems(item, amount);

        if (!CanAddItem(item, amount))
            return InventoryOperationResult.Failed(InventoryOperationStatus.NotEnoughSpace, InventoryChangeType.Added, amount);

        int remaining = amount;
        HashSet<int> changedSlots = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (remaining <= 0) break;
            InventorySlotData slot = slots[i];
            if (slot == null || !slot.CanStack(item)) continue;

            int added = slot.AddQuantity(remaining);
            if (added <= 0) continue;

            remaining -= added;
            changedSlots.Add(i);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (remaining <= 0) break;
            InventorySlotData slot = slots[i];
            if (slot == null || !slot.IsEmpty) continue;

            int stackAmount = Mathf.Min(remaining, item.MaxStack);
            slot.Set(item, stackAmount);
            remaining -= stackAmount;
            changedSlots.Add(i);
        }

        return InventoryOperationResult.Succeeded(
            InventoryChangeType.Added,
            changedSlots,
            amount,
            amount - remaining);
    }

    public InventoryOperationResult AddEquipmentInstance(ItemDefinition item, EquipmentInstanceData equipmentInstance)
    {
        if (item == null || !item.IsValid || item.Category != ItemCategory.Equipment)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidItem, requestedAmount: 1);

        EquipmentInstanceData safeInstance = equipmentInstance != null && equipmentInstance.IsValid
            ? equipmentInstance.Clone()
            : item.CreateEquipmentInstance();

        if (safeInstance == null || !safeInstance.IsValid)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidItem, requestedAmount: 1);

        if (!CanAddItem(item, 1))
            return InventoryOperationResult.Failed(InventoryOperationStatus.NotEnoughSpace, InventoryChangeType.Added, 1);

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || !slot.IsEmpty) continue;

            slot.Set(item, 1, safeInstance);
            return InventoryOperationResult.Succeeded(InventoryChangeType.Added, new[] { i }, 1, 1);
        }

        return InventoryOperationResult.Failed(InventoryOperationStatus.NotEnoughSpace, InventoryChangeType.Added, 1);
    }

    private InventoryOperationResult AddEquipmentItems(ItemDefinition item, int amount)
    {
        if (!CanAddItem(item, amount))
            return InventoryOperationResult.Failed(InventoryOperationStatus.NotEnoughSpace, InventoryChangeType.Added, amount);

        HashSet<int> changedSlots = new();
        int accepted = 0;

        for (int i = 0; i < amount; i++)
        {
            InventoryOperationResult result = AddEquipmentInstance(item, item.CreateEquipmentInstance());
            if (result == null || !result.Success) break;

            accepted += result.AcceptedAmount;
            foreach (int slotIndex in result.ChangedSlots)
                changedSlots.Add(slotIndex);
        }

        return InventoryOperationResult.Succeeded(InventoryChangeType.Added, changedSlots, amount, accepted);
    }

    public InventoryOperationResult RemoveItem(ItemDefinition item, int amount)
    {
        if (item == null)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidItem, requestedAmount: amount);

        if (amount <= 0)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidAmount);

        if (GetAmount(item) < amount)
            return InventoryOperationResult.Failed(InventoryOperationStatus.NotEnoughItems, InventoryChangeType.Removed, amount);

        int remaining = amount;
        HashSet<int> changedSlots = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (remaining <= 0) break;
            InventorySlotData slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.Item != item) continue;

            int removeAmount = Mathf.Min(slot.Amount, remaining);
            slot.TryRemoveQuantity(removeAmount);
            remaining -= removeAmount;
            changedSlots.Add(i);
        }

        return InventoryOperationResult.Succeeded(
            InventoryChangeType.Removed,
            changedSlots,
            amount,
            amount - remaining);
    }

    public InventoryOperationResult RemoveSlot(int index)
    {
        if (!IsValidIndex(index))
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidSlot, InventoryChangeType.SlotCleared);

        InventorySlotData slot = GetSlot(index);
        if (slot == null || slot.IsEmpty)
            return InventoryOperationResult.Failed(InventoryOperationStatus.EmptySlot, InventoryChangeType.SlotCleared);

        slot.Clear();
        return InventoryOperationResult.Succeeded(InventoryChangeType.SlotCleared, new[] { index });
    }

    public InventoryOperationResult Swap(int indexA, int indexB)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB))
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidSlot, InventoryChangeType.Swapped);

        if (indexA == indexB)
            return InventoryOperationResult.NoChange(InventoryChangeType.Swapped);

        (slots[indexA], slots[indexB]) = (slots[indexB], slots[indexA]);
        return InventoryOperationResult.Succeeded(InventoryChangeType.Swapped, new[] { indexA, indexB });
    }

    public int GetAmount(ItemDefinition item)
    {
        if (item == null) return 0;

        int total = 0;
        foreach (InventorySlotData slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.Item != item) continue;
            total += slot.Amount;
        }

        return total;
    }

    public bool CanAddItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return false;

        if (item.Category == ItemCategory.Equipment)
        {
            int emptySlots = 0;
            foreach (InventorySlotData slot in slots)
            {
                if (slot != null && slot.IsEmpty)
                    emptySlots++;
            }

            return emptySlots >= amount;
        }

        int remaining = amount;

        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0) return true;
            if (slot == null) continue;

            remaining -= slot.GetAvailableSpace(item);
        }

        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0) return true;
            if (slot == null || !slot.IsEmpty) continue;

            remaining -= item.MaxStack;
        }

        return remaining <= 0;
    }

    public InventoryOperationResult ClearAll()
    {
        List<int> changedSlots = new();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || slot.IsEmpty) continue;

            slot.Clear();
            changedSlots.Add(i);
        }

        if (changedSlots.Count == 0)
            return InventoryOperationResult.NoChange(InventoryChangeType.Cleared);

        return InventoryOperationResult.Succeeded(InventoryChangeType.Cleared, changedSlots);
    }

    public InventoryOperationResult LoadFromStacks(IEnumerable<InventoryItemStack> stacks, ItemDatabase itemDatabase)
    {
        ClearAll();
        if (stacks == null)
            return InventoryOperationResult.NoChange(InventoryChangeType.Loaded);

        if (itemDatabase == null)
            return InventoryOperationResult.Failed(InventoryOperationStatus.DatabaseMissing, InventoryChangeType.Loaded);

        HashSet<int> changedSlots = new();

        foreach (InventoryItemStack stack in stacks)
        {
            if (stack == null || string.IsNullOrWhiteSpace(stack.ItemId)) continue;
            if (!itemDatabase.TryGetItem(stack.ItemId, out ItemDefinition item)) continue;

            InventoryOperationResult result = stack.HasEquipmentInstance
                ? AddEquipmentInstance(item, stack.EquipmentInstance)
                : AddItem(item, stack.Amount);

            foreach (int slotIndex in result.ChangedSlots)
                changedSlots.Add(slotIndex);
        }

        return InventoryOperationResult.Succeeded(InventoryChangeType.Loaded, changedSlots);
    }

    public List<InventoryItemStack> ToStacks()
    {
        List<InventoryItemStack> stacks = new();

        foreach (InventorySlotData slot in slots)
        {
            InventoryItemStack stack = slot?.ToStack();
            if (stack != null)
                stacks.Add(stack);
        }

        return stacks;
    }

    public List<int> GetAllSlotIndexes()
    {
        return GetSlotIndexes(slots.Count).ToList();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Count;
    }

    private static IEnumerable<int> GetSlotIndexes(int count)
    {
        for (int i = 0; i < count; i++)
            yield return i;
    }
}
