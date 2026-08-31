using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillTreeEquipSlotDropTarget : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    private int slotIndex;
    public event System.Action<int> SlotSelected;
    public event System.Action<int> SlotDropped;

    public void Bind(int index)
    {
        slotIndex = Mathf.Max(0, index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        SlotSelected?.Invoke(slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        SkillTreeEquipDragSource source = SkillTreeEquipDragSource.DraggingSource;
        if (source == null)
            return;

        SlotDropped?.Invoke(slotIndex);
    }
}
