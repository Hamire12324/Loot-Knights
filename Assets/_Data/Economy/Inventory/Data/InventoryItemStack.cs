using System;
using UnityEngine;

[Serializable]
public class InventoryItemStack
{
    [SerializeField] private string itemId;
    [SerializeField] private int amount;
    [SerializeField] private EquipmentInstanceData equipmentInstance;

    public string ItemId => equipmentInstance != null && equipmentInstance.IsValid ? equipmentInstance.ItemId : itemId;
    public int Amount => equipmentInstance != null && equipmentInstance.IsValid ? 1 : amount;
    public EquipmentInstanceData EquipmentInstance => equipmentInstance;
    public bool HasEquipmentInstance => equipmentInstance != null && equipmentInstance.IsValid;

    public InventoryItemStack(string itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = Mathf.Max(0, amount);
        equipmentInstance = null;
    }

    public InventoryItemStack(EquipmentInstanceData equipmentInstance)
    {
        this.equipmentInstance = equipmentInstance?.Clone();
        itemId = this.equipmentInstance != null ? this.equipmentInstance.ItemId : string.Empty;
        amount = this.equipmentInstance != null ? 1 : 0;
    }

    public void Add(int value)
    {
        if (value <= 0) return;

        amount += value;
    }

    public bool TryRemove(int value)
    {
        if (value <= 0) return true;
        if (amount < value) return false;

        amount -= value;
        return true;
    }

    public InventoryItemStack Clone()
    {
        return HasEquipmentInstance
            ? new InventoryItemStack(equipmentInstance)
            : new InventoryItemStack(itemId, amount);
    }
}
