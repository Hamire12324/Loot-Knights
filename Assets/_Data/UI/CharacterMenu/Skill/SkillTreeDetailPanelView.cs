using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SkillTreeDetailPanelView : BaseMonoBehaviour
{
    [SerializeField] private Image iconChooseSkill;
    [SerializeField] private SkillTreeTextView nameText;
    [SerializeField] private SkillTreeTextView rankText;
    [SerializeField] private SkillTreeTextView descriptionText;
    [SerializeField] private SkillTreeTextView requirementText;
    [SerializeField] private SkillTreeTextView costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button equipButton;

    public event Action UpgradeRequested;
    public event Action EquipRequested;
    protected override void OnEnable() 
    { 
        BindButtons();
    }
    protected override void OnDisable()
    {
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(RaiseUpgrade);
        if (equipButton != null) equipButton.onClick.RemoveListener(RaiseEquip);
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadIconChooseSkill();
        nameText ??= FindText("SkillNameText");
        rankText ??= FindText("RankText"); descriptionText ??= FindText("DescriptionText");
        requirementText ??= FindText("RequirementText"); costText ??= FindText("CostText");
        upgradeButton ??= transform.Find("UpgradeButton")?.GetComponent<Button>();
        equipButton ??= transform.Find("EquipButton")?.GetComponent<Button>();
    }
    protected virtual void LoadIconChooseSkill() 
    { 
        if (iconChooseSkill != null) return; 
        iconChooseSkill = transform.Find("SkillIcon/IconMask/Icon")?.GetComponent<Image>(); 
    }
    public void Render(SkillTreeUiState state)
    {
        SkillTreeNodeDefinition node = state.SelectedNode;
        if (node == null) { Clear(); return; }
        Sprite skillIcon = node.Icon != null ? node.Icon : node.ActiveSkill != null ? node.ActiveSkill.Icon : null;
        if (iconChooseSkill != null) { iconChooseSkill.sprite = skillIcon; iconChooseSkill.enabled = skillIcon != null; }
        Set(nameText, node.DisplayName);
        Set(rankText, $"RANK {state.SelectedRank}/{node.MaxRank}");
        Set(descriptionText, string.IsNullOrWhiteSpace(node.Description) ? node.Kind.ToString() : node.Description);
        Set(requirementText, state.SelectedIsSpecialSkill && state.SelectedIsUnlockedSpecialSkill ? "Element Core ready" : state.SelectedCanUpgrade ? $"Requires level {node.RequiredPlayerLevel}" : state.SelectedUpgradeReason);
        Set(costText, $"Cost: {node.PointCost}");
        if (upgradeButton != null) upgradeButton.interactable = state.SelectedCanUpgrade;
        if (equipButton == null) return;
        equipButton.interactable = state.SelectedIsUnlockedActiveSkill;
        SetButtonLabel(equipButton, state.SelectedIsSpecialSkill ? (state.SelectedIsUnlockedSpecialSkill ? "CORE" : "LOCKED") : !state.SelectedIsUnlockedActiveSkill ? "LOCKED" : state.SelectedIsEquipped ? "UNEQUIP" : state.SelectingEquipSlot ? "CANCEL" : "EQUIP");
    }

    public void Clear()
    {
        if (iconChooseSkill != null) { iconChooseSkill.sprite = null; iconChooseSkill.enabled = false; }
        Set(nameText, string.Empty); Set(rankText, string.Empty); Set(descriptionText, string.Empty); Set(requirementText, string.Empty); Set(costText, string.Empty);
        if (upgradeButton != null) upgradeButton.interactable = false;
        if (equipButton != null) { equipButton.interactable = false; SetButtonLabel(equipButton, "LOCKED"); }
    }
    private SkillTreeTextView FindText(string path) => SkillTreeTextView.GetOrAdd(transform.Find(path));
    private void BindButtons()
    {
        if (upgradeButton != null) { upgradeButton.onClick.RemoveListener(RaiseUpgrade); upgradeButton.onClick.AddListener(RaiseUpgrade); }
        if (equipButton != null) { equipButton.onClick.RemoveListener(RaiseEquip); equipButton.onClick.AddListener(RaiseEquip); }
    }
    private void RaiseUpgrade() => UpgradeRequested?.Invoke();
    private void RaiseEquip() => EquipRequested?.Invoke();
    private static void Set(SkillTreeTextView text, string value) { if (text != null) text.Value = value; }
    private static void SetButtonLabel(Button button, string value)
    {
        SkillTreeTextView label = button != null ? SkillTreeTextView.GetOrAdd(button.GetComponentInChildren<TMP_Text>(true)?.transform) : null;
        if (label != null) label.Value = value;
    }
}
