using System;
using UnityEngine;

[Serializable]
public class ItemDropEntry
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private float weight = 1f;
    [SerializeField] private int minAmount = 1;
    [SerializeField] private int maxAmount = 1;

    public ItemDefinition Item => item;
    public float Weight => Mathf.Max(0f, weight);
    public int MinAmount => Mathf.Max(1, minAmount);
    public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
    public bool IsValid => item != null && item.IsValid && Weight > 0f;

    public int RollAmount()
    {
        return UnityEngine.Random.Range(MinAmount, MaxAmount + 1);
    }
}
