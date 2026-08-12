using UnityEngine;

/// <summary>Persists inventory space purchased by the player.</summary>
public static class PlayerInventoryCapacityStorage
{
    private const string ExpansionCountKey = "LootKnights.Inventory.ExpansionCount";
    public const int SlotsPerExpansion = 5;

    public static int ExpansionCount => Mathf.Max(0, PlayerPrefs.GetInt(ExpansionCountKey, 0));

    public static int GetCapacity(int baseCapacity)
    {
        return Mathf.Max(1, baseCapacity) + ExpansionCount * SlotsPerExpansion;
    }

    public static bool TryBuyWithCoins(int baseCapacity, out int cost)
    {
        int expansionCount = ExpansionCount;
        cost = EconomyPricing.GetInventoryExpansionCoinCost(expansionCount);
        if (!PlayerCurrencyStorage.TrySpend(CurrencyType.Coins, cost))
            return false;

        SaveExpansionCount(expansionCount + 1);
        return true;
    }

    public static bool TryBuyWithDiamonds(int baseCapacity, out int cost)
    {
        int expansionCount = ExpansionCount;
        cost = EconomyPricing.GetInventoryExpansionDiamondCost(expansionCount);
        if (!PlayerCurrencyStorage.TrySpend(CurrencyType.Diamonds, cost))
            return false;

        SaveExpansionCount(expansionCount + 1);
        return true;
    }

    private static void SaveExpansionCount(int value)
    {
        PlayerPrefs.SetInt(ExpansionCountKey, Mathf.Max(0, value));
        PlayerPrefs.Save();
    }
}
