using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount;
    [SerializeField] private EquipmentInstanceData equipmentInstance;

    public ItemDefinition Item => item;
    public int Amount => amount;
    public EquipmentInstanceData EquipmentInstance => equipmentInstance;
    public bool IsEmpty => item == null || amount <= 0;
    public bool HasEquipmentInstance => equipmentInstance != null && equipmentInstance.IsValid;
    public bool IsStackable => item != null && item.MaxStack > 1 && !HasEquipmentInstance;
    public int MaxStack => item != null ? item.MaxStack : 0;

    public InventorySlotData()
    {
        Clear();
    }

    public InventorySlotData(ItemDefinition item, int amount)
    {
        Set(item, amount);
    }

    public InventorySlotData(ItemDefinition item, int amount, EquipmentInstanceData equipmentInstance)
    {
        Set(item, amount, equipmentInstance);
    }

    public void Set(ItemDefinition item, int amount)
    {
        EquipmentInstanceData instance = item != null && item.Category == ItemCategory.Equipment
            ? item.CreateEquipmentInstance()
            : null;

        Set(item, amount, instance);
    }

    public void Set(ItemDefinition item, int amount, EquipmentInstanceData equipmentInstance)
    {
        this.item = item;
        this.equipmentInstance = equipmentInstance?.Clone();
        this.amount = item != null ? Mathf.Max(1, amount) : 0;

        if (this.equipmentInstance != null)
            this.amount = 1;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        equipmentInstance = null;
    }

    public bool CanStack(ItemDefinition target)
    {
        return !IsEmpty &&
               target != null &&
               item == target &&
               IsStackable &&
               amount < MaxStack;
    }

    public int GetAvailableSpace(ItemDefinition target)
    {
        if (!CanStack(target)) return 0;

        return Mathf.Max(0, MaxStack - amount);
    }

    public int AddQuantity(int value)
    {
        if (IsEmpty || !IsStackable || value <= 0) return 0;

        int added = Mathf.Min(value, MaxStack - amount);
        amount += added;
        return added;
    }

    public bool TryRemoveQuantity(int value)
    {
        if (value <= 0) return true;
        if (amount < value) return false;

        amount -= value;
        if (amount <= 0)
            Clear();

        return true;
    }

    public InventoryItemStack ToStack()
    {
        if (IsEmpty) return null;

        return HasEquipmentInstance
            ? new InventoryItemStack(equipmentInstance)
            : new InventoryItemStack(item.ItemId, amount);
    }

    public InventorySlotData Clone()
    {
        return IsEmpty ? new InventorySlotData() : new InventorySlotData(item, amount, equipmentInstance);
    }
}
