using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ForgeEnhancePanel : BaseMonoBehaviour
{
    [SerializeField] private ForgeEnhanceInventoryView inventoryView;
    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private Image selectedIcon;
    [SerializeField] private ForgeEnhanceStatRow[] attributeRows;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    private InventorySlotUI selectedSlot;

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadComponents();

        if (inventoryView != null)
            inventoryView.OnSlotSelected += SelectSlot;

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(TryUpgrade);
        Refresh();
    }

    protected override void OnDisable()
    {
        if (inventoryView != null)
            inventoryView.OnSlotSelected -= SelectSlot;

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(TryUpgrade);

        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        Transform viewRoot = transform.parent != null ? transform.parent : transform;

        if (inventoryView == null)
            inventoryView = viewRoot.GetComponentInChildren<ForgeEnhanceInventoryView>(true);

        if (inventoryManager == null)
        {
            inventoryManager = PlayerInventoryManager.InstanceOrNull ??
                               FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        }

        if (selectedIcon == null)
            selectedIcon = transform.Find("Frame/Icon")?.GetComponent<Image>() ??
                           transform.Find("Icon")?.GetComponent<Image>();

        LoadAttributeRows();

        if (upgradeButton == null)
            upgradeButton = transform.Find("Btn_Upgrade")?.GetComponent<Button>();

        if (upgradeButtonText == null && upgradeButton != null)
            upgradeButtonText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void LoadAttributeRows()
    {
        if (attributeRows != null && attributeRows.Length > 0)
            return;

        Transform attributeRoot = transform.Find("Attribute");
        if (attributeRoot == null)
            return;

        List<ForgeEnhanceStatRow> loadedRows = new();
        foreach (Transform child in attributeRoot)
        {
            ForgeEnhanceStatRow row = child.GetComponent<ForgeEnhanceStatRow>();
            if (row == null)
                row = child.gameObject.AddComponent<ForgeEnhanceStatRow>();

            if (row != null)
                loadedRows.Add(row);
        }

        attributeRows = loadedRows.ToArray();
    }

    private void SelectSlot(InventorySlotUI slot)
    {
        selectedSlot = slot != null && slot.HasItem && slot.CurrentItem.Category == ItemCategory.Equipment
            ? slot
            : null;
        Refresh();
    }

    private void Refresh()
    {
        ItemDefinition item = selectedSlot != null ? selectedSlot.CurrentItem : null;
        EquipmentInstanceData instance = selectedSlot != null ? selectedSlot.CurrentEquipmentInstance : null;
        bool canUpgrade = item != null && instance != null && instance.IsValid &&
                          instance.UpgradeLevel < item.MaxUpgradeLevel;
        int cost = canUpgrade ? EquipmentUpgradeService.GetCost(item, instance.UpgradeLevel) : 0;

        if (selectedIcon != null)
        {
            selectedIcon.sprite = item != null ? item.Icon : null;
            selectedIcon.enabled = item != null && item.Icon != null;
        }

        RefreshAttributes(item, instance);

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade && PlayerCurrencyManager.Service.Coins >= cost;

        if (upgradeButtonText != null)
            upgradeButtonText.text = item == null ? "-" :
                !canUpgrade ? "MAX" : $"{cost:N0} Gold";
    }

    private void RefreshAttributes(ItemDefinition item, EquipmentInstanceData instance)
    {
        if (attributeRows == null)
            return;

        int currentLevel = instance?.UpgradeLevel ?? 0;
        List<StatModifier> current = item != null ? item.BuildEquipmentModifiers(currentLevel) : new();
        List<StatModifier> next = item != null
            ? item.BuildEquipmentModifiers(Mathf.Min(currentLevel + 1, item.MaxUpgradeLevel))
            : new();

        foreach (ForgeEnhanceStatRow row in attributeRows)
        {
            if (row == null) continue;

            float now = GetStatAmount(current, row.StatType);
            float after = GetStatAmount(next, row.StatType);
            // The forge preview should list only stats affected by the next upgrade.
            bool show = item != null && !Mathf.Approximately(now, after);
            row.SetValues(now, after, show);
        }
    }

    private static float GetStatAmount(IEnumerable<StatModifier> modifiers, StatType statType)
    {
        float total = 0f;
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier != null && modifier.StatType == statType)
                total += modifier.Amount;
        }

        return total;
    }

    private void TryUpgrade()
    {
        if (inventoryManager == null || selectedSlot == null) return;

        InventoryOperationResult result = inventoryManager.UpgradeEquipmentAtSlot(selectedSlot.CurrentInventoryIndex);
        if (result != null && result.Success)
            Refresh();
    }
}
