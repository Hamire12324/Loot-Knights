using UnityEngine;

/// <summary>Convenience actions that can be called from UI buttons or UnityEvents.</summary>
public static class EconomyActions
{
    public static bool TryExchangeDiamondForCoins(int diamondAmount = 1)
    {
        int safeAmount = Mathf.Max(1, diamondAmount);
        if (!PlayerCurrencyStorage.TrySpend(CurrencyType.Diamonds, safeAmount))
            return false;

        PlayerCurrencyStorage.Add(CurrencyType.Coins, safeAmount * EconomyPricing.CoinsPerDiamondExchange);
        return true;
    }
}
