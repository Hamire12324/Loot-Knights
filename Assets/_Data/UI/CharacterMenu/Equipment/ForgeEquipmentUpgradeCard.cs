using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>View controller for one designer-authored equipment upgrade card.</summary>
public class ForgeEquipmentUpgradeCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text equipmentNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;

    private EquipmentSlotType slotType;
    private PlayerEquipmentManager equipmentManager;
    private UnityAction<string> onUpgradeResult;

    private void OnDisable()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(TryUpgrade);
    }

    public void Bind(
        EquipmentSlotType targetSlotType,
        PlayerEquipmentManager targetEquipmentManager,
        UnityAction<string> onResult)
    {
        slotType = targetSlotType;
        equipmentManager = targetEquipmentManager;
        onUpgradeResult = onResult;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(TryUpgrade);
            upgradeButton.onClick.AddListener(TryUpgrade);
        }

        Refresh();
    }

    private void TryUpgrade()
    {
        ItemDefinition item = equipmentManager != null ? equipmentManager.GetItem(slotType) : null;
        if (item == null)
        {
            onUpgradeResult?.Invoke($"Equip a {slotType} first.");
            return;
        }

        int cost = equipmentManager.GetUpgradeCost(slotType);
        if (!equipmentManager.UpgradeEquippedItem(slotType))
        {
        onUpgradeResult?.Invoke(PlayerCurrencyManager.Service.Coins < cost
                ? $"Need {cost:N0} gold."
                : "This item is already at maximum level.");
            return;
        }

        onUpgradeResult?.Invoke($"{item.DisplayName} upgraded for {cost:N0} gold.");
    }

    private void Refresh()
    {
        ItemDefinition item = equipmentManager != null ? equipmentManager.GetItem(slotType) : null;
        bool equipped = item != null;
        int level = equipped ? equipmentManager.GetUpgradeLevel(slotType) : 0;
        int cost = equipped ? equipmentManager.GetUpgradeCost(slotType) : 0;
        bool atMax = equipped && level >= item.MaxUpgradeLevel;

        if (iconImage != null)
        {
            iconImage.sprite = equipped ? item.Icon : null;
            iconImage.enabled = equipped && item.Icon != null;
        }

        SetText(equipmentNameText, equipped ? item.DisplayName : slotType + " - Empty");
        SetText(levelText, equipped ? $"Lv. {level}/{item.MaxUpgradeLevel}" : "Equip an item first");
        SetText(costText, atMax ? "MAX" : equipped ? $"{cost:N0} Gold" : "-");

        if (upgradeButton != null)
        upgradeButton.interactable = equipped && !atMax && PlayerCurrencyManager.Service.Coins >= cost;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
