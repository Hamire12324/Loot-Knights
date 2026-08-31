using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InventoryView
{
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
        if (ArrangeButton != null && SellButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonName = GetButtonSearchText(button);

            if (ArrangeButton == null && buttonName.Contains("arrange"))
            {
                ArrangeButton = button;
                continue;
            }

            if (SellButton == null && buttonName.Contains("sell"))
                SellButton = button;
        }
    }

    private void LoadSellDropTarget()
    {
        if (SellDropTarget != null) return;
        if (SellButton == null) return;

        SellDropTarget = SellButton.GetComponent<InventorySellDropTarget>();
    }

    private void LoadFilterButtons()
    {
        if (AllFilterButton != null && EquipmentFilterButton != null && ItemsFilterButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonText = GetButtonSearchText(button);

            if (AllFilterButton == null && ContainsToken(buttonText, "all"))
            {
                AllFilterButton = button;
                continue;
            }

            if (EquipmentFilterButton == null && ContainsToken(buttonText, "equipment"))
            {
                EquipmentFilterButton = button;
                continue;
            }

            if (ItemsFilterButton == null && ContainsToken(buttonText, "items"))
                ItemsFilterButton = button;
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

            slot.Configure(button);
            slot.SetDragEnabled(SupportsSlotReordering);

            slot.OnClicked -= HandleSlotClicked;
            slot.OnClicked += HandleSlotClicked;
            slot.OnDropped -= HandleSlotDropped;
            if (SupportsSlotReordering)
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
        if (ArrangeButton != null)
        {
            ArrangeButton.onClick.RemoveListener(Arrange);
            ArrangeButton.onClick.AddListener(Arrange);
        }

        if (SellButton != null)
        {
            SellButton.onClick.RemoveListener(SellSelected);
            SellButton.onClick.AddListener(SellSelected);
        }
    }

    private void BindSellDropTarget()
    {
        if (SellDropTarget == null)
            return;

        SellDropTarget.OnSlotDropped -= SellSlot;
        SellDropTarget.OnSlotDropped += SellSlot;
    }

    private void BindFilterButtons()
    {
        if (AllFilterButton != null)
        {
            AllFilterButton.onClick.RemoveListener(ShowAll);
            AllFilterButton.onClick.AddListener(ShowAll);
        }

        if (EquipmentFilterButton != null)
        {
            EquipmentFilterButton.onClick.RemoveListener(ShowEquipment);
            EquipmentFilterButton.onClick.AddListener(ShowEquipment);
        }

        if (ItemsFilterButton != null)
        {
            ItemsFilterButton.onClick.RemoveListener(ShowItems);
            ItemsFilterButton.onClick.AddListener(ShowItems);
        }
    }
}
