using System;
using UnityEngine;

[Serializable]
public class EquipmentItemSaveData
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private string itemId;
    [SerializeField] private EquipmentInstanceData equipmentInstance;

    public EquipmentSlotType SlotType => slotType;
    public string ItemId => equipmentInstance != null && equipmentInstance.IsValid ? equipmentInstance.ItemId : itemId;
    public EquipmentInstanceData EquipmentInstance => equipmentInstance;
    public bool HasEquipmentInstance => equipmentInstance != null && equipmentInstance.IsValid;

    public EquipmentItemSaveData(EquipmentSlotType slotType, string itemId)
    {
        this.slotType = slotType;
        this.itemId = itemId;
        equipmentInstance = null;
    }

    public EquipmentItemSaveData(EquipmentSlotType slotType, EquipmentInstanceData equipmentInstance)
    {
        this.slotType = slotType;
        this.equipmentInstance = equipmentInstance?.Clone();
        itemId = this.equipmentInstance != null ? this.equipmentInstance.ItemId : string.Empty;
    }

    public EquipmentItemSaveData Clone()
    {
        return HasEquipmentInstance
            ? new EquipmentItemSaveData(slotType, equipmentInstance)
            : new EquipmentItemSaveData(slotType, itemId);
    }
}
