using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTreeNodeView : MonoBehaviour, IPointerClickHandler
{
    private const float RankFontSize = 24f;
    private static readonly Color RankOutlineColor = new(0.04f, 0.02f, 0.1f, 1f);

    [SerializeField] private SkillTreeNodeDefinition definition;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject availableGlow;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text costText;

    private SkillTreeView owner;
    private Color defaultRankTextColor = Color.white;
    private Color defaultCostTextColor = Color.white;
    private bool capturedDefaultTextColors;
    public SkillTreeNodeDefinition Definition => definition;

    private void Awake()
    {
        LoadComponents();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(SkillTreeView treeView, SkillTreeNodeDefinition nodeDefinition)
    {
        owner = treeView;
        definition = nodeDefinition;
        LoadComponents();
        ApplyElementNodeChrome();
        BindButton();
    }

    public void Render(SkillTreeNodeViewState state)
    {
        if (state == null || state.Definition == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = state.Icon;
            iconImage.enabled = state.Icon != null;
            iconImage.color = GetIconColor(state);
        }

        if (rankText != null)
        {
            ApplyRankTextStyle();
            rankText.text = $"{state.Rank}/{state.MaxRank}";
            rankText.color = GetTextColor(defaultRankTextColor, state);
        }

        if (costText != null)
        {
            costText.text = state.PointCost.ToString();
            costText.color = GetTextColor(defaultCostTextColor, state);
        }

        if (availableGlow != null)
            availableGlow.SetActive(state.CanUpgrade && !state.IsMaxed);

        if (selectedFrame != null)
            selectedFrame.SetActive(state.Selected);

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        if (button != null)
            button.interactable = true;

        ApplyElementNodeChrome();
    }

    private void LoadComponents()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
        {
            Transform icon = transform.Find("IconMask/Icon");
            if (icon != null)
                iconImage = icon.GetComponent<Image>();
        }

        availableGlow ??= FindChild("AvailableGlow");
        selectedFrame ??= FindChild("SelectedFrame");
        lockOverlay ??= FindChild("LockOverlay");

        if (rankText == null)
        {
            Transform rank = transform.Find("RankText");
            if (rank != null)
                rankText = rank.GetComponent<TMP_Text>();
        }

        ApplyRankTextStyle();

        if (costText == null)
        {
            Transform cost = transform.Find("CostText");
            if (cost != null)
                costText = cost.GetComponent<TMP_Text>();
        }

        CaptureDefaultColors();
    }

    private GameObject FindChild(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void ApplyElementNodeChrome()
    {
        if (!IsElementNode(definition))
            return;

        SetChildActive("ElementGlow", false);
        SetChildActive("IconBack", false);
    }

    private void SetChildActive(string childName, bool active)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (owner == null)
            owner = GetComponentInParent<SkillTreeView>();

        owner?.Select(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleClick();
    }

    private void CaptureDefaultColors()
    {
        if (capturedDefaultTextColors)
            return;

        if (rankText != null)
            defaultRankTextColor = rankText.color;

        if (costText != null)
            defaultCostTextColor = costText.color;

        capturedDefaultTextColors = true;
    }

    private void ApplyRankTextStyle()
    {
        if (rankText == null)
            return;

        rankText.enableAutoSizing = false;
        rankText.fontSize = RankFontSize;
        rankText.fontStyle |= FontStyles.Bold;
        rankText.alignment = TextAlignmentOptions.Center;
        rankText.outlineColor = RankOutlineColor;
        rankText.outlineWidth = 0.18f;
        rankText.raycastTarget = false;

        if (rankText.transform is RectTransform rect)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 72f), Mathf.Max(rect.sizeDelta.y, 32f));
        }
    }

    private static Color GetIconColor(SkillTreeNodeViewState state)
    {
        if (state == null)
            return Color.white;

        if (state.IsLocked)
            return new Color(1f, 1f, 1f, 0.38f);

        return Color.white;
    }

    private static bool IsElementNode(SkillTreeNodeDefinition node)
    {
        return node != null &&
               (node.Element != ElementType.None ||
                node.Reaction != ElementalReactionType.None);
    }

    private static Color GetTextColor(Color defaultColor, SkillTreeNodeViewState state)
    {
        if (state == null)
            return defaultColor;

        if (state.IsLocked)
            return new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.45f);

        if (state.CanUpgrade && !state.IsMaxed)
            return Color.Lerp(defaultColor, Color.white, 0.25f);

        return defaultColor;
    }
}
