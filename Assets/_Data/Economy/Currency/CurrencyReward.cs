using UnityEngine;

public class CurrencyReward : PoolObj
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Coins;
    [SerializeField] private int amount = 1;

    public CurrencyType CurrencyType => currencyType;
    public int Amount => Mathf.Max(0, amount);

    public void Grant()
    {
        PlayerCurrencyManager.Service.Add(currencyType, Amount);
    }
}
