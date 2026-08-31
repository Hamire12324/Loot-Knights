using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentInventory
{
    [SerializeField] private List<EquipmentSlotData> slots = new();

    public IReadOnlyList<EquipmentSlotData> Slots => slots;

    public void EnsureDefaultSlots()
    {
        SanitizeSlots();

        foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            if (!IsValidSlotType(slotType)) continue;

            GetOrCreateSlot(slotType);
        }
    }

    public ItemDefinition GetItem(EquipmentSlotType slotType)
    {
        EquipmentSlotData slot = GetOrCreateSlot(slotType);
        return slot != null ? slot.Item : null;
    }

    public ItemDefinition SetItem(
        EquipmentSlotType slotType,
        ItemDefinition item,
        EquipmentInstanceData equipmentInstance)
    {
        EquipmentSlotData slot = GetOrCreateSlot(slotType);
        if (slot == null) return null;

        ItemDefinition previousItem = slot.Item;
        slot.Set(item, equipmentInstance);
        return previousItem;
    }

    public EquipmentInstanceData GetInstance(EquipmentSlotType slotType)
    {
        EquipmentSlotData slot = GetOrCreateSlot(slotType);
        return slot != null ? slot.EquipmentInstance : null;
    }

    public ItemDefinition ClearSlot(EquipmentSlotType slotType)
    {
        EquipmentSlotData slot = GetOrCreateSlot(slotType);
        if (slot == null) return null;

        ItemDefinition previousItem = slot.Item;
        slot.Clear();
        return previousItem;
    }

    public void ClearAll()
    {
        foreach (EquipmentSlotData slot in slots)
            slot?.Clear();
    }

    private EquipmentSlotData GetOrCreateSlot(EquipmentSlotType slotType)
    {
        if (!IsValidSlotType(slotType)) return null;

        foreach (EquipmentSlotData slot in slots)
        {
            if (slot != null && slot.SlotType == slotType)
                return slot;
        }

        EquipmentSlotData createdSlot = new(slotType);
        slots.Add(createdSlot);
        return createdSlot;
    }

    private void SanitizeSlots()
    {
        HashSet<EquipmentSlotType> seenSlotTypes = new();

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            EquipmentSlotData slot = slots[i];

            if (slot == null ||
                !IsValidSlotType(slot.SlotType) ||
                !seenSlotTypes.Add(slot.SlotType))
            {
                slots.RemoveAt(i);
            }
        }
    }

    private bool IsValidSlotType(EquipmentSlotType slotType)
    {
        return slotType != EquipmentSlotType.None;
    }
}
