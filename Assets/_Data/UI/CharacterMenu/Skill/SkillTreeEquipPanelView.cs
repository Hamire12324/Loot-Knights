using System;
using UnityEngine;
public class SkillTreeEquipPanelView : BaseMonoBehaviour
{
    [SerializeField] private SkillTreeTextView titleText;
    [SerializeField] private SkillTreeEquipSlotsView slotsView;
    [SerializeField] private SkillTreeElementCoreSlotView elementCoreSlotView;

    public event Action<int> SlotSelected;
    public event Action<int> SlotDropped;
    protected override void OnEnable()
    {
        SubscribeSlots();
    }

    protected override void OnDisable() => UnsubscribeSlots();

    public void Render(SkillTreeUiState state, bool elementMode)
    {
        LoadComponents();
        if (titleText != null)
            titleText.Value = elementMode ? "ELEMENT CORE" : "EQUIP SKILLS";

        if (slotsView != null)
            slotsView.gameObject.SetActive(!elementMode);
        if (elementCoreSlotView != null)
            elementCoreSlotView.gameObject.SetActive(elementMode);

        if (elementMode)
            RenderElementCore(state);
        else
            slotsView?.Render(state);
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        titleText ??= SkillTreeTextView.GetOrAdd(transform.Find("TitleText"));
        this.LoadSlotsView();
        this.LoadElementCoreSlotView();
    }
    protected virtual void LoadSlotsView()
    {
        if (this.slotsView != null) return;
        this.slotsView = GetComponentInChildren<SkillTreeEquipSlotsView>(true);
    }

    private void LoadElementCoreSlotView()
    {
        if (this.elementCoreSlotView != null) return;
        this.elementCoreSlotView = GetComponentInChildren<SkillTreeElementCoreSlotView>(true);
    }
    private void SubscribeSlots()
    {
        if (slotsView == null)
            return;

        slotsView.SlotSelected -= RaiseSlotSelected;
        slotsView.SlotSelected += RaiseSlotSelected;
        slotsView.SlotDropped -= RaiseSlotDropped;
        slotsView.SlotDropped += RaiseSlotDropped;
    }

    private void UnsubscribeSlots()
    {
        if (slotsView == null)
            return;

        slotsView.SlotSelected -= RaiseSlotSelected;
        slotsView.SlotDropped -= RaiseSlotDropped;
    }

    private void RenderElementCore(SkillTreeUiState state)
    {
        elementCoreSlotView?.Render(state != null ? state.SpecialSkill : null);
    }

    private void RaiseSlotSelected(int slotIndex) => SlotSelected?.Invoke(slotIndex);
    private void RaiseSlotDropped(int slotIndex) => SlotDropped?.Invoke(slotIndex);
}
