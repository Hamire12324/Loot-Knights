using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeView : MonoBehaviour
{
    private const string TreeContentPrefix = "TreeContent_";
    private const float RuntimeLayoutPadding = 160f;
    private const float RuntimeLayoutMaxNodeSize = 76f;

    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private SkillTreeDefinition primarySkillTree;
    [SerializeField] private SkillTreeDefinition secondarySkillTree;
    [SerializeField] private string primarySkillTreeLabel = "CLASS";
    [SerializeField] private string secondarySkillTreeLabel = "ELEMENT";
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
    [SerializeField] private TMP_Text equipPanelTitleText;
    [SerializeField] private RectTransform equipSlotsRoot;
    [SerializeField] private Image[] equipSlotIcons;
    [SerializeField] private TMP_Text[] equipSlotLabels;
    [SerializeField] private RectTransform specialSkillSlotRoot;
    [SerializeField] private Image specialSkillIcon;
    [SerializeField] private TMP_Text specialSkillLabel;
    [SerializeField] private SkillTreePresenter presenter;
    [SerializeField] private bool buildMissingNodeViews = true;
    [SerializeField] private bool buildTreeSwitcher = true;

    private bool missingPresenterLogged;
    private SkillTreeViewState currentState;
    private Transform generatedNodesRoot;
    private Transform generatedLinesRoot;
    private ScrollRect treeScrollRect;
    private RectTransform treeSwitcherRoot;
    private Button primarySkillTreeButton;
    private Button secondarySkillTreeButton;
    private TMP_Text primarySkillTreeButtonLabel;
    private TMP_Text secondarySkillTreeButtonLabel;
    private readonly List<SkillTreeDefinition> skillTreeBuffer = new();

    public event Action<SkillTreeNodeView> NodeSelected;
    public event Action UpgradeClicked;
    public event Action EquipClicked;
    public event Action ResetClicked;
    public event Action<int> EquipSlotClicked;

    public SkillTreeDefinition SkillTree => skillTree;
    public SkillTreeDefinition PrimarySkillTree => primarySkillTree != null ? primarySkillTree : skillTree;
    public int EquipSlotCount
    {
        get
        {
            LoadEquipSlots();
            return equipSlotIcons != null && equipSlotIcons.Length > 0 ? equipSlotIcons.Length : 4;
        }
    }

    private void Awake()
    {
        LoadComponents();
        CapturePrimarySkillTree();
        BindButtons();
        BindTreeSwitcherButtons();
        EnsurePresenter();
    }

    private void OnEnable()
    {
        LoadComponents();
        CapturePrimarySkillTree();
        BindButtons();
        BindTreeSwitcherButtons();
        EnsurePresenter();
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(RaiseUpgradeClicked);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(RaiseEquipClicked);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(RaiseResetClicked);

        if (primarySkillTreeButton != null)
            primarySkillTreeButton.onClick.RemoveListener(ShowPrimarySkillTree);

        if (secondarySkillTreeButton != null)
            secondarySkillTreeButton.onClick.RemoveListener(ShowSecondarySkillTree);
    }

    public void SetSkillTree(SkillTreeDefinition tree)
    {
        skillTree = tree;
        CapturePrimarySkillTree();
        EnsureTreeSwitcher();
        ApplyTreeVisibility();
        ApplyEquipPanelMode(IsElementalSkillTree(skillTree));
        RenderTreeSwitcher();
        ResetTreeScroll();

        SkillTreePresenter activePresenter = EnsurePresenter();
        if (activePresenter == null)
            return;

        activePresenter.SetSkillTree(tree);
        activePresenter.Refresh();
    }

    public void SetSkillTrees(
        SkillTreeDefinition primaryTree,
        SkillTreeDefinition secondaryTree,
        string primaryLabel,
        string secondaryLabel)
    {
        if (primaryTree != null)
            primarySkillTree = primaryTree;

        if (primarySkillTree == null)
            primarySkillTree = skillTree;

        secondarySkillTree = secondaryTree;

        if (!string.IsNullOrWhiteSpace(primaryLabel))
            primarySkillTreeLabel = primaryLabel;

        if (!string.IsNullOrWhiteSpace(secondaryLabel))
            secondarySkillTreeLabel = secondaryLabel;

        if (!IsConfiguredSkillTree(skillTree))
            skillTree = primarySkillTree != null ? primarySkillTree : secondarySkillTree;

        EnsureTreeSwitcher();
        BindTreeSwitcherButtons();
        ApplyTreeVisibility();
        ApplyEquipPanelMode(IsElementalSkillTree(skillTree));
        RenderTreeSwitcher();
        ResetTreeScroll();

        SkillTreePresenter activePresenter = EnsurePresenter();
        if (activePresenter == null)
            return;

        activePresenter.SetSkillTree(skillTree);
        activePresenter.Refresh();
    }

    public IReadOnlyList<SkillTreeDefinition> GetSkillTrees()
    {
        skillTreeBuffer.Clear();
        AddSkillTree(primarySkillTree);
        AddSkillTree(secondarySkillTree);

        if (skillTreeBuffer.Count == 0)
            AddSkillTree(skillTree);

        return skillTreeBuffer;
    }

    public void Refresh()
    {
        SkillTreePresenter activePresenter = EnsurePresenter();
        activePresenter?.Refresh();
    }

    public void Select(SkillTreeNodeView nodeView)
    {
        NodeSelected?.Invoke(nodeView);
    }

    public void ClickEquipSlot(int slotIndex)
    {
        EquipSlotClicked?.Invoke(slotIndex);
    }

    public bool TryBeginEquipDrag()
    {
        if (currentState == null ||
            !currentState.SelectedIsUnlockedActiveSkill ||
            currentState.SelectedIsEquipped)
            return false;

        if (!currentState.SelectingEquipSlot)
            EquipClicked?.Invoke();

        return true;
    }

    public bool CompleteEquipDrag(int slotIndex)
    {
        if (currentState == null || !currentState.HasPendingEquip)
            return false;

        ClickEquipSlot(slotIndex);
        return true;
    }

    public void CancelEquipDrag()
    {
        if (currentState != null && currentState.SelectingEquipSlot)
            EquipClicked?.Invoke();
    }

    public void RegisterNode(SkillTreeNodeView nodeView, SkillTreeNodeDefinition definition)
    {
        if (nodeView == null)
            return;

        nodeView.Bind(this, definition);
    }

    public void BindNodeViews()
    {
        foreach (SkillTreeNodeView nodeView in GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            nodeView.Bind(this, nodeView.Definition);
        }
    }

    public SkillTreeNodeView[] GetNodeViews()
    {
        EnsureGeneratedNodeViews();
        ApplyTreeVisibility();

        List<SkillTreeNodeView> nodeViews = new();
        Transform searchRoot = GetTreeContentRoot(skillTree);
        SkillTreeNodeView[] views = searchRoot != null
            ? searchRoot.GetComponentsInChildren<SkillTreeNodeView>(true)
            : GetComponentsInChildren<SkillTreeNodeView>(true);

        foreach (SkillTreeNodeView nodeView in views)
        {
            if (nodeView == null)
                continue;

            bool belongsToTree = NodeBelongsToTree(nodeView.Definition);
            nodeView.gameObject.SetActive(belongsToTree);
            if (belongsToTree)
                nodeViews.Add(nodeView);
        }

        return nodeViews.ToArray();
    }

    public void Render(SkillTreeViewState state)
    {
        LoadComponents();
        ApplyTreeVisibility();
        RenderTreeSwitcher();
        currentState = state;

        if (state == null || state.SkillTree == null)
        {
            ClearDetail();
            RenderPoints(0);
            RenderEquipSlots(state);
            return;
        }

        RenderPoints(state.AvailablePoints);
        RenderNodes(state);
        RenderDetail(state);
        RenderEquipSlots(state);
    }

    private void RenderPoints(int points)
    {
        if (pointsText != null)
            pointsText.text = $"POINTS: {points}";
    }

    private void RenderNodes(SkillTreeViewState state)
    {
        foreach (SkillTreeNodeView nodeView in GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            nodeView.Bind(this, nodeView.Definition);
            if (state.TryGetNodeState(nodeView.Definition, out SkillTreeNodeViewState nodeState))
                nodeView.Render(nodeState);
        }
    }

    private void RenderDetail(SkillTreeViewState state)
    {
        SkillTreeNodeDefinition node = state.SelectedNode;
        if (node == null)
        {
            ClearDetail();
            return;
        }

        if (detailIcon != null)
        {
            Sprite icon = GetNodeIcon(node);
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
        }

        if (detailNameText != null)
            detailNameText.text = node.DisplayName;

        if (detailRankText != null)
            detailRankText.text = $"RANK {state.SelectedRank}/{node.MaxRank}";

        if (detailDescriptionText != null)
            detailDescriptionText.text = string.IsNullOrWhiteSpace(node.Description)
                ? node.Kind.ToString()
                : node.Description;

        if (detailRequirementText != null)
        {
            if (state.SelectedIsSpecialSkill && state.SelectedIsUnlockedSpecialSkill)
                detailRequirementText.text = "Element Core ready";
            else if (state.SelectedIsSpecialSkill && state.SelectedCanUpgrade)
                detailRequirementText.text = $"Unlocks Element Core at level {node.RequiredPlayerLevel}";
            else
                detailRequirementText.text = state.SelectedCanUpgrade
                    ? $"Requires level {node.RequiredPlayerLevel}"
                    : state.SelectedUpgradeReason;
        }

        if (detailCostText != null)
            detailCostText.text = $"Cost: {node.PointCost}";

        if (upgradeButton != null)
            upgradeButton.interactable = state.SelectedCanUpgrade;

        RenderEquipButton(state);
    }

    private void RenderEquipButton(SkillTreeViewState state)
    {
        if (equipButton == null)
            return;

        if (state.SelectedIsSpecialSkill)
        {
            equipButton.interactable = false;
            SetButtonLabel(equipButton, state.SelectedIsUnlockedSpecialSkill ? "CORE" : "LOCKED");
            return;
        }

        equipButton.interactable = state.SelectedIsUnlockedActiveSkill;

        if (!state.SelectedIsUnlockedActiveSkill)
            SetButtonLabel(equipButton, "LOCKED");
        else if (state.SelectedIsEquipped)
            SetButtonLabel(equipButton, "UNEQUIP");
        else
            SetButtonLabel(equipButton, state.SelectingEquipSlot ? "CANCEL" : "EQUIP");
    }

    private void RenderEquipSlots(SkillTreeViewState state)
    {
        LoadEquipSlots();

        bool showingElementTree = IsElementalSkillTree(state != null ? state.SkillTree : GetVisibleSkillTreeForEquipPanel());
        ApplyEquipPanelMode(showingElementTree);

        if (showingElementTree)
        {
            RenderSpecialSkillSlot(state);
            return;
        }

        int iconCount = equipSlotIcons != null ? equipSlotIcons.Length : 0;
        for (int i = 0; i < iconCount; i++)
        {
            HeroSkillDefinition skill = state != null && state.IsEquipSlotOccupied(i)
                ? state.GetEquippedSkill(i)
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
                equipSlotLabels[i].color = state != null && state.HasPendingEquip
                    ? new Color(1f, 0.86f, 0.28f, 1f)
                    : Color.white;
            }
        }

        if (specialSkillSlotRoot != null)
            specialSkillSlotRoot.gameObject.SetActive(false);
    }

    private void RenderSpecialSkillSlot(SkillTreeViewState state)
    {
        LoadSpecialSkillSlot();

        if (specialSkillSlotRoot == null)
            return;

        HeroSkillDefinition skill = state != null ? state.SpecialSkill : null;
        Sprite sprite = skill != null ? skill.Icon : null;
        bool hasIcon = sprite != null;

        PositionSpecialSkillSlotForElementPanel();
        specialSkillSlotRoot.gameObject.SetActive(true);

        if (specialSkillIcon != null)
        {
            specialSkillIcon.sprite = sprite;
            specialSkillIcon.color = hasIcon ? Color.white : new Color(1f, 1f, 1f, 0f);
            specialSkillIcon.enabled = hasIcon;
            specialSkillIcon.preserveAspect = true;
        }

        if (specialSkillLabel != null)
        {
            specialSkillLabel.text = "ELEMENT";
            specialSkillLabel.color = skill != null
                ? new Color(0.45f, 1f, 0.95f, 1f)
                : new Color(0.55f, 0.62f, 0.75f, 0.85f);
        }
    }

    private void ApplyEquipPanelMode(bool elementMode)
    {
        if (equipPanelTitleText != null)
            equipPanelTitleText.text = elementMode ? "ELEMENT CORE" : "EQUIP SKILLS";

        if (equipSlotsRoot != null)
            equipSlotsRoot.gameObject.SetActive(!elementMode);

        if (specialSkillSlotRoot != null)
        {
            if (elementMode)
                PositionSpecialSkillSlotForElementPanel();

            specialSkillSlotRoot.gameObject.SetActive(elementMode);
        }
    }

    private void PositionSpecialSkillSlotForElementPanel()
    {
        if (specialSkillSlotRoot == null)
            return;

        RectTransform templateSlot = GetFirstEquipSlotRoot();
        RectTransform templateMask = templateSlot != null ? templateSlot.Find("IconMask") as RectTransform : null;
        RectTransform templateIcon = templateMask != null ? templateMask.Find("Icon") as RectTransform : null;
        RectTransform templateLabel = templateSlot != null ? templateSlot.Find("IndexText") as RectTransform : null;
        TMP_Text templateLabelText = templateLabel != null ? templateLabel.GetComponent<TMP_Text>() : null;

        Vector2 slotSize = templateSlot != null ? templateSlot.sizeDelta : new Vector2(64f, 64f);
        Vector2 maskSize = templateMask != null ? templateMask.sizeDelta : new Vector2(48f, 48f);
        Vector2 iconSize = templateIcon != null ? templateIcon.sizeDelta : maskSize;
        Vector2 labelPosition = templateLabel != null ? templateLabel.anchoredPosition : new Vector2(0f, -slotSize.y * 0.5f);
        Vector2 labelSize = templateLabel != null
            ? new Vector2(Mathf.Max(templateLabel.sizeDelta.x, slotSize.x), templateLabel.sizeDelta.y)
            : new Vector2(slotSize.x, 18f);

        specialSkillSlotRoot.anchorMin = new Vector2(0.5f, 0.5f);
        specialSkillSlotRoot.anchorMax = new Vector2(0.5f, 0.5f);
        specialSkillSlotRoot.pivot = new Vector2(0.5f, 0.5f);
        specialSkillSlotRoot.anchoredPosition = Vector2.zero;
        specialSkillSlotRoot.sizeDelta = slotSize;
        HideSpecialSkillSlotFrame();
        EnsureSpecialSkillNodeFrame(maskSize);

        Transform mask = specialSkillSlotRoot.Find("IconMask");
        if (mask is RectTransform maskRect)
        {
            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
            maskRect.pivot = new Vector2(0.5f, 0.5f);
            maskRect.anchoredPosition = Vector2.zero;
            maskRect.sizeDelta = maskSize;
        }

        if (specialSkillIcon != null && specialSkillIcon.transform is RectTransform iconRect)
        {
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localRotation = Quaternion.identity;
            iconRect.localScale = Vector3.one;
            iconRect.sizeDelta = iconSize;
        }

        if (specialSkillLabel != null)
        {
            specialSkillLabel.alignment = TextAlignmentOptions.Center;
            specialSkillLabel.enableAutoSizing = true;
            specialSkillLabel.fontSizeMin = 8f;
            specialSkillLabel.fontSizeMax = 13f;

            if (specialSkillLabel.transform is RectTransform labelRect)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = labelPosition;
                labelRect.sizeDelta = labelSize;
            }

            if (templateLabelText != null)
                specialSkillLabel.fontSizeMax = Mathf.Max(specialSkillLabel.fontSizeMax, templateLabelText.fontSize);
        }
    }

    private void ClearDetail()
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = null;
            detailIcon.enabled = false;
        }

        SetText(detailNameText, string.Empty);
        SetText(detailRankText, string.Empty);
        SetText(detailDescriptionText, string.Empty);
        SetText(detailRequirementText, string.Empty);
        SetText(detailCostText, string.Empty);

        if (upgradeButton != null)
            upgradeButton.interactable = false;

        if (equipButton != null)
        {
            equipButton.interactable = false;
            SetButtonLabel(equipButton, "LOCKED");
        }
    }

    private void LoadComponents()
    {
        CapturePrimarySkillTree();

        pointsText ??= FindText("TreeArea/SkillPointText");
        pointsText ??= FindText("DetailPanel/SkillPointText");
        detailIcon ??= FindImage("DetailPanel/SkillIcon");
        detailIcon ??= FindImage("DetailPanel/SkillIcon/IconMask/Icon");
        detailNameText ??= FindText("DetailPanel/SkillNameText");
        detailRankText ??= FindText("DetailPanel/RankText");
        detailDescriptionText ??= FindText("DetailPanel/DescriptionText");
        detailRequirementText ??= FindText("DetailPanel/RequirementText");
        detailCostText ??= FindText("DetailPanel/CostText");
        ConfigureDetailDescriptionText();

        upgradeButton ??= FindButton("DetailPanel/UpgradeButton");
        equipButton ??= FindButton("DetailPanel/EquipButton");
        resetButton ??= FindButton("TreeArea/ResetButton");
        resetButton ??= FindButton("DetailPanel/ResetButton");
        equipPanelTitleText ??= FindText("EquipSkillPanel/TitleText");
        treeScrollRect ??= FindContentChild("TreeArea")?.GetComponent<ScrollRect>();

        LoadEquipSlots();
        LoadSpecialSkillSlot();
        ApplyEquipPanelMode(IsElementalSkillTree(GetVisibleSkillTreeForEquipPanel()));
        EnsureTreeSwitcher();
    }

    private void CapturePrimarySkillTree()
    {
        if (primarySkillTree == null && skillTree != null)
            primarySkillTree = skillTree;
    }

    private void EnsureGeneratedNodeViews()
    {
        if (!buildMissingNodeViews || skillTree == null)
            return;

        if (HasBuiltTreeContent())
            return;

        EnsureGeneratedNodeLines();

        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null || HasNodeView(node))
                continue;

            CreateGeneratedNodeView(node);
        }
    }

    private bool HasNodeView(SkillTreeNodeDefinition node)
    {
        foreach (SkillTreeNodeView nodeView in GetComponentsInChildren<SkillTreeNodeView>(true))
        {
            if (nodeView != null && nodeView.Definition == node)
                return true;
        }

        return false;
    }

    private bool NodeBelongsToTree(SkillTreeNodeDefinition node)
    {
        if (skillTree == null || node == null)
            return false;

        foreach (SkillTreeNodeDefinition treeNode in skillTree.Nodes)
        {
            if (treeNode == node)
                return true;
        }

        return false;
    }

    private SkillTreeNodeView FindNodeTemplate(SkillTreeNodeDefinition targetNode)
    {
        foreach (SkillTreeNodeView nodeView in GetComponentsInChildren<SkillTreeNodeView>(true))
        {
            if (nodeView == null ||
                nodeView.Definition == null ||
                nodeView.Definition == targetNode ||
                IsInsideGeneratedRoot(nodeView.transform))
            {
                continue;
            }

            return nodeView;
        }

        return null;
    }

    private RectTransform FindLineTemplate()
    {
        Transform content = FindContentChild("TreeArea/Viewport/Content");
        Transform searchRoot = content != null ? content : FindContentChild("TreeArea");
        if (searchRoot == null)
            return null;

        foreach (Image image in searchRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null || image.raycastTarget)
                continue;

            RectTransform rect = image.transform as RectTransform;
            if (rect != null && image.gameObject.name.StartsWith("Line_", StringComparison.Ordinal))
                return rect;
        }

        return null;
    }

    private bool IsInsideGeneratedRoot(Transform target)
    {
        if (target == null)
            return false;

        return generatedNodesRoot != null && target.IsChildOf(generatedNodesRoot);
    }

    private void CreateGeneratedNodeView(SkillTreeNodeDefinition node)
    {
        Transform parent = GetGeneratedNodesRoot();
        SkillTreeNodeView template = FindNodeTemplate(node);
        if (template != null)
        {
            GameObject clonedNodeObject = Instantiate(template.gameObject, parent, false);
            clonedNodeObject.name = $"Node_{SanitizeName(node.NodeId)}";
            clonedNodeObject.SetActive(true);

            RectTransform clonedRect = clonedNodeObject.transform as RectTransform;
            if (clonedRect != null)
                clonedRect.anchoredPosition = node.TreePosition;

            SkillTreeNodeView clonedNodeView = clonedNodeObject.GetComponent<SkillTreeNodeView>();
            if (clonedNodeView == null)
                clonedNodeView = clonedNodeObject.AddComponent<SkillTreeNodeView>();

            clonedNodeView.Bind(this, node);
            return;
        }

        GameObject nodeObject = new($"Node_{node.NodeId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        nodeObject.layer = parent.gameObject.layer;
        nodeObject.transform.SetParent(parent, false);

        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = node.TreePosition;
        rect.sizeDelta = new Vector2(78f, 78f);

        Image background = nodeObject.GetComponent<Image>();
        background.color = new Color(0.23f, 0.15f, 0.48f, 0.92f);

        Button button = nodeObject.GetComponent<Button>();
        button.targetGraphic = background;

        CreateIconMask(rect, node.Icon);
        CreateNodeText("RankText", rect, new Vector2(0f, -58f), new Vector2(112f, 34f), string.Empty, 24);
        CreateNodeText("CostText", rect, new Vector2(42f, 42f), new Vector2(42f, 22f), string.Empty, 14);
        CreateCircle("AvailableGlow", rect, new Vector2(92f, 92f), new Color(0.2f, 1f, 0.82f, 0.2f), false);
        CreateCircle("SelectedFrame", rect, new Vector2(88f, 88f), new Color(1f, 0.86f, 0.28f, 0.32f), false);
        CreateCircle("LockOverlay", rect, new Vector2(82f, 82f), new Color(0f, 0f, 0f, 0.48f), false);

        SkillTreeNodeView nodeView = nodeObject.AddComponent<SkillTreeNodeView>();
        nodeView.Bind(this, node);
    }

    private Transform GetGeneratedNodesRoot()
    {
        if (generatedNodesRoot != null)
            return generatedNodesRoot;

        Transform content = FindContentChild("TreeArea/Viewport/Content");
        Transform treeArea = FindContentChild("TreeArea");
        Transform parent = content != null ? content : treeArea != null ? treeArea : transform;
        Transform existing = parent.Find("GeneratedNodes");
        if (existing != null)
        {
            generatedNodesRoot = existing;
            return generatedNodesRoot;
        }

        GameObject rootObject = new("GeneratedNodes", typeof(RectTransform));
        rootObject.layer = parent.gameObject.layer;
        rootObject.transform.SetParent(parent, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        generatedNodesRoot = rootObject.transform;
        return generatedNodesRoot;
    }

    private Transform GetGeneratedLinesRoot()
    {
        if (generatedLinesRoot != null)
            return generatedLinesRoot;

        Transform content = FindContentChild("TreeArea/Viewport/Content");
        Transform treeArea = FindContentChild("TreeArea");
        Transform parent = content != null ? content : treeArea != null ? treeArea : transform;
        Transform existing = parent.Find("GeneratedLines");
        if (existing != null)
        {
            generatedLinesRoot = existing;
            return generatedLinesRoot;
        }

        GameObject rootObject = new("GeneratedLines", typeof(RectTransform));
        rootObject.layer = parent.gameObject.layer;
        rootObject.transform.SetParent(parent, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        generatedLinesRoot = rootObject.transform;
        generatedLinesRoot.SetAsFirstSibling();
        return generatedLinesRoot;
    }

    private void EnsureGeneratedNodeLines()
    {
        if (skillTree == null)
            return;

        Transform parent = GetGeneratedLinesRoot();
        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null)
                continue;

            foreach (SkillTreePrerequisite prerequisite in node.Prerequisites)
            {
                SkillTreeNodeDefinition fromNode = prerequisite?.Node;
                if (fromNode == null || !NodeBelongsToTree(fromNode))
                    continue;

                string lineName = $"Line_{SanitizeName(fromNode.NodeId)}_To_{SanitizeName(node.NodeId)}";
                if (parent.Find(lineName) != null)
                    continue;

                CreateGeneratedLine(parent, lineName, fromNode.TreePosition, node.TreePosition);
            }
        }
    }

    private void CreateGeneratedLine(Transform parent, string lineName, Vector2 from, Vector2 to)
    {
        RectTransform template = FindLineTemplate();
        GameObject lineObject;
        if (template != null)
        {
            lineObject = Instantiate(template.gameObject, parent, false);
            lineObject.name = lineName;
        }
        else
        {
            lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObject.layer = parent.gameObject.layer;
            lineObject.transform.SetParent(parent, false);
        }

        RectTransform rect = lineObject.transform as RectTransform;
        if (rect == null)
            return;

        Vector2 direction = to - from;
        float length = Mathf.Max(1f, direction.magnitude);
        float thickness = template != null ? Mathf.Max(1f, template.sizeDelta.x) : 5f;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(thickness, length);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        lineObject.SetActive(true);
    }

    private void EnsureTreeSwitcher()
    {
        Transform treeArea = FindContentChild("TreeArea");
        if (treeArea == null)
            return;

        if (treeSwitcherRoot == null)
        {
            Transform existing = treeArea.Find("TreeSwitcher");
            treeSwitcherRoot = existing != null ? existing as RectTransform : null;
        }

        if (treeSwitcherRoot == null && buildTreeSwitcher)
            treeSwitcherRoot = CreateTreeSwitcher(treeArea);

        if (treeSwitcherRoot == null)
            return;

        treeSwitcherRoot.gameObject.SetActive(HasMultipleSkillTrees());
        LoadTreeSwitcherButtons();
        RenderTreeSwitcher();
    }

    private RectTransform CreateTreeSwitcher(Transform parent)
    {
        GameObject rootObject = new("TreeSwitcher", typeof(RectTransform));
        rootObject.layer = parent.gameObject.layer;
        rootObject.transform.SetParent(parent, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(420f, 56f);

        primarySkillTreeButton = CreateTreeSwitcherButton(
            "ClassTreeButton",
            rect,
            new Vector2(-110f, 0f));

        secondarySkillTreeButton = CreateTreeSwitcherButton(
            "ElementTreeButton",
            rect,
            new Vector2(110f, 0f));

        LoadTreeSwitcherButtons();
        return rect;
    }

    private Button CreateTreeSwitcherButton(string name, RectTransform parent, Vector2 position)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200f, 46f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = GetSwitcherNormalColor();

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        RectTransform labelRect = CreateRect("Label", rect, Vector2.zero, new Vector2(188f, 40f));
        TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 20f;
        label.color = Color.white;
        label.raycastTarget = false;
        ApplySwitcherButtonTemplate(button, label);

        return button;
    }

    private void ApplySwitcherButtonTemplate(Button button, TMP_Text label)
    {
        Button templateButton = resetButton != null ? resetButton : upgradeButton != null ? upgradeButton : equipButton;
        if (templateButton == null || button == null)
            return;

        Image templateImage = templateButton.targetGraphic as Image;
        if (templateImage == null)
            templateImage = templateButton.GetComponent<Image>();

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        if (templateImage != null && image != null)
        {
            image.sprite = templateImage.sprite;
            image.type = templateImage.type;
            image.preserveAspect = templateImage.preserveAspect;
            image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
            image.material = templateImage.material;
            image.color = templateImage.color;
        }

        TMP_Text templateLabel = templateButton.GetComponentInChildren<TMP_Text>(true);
        if (templateLabel != null && label != null)
        {
            label.font = templateLabel.font;
            label.fontSharedMaterial = templateLabel.fontSharedMaterial;
            label.color = templateLabel.color;
        }
    }

    private void LoadTreeSwitcherButtons()
    {
        if (treeSwitcherRoot == null)
            return;

        primarySkillTreeButton ??= treeSwitcherRoot.Find("ClassTreeButton")?.GetComponent<Button>();
        secondarySkillTreeButton ??= treeSwitcherRoot.Find("ElementTreeButton")?.GetComponent<Button>();

        primarySkillTreeButtonLabel ??= primarySkillTreeButton != null
            ? primarySkillTreeButton.GetComponentInChildren<TMP_Text>(true)
            : null;

        secondarySkillTreeButtonLabel ??= secondarySkillTreeButton != null
            ? secondarySkillTreeButton.GetComponentInChildren<TMP_Text>(true)
            : null;
    }

    private void CreateIconMask(RectTransform parent, Sprite sprite)
    {
        RectTransform mask = CreateRect("IconMask", parent, Vector2.zero, new Vector2(58f, 58f));
        Image maskImage = mask.gameObject.AddComponent<Image>();
        maskImage.color = new Color(0.07f, 0.12f, 0.28f, 0.95f);
        Mask maskComponent = mask.gameObject.AddComponent<Mask>();
        maskComponent.showMaskGraphic = true;

        RectTransform iconRect = CreateRect("Icon", mask, Vector2.zero, new Vector2(48f, 48f));
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private TMP_Text CreateNodeText(
        string name,
        RectTransform parent,
        Vector2 position,
        Vector2 size,
        string value,
        int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8f;
        text.fontSizeMax = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.86f, 0.28f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private void CreateCircle(string name, RectTransform parent, Vector2 size, Color color, bool active)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        rect.gameObject.SetActive(active);
    }

    private RectTransform CreateRect(string name, RectTransform parent, Vector2 position, Vector2 size)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void LoadEquipSlots()
    {
        Transform slotsRoot = FindContentChild("EquipSkillPanel/Slots");
        if (slotsRoot == null)
            return;

        equipSlotsRoot = slotsRoot as RectTransform;
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

    private void LoadSpecialSkillSlot()
    {
        if (specialSkillIcon != null && specialSkillLabel != null && specialSkillSlotRoot != null)
            return;

        Transform slot = FindContentChild("EquipSkillPanel/ElementCoreSlot");
        if (slot == null)
        {
            RectTransform equipPanel = FindContentChild("EquipSkillPanel") as RectTransform;
            if (equipPanel == null)
                return;

            slot = CreateSpecialSkillSlot(equipPanel);
        }

        specialSkillSlotRoot = slot as RectTransform;
        specialSkillIcon = slot.Find("IconMask/Icon")?.GetComponent<Image>();
        specialSkillLabel = slot.Find("LabelText")?.GetComponent<TMP_Text>();
    }

    private RectTransform CreateSpecialSkillSlot(RectTransform parent)
    {
        RectTransform templateSlot = GetFirstEquipSlotRoot();
        RectTransform templateMask = templateSlot != null ? templateSlot.Find("IconMask") as RectTransform : null;
        RectTransform templateIcon = templateMask != null ? templateMask.Find("Icon") as RectTransform : null;
        RectTransform templateLabel = templateSlot != null ? templateSlot.Find("IndexText") as RectTransform : null;

        Vector2 slotSize = templateSlot != null ? templateSlot.sizeDelta : new Vector2(64f, 64f);
        Vector2 maskSize = templateMask != null ? templateMask.sizeDelta : new Vector2(48f, 48f);
        Vector2 iconSize = templateIcon != null ? templateIcon.sizeDelta : maskSize;
        Vector2 labelPosition = templateLabel != null ? templateLabel.anchoredPosition : new Vector2(0f, -slotSize.y * 0.5f);
        Vector2 labelSize = templateLabel != null
            ? new Vector2(Mathf.Max(templateLabel.sizeDelta.x, slotSize.x), templateLabel.sizeDelta.y)
            : new Vector2(slotSize.x, 18f);

        RectTransform slot = CreateRect("ElementCoreSlot", parent, Vector2.zero, slotSize);
        slot.anchorMin = new Vector2(0.5f, 0.5f);
        slot.anchorMax = new Vector2(0.5f, 0.5f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.anchoredPosition = Vector2.zero;

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = null;
        background.color = Color.clear;
        background.raycastTarget = false;

        CreateSpecialSkillNodeFrame(slot, maskSize);

        RectTransform mask = CreateRect("IconMask", slot, Vector2.zero, maskSize);
        UICircleGraphic maskGraphic = mask.gameObject.AddComponent<UICircleGraphic>();
        maskGraphic.color = Color.white;
        maskGraphic.raycastTarget = false;

        Mask maskComponent = mask.gameObject.AddComponent<Mask>();
        maskComponent.showMaskGraphic = false;

        RectTransform icon = CreateRect("Icon", mask, Vector2.zero, iconSize);
        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = false;

        TMP_Text label = CreateNodeText(
            "LabelText",
            slot,
            labelPosition,
            labelSize,
            "ELEMENT",
            11);
        label.color = new Color(0.45f, 1f, 0.95f, 1f);
        slot.gameObject.SetActive(false);

        return slot;
    }

    private void EnsureSpecialSkillNodeFrame(Vector2 maskSize)
    {
        if (specialSkillSlotRoot == null)
            return;

        Vector2 safeMaskSize = maskSize.x > 1f && maskSize.y > 1f
            ? maskSize
            : new Vector2(70f, 70f);

        Transform existing = specialSkillSlotRoot.Find("NodeFrame");
        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;

        if (rect == null || image == null)
        {
            rect = CreateSpecialSkillNodeFrame(specialSkillSlotRoot, safeMaskSize);
            image = rect.GetComponent<Image>();
        }

        Vector2 defaultFrameSize = safeMaskSize + new Vector2(26f, 26f);
        Vector2 frameSize = rect.sizeDelta.x > defaultFrameSize.x || rect.sizeDelta.y > defaultFrameSize.y
            ? rect.sizeDelta
            : defaultFrameSize;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = frameSize;
        rect.SetAsFirstSibling();

        image.sprite = GetFirstSkillNodeFrameSprite();
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        SetChildActive("CoreBack", false);
        SetChildActive("CoreFrame", false);
    }

    private RectTransform CreateSpecialSkillNodeFrame(RectTransform parent, Vector2 maskSize)
    {
        RectTransform rect = CreateRect("NodeFrame", parent, Vector2.zero, maskSize + new Vector2(26f, 26f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetFirstSkillNodeFrameSprite();
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;
        rect.SetAsFirstSibling();
        return rect;
    }

    private void HideSpecialSkillSlotFrame()
    {
        if (specialSkillSlotRoot == null)
            return;

        Image background = specialSkillSlotRoot.GetComponent<Image>();
        if (background == null)
            return;

        background.sprite = null;
        background.color = Color.clear;
        background.raycastTarget = false;
    }

    private void SetChildActive(string childName, bool active)
    {
        if (specialSkillSlotRoot == null)
            return;

        Transform child = specialSkillSlotRoot.Find(childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private SkillTreeDefinition GetVisibleSkillTreeForEquipPanel()
    {
        Transform secondaryTreeRoot = GetTreeContentRoot(secondarySkillTree);
        if (secondaryTreeRoot != null && secondaryTreeRoot.gameObject.activeSelf)
            return secondarySkillTree;

        Transform primaryTreeRoot = GetTreeContentRoot(PrimarySkillTree);
        if (primaryTreeRoot != null && primaryTreeRoot.gameObject.activeSelf)
            return PrimarySkillTree;

        return skillTree;
    }

    private void RefreshEquipPanelModeForCurrentTree()
    {
        LoadComponents();
        ApplyEquipPanelMode(IsElementalSkillTree(GetVisibleSkillTreeForEquipPanel()));
    }

    private Sprite GetFirstEquipSlotSprite()
    {
        if (equipSlotIcons == null)
            return null;

        foreach (Image icon in equipSlotIcons)
        {
            if (icon == null || icon.transform == null || icon.transform.parent == null)
                continue;

            Transform slotTransform = icon.transform.parent;
            if (slotTransform != null && slotTransform.name == "IconMask")
                slotTransform = slotTransform.parent;

            Image slotImage = slotTransform != null ? slotTransform.GetComponent<Image>() : null;
            if (slotImage != null && slotImage.sprite != null)
                return slotImage.sprite;
        }

        return null;
    }

    private Sprite GetFirstSkillNodeFrameSprite()
    {
        Transform content = FindContentChild("TreeArea/Viewport/Content");
        if (content == null)
            return null;

        SkillTreeNodeView[] nodeViews = content.GetComponentsInChildren<SkillTreeNodeView>(true);
        foreach (SkillTreeNodeView nodeView in nodeViews)
        {
            if (nodeView == null)
                continue;

            Image frame = nodeView.GetComponent<Image>();
            if (frame != null && frame.sprite != null)
                return frame.sprite;
        }

        return GetFirstEquipSlotSprite();
    }

    private RectTransform GetFirstEquipSlotRoot()
    {
        if (equipSlotIcons == null)
            return null;

        foreach (Image icon in equipSlotIcons)
        {
            if (icon == null || icon.transform == null || icon.transform.parent == null)
                continue;

            Transform slotTransform = icon.transform.parent;
            if (slotTransform != null && slotTransform.name == "IconMask")
                slotTransform = slotTransform.parent;

            if (slotTransform is RectTransform slotRect)
                return slotRect;
        }

        return null;
    }

    private void BindButtons()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(RaiseUpgradeClicked);
            upgradeButton.onClick.AddListener(RaiseUpgradeClicked);
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(RaiseEquipClicked);
            equipButton.onClick.AddListener(RaiseEquipClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(RaiseResetClicked);
            resetButton.onClick.AddListener(RaiseResetClicked);
        }
    }

    private void BindTreeSwitcherButtons()
    {
        if (primarySkillTreeButton != null)
        {
            primarySkillTreeButton.onClick.RemoveListener(ShowPrimarySkillTree);
            primarySkillTreeButton.onClick.AddListener(ShowPrimarySkillTree);
        }

        if (secondarySkillTreeButton != null)
        {
            secondarySkillTreeButton.onClick.RemoveListener(ShowSecondarySkillTree);
            secondarySkillTreeButton.onClick.AddListener(ShowSecondarySkillTree);
        }
    }

    private void RaiseUpgradeClicked()
    {
        UpgradeClicked?.Invoke();
    }

    private void RaiseEquipClicked()
    {
        EquipClicked?.Invoke();
    }

    private void RaiseResetClicked()
    {
        ResetClicked?.Invoke();
    }

    private void ShowPrimarySkillTree()
    {
        SwitchToSkillTree(primarySkillTree);
    }

    private void ShowSecondarySkillTree()
    {
        SwitchToSkillTree(secondarySkillTree);
    }

    private void SwitchToSkillTree(SkillTreeDefinition targetTree)
    {
        if (targetTree == null || skillTree == targetTree)
            return;

        SetSkillTree(targetTree);
    }

    private void ApplyTreeVisibility()
    {
        Transform content = FindContentChild("TreeArea/Viewport/Content");
        if (content == null)
            return;

        bool hasTreeContent = false;
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child == null || !child.name.StartsWith(TreeContentPrefix, StringComparison.Ordinal))
                continue;

            hasTreeContent = true;
            child.gameObject.SetActive(IsTreeContentFor(child, skillTree));
        }

        if (hasTreeContent)
        {
            ConfigureActiveTreeScroll();
            return;
        }

        Transform legacyLines = content.Find("Lines");
        if (legacyLines != null)
            legacyLines.gameObject.SetActive(skillTree == PrimarySkillTree);

        Transform generatedLines = content.Find("GeneratedLines");
        if (generatedLines != null)
            generatedLines.gameObject.SetActive(skillTree != PrimarySkillTree);

        ConfigureActiveTreeScroll();
    }

    private SkillTreePresenter EnsurePresenter()
    {
        if (presenter == null)
            presenter = GetComponent<SkillTreePresenter>();

        if (presenter == null)
        {
            if (!missingPresenterLogged)
            {
                missingPresenterLogged = true;
                Debug.LogError($"{nameof(SkillTreeView)} needs a {nameof(SkillTreePresenter)} on the same GameObject.", this);
            }

            return null;
        }

        return presenter;
    }

    private TMP_Text FindText(string path)
    {
        Transform child = FindContentChild(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string path)
    {
        Transform child = FindContentChild(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private Button FindButton(string path)
    {
        Transform child = FindContentChild(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private Transform FindContentChild(string path)
    {
        return transform.Find(path);
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

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetButtonLabel(Button button, string labelText)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (label != null)
            label.text = labelText;
    }

    private void ConfigureDetailDescriptionText()
    {
        if (detailDescriptionText == null)
            return;

        detailDescriptionText.enableAutoSizing = true;
        detailDescriptionText.fontSizeMin = Mathf.Max(7f, detailDescriptionText.fontSize * 0.68f);
        detailDescriptionText.fontSizeMax = detailDescriptionText.fontSize;
        detailDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void RenderTreeSwitcher()
    {
        LoadTreeSwitcherButtons();

        if (primarySkillTreeButtonLabel != null)
            primarySkillTreeButtonLabel.text = primarySkillTreeLabel;

        if (secondarySkillTreeButtonLabel != null)
            secondarySkillTreeButtonLabel.text = secondarySkillTreeLabel;

        SetTreeSwitcherButtonState(primarySkillTreeButton, skillTree == primarySkillTree);
        SetTreeSwitcherButtonState(secondarySkillTreeButton, skillTree == secondarySkillTree);
    }

    private void SetTreeSwitcherButtonState(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        if (image != null)
            image.color = selected ? GetSwitcherSelectedColor() : GetSwitcherNormalColor();
    }

    private bool HasMultipleSkillTrees()
    {
        return primarySkillTree != null &&
               secondarySkillTree != null &&
               primarySkillTree != secondarySkillTree;
    }

    private bool IsConfiguredSkillTree(SkillTreeDefinition tree)
    {
        if (tree == null)
            return false;

        return tree == primarySkillTree || tree == secondarySkillTree;
    }

    private void AddSkillTree(SkillTreeDefinition tree)
    {
        if (tree != null && !skillTreeBuffer.Contains(tree))
            skillTreeBuffer.Add(tree);
    }

    private static Color GetSwitcherNormalColor()
    {
        return new Color(0.14f, 0.22f, 0.54f, 0.92f);
    }

    private static Color GetSwitcherSelectedColor()
    {
        return new Color(0.05f, 0.72f, 0.95f, 0.96f);
    }

    private bool HasBuiltTreeContent()
    {
        Transform content = FindContentChild("TreeArea/Viewport/Content");
        if (content == null)
            return false;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child != null && child.name.StartsWith(TreeContentPrefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void ApplyAuthoredTreeLayout()
    {
        Transform treeContent = GetTreeContentRoot(skillTree);
        RectTransform contentRect = FindContentChild("TreeArea/Viewport/Content") as RectTransform;
        if (treeContent == null || contentRect == null || skillTree == null)
            return;

        Dictionary<SkillTreeNodeDefinition, Vector2> positions = GetAlignedAuthoredPositions(contentRect);
        if (positions.Count == 0)
            return;

        foreach (SkillTreeNodeView nodeView in treeContent.GetComponentsInChildren<SkillTreeNodeView>(true))
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            if (positions.TryGetValue(nodeView.Definition, out Vector2 position) &&
                nodeView.transform is RectTransform nodeRect)
            {
                nodeRect.anchoredPosition = position;
            }
        }

        ApplyAuthoredLineLayout(treeContent, positions);
    }

    private Dictionary<SkillTreeNodeDefinition, Vector2> GetAlignedAuthoredPositions(RectTransform contentRect)
    {
        Dictionary<SkillTreeNodeDefinition, Vector2> rawPositions = new();
        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null || IsZero(node.TreePosition))
                continue;

            rawPositions[node] = node.TreePosition;
        }

        if (rawPositions.Count == 0)
            return rawPositions;

        Vector2 contentSize = contentRect.rect.size;
        if (contentSize.x <= 1f || contentSize.y <= 1f)
            contentSize = contentRect.sizeDelta;

        if (contentSize.x <= 1f || contentSize.y <= 1f)
            contentSize = new Vector2(1100f, 860f);

        bool hasPosition = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        foreach (Vector2 position in rawPositions.Values)
        {
            if (!hasPosition)
            {
                min = position;
                max = position;
                hasPosition = true;
                continue;
            }

            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }

        float targetTopY = contentSize.y * 0.5f - RuntimeLayoutPadding - RuntimeLayoutMaxNodeSize * 0.5f;
        Vector2 offset = new Vector2(-(min.x + max.x) * 0.5f, targetTopY - max.y);
        Dictionary<SkillTreeNodeDefinition, Vector2> aligned = new();

        foreach (KeyValuePair<SkillTreeNodeDefinition, Vector2> pair in rawPositions)
            aligned[pair.Key] = pair.Value + offset;

        return aligned;
    }

    private void ApplyAuthoredLineLayout(
        Transform treeContent,
        IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions)
    {
        Transform linesRoot = treeContent.Find("Lines");
        if (linesRoot == null)
            return;

        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null || !positions.TryGetValue(node, out Vector2 to))
                continue;

            foreach (SkillTreePrerequisite prerequisite in node.Prerequisites)
            {
                SkillTreeNodeDefinition fromNode = prerequisite?.Node;
                if (fromNode == null || !positions.TryGetValue(fromNode, out Vector2 from))
                    continue;

                Transform line = FindLineTransform(linesRoot, fromNode, node);
                if (line == null || line is not RectTransform lineRect)
                    continue;

                ApplyLineTransform(lineRect, from, to);

                Image lineImage = lineRect.GetComponent<Image>();
                if (lineImage != null)
                    lineImage.color = GetRuntimeConnectionLineColor(fromNode, node);
            }
        }
    }

    private static Transform FindLineTransform(Transform linesRoot, SkillTreeNodeDefinition fromNode, SkillTreeNodeDefinition toNode)
    {
        Transform exact = linesRoot.Find($"Line_{fromNode.NodeId}_To_{toNode.NodeId}");
        if (exact != null)
            return exact;

        return linesRoot.Find($"Line_{SanitizeName(fromNode.NodeId)}_To_{SanitizeName(toNode.NodeId)}");
    }

    private static void ApplyLineTransform(RectTransform line, Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float length = Mathf.Max(1f, direction.magnitude);
        float thickness = Mathf.Max(1f, line.sizeDelta.x);

        line.anchoredPosition = (from + to) * 0.5f;
        line.sizeDelta = new Vector2(thickness, length);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    private static Color GetRuntimeConnectionLineColor(SkillTreeNodeDefinition fromNode, SkillTreeNodeDefinition toNode)
    {
        if (toNode != null && toNode.Kind == SkillTreeNodeKind.ElementReaction && fromNode != null)
            return GetRuntimeNodeAccentColor(fromNode);

        return GetRuntimeNodeAccentColor(toNode);
    }

    private static Color GetRuntimeNodeAccentColor(SkillTreeNodeDefinition node)
    {
        if (node == null)
            return new Color(0.98f, 0.84f, 0.36f, 0.82f);

        return node.Element switch
        {
            ElementType.Fire => new Color(1f, 0.38f, 0.24f, 1f),
            ElementType.Frost => new Color(0.55f, 0.9f, 1f, 1f),
            ElementType.Lightning => new Color(0.8f, 0.56f, 1f, 1f),
            ElementType.Poison => new Color(0.56f, 0.95f, 0.36f, 1f),
            _ => new Color(0.98f, 0.84f, 0.36f, 0.82f)
        };
    }

    private static bool IsElementalSkillTree(SkillTreeDefinition tree)
    {
        if (tree == null)
            return false;

        if (!string.IsNullOrWhiteSpace(tree.TreeId) &&
            tree.TreeId.IndexOf("element", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null &&
                (node.Element != ElementType.None ||
                 node.Reaction != ElementalReactionType.None ||
                 node.Kind == SkillTreeNodeKind.ElementUnlock ||
                 node.Kind == SkillTreeNodeKind.ElementReaction))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsZero(Vector2 value)
    {
        return Mathf.Approximately(value.x, 0f) && Mathf.Approximately(value.y, 0f);
    }

    private void ResetTreeScroll()
    {
        if (treeScrollRect == null)
            treeScrollRect = FindContentChild("TreeArea")?.GetComponent<ScrollRect>();

        if (treeScrollRect == null)
            return;

        ConfigureActiveTreeScroll();
        Canvas.ForceUpdateCanvases();
        treeScrollRect.horizontalNormalizedPosition = 0.5f;
        treeScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ConfigureActiveTreeScroll()
    {
        if (treeScrollRect == null)
            treeScrollRect = FindContentChild("TreeArea")?.GetComponent<ScrollRect>();

        if (treeScrollRect == null)
            return;

        RectTransform scrollContent = FindContentChild("TreeArea/Viewport/Content") as RectTransform;
        if (scrollContent != null && treeScrollRect.content != scrollContent)
            treeScrollRect.content = scrollContent;

        RectTransform viewport = treeScrollRect.viewport != null
            ? treeScrollRect.viewport
            : FindContentChild("TreeArea/Viewport") as RectTransform;

        if (scrollContent == null || viewport == null)
            return;

        treeScrollRect.horizontal = false;
        treeScrollRect.horizontalNormalizedPosition = 0.5f;
        treeScrollRect.vertical = ActiveTreeNeedsVerticalScroll(viewport);

        if (!treeScrollRect.horizontal && !treeScrollRect.vertical)
            treeScrollRect.velocity = Vector2.zero;
    }

    private bool ActiveTreeNeedsVerticalScroll(RectTransform viewport)
    {
        Transform treeRoot = GetTreeContentRoot(skillTree);
        if (viewport == null || treeRoot == null)
            return false;

        SkillTreeNodeView[] nodeViews = treeRoot.GetComponentsInChildren<SkillTreeNodeView>(true);
        if (nodeViews == null || nodeViews.Length == 0)
            return false;

        bool hasBounds = false;
        float minY = 0f;
        float maxY = 0f;
        Vector3[] corners = new Vector3[4];

        foreach (SkillTreeNodeView nodeView in nodeViews)
        {
            if (nodeView == null || nodeView.transform is not RectTransform rect)
                continue;

            rect.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                float y = viewport.InverseTransformPoint(corners[i]).y;
                if (!hasBounds)
                {
                    minY = y;
                    maxY = y;
                    hasBounds = true;
                    continue;
                }

                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (!hasBounds)
            return false;

        return maxY - minY > viewport.rect.height - 24f;
    }

    private Transform GetTreeContentRoot(SkillTreeDefinition tree)
    {
        Transform content = FindContentChild("TreeArea/Viewport/Content");
        if (content == null || tree == null)
            return null;

        Transform exact = content.Find(GetTreeContentName(tree));
        if (exact != null)
            return exact;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child != null && IsTreeContentFor(child, tree))
                return child;
        }

        return null;
    }

    private static bool IsTreeContentFor(Transform contentRoot, SkillTreeDefinition tree)
    {
        return contentRoot != null &&
               tree != null &&
               contentRoot.name == GetTreeContentName(tree);
    }

    private static string GetTreeContentName(SkillTreeDefinition tree)
    {
        string rawName = tree != null ? tree.TreeId : "Tree";
        return TreeContentPrefix + SanitizeName(rawName);
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Tree";

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                chars[i] = '_';
        }

        return new string(chars);
    }

    private static Sprite GetNodeIcon(SkillTreeNodeDefinition node)
    {
        if (node == null)
            return null;

        if (node.Icon != null)
            return node.Icon;

        return node.ActiveSkill != null ? node.ActiveSkill.Icon : null;
    }
}
