using System;
using UnityEngine;

public static class PlayerCurrencyStorage
{
    public const string CoinsKey = "LootKnights.Currency.Coins";
    public const string DiamondsKey = "LootKnights.Currency.Diamonds";

    private const int DefaultCoins = 0;
    private const int DefaultDiamonds = 0;

    public static event Action<CurrencyType, int> OnCurrencyChanged;

    public static int Coins => Get(CurrencyType.Coins);
    public static int Diamonds => Get(CurrencyType.Diamonds);

    public static int Get(CurrencyType type)
    {
        return PlayerPrefs.GetInt(GetKey(type), GetDefaultValue(type));
    }

    public static void Set(CurrencyType type, int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        PlayerPrefs.SetInt(GetKey(type), safeAmount);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke(type, safeAmount);
    }

    public static void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        Set(type, Get(type) + amount);
    }

    public static bool CanSpend(CurrencyType type, int amount)
    {
        if (amount < 0) return false;

        return Get(type) >= amount;
    }

    public static bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0) return true;
        if (!CanSpend(type, amount)) return false;

        Set(type, Get(type) - amount);
        return true;
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(DiamondsKey);
        PlayerPrefs.Save();

        OnCurrencyChanged?.Invoke(CurrencyType.Coins, Get(CurrencyType.Coins));
        OnCurrencyChanged?.Invoke(CurrencyType.Diamonds, Get(CurrencyType.Diamonds));
    }

    private static string GetKey(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Diamonds => DiamondsKey,
            _ => CoinsKey
        };
    }

    private static int GetDefaultValue(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Diamonds => DefaultDiamonds,
            _ => DefaultCoins
        };
    }
}
