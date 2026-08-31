using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class InventoryView : BaseMonoBehaviour
{
    public event Action<InventorySlotUI> OnSlotSelected;
    protected enum InventoryFilter
    {
        All,
        Equipment,
        Items
    }

    private const string ContentName = "Content";
    private const string SlotName = "btn_prop";
    private const string LegacyEmptySlotName = "btn_addprop";

    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private int inventoryCapacity = PlayerInventoryManager.DefaultCapacity;
    [SerializeField] private RectTransform contentRoot;
    private readonly List<InventorySlotUI> slots = new();
    private readonly List<int> displayedInventoryIndexes = new();
    private InventorySlotUI selectedSlot;

    protected virtual bool SupportsArrange => false;
    protected virtual bool SupportsSelling => false;
    protected virtual bool SupportsFiltering => false;
    protected virtual bool SupportsSlotReordering => false;
    protected virtual bool ShowEmptySlots => true;

    protected virtual bool IsItemVisible(ItemDefinition item) => true;

    protected virtual Button ArrangeButton { get => null; set { } }
    protected virtual Button SellButton { get => null; set { } }
    protected virtual InventorySellDropTarget SellDropTarget { get => null; set { } }
    protected virtual Button AllFilterButton { get => null; set { } }
    protected virtual Button EquipmentFilterButton { get => null; set { } }
    protected virtual Button ItemsFilterButton { get => null; set { } }
    protected virtual InventoryFilter CurrentFilter { get => InventoryFilter.All; set { } }

    protected override void OnEnable()
    {
        base.OnEnable();

        LoadComponents();

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= HandleInventoryChanged;
            inventoryManager.OnInventoryChanged += HandleInventoryChanged;
        }

        Refresh();
    }

    protected override void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= HandleInventoryChanged;

        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (itemDatabase == null)
            itemDatabase = ItemDatabase.LoadDefault();

        if (inventoryCapacity <= 0)
            inventoryCapacity = PlayerInventoryManager.DefaultCapacity;

        LoadInventoryManager();
        LoadContentRoot();
        if (SupportsArrange || SupportsSelling)
            LoadActionButtons();

        if (SupportsSelling)
            LoadSellDropTarget();

        if (SupportsFiltering)
            LoadFilterButtons();
        LoadSlots();

        if (SupportsArrange || SupportsSelling)
            BindActionButtons();

        if (SupportsSelling)
            BindSellDropTarget();

        if (SupportsFiltering)
            BindFilterButtons();
    }

    public void Show()
    {
        SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        SetActive(false);
    }

    public void Refresh()
    {
        if (itemDatabase == null)
            itemDatabase = ItemDatabase.LoadDefault();

        LoadComponents();
        ClearSelection();

        if (inventoryManager == null)
        {
            ClearSlots();
            RefreshActionButtonStates();
            return;
        }

        inventoryManager.SetCapacity(PlayerInventoryCapacityStorage.GetCapacity(inventoryCapacity));
        InventoryContainer inventory = inventoryManager.Inventory;
        BuildDisplayedInventoryIndexes(inventory);

        for (int i = 0; i < slots.Count; i++)
            RenderSlot(i, inventory);

        RefreshActionButtonStates();
    }

    private void HandleInventoryChanged(InventoryOperationResult result)
    {
        if (!isActiveAndEnabled) return;

        Refresh();
    }

    private void RenderSlot(int displayIndex, InventoryContainer inventory)
    {
        if (displayIndex < 0 || displayIndex >= slots.Count) return;

        InventorySlotUI slot = slots[displayIndex];
        if (slot == null) return;

        int inventoryIndex = GetInventoryIndex(displayIndex);
        InventorySlotData stack = inventory != null && inventoryIndex >= 0 ? inventory.GetSlot(inventoryIndex) : null;
        if (stack == null || stack.IsEmpty)
        {
            slot.SetEmpty();
            return;
        }

        slot.SetInventoryIndex(inventoryIndex);
        slot.SetItem(stack.Item, stack.Amount, stack.EquipmentInstance);
    }

    private void HandleSlotClicked(InventorySlotUI slot)
    {
        if (SupportsSlotReordering && selectedSlot != null && selectedSlot != slot)
        {
            SwapSelectedWith(slot);
            return;
        }

        if (selectedSlot == slot)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            RefreshActionButtonStates();
            OnSlotSelected?.Invoke(null);
            return;
        }

        selectedSlot = slot != null && slot.HasItem ? slot : null;

        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        RefreshActionButtonStates();
        OnSlotSelected?.Invoke(selectedSlot);
    }

    private void Arrange()
    {
        if (!SupportsArrange || inventoryManager == null) return;

        inventoryManager.ArrangeByRarityAndName();
    }

    private void SellSelected()
    {
        SellSlot(selectedSlot);
    }

    private void SellSlot(InventorySlotUI slot)
    {
        if (!SupportsSelling || slot == null || !slot.HasItem) return;

        int displayIndex = slots.IndexOf(slot);
        int inventoryIndex = GetInventoryIndex(displayIndex);
        if (inventoryIndex >= 0 && inventoryManager != null)
        {
            InventorySlotData inventorySlot = inventoryManager.Inventory.GetSlot(inventoryIndex);
            ItemDefinition item = inventorySlot != null ? inventorySlot.Item : null;
            int amount = inventorySlot != null ? inventorySlot.Amount : 0;

            InventoryOperationResult result = inventoryManager.RemoveSlot(inventoryIndex);
            if (result != null && result.Success)
        PlayerCurrencyManager.Service.Add(CurrencyType.Coins, EconomyPricing.GetSellValue(item, amount));
        }

        selectedSlot = null;
        RefreshActionButtonStates();
    }

    private void SwapSelectedWith(InventorySlotUI targetSlot)
    {
        if (inventoryManager == null || selectedSlot == null || targetSlot == null) return;

        int selectedDisplayIndex = slots.IndexOf(selectedSlot);
        int targetDisplayIndex = slots.IndexOf(targetSlot);

        int selectedInventoryIndex = GetInventoryIndex(selectedDisplayIndex);
        int targetInventoryIndex = GetInventoryIndex(targetDisplayIndex);

        ClearSelection();

        if (selectedInventoryIndex < 0 || targetInventoryIndex < 0)
        {
            RefreshActionButtonStates();
            return;
        }

        if (selectedInventoryIndex == targetInventoryIndex)
        {
            RefreshActionButtonStates();
            return;
        }

        inventoryManager.SwapSlots(selectedInventoryIndex, targetInventoryIndex);
    }

}
