using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeView : MonoBehaviour
{
    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailRankText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailRequirementText;
    [SerializeField] private TMP_Text detailCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Image[] equipSlotIcons;
    [SerializeField] private TMP_Text[] equipSlotLabels;

    private SkillTreeRuntime runtime;
    private SkillTreeNodeView selectedNodeView;
    private SkillTreeNodeDefinition pendingEquipNode;

    public SkillTreeDefinition SkillTree => skillTree;

    private void Awake()
    {
        LoadComponents();
        RefreshRuntime();
    }

    private void OnEnable()
    {
        PlayerSkillTreeStorage.OnChanged += Refresh;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelChanged;
        BindUpgradeButton();
        BindEquipButton();
        BindResetButton();
        PlayerSkillTreeStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        Refresh();
    }

    private void OnDisable()
    {
        PlayerSkillTreeStorage.OnChanged -= Refresh;
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelChanged;

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(UpgradeSelected);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipSelected);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetSkillTree);
    }

    public void SetSkillTree(SkillTreeDefinition tree)
    {
        skillTree = tree;
        RefreshRuntime();
        Refresh();
    }

    public void Select(SkillTreeNodeView nodeView)
    {
        if (selectedNodeView != nodeView)
            pendingEquipNode = null;

        selectedNodeView = nodeView;
        Refresh();
    }

    public void Refresh()
    {
        LoadComponents();
        RefreshRuntime();

        if (skillTree == null || runtime == null)
            return;

        int playerLevel = PlayerExperienceStorage.Level;

        if (pointsText != null)
            pointsText.text = $"POINTS: {PlayerSkillTreeStorage.AvailablePoints}";

        SkillTreeNodeView[] nodeViews = GetComponentsInChildren<SkillTreeNodeView>(true);
        foreach (SkillTreeNodeView nodeView in nodeViews)
        {
            if (nodeView == null) continue;

            if (nodeView.Definition == null)
                continue;

            nodeView.Bind(this, nodeView.Definition);
            nodeView.Refresh(runtime, nodeView == selectedNodeView, playerLevel);
        }

        if (selectedNodeView == null && nodeViews.Length > 0)
            selectedNodeView = nodeViews[0];

        RefreshDetail();
        RefreshEquipSlots();
    }

    public void RegisterNode(SkillTreeNodeView nodeView, SkillTreeNodeDefinition definition)
    {
        if (nodeView == null) return;

        nodeView.Bind(this, definition);
    }

    private void UpgradeSelected()
    {
        SkillTreeNodeDefinition node = selectedNodeView != null ? selectedNodeView.Definition : null;
        if (node == null || runtime == null)
            return;

        if (!runtime.TryUpgrade(node, PlayerExperienceStorage.Level, out string reason))
        {
            if (!string.IsNullOrWhiteSpace(reason))
                Debug.LogWarning(reason, this);
            return;
        }

        ApplySkillTreeStats();
        Refresh();
    }

    private void EquipSelected()
    {
        SkillTreeNodeDefinition node = selectedNodeView != null ? selectedNodeView.Definition : null;
        int slotCount = equipSlotIcons != null && equipSlotIcons.Length > 0 ? equipSlotIcons.Length : 4;

        if (!CanEquip(node, out string reason))
        {
            if (!string.IsNullOrWhiteSpace(reason))
                Debug.LogWarning(reason, this);
            return;
        }

        if (PlayerSkillTreeStorage.IsEquipped(skillTree, node, slotCount))
        {
            if (!PlayerSkillTreeStorage.TryUnequipActiveSkill(skillTree, node, slotCount, out reason))
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    Debug.LogWarning(reason, this);
                return;
            }

            pendingEquipNode = null;
            ApplyEquippedSkillsToHero(slotCount);
            Refresh();
            return;
        }

        pendingEquipNode = pendingEquipNode == node ? null : node;
        Refresh();
    }

    private void ResetSkillTree()
    {
        if (skillTree == null)
            return;

        int slotCount = equipSlotIcons != null && equipSlotIcons.Length > 0 ? equipSlotIcons.Length : 4;
        int refunded = PlayerSkillTreeStorage.ResetTreeAndRefund(skillTree, slotCount);
        pendingEquipNode = null;
        RefreshRuntime();
        ApplySkillTreeStats();
        ApplyEquippedSkillsToHero(slotCount);
        Refresh();

        if (refunded > 0)
            Debug.Log($"Skill tree reset. Refunded {refunded} points.", this);
        else
            Debug.Log("Skill tree reset requested, but there are no spent points to refund.", this);
    }

    public void ClickEquipSlot(int slotIndex)
    {
        if (pendingEquipNode == null)
            return;

        EquipNodeToSlot(pendingEquipNode, slotIndex);
    }

    private void EquipNodeToSlot(SkillTreeNodeDefinition node, int slotIndex)
    {
        int slotCount = equipSlotIcons != null && equipSlotIcons.Length > 0 ? equipSlotIcons.Length : 4;
        if (!PlayerSkillTreeStorage.TryEquipActiveSkill(skillTree, node, slotIndex, slotCount, out string reason))
        {
            if (!string.IsNullOrWhiteSpace(reason))
                Debug.LogWarning(reason, this);

            return;
        }

        pendingEquipNode = null;
        ApplyEquippedSkillsToHero(slotCount);
        Refresh();
    }

    private void RefreshDetail()
    {
        SkillTreeNodeDefinition node = selectedNodeView != null ? selectedNodeView.Definition : null;
        if (node == null || runtime == null)
            return;

        int rank = runtime.GetRank(node);
        bool canUpgrade = runtime.CanUpgrade(node, PlayerExperienceStorage.Level, out string reason);

        if (detailIcon != null)
        {
            Sprite icon = GetNodeIcon(node);
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
        }

        if (detailNameText != null)
            detailNameText.text = node.DisplayName;

        if (detailRankText != null)
            detailRankText.text = $"RANK {rank}/{node.MaxRank}";

        if (detailDescriptionText != null)
            detailDescriptionText.text = string.IsNullOrWhiteSpace(node.Description) ? node.Kind.ToString() : node.Description;

        if (detailRequirementText != null)
            detailRequirementText.text = canUpgrade ? $"Requires level {node.RequiredPlayerLevel}" : reason;

        if (detailCostText != null)
            detailCostText.text = $"Cost: {node.PointCost}";

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;

        RefreshEquipButton(node, rank);
    }

    private void RefreshEquipButton(SkillTreeNodeDefinition node, int rank)
    {
        if (equipButton == null)
            return;

        TMP_Text label = equipButton.GetComponentInChildren<TMP_Text>(true);
        int slotCount = equipSlotIcons != null && equipSlotIcons.Length > 0 ? equipSlotIcons.Length : 4;
        bool isUnlockedActive = node != null &&
                                node.Kind == SkillTreeNodeKind.ActiveSkill &&
                                node.ActiveSkill != null &&
                                rank > 0;

        bool alreadyEquipped = isUnlockedActive && PlayerSkillTreeStorage.IsEquipped(skillTree, node, slotCount);
        bool selectingSlot = isUnlockedActive && pendingEquipNode == node;
        equipButton.interactable = isUnlockedActive;

        if (label != null)
        {
            if (!isUnlockedActive)
                label.text = "LOCKED";
            else if (alreadyEquipped)
                label.text = "UNEQUIP";
            else
                label.text = selectingSlot ? "CANCEL" : "EQUIP";
        }
    }

    private void RefreshEquipSlots()
    {
        if (runtime == null)
            return;

        LoadEquipSlots();
        int iconCount = equipSlotIcons != null ? equipSlotIcons.Length : 0;
        HeroSkillDefinition[] equippedSkills = PlayerSkillTreeStorage.GetEquippedActiveSkills(skillTree, iconCount);
        for (int i = 0; i < iconCount; i++)
        {
            HeroSkillDefinition skill = i < equippedSkills.Length && PlayerSkillTreeStorage.HasEquippedSlot(skillTree, i)
                ? equippedSkills[i]
                : null;

            Image icon = equipSlotIcons[i];
            if (icon != null)
            {
                icon.sprite = skill != null ? skill.Icon : null;
                icon.color = skill != null && skill.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                icon.enabled = skill != null && skill.Icon != null;
            }

            if (equipSlotLabels != null && i < equipSlotLabels.Length && equipSlotLabels[i] != null)
            {
                equipSlotLabels[i].text = (i + 1).ToString();
                equipSlotLabels[i].color = pendingEquipNode != null ? new Color(1f, 0.86f, 0.28f, 1f) : Color.white;
            }
        }

        ApplyEquippedSkillsToHero(iconCount);
    }

    private void LoadComponents()
    {
        pointsText ??= FindText("TreeArea/SkillPointText");
        pointsText ??= FindText("DetailPanel/SkillPointText");
        detailIcon ??= FindImage("DetailPanel/SkillIcon");
        detailIcon ??= FindImage("DetailPanel/SkillIcon/IconMask/Icon");
        detailNameText ??= FindText("DetailPanel/SkillNameText");
        detailRankText ??= FindText("DetailPanel/RankText");
        detailDescriptionText ??= FindText("DetailPanel/DescriptionText");
        detailRequirementText ??= FindText("DetailPanel/RequirementText");
        detailCostText ??= FindText("DetailPanel/CostText");

        if (upgradeButton == null)
        {
            Transform button = transform.Find("DetailPanel/UpgradeButton");
            if (button != null)
                upgradeButton = button.GetComponent<Button>();
        }

        if (equipButton == null)
        {
            Transform button = transform.Find("DetailPanel/EquipButton");
            if (button != null)
                equipButton = button.GetComponent<Button>();
        }

        if (resetButton == null)
        {
            Transform button = transform.Find("TreeArea/ResetButton");
            if (button == null)
                button = transform.Find("DetailPanel/ResetButton");

            if (button != null)
                resetButton = button.GetComponent<Button>();
        }

        LoadEquipSlots();
    }

    private void LoadEquipSlots()
    {
        Transform slotsRoot = transform.Find("EquipSkillPanel/Slots");
        if (slotsRoot == null)
            return;

        int childCount = slotsRoot.childCount;
        if (equipSlotIcons == null || equipSlotIcons.Length != childCount)
            equipSlotIcons = new Image[childCount];

        if (equipSlotLabels == null || equipSlotLabels.Length != childCount)
            equipSlotLabels = new TMP_Text[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform slot = slotsRoot.GetChild(i);
            Transform icon = slot.Find("Icon");
            if (icon == null)
                icon = slot.Find("IconMask/Icon");
            Transform label = slot.Find("IndexText");

            equipSlotIcons[i] = icon != null ? icon.GetComponent<Image>() : null;
            equipSlotLabels[i] = label != null ? label.GetComponent<TMP_Text>() : null;
            SetupEquipDropTarget(slot.gameObject, i);
        }
    }

    private TMP_Text FindText(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Sprite GetNodeIcon(SkillTreeNodeDefinition node)
    {
        return node != null
            ? node.Icon != null ? node.Icon : node.ActiveSkill != null ? node.ActiveSkill.Icon : null
            : null;
    }

    private void RefreshRuntime()
    {
        runtime = new SkillTreeRuntime(skillTree);
    }

    private void BindUpgradeButton()
    {
        if (upgradeButton == null)
            return;

        upgradeButton.onClick.RemoveListener(UpgradeSelected);
        upgradeButton.onClick.AddListener(UpgradeSelected);
    }

    private void BindEquipButton()
    {
        if (equipButton == null)
            return;

        equipButton.onClick.RemoveListener(EquipSelected);
        equipButton.onClick.AddListener(EquipSelected);
    }

    private void BindResetButton()
    {
        if (resetButton == null)
            return;

        resetButton.onClick.RemoveListener(ResetSkillTree);
        resetButton.onClick.AddListener(ResetSkillTree);
    }

    private void HandleLevelChanged(PlayerLevelSnapshot snapshot)
    {
        PlayerSkillTreeStorage.EnsureLevelRewarded(snapshot.Level);
        Refresh();
    }

    private void ApplySkillTreeStats()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.CharacterStat == null)
            return;

        hero.CharacterStat.RecalculateSkillTree(runtime.CreateStatModifiers());
    }

    private void ApplyEquippedSkillsToHero(int slotCount)
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null)
            return;

        HeroSkillDefinition[] skills = PlayerSkillTreeStorage.GetEquippedActiveSkills(skillTree, slotCount);
        for (int i = 0; i < slotCount; i++)
            hero.HeroSkillController.SetEquippedSkill(i, i < skills.Length ? skills[i] : null);
    }

    private bool CanEquip(SkillTreeNodeDefinition node, out string reason)
    {
        reason = string.Empty;

        if (runtime == null)
        {
            reason = "Missing skill tree runtime.";
            return false;
        }

        if (node == null ||
            node.Kind != SkillTreeNodeKind.ActiveSkill ||
            node.ActiveSkill == null)
        {
            reason = "Select an active skill first.";
            return false;
        }

        if (runtime.GetRank(node) <= 0)
        {
            reason = "Unlock this active skill first.";
            return false;
        }

        return true;
    }

    private void SetupEquipDropTarget(GameObject slotObject, int slotIndex)
    {
        if (slotObject == null)
            return;

        Graphic graphic = slotObject.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;

        SkillTreeEquipSlotDropTarget target = slotObject.GetComponent<SkillTreeEquipSlotDropTarget>();
        if (target == null)
        {
            Debug.LogError($"{slotObject.name} is missing {nameof(SkillTreeEquipSlotDropTarget)}. Rebuild the skill view with the builder.", slotObject);
            return;
        }

        target.Bind(this, slotIndex);
    }
}
