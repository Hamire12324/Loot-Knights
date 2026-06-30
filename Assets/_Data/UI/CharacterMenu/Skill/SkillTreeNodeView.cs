using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTreeNodeView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SkillTreeNodeDefinition definition;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject availableGlow;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text costText;

    private SkillTreeView owner;

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
        BindButton();
    }

    public void Refresh(SkillTreeRuntime runtime, bool selected, int playerLevel)
    {
        if (definition == null || runtime == null)
            return;

        int rank = runtime.GetRank(definition);
        bool canUpgrade = runtime.CanUpgrade(definition, playerLevel, out _);
        bool maxed = rank >= definition.MaxRank;

        if (iconImage != null)
        {
            Sprite icon = definition.Icon != null
                ? definition.Icon
                : definition.ActiveSkill != null ? definition.ActiveSkill.Icon : null;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (rankText != null)
            rankText.text = $"{rank}/{definition.MaxRank}";

        if (costText != null)
            costText.text = definition.PointCost.ToString();

        if (availableGlow != null)
            availableGlow.SetActive(canUpgrade && !maxed);

        if (selectedFrame != null)
            selectedFrame.SetActive(selected);

        if (lockOverlay != null)
            lockOverlay.SetActive(rank <= 0 && !canUpgrade);

        if (button != null)
            button.interactable = true;
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

        if (costText == null)
        {
            Transform cost = transform.Find("CostText");
            if (cost != null)
                costText = cost.GetComponent<TMP_Text>();
        }
    }

    private GameObject FindChild(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
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
}
