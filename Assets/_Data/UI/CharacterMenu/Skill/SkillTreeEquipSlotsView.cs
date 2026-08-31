using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeEquipSlotsView : BaseMonoBehaviour
{
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private SkillTreeTextView[] slotLabels;

    public event Action<int> SlotSelected;
    public event Action<int> SlotDropped;

    private void OnValidate() => LoadComponents();
    protected override void OnEnable() => LoadComponents();
    protected override void OnDisable() => UnbindSlots();

    public void Render(SkillTreeUiState state)
    {
        LoadComponents();
        for (int i = 0; i < slotIcons.Length; i++)
        {
            HeroSkillDefinition skill = state != null ? state.GetEquippedSkill(i) : null;
            Image icon = slotIcons[i];

            if (icon != null)
            {
                icon.sprite = skill != null ? skill.Icon : null;
                icon.enabled = skill != null && skill.Icon != null;
                Color color = icon.color;
                color.a = icon.enabled ? 1f : 0f;
                icon.color = color;
            }

            if (slotLabels[i] != null)
            {
                slotLabels[i].Value = (i + 1).ToString();
                slotLabels[i].SetColor(state != null && state.HasPendingEquip
                    ? new Color(1f, .86f, .28f)
                    : Color.white);
            }
        }
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        if (slotIcons == null || slotIcons.Length != transform.childCount)
        {
            slotIcons = new Image[transform.childCount];
            slotLabels = new SkillTreeTextView[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform slot = transform.GetChild(i);
                slotIcons[i] = slot.Find("Icon")?.GetComponent<Image>() ??
                               slot.Find("IconMask/Icon")?.GetComponent<Image>();
                slotLabels[i] = SkillTreeTextView.GetOrAdd(slot.Find("IndexText"));
            }
        }

        BindSlots();
    }

    private void BindSlots()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slot = transform.GetChild(i);
            Graphic graphic = slot.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;

            SkillTreeEquipSlotDropTarget target = slot.GetComponent<SkillTreeEquipSlotDropTarget>() ??
                                                   slot.gameObject.AddComponent<SkillTreeEquipSlotDropTarget>();
            target.Bind(i);
            target.SlotSelected -= RaiseSlotSelected;
            target.SlotSelected += RaiseSlotSelected;
            target.SlotDropped -= RaiseSlotDropped;
            target.SlotDropped += RaiseSlotDropped;
        }
    }

    private void UnbindSlots()
    {
        foreach (SkillTreeEquipSlotDropTarget target in GetComponentsInChildren<SkillTreeEquipSlotDropTarget>(true))
        {
            target.SlotSelected -= RaiseSlotSelected;
            target.SlotDropped -= RaiseSlotDropped;
        }
    }

    private void RaiseSlotSelected(int slotIndex) => SlotSelected?.Invoke(slotIndex);

    private void RaiseSlotDropped(int slotIndex) => SlotDropped?.Invoke(slotIndex);
}
