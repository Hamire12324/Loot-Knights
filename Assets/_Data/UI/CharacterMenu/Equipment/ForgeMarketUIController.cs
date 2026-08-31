using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for a designer-authored Forge & Market UI. It creates no visual
/// objects: assign the fields in the Inspector after building the UI prefab.
/// </summary>
public class ForgeMarketUIController : MonoBehaviour
{
    [Header("Wallet")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text bagCapacityText;
    [SerializeField] private TMP_Text messageText;

    [Header("Equipment upgrade list")]
    [SerializeField] private Transform upgradeCardContent;
    [SerializeField] private ForgeEquipmentUpgradeCard upgradeCardPrefab;

    [Header("Convenience shop")]
    [SerializeField] private Button bagGoldButton;
    [SerializeField] private Button bagDiamondButton;
    [SerializeField] private Button diamondExchangeButton;

    private readonly List<ForgeEquipmentUpgradeCard> upgradeCards = new();
    private PlayerEquipmentManager equipmentManager;
    private PlayerInventoryManager inventoryManager;

    protected virtual void OnEnable()
    {
        LoadManagers();
        BindButtons();
        PlayerCurrencyManager.Service.OnCurrencyChanged += HandleCurrencyChanged;

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged += Refresh;

        Refresh();
    }

    protected virtual void OnDisable()
    {
        PlayerCurrencyManager.Service.OnCurrencyChanged -= HandleCurrencyChanged;

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged -= Refresh;

        UnbindButtons();
    }

    /// <summary>Safe to call after equipment, currency or inventory changes.</summary>
    public void Refresh()
    {
        LoadManagers();
        RefreshWallet();
        RebuildUpgradeCards();
        RefreshShopButtons();
    }

    private void LoadManagers()
    {
        if (equipmentManager == null)
            equipmentManager = PlayerEquipmentManager.InstanceOrNull ??
                               FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);

        if (inventoryManager == null)
            inventoryManager = PlayerInventoryManager.InstanceOrNull ??
                               FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
    }

    private void BindButtons()
    {
        BindButton(bagGoldButton, BuyBagWithGold);
        BindButton(bagDiamondButton, BuyBagWithDiamonds);
        BindButton(diamondExchangeButton, ExchangeDiamondForGold);
    }

    private void UnbindButtons()
    {
        UnbindButton(bagGoldButton, BuyBagWithGold);
        UnbindButton(bagDiamondButton, BuyBagWithDiamonds);
        UnbindButton(diamondExchangeButton, ExchangeDiamondForGold);
    }

    private void RebuildUpgradeCards()
    {
        ClearUpgradeCards();
        if (upgradeCardContent == null || upgradeCardPrefab == null)
            return;

        foreach (EquipmentSlotType slotType in (EquipmentSlotType[])System.Enum.GetValues(typeof(EquipmentSlotType)))
        {
            if (slotType == EquipmentSlotType.None)
                continue;

            ForgeEquipmentUpgradeCard card = Instantiate(upgradeCardPrefab, upgradeCardContent);
            card.gameObject.SetActive(true);
            card.Bind(slotType, equipmentManager, HandleUpgradeResult);
            upgradeCards.Add(card);
        }
    }

    private void ClearUpgradeCards()
    {
        foreach (ForgeEquipmentUpgradeCard card in upgradeCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        upgradeCards.Clear();
    }

    private void HandleUpgradeResult(string resultMessage)
    {
        SetMessage(resultMessage);
        Refresh();
    }

    private void BuyBagWithGold()
    {
        if (!PlayerInventoryCapacityStorage.TryBuyWithCoins(PlayerInventoryManager.DefaultCapacity, out int cost))
        {
            SetMessage($"Need {cost:N0} gold for the next bag expansion.");
            return;
        }

        SetMessage($"Bag expanded by {PlayerInventoryCapacityStorage.SlotsPerExpansion} slots.");
        Refresh();
    }

    private void BuyBagWithDiamonds()
    {
        if (!PlayerInventoryCapacityStorage.TryBuyWithDiamonds(PlayerInventoryManager.DefaultCapacity, out int cost))
        {
            SetMessage($"Need {cost:N0} diamonds for the next bag expansion.");
            return;
        }

        SetMessage($"Bag expanded by {PlayerInventoryCapacityStorage.SlotsPerExpansion} slots.");
        Refresh();
    }

    private void ExchangeDiamondForGold()
    {
        SetMessage(EconomyActions.TryExchangeDiamondForCoins()
            ? $"Exchanged 1 diamond for {EconomyPricing.CoinsPerDiamondExchange:N0} gold."
            : "Need 1 diamond.");
        Refresh();
    }

    private void RefreshWallet()
    {
        SetText(goldText, PlayerCurrencyManager.Service.Coins.ToString("N0"));
        SetText(diamondText, PlayerCurrencyManager.Service.Diamonds.ToString("N0"));

        if (inventoryManager != null)
            SetText(bagCapacityText, inventoryManager.Inventory.Capacity.ToString("N0"));
    }

    private void RefreshShopButtons()
    {
        int expansionCount = PlayerInventoryCapacityStorage.ExpansionCount;
        int goldCost = EconomyPricing.GetInventoryExpansionCoinCost(expansionCount);
        int diamondCost = EconomyPricing.GetInventoryExpansionDiamondCost(expansionCount);

        if (bagGoldButton != null)
        bagGoldButton.interactable = PlayerCurrencyManager.Service.Coins >= goldCost;

        if (bagDiamondButton != null)
        bagDiamondButton.interactable = PlayerCurrencyManager.Service.Diamonds >= diamondCost;

        if (diamondExchangeButton != null)
        diamondExchangeButton.interactable = PlayerCurrencyManager.Service.Diamonds >= 1;
    }

    private void HandleCurrencyChanged(CurrencyType type, int amount) => Refresh();

    private void SetMessage(string value) => SetText(messageText, value);

    private static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null) return;
        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private static void UnbindButton(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button != null)
            button.onClick.RemoveListener(callback);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
