using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillTreeEquipSlotDropTarget : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    private SkillTreeView owner;
    private int slotIndex;

    public void Bind(SkillTreeView view, int index)
    {
        owner = view;
        slotIndex = Mathf.Max(0, index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.ClickEquipSlot(slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        SkillTreeEquipDragSource source = SkillTreeEquipDragSource.DraggingSource;
        if (source == null || owner == null)
            return;

        if (owner.CompleteEquipDrag(slotIndex))
            source.MarkDropped();
    }
}
