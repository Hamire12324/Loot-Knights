using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A self-contained forge added to the currently empty Strengthen view. It keeps
/// the existing prefab untouched while making gold and diamonds spendable now.
/// </summary>
public class EconomyForgePanel : MonoBehaviour
{
    private PlayerEquipmentManager equipmentManager;
    private PlayerInventoryManager inventoryManager;
    private Text statusText;

    private void Awake()
    {
        BuildIfNeeded();
    }

    private void OnEnable()
    {
        PlayerCurrencyStorage.OnCurrencyChanged += HandleCurrencyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerCurrencyStorage.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    public void Refresh()
    {
        BuildIfNeeded();

        if (inventoryManager != null)
            inventoryManager.SetCapacity(PlayerInventoryCapacityStorage.GetCapacity(PlayerInventoryManager.DefaultCapacity));

        if (statusText == null) return;

        int capacity = inventoryManager != null ? inventoryManager.Inventory.Capacity : PlayerInventoryManager.DefaultCapacity;
        statusText.text = $"GOLD {PlayerCurrencyStorage.Coins:N0}    DIAMONDS {PlayerCurrencyStorage.Diamonds:N0}\n" +
                          $"Bag: {capacity} slots  |  Sell unwanted loot in Backpack for gold.";
    }

    private void BuildIfNeeded()
    {
        if (statusText != null) return;

        equipmentManager = PlayerEquipmentManager.InstanceOrNull ?? FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);
        inventoryManager = PlayerInventoryManager.InstanceOrNull ?? FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);

        VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
        }

        statusText = CreateText("EconomyStatus", 24, TextAnchor.MiddleCenter);
        CreateText("ForgeTitle", 32, TextAnchor.MiddleCenter).text = "FORGE & MARKET";
        CreateText("ForgeHint", 18, TextAnchor.MiddleCenter).text = "Upgrade equipment with gold. Diamonds buy convenience.";

        foreach (EquipmentSlotType slotType in (EquipmentSlotType[])Enum.GetValues(typeof(EquipmentSlotType)))
        {
            if (slotType == EquipmentSlotType.None) continue;
            CreateButton($"Upgrade {slotType}", () => Upgrade(slotType));
        }

        CreateButton("Expand bag (+5) — Gold", BuyBagWithCoins);
        CreateButton("Expand bag (+5) — Diamonds", BuyBagWithDiamonds);
        CreateButton($"Exchange 1 Diamond → {EconomyPricing.CoinsPerDiamondExchange} Gold", ExchangeDiamond);
    }

    private void Upgrade(EquipmentSlotType slotType)
    {
        if (equipmentManager == null)
        {
            SetStatus("No equipment manager found.");
            return;
        }

        ItemDefinition item = equipmentManager.GetItem(slotType);
        if (item == null)
        {
            SetStatus($"Equip a {slotType} first.");
            return;
        }

        int cost = equipmentManager.GetUpgradeCost(slotType);
        if (!equipmentManager.UpgradeEquippedItem(slotType))
        {
            SetStatus(PlayerCurrencyStorage.Coins < cost ? $"Need {cost:N0} gold." : "Item is already at maximum level.");
            return;
        }

        SetStatus($"{item.DisplayName} upgraded for {cost:N0} gold.");
    }

    private void BuyBagWithCoins()
    {
        if (!PlayerInventoryCapacityStorage.TryBuyWithCoins(PlayerInventoryManager.DefaultCapacity, out int cost))
        {
            SetStatus($"Need {cost:N0} gold for the next bag expansion.");
            return;
        }

        Refresh();
        SetStatus($"Bag expanded by {PlayerInventoryCapacityStorage.SlotsPerExpansion} slots for {cost:N0} gold.");
    }

    private void BuyBagWithDiamonds()
    {
        if (!PlayerInventoryCapacityStorage.TryBuyWithDiamonds(PlayerInventoryManager.DefaultCapacity, out int cost))
        {
            SetStatus($"Need {cost:N0} diamonds for the next bag expansion.");
            return;
        }

        Refresh();
        SetStatus($"Bag expanded by {PlayerInventoryCapacityStorage.SlotsPerExpansion} slots for {cost:N0} diamonds.");
    }

    private void ExchangeDiamond()
    {
        SetStatus(EconomyActions.TryExchangeDiamondForCoins()
            ? $"Exchanged 1 diamond for {EconomyPricing.CoinsPerDiamondExchange:N0} gold."
            : "Need 1 diamond.");
    }

    private Text CreateText(string objectName, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textObject.transform.SetParent(transform, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        textObject.GetComponent<LayoutElement>().minHeight = fontSize + 12;
        return text;
    }

    private void CreateButton(string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(transform, false);
        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.34f, 0.5f, 0.96f);
        buttonObject.GetComponent<LayoutElement>().minHeight = 52f;
        buttonObject.GetComponent<Button>().onClick.AddListener(action);

        Text text = CreateText("Label", 20, TextAnchor.MiddleCenter);
        text.transform.SetParent(buttonObject.transform, false);
        text.text = label;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void HandleCurrencyChanged(CurrencyType type, int amount) => Refresh();

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message + "\nGOLD " + PlayerCurrencyStorage.Coins.ToString("N0") +
                              "    DIAMONDS " + PlayerCurrencyStorage.Diamonds.ToString("N0");
    }
}
