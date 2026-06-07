using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInventoryManager : BaseSingleton<PlayerInventoryManager>
{
    public const int DefaultCapacity = 30;

    public event Action<InventoryOperationResult> OnInventoryChanged;

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private int inventoryCapacity = DefaultCapacity;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float saveDelay = 0.25f;
    [SerializeField] private List<InventoryItemStack> debugRuntimeItems = new();

    private InventoryContainer container;
    private bool loaded;
    private bool saveDirty;
    private float nextSaveTime;

    public InventoryContainer Inventory
    {
        get
        {
            EnsureLoaded();
            return container;
        }
    }

    public ItemDatabase Database
    {
        get
        {
            EnsureLoaded();
            return itemDatabase;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (InstanceOrNull != this) return;

        if (loadOnAwake)
            EnsureLoaded();
    }

    protected override void Update()
    {
        base.Update();

        if (!autoSave || !saveDirty) return;
        if (Time.unscaledTime < nextSaveTime) return;

        FlushSave();
    }

    protected override void OnDisable()
    {
        FlushSave();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        FlushSave();
        base.OnDestroy();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (itemDatabase == null)
            itemDatabase = global::ItemDatabase.LoadDefault();

        if (inventoryCapacity <= 0)
            inventoryCapacity = DefaultCapacity;
    }

    public InventoryOperationResult SetCapacity(int value)
    {
        EnsureLoaded();

        int safeCapacity = Mathf.Max(1, value);
        List<InventoryItemStack> stacks = container.ToStacks();
        safeCapacity = Mathf.Max(safeCapacity, stacks.Count);

        if (inventoryCapacity == safeCapacity && container.Capacity == safeCapacity)
            return InventoryOperationResult.NoChange(InventoryChangeType.CapacityChanged);

        inventoryCapacity = safeCapacity;
        container = new InventoryContainer(inventoryCapacity);
        container.LoadFromStacks(stacks, itemDatabase);

        InventoryOperationResult result = InventoryOperationResult.Succeeded(
            InventoryChangeType.CapacityChanged,
            container.GetAllSlotIndexes());

        return CommitResult(result, true);
    }

    public InventoryOperationResult AddItem(ItemDefinition item, int amount = 1)
    {
        EnsureLoaded();

        InventoryOperationResult result = container.AddItem(item, amount);
        return CommitResult(result, true);
    }

    public InventoryOperationResult AddEquipmentInstance(
        ItemDefinition item,
        EquipmentInstanceData equipmentInstance)
    {
        EnsureLoaded();

        InventoryOperationResult result = container.AddEquipmentInstance(item, equipmentInstance);
        return CommitResult(result, true);
    }

    public EquipmentInstanceData GetEquipmentInstanceAtSlot(int index)
    {
        EnsureLoaded();

        InventorySlotData slot = container.GetSlot(index);
        return slot != null && !slot.IsEmpty ? slot.EquipmentInstance : null;
    }

    public InventoryOperationResult AddItem(string itemId, int amount = 1)
    {
        if (Database == null)
            return InventoryOperationResult.Failed(InventoryOperationStatus.DatabaseMissing, requestedAmount: amount);

        if (!Database.TryGetItem(itemId, out ItemDefinition item))
            return InventoryOperationResult.Failed(InventoryOperationStatus.ItemNotFound, requestedAmount: amount);

        return AddItem(item, amount);
    }

    public InventoryOperationResult TryRemoveItem(ItemDefinition item, int amount = 1)
    {
        EnsureLoaded();

        InventoryOperationResult result = container.RemoveItem(item, amount);
        return CommitResult(result, true);
    }

    public InventoryOperationResult RemoveSlot(int index)
    {
        EnsureLoaded();

        InventoryOperationResult result = container.RemoveSlot(index);
        return CommitResult(result, true);
    }

    public InventoryOperationResult RemoveItemAtSlot(int index, int amount = 1)
    {
        EnsureLoaded();

        InventorySlotData slot = container.GetSlot(index);
        if (slot == null)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidSlot, InventoryChangeType.Removed, amount);

        if (slot.IsEmpty)
            return InventoryOperationResult.Failed(InventoryOperationStatus.EmptySlot, InventoryChangeType.Removed, amount);

        if (amount <= 0)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidAmount, InventoryChangeType.Removed, amount);

        int removedAmount = Mathf.Min(amount, slot.Amount);
        slot.TryRemoveQuantity(removedAmount);

        InventoryOperationResult result = InventoryOperationResult.Succeeded(
            InventoryChangeType.Removed,
            new[] { index },
            amount,
            removedAmount);

        return CommitResult(result, true);
    }

    public InventoryOperationResult SwapSlots(int indexA, int indexB)
    {
        EnsureLoaded();

        InventoryOperationResult result = container.Swap(indexA, indexB);
        return CommitResult(result, true);
    }

    public InventoryOperationResult ArrangeByRarityAndName()
    {
        EnsureLoaded();

        List<InventoryItemStack> stacks = container.ToStacks();
        if (stacks.Count <= 1)
            return InventoryOperationResult.NoChange(InventoryChangeType.Arranged);

        List<InventoryItemStack> previousStacks = CloneStacks(stacks);
        stacks.Sort(CompareStacks);

        if (StacksEqual(previousStacks, stacks) && IsCompacted(stacks.Count))
            return InventoryOperationResult.NoChange(InventoryChangeType.Arranged);

        container.ClearAll();
        container.LoadFromStacks(stacks, itemDatabase);

        InventoryOperationResult result = InventoryOperationResult.Succeeded(
            InventoryChangeType.Arranged,
            container.GetAllSlotIndexes());

        return CommitResult(result, true);
    }

    public InventoryOperationResult Reload()
    {
        FlushSave();
        loaded = false;
        EnsureLoaded();

        InventoryOperationResult result = InventoryOperationResult.Succeeded(
            InventoryChangeType.Loaded,
            container.GetAllSlotIndexes());

        OnInventoryChanged?.Invoke(result);
        return result;
    }

    public InventoryOperationResult Clear()
    {
        EnsureLoaded();

        container.ClearAll();
        InventorySaveService.Clear();
        saveDirty = false;
        RefreshDebugSnapshot();

        InventoryOperationResult result = InventoryOperationResult.Succeeded(
            InventoryChangeType.Cleared,
            container.GetAllSlotIndexes());

        OnInventoryChanged?.Invoke(result);
        return result;
    }

    private void EnsureLoaded()
    {
        if (loaded) return;

        if (itemDatabase == null)
            itemDatabase = global::ItemDatabase.LoadDefault();

        IReadOnlyList<InventoryItemStack> savedItems = InventorySaveService.LoadItems();
        int safeCapacity = Mathf.Max(inventoryCapacity, savedItems != null ? savedItems.Count : 0);

        container = new InventoryContainer(safeCapacity);
        container.LoadFromStacks(savedItems, itemDatabase);
        loaded = true;
        RefreshDebugSnapshot();
    }

    private InventoryOperationResult CommitResult(InventoryOperationResult result, bool save)
    {
        if (result == null || !result.Success || result.Status == InventoryOperationStatus.NoChange)
            return result;

        RefreshDebugSnapshot();

        if (save)
            MarkSaveDirty();

        OnInventoryChanged?.Invoke(result);
        return result;
    }

    private void MarkSaveDirty()
    {
        saveDirty = true;
        nextSaveTime = Time.unscaledTime + Mathf.Max(0f, saveDelay);

        if (autoSave && saveDelay <= 0f)
            FlushSave();
    }

    [ContextMenu("Save Runtime Inventory Now")]
    public void SaveNow()
    {
        EnsureLoaded();
        saveDirty = true;
        FlushSave();
    }

    private void FlushSave()
    {
        if (!loaded || container == null || !saveDirty) return;

        InventorySaveService.SaveItems(container.ToStacks());
        saveDirty = false;
        RefreshDebugSnapshot();
    }

    private int CompareStacks(InventoryItemStack left, InventoryItemStack right)
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

    private bool IsCompacted(int itemStackCount)
    {
        if (container == null)
            return true;

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
            if (stack == null) continue;
            stacks.Add(stack.Clone());
        }

        return stacks;
    }

    private static bool StacksEqual(IReadOnlyList<InventoryItemStack> left, IReadOnlyList<InventoryItemStack> right)
    {
        if (left == null || right == null) return left == right;
        if (left.Count != right.Count) return false;

        for (int i = 0; i < left.Count; i++)
        {
            InventoryItemStack leftStack = left[i];
            InventoryItemStack rightStack = right[i];

            if (leftStack == null || rightStack == null)
            {
                if (leftStack != rightStack) return false;
                continue;
            }

            if (leftStack.ItemId != rightStack.ItemId) return false;
            if (leftStack.Amount != rightStack.Amount) return false;
        }

        return true;
    }

    [ContextMenu("Refresh Debug Runtime Items")]
    public void RefreshDebugSnapshot()
    {
        debugRuntimeItems.Clear();

        if (container == null) return;

        foreach (InventoryItemStack stack in container.ToStacks())
            debugRuntimeItems.Add(stack);
    }

    [ContextMenu("Log Runtime Inventory")]
    public void LogRuntimeInventory()
    {
        EnsureLoaded();
        RefreshDebugSnapshot();

        StringBuilder builder = new();
        builder.AppendLine("Runtime Inventory:");

        if (debugRuntimeItems.Count == 0)
        {
            builder.AppendLine("- Empty");
        }
        else
        {
            for (int i = 0; i < Inventory.Capacity; i++)
            {
                InventorySlotData slot = Inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;

                builder.Append("- Slot ");
                builder.Append(i);
                builder.Append(": ");
                builder.Append(slot.Item.ItemId);
                builder.Append(" x");
                builder.Append(slot.Amount);
                builder.Append(" (");
                builder.Append(slot.Item.DisplayName);
                builder.AppendLine(")");
            }
        }

        Debug.Log(builder.ToString(), this);
    }

    private void OnValidate()
    {
        if (inventoryCapacity <= 0)
            inventoryCapacity = DefaultCapacity;

        saveDelay = Mathf.Max(0f, saveDelay);
    }
}
