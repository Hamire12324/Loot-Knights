using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : BaseMonoBehaviour
{
    private enum InventoryFilter
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
    [SerializeField] private Button arrangeButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private InventorySellDropTarget sellDropTarget;
    [SerializeField] private Button allFilterButton;
    [SerializeField] private Button equipmentFilterButton;
    [SerializeField] private Button itemsFilterButton;
    [SerializeField] private InventoryFilter currentFilter;

    private readonly List<InventorySlotUI> slots = new();
    private readonly List<int> displayedInventoryIndexes = new();
    private InventorySlotUI selectedSlot;

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
        LoadActionButtons();
        LoadSellDropTarget();
        LoadFilterButtons();
        LoadSlots();
        BindActionButtons();
        BindSellDropTarget();
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
        if (selectedSlot != null && selectedSlot != slot)
        {
            SwapSelectedWith(slot);
            return;
        }

        if (selectedSlot == slot)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            RefreshActionButtonStates();
            return;
        }

        selectedSlot = slot != null && slot.HasItem ? slot : null;

        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        RefreshActionButtonStates();
    }

    private void Arrange()
    {
        if (inventoryManager == null) return;

        inventoryManager.ArrangeByRarityAndName();
    }

    private void SellSelected()
    {
        SellSlot(selectedSlot);
    }

    private void SellSlot(InventorySlotUI slot)
    {
        if (slot == null || !slot.HasItem) return;

        int displayIndex = slots.IndexOf(slot);
        int inventoryIndex = GetInventoryIndex(displayIndex);
        if (inventoryIndex >= 0 && inventoryManager != null)
        {
            InventorySlotData inventorySlot = inventoryManager.Inventory.GetSlot(inventoryIndex);
            ItemDefinition item = inventorySlot != null ? inventorySlot.Item : null;
            int amount = inventorySlot != null ? inventorySlot.Amount : 0;

            InventoryOperationResult result = inventoryManager.RemoveSlot(inventoryIndex);
            if (result != null && result.Success)
                PlayerCurrencyStorage.Add(CurrencyType.Coins, EconomyPricing.GetSellValue(item, amount));
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

    private void LoadContentRoot()
    {
        if (contentRoot != null) return;

        Transform content = transform.Find(ContentName);
        if (content != null)
        {
            contentRoot = content as RectTransform;
            return;
        }

        foreach (RectTransform rectTransform in GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform == null || rectTransform.name != ContentName) continue;

            contentRoot = rectTransform;
            return;
        }
    }

    private void LoadInventoryManager()
    {
        if (inventoryManager != null) return;

        if (PlayerInventoryManager.InstanceOrNull != null)
        {
            inventoryManager = PlayerInventoryManager.InstanceOrNull;
            return;
        }

        inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
    }

    private void LoadActionButtons()
    {
        if (arrangeButton != null && sellButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonName = GetButtonSearchText(button);

            if (arrangeButton == null && buttonName.Contains("arrange"))
            {
                arrangeButton = button;
                continue;
            }

            if (sellButton == null && buttonName.Contains("sell"))
                sellButton = button;
        }
    }

    private void LoadSellDropTarget()
    {
        if (sellDropTarget != null) return;
        if (sellButton == null) return;

        sellDropTarget = sellButton.GetComponent<InventorySellDropTarget>();
    }

    private void LoadFilterButtons()
    {
        if (allFilterButton != null && equipmentFilterButton != null && itemsFilterButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonText = GetButtonSearchText(button);

            if (allFilterButton == null && ContainsToken(buttonText, "all"))
            {
                allFilterButton = button;
                continue;
            }

            if (equipmentFilterButton == null && ContainsToken(buttonText, "equipment"))
            {
                equipmentFilterButton = button;
                continue;
            }

            if (itemsFilterButton == null && ContainsToken(buttonText, "items"))
                itemsFilterButton = button;
        }
    }

    private void LoadSlots()
    {
        foreach (InventorySlotUI slot in slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= HandleSlotClicked;
            slot.OnDropped -= HandleSlotDropped;
        }

        slots.Clear();
        HideLegacyEmptySlots();

        Dictionary<int, InventorySlotUI> indexedSlots = new();
        Transform searchRoot = contentRoot != null ? contentRoot : transform;

        foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == searchRoot) continue;

            string childName = child.name.ToLowerInvariant();
            if (!childName.StartsWith(SlotName)) continue;
            if (childName.StartsWith("btn_arrange") || childName.StartsWith("btn_sell")) continue;

            int slotIndex = ParseSlotIndex(childName);
            if (indexedSlots.ContainsKey(slotIndex)) continue;

            InventorySlotUI slot = child.GetComponent<InventorySlotUI>();
            if (slot == null)
                slot = child.gameObject.AddComponent<InventorySlotUI>();

            Button button = child.GetComponent<Button>();
            if (button == null)
                button = child.GetComponentInChildren<Button>(true);

            slot.Configure(
                button,
                null,
                null,
                null,
                null,
                null);

            slot.OnClicked -= HandleSlotClicked;
            slot.OnClicked += HandleSlotClicked;
            slot.OnDropped -= HandleSlotDropped;
            slot.OnDropped += HandleSlotDropped;
            indexedSlots.Add(slotIndex, slot);
        }

        List<int> indexes = new(indexedSlots.Keys);
        indexes.Sort();

        foreach (int index in indexes)
            slots.Add(indexedSlots[index]);
    }

    private void HideLegacyEmptySlots()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == transform) continue;
            if (!child.name.ToLowerInvariant().StartsWith(LegacyEmptySlotName)) continue;

            child.gameObject.SetActive(false);
        }
    }

    private int ParseSlotIndex(string objectName)
    {
        int open = objectName.LastIndexOf('(');
        int close = objectName.LastIndexOf(')');

        if (open < 0 || close <= open)
            return 0;

        string indexText = objectName.Substring(open + 1, close - open - 1);
        return int.TryParse(indexText, out int index) ? Mathf.Max(0, index) : 0;
    }

    private void BindActionButtons()
    {
        if (arrangeButton != null)
        {
            arrangeButton.onClick.RemoveListener(Arrange);
            arrangeButton.onClick.AddListener(Arrange);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(SellSelected);
            sellButton.onClick.AddListener(SellSelected);
        }
    }

    private void BindSellDropTarget()
    {
        if (sellDropTarget == null)
            return;

        sellDropTarget.OnSlotDropped -= SellSlot;
        sellDropTarget.OnSlotDropped += SellSlot;
    }

    private void BindFilterButtons()
    {
        if (allFilterButton != null)
        {
            allFilterButton.onClick.RemoveListener(ShowAll);
            allFilterButton.onClick.AddListener(ShowAll);
        }

        if (equipmentFilterButton != null)
        {
            equipmentFilterButton.onClick.RemoveListener(ShowEquipment);
            equipmentFilterButton.onClick.AddListener(ShowEquipment);
        }

        if (itemsFilterButton != null)
        {
            itemsFilterButton.onClick.RemoveListener(ShowItems);
            itemsFilterButton.onClick.AddListener(ShowItems);
        }
    }

    private void ShowAll() => SetFilter(InventoryFilter.All);
    private void ShowEquipment() => SetFilter(InventoryFilter.Equipment);
    private void ShowItems() => SetFilter(InventoryFilter.Items);

    private void SetFilter(InventoryFilter filter)
    {
        if (currentFilter == filter) return;

        currentFilter = filter;
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

        if (currentFilter == InventoryFilter.All)
        {
            for (int i = 0; i < capacity; i++)
                displayedInventoryIndexes.Add(i);

            return;
        }

        for (int i = 0; i < capacity; i++)
        {
            InventorySlotData slot = inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;
            if (!MatchesFilter(slot.Item)) continue;

            displayedInventoryIndexes.Add(i);
        }

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

        return currentFilter switch
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
        if (arrangeButton != null)
            arrangeButton.interactable =
                currentFilter == InventoryFilter.All &&
                inventoryManager != null &&
                inventoryManager.Inventory.ToStacks().Count > 1;

        if (sellButton != null)
            sellButton.interactable = selectedSlot != null && selectedSlot.HasItem;

        if (allFilterButton != null)
            allFilterButton.interactable = currentFilter != InventoryFilter.All;

        if (equipmentFilterButton != null)
            equipmentFilterButton.interactable = currentFilter != InventoryFilter.Equipment;

        if (itemsFilterButton != null)
            itemsFilterButton.interactable = currentFilter != InventoryFilter.Items;
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
