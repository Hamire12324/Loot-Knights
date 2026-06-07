using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySellDropTarget : MonoBehaviour, IDropHandler
{
    public event Action<InventorySlotUI> OnSlotDropped;

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI slot = InventorySlotUI.DraggingSlot;
        if (slot == null || !slot.HasItem) return;

        OnSlotDropped?.Invoke(slot);
    }
}
