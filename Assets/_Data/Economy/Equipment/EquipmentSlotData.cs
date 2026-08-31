using System;
using UnityEngine;

[Serializable]
public class EquipmentSlotData
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private ItemDefinition item;
    [SerializeField] private EquipmentInstanceData equipmentInstance;

    public EquipmentSlotType SlotType => slotType;
    public ItemDefinition Item => item;
    public EquipmentInstanceData EquipmentInstance => equipmentInstance;
    public bool IsEmpty => item == null;

    public EquipmentSlotData(EquipmentSlotType slotType)
    {
        this.slotType = slotType;
    }

    public void Set(ItemDefinition item, EquipmentInstanceData equipmentInstance)
    {
        this.item = item;
        this.equipmentInstance = equipmentInstance?.Clone();
    }

    public void Clear()
    {
        item = null;
        equipmentInstance = null;
    }
}
