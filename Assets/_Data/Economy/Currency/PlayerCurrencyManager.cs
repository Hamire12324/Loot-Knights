using System;
using UnityEngine;

/// <summary>
/// Runtime owner for the player's wallet. Values are persisted by PlayerCurrencyStorage.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class PlayerCurrencyManager : BaseSingleton<PlayerCurrencyManager>
{
    public event Action<CurrencyType, int> OnCurrencyChanged;

    public static PlayerCurrencyManager Service
    {
        get
        {
            if (InstanceOrNull != null)
                return InstanceOrNull;

            PlayerCurrencyManager existing = FindAnyObjectByType<PlayerCurrencyManager>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            Debug.LogError("PlayerCurrencyManager is missing. Add one to the scene before using currency.");
            return null;
        }
    }

    public int Coins => Get(CurrencyType.Coins);
    public int Diamonds => Get(CurrencyType.Diamonds);

    public int Get(CurrencyType type)
    {
        return PlayerCurrencyStorage.Get(type);
    }

    public void Set(CurrencyType type, int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        PlayerCurrencyStorage.Set(type, safeAmount);
        OnCurrencyChanged?.Invoke(type, safeAmount);
    }

    public void Add(CurrencyType type, int amount)
    {
        if (amount > 0)
            Set(type, Get(type) + amount);
    }

    public bool CanSpend(CurrencyType type, int amount)
    {
        return amount >= 0 && Get(type) >= amount;
    }

    public bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0)
            return true;

        if (!CanSpend(type, amount))
            return false;

        Set(type, Get(type) - amount);
        return true;
    }

    public void Clear()
    {
        PlayerCurrencyStorage.Delete();
        OnCurrencyChanged?.Invoke(CurrencyType.Coins, Coins);
        OnCurrencyChanged?.Invoke(CurrencyType.Diamonds, Diamonds);
    }
}
