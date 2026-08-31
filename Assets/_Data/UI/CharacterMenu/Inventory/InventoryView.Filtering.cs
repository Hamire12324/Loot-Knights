using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InventoryView
{
    private void ShowAll() => SetFilter(InventoryFilter.All);
    private void ShowEquipment() => SetFilter(InventoryFilter.Equipment);
    private void ShowItems() => SetFilter(InventoryFilter.Items);

    private void SetFilter(InventoryFilter filter)
    {
        if (!SupportsFiltering) return;
        if (CurrentFilter == filter) return;

        CurrentFilter = filter;
        Refresh();
    }

    private void HandleSlotDropped(InventorySlotUI sourceSlot, InventorySlotUI targetSlot)
    {
        if (inventoryManager == null || sourceSlot == null || targetSlot == null) return;

        int sourceIndex = slots.IndexOf(sourceSlot);
        int targetIndex = slots.IndexOf(targetSlot);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        sourceIndex = GetInventoryIndex(sourceIndex);
        targetIndex = GetInventoryIndex(targetIndex);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        ClearSelection();
        inventoryManager.SwapSlots(sourceIndex, targetIndex);
    }

    private void BuildDisplayedInventoryIndexes(InventoryContainer inventory)
    {
        displayedInventoryIndexes.Clear();

        if (inventory == null)
            return;

        int capacity = Mathf.Min(inventory.Capacity, inventoryCapacity);

        if (!SupportsFiltering || CurrentFilter == InventoryFilter.All)
        {
            for (int i = 0; i < capacity; i++)
            {
                InventorySlotData slot = inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty)
                {
                    if (ShowEmptySlots)
                        displayedInventoryIndexes.Add(i);

                    continue;
                }

                if (IsItemVisible(slot.Item))
                    displayedInventoryIndexes.Add(i);
            }

            return;
        }

        for (int i = 0; i < capacity; i++)
        {
            InventorySlotData slot = inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;
            if (!IsItemVisible(slot.Item) || !MatchesFilter(slot.Item)) continue;

            displayedInventoryIndexes.Add(i);
        }

        if (!ShowEmptySlots)
            return;

        for (int i = 0; i < capacity; i++)
        {
            InventorySlotData slot = inventory.GetSlot(i);
            if (slot != null && !slot.IsEmpty) continue;

            displayedInventoryIndexes.Add(i);
        }
    }

    private int GetInventoryIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= displayedInventoryIndexes.Count)
            return -1;

        return displayedInventoryIndexes[displayIndex];
    }

    private bool MatchesFilter(ItemDefinition item)
    {
        if (item == null)
            return false;

        return CurrentFilter switch
        {
            InventoryFilter.Equipment => item.Category == ItemCategory.Equipment,
            InventoryFilter.Items => item.Category == ItemCategory.Item,
            _ => true
        };
    }

    private void ClearSelection()
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = null;
    }

    private void ClearSlots()
    {
        foreach (InventorySlotUI slot in slots)
            slot?.SetEmpty();
    }

    private void RefreshActionButtonStates()
    {
        if (ArrangeButton != null)
            ArrangeButton.interactable =
                SupportsArrange &&
                (!SupportsFiltering || CurrentFilter == InventoryFilter.All) &&
                inventoryManager != null &&
                inventoryManager.Inventory.ToStacks().Count > 1;

        if (SellButton != null)
            SellButton.interactable = SupportsSelling && selectedSlot != null && selectedSlot.HasItem;

        if (AllFilterButton != null)
            AllFilterButton.interactable = SupportsFiltering && CurrentFilter != InventoryFilter.All;

        if (EquipmentFilterButton != null)
            EquipmentFilterButton.interactable = SupportsFiltering && CurrentFilter != InventoryFilter.Equipment;

        if (ItemsFilterButton != null)
            ItemsFilterButton.interactable = SupportsFiltering && CurrentFilter != InventoryFilter.Items;
    }

    private static string GetButtonSearchText(Button button)
    {
        string text = button.name;

        foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            text += " " + label.text;

        foreach (Text label in button.GetComponentsInChildren<Text>(true))
            text += " " + label.text;

        return text.ToLowerInvariant();
    }

    private static bool ContainsToken(string text, string token)
    {
        return !string.IsNullOrWhiteSpace(text) && text.Contains(token);
    }

    private void OnValidate()
    {
        if (inventoryCapacity <= 0)
            inventoryCapacity = PlayerInventoryManager.DefaultCapacity;
    }
}
