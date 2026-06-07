using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : BaseMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public event Action<InventorySlotUI> OnClicked;
    public event Action<InventorySlotUI, InventorySlotUI> OnDropped;

    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 dragPreviewSize = new(72f, 72f);
    [SerializeField] private float draggingSlotAlpha = 0.45f;

    private Text legacyNameText;
    private Text legacyAmountText;
    private Text legacyRarityText;
    private ItemDefinition currentItem;
    private EquipmentInstanceData currentEquipmentInstance;
    private int currentAmount;
    private int currentInventoryIndex = -1;
    private GameObject dragPreviewObject;
    private RectTransform dragPreviewRect;
    private Image dragPreviewIcon;
    private TextMeshProUGUI dragPreviewAmountText;
    private static InventorySlotUI draggingSlot;

    public ItemDefinition CurrentItem => currentItem;
    public EquipmentInstanceData CurrentEquipmentInstance => currentEquipmentInstance;
    public int CurrentAmount => currentAmount;
    public int CurrentInventoryIndex => currentInventoryIndex;
    public bool HasItem => currentItem != null && currentAmount > 0;
    public static InventorySlotUI DraggingSlot => draggingSlot;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadButton();
        LoadImageReferences();
        LoadTextReferences();
        LoadSelectionFrame();
        LoadRootCanvas();
        LoadCanvasGroup();
        BindButton();
    }

    public void Configure(
        Button slotButton,
        Image icon,
        TextMeshProUGUI itemName,
        TextMeshProUGUI amount,
        TextMeshProUGUI rarity,
        Image selectedFrame)
    {
        button = slotButton;
        iconImage = icon;
        nameText = itemName;
        amountText = amount;
        rarityText = rarity;
        selectionFrame = selectedFrame;
        BindButton();
    }

    public void SetItem(ItemDefinition item, int amount)
    {
        SetItem(item, amount, null);
    }

    public void SetItem(ItemDefinition item, int amount, EquipmentInstanceData equipmentInstance)
    {
        LoadComponents();

        if (item == null)
        {
            SetEmpty();
            return;
        }

        currentItem = item;
        currentEquipmentInstance = equipmentInstance;
        currentAmount = Mathf.Max(1, amount);

        SetInteractable(true);

        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = item.Icon != null;
        }

        if (nameText != null)
            nameText.text = item.DisplayName;

        if (legacyNameText != null)
            legacyNameText.text = item.DisplayName;

        if (rarityText != null)
        {
            rarityText.text = item.Rarity.ToString();
            rarityText.color = GetRarityColor(item.Rarity);
        }

        if (legacyRarityText != null)
        {
            legacyRarityText.text = item.Rarity.ToString();
            legacyRarityText.color = GetRarityColor(item.Rarity);
        }

        string amountValue = GetAmountTextValue(item, amount, equipmentInstance);

        if (amountText != null)
            amountText.text = amountValue;

        if (legacyAmountText != null)
            legacyAmountText.text = amountValue;

        SetActive(true);
    }

    public void SetInventoryIndex(int inventoryIndex)
    {
        currentInventoryIndex = inventoryIndex;
    }

    public void SetEmpty()
    {
        LoadComponents();

        currentItem = null;
        currentEquipmentInstance = null;
        currentAmount = 0;
        currentInventoryIndex = -1;
        SetInteractable(true);
        SetSelected(false);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (legacyNameText != null)
            legacyNameText.text = string.Empty;

        if (rarityText != null)
            rarityText.text = string.Empty;

        if (legacyRarityText != null)
            legacyRarityText.text = string.Empty;

        if (amountText != null)
            amountText.text = string.Empty;

        if (legacyAmountText != null)
            legacyAmountText.text = string.Empty;
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
            selectionFrame.enabled = selected;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (!HasItem) return;

        draggingSlot = this;
        SetSelected(true);
        SetDraggingVisual(true);
        CreateDragPreview();
        UpdateDragPreviewPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDragPreviewPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingSlot == this)
            draggingSlot = null;

        DestroyDragPreview();
        SetDraggingVisual(false);
        SetSelected(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggingSlot == null || draggingSlot == this) return;

        OnDropped?.Invoke(draggingSlot, this);
    }

    private void HandleClick()
    {
        OnClicked?.Invoke(this);
    }

    private void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private void SetDraggingVisual(bool dragging)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = dragging ? draggingSlotAlpha : 1f;
    }

    private void CreateDragPreview()
    {
        DestroyDragPreview();
        if (rootCanvas == null || currentItem == null || currentItem.Icon == null) return;

        dragPreviewObject = new GameObject("InventoryDragPreview", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragPreviewObject.transform.SetParent(rootCanvas.transform, false);
        dragPreviewObject.transform.SetAsLastSibling();

        CanvasGroup previewCanvasGroup = dragPreviewObject.GetComponent<CanvasGroup>();
        previewCanvasGroup.blocksRaycasts = false;
        previewCanvasGroup.interactable = false;
        previewCanvasGroup.alpha = 0.95f;

        dragPreviewRect = dragPreviewObject.GetComponent<RectTransform>();
        dragPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragPreviewRect.pivot = new Vector2(0.5f, 0.5f);
        dragPreviewRect.sizeDelta = GetDragPreviewSize();

        dragPreviewIcon = dragPreviewObject.GetComponent<Image>();
        dragPreviewIcon.sprite = currentItem.Icon;
        dragPreviewIcon.preserveAspect = true;
        dragPreviewIcon.raycastTarget = false;
        dragPreviewIcon.color = Color.white;

        if (currentAmount > 1)
            CreateDragPreviewAmountText();
    }

    private void CreateDragPreviewAmountText()
    {
        GameObject textObject = new("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(dragPreviewObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 2f);
        textRect.sizeDelta = new Vector2(0f, 24f);

        dragPreviewAmountText = textObject.GetComponent<TextMeshProUGUI>();
        dragPreviewAmountText.raycastTarget = false;
        dragPreviewAmountText.alignment = TextAlignmentOptions.BottomRight;
        dragPreviewAmountText.fontSize = 18f;
        dragPreviewAmountText.fontStyle = FontStyles.Bold;
        dragPreviewAmountText.color = Color.white;
        dragPreviewAmountText.outlineWidth = 0.25f;
        dragPreviewAmountText.outlineColor = Color.black;
        dragPreviewAmountText.text = currentAmount.ToString();
    }

    private Vector2 GetDragPreviewSize()
    {
        if (dragPreviewSize.x > 0f && dragPreviewSize.y > 0f)
            return dragPreviewSize;

        if (iconImage != null)
        {
            RectTransform iconRect = iconImage.rectTransform;
            if (iconRect.rect.width > 0f && iconRect.rect.height > 0f)
                return iconRect.rect.size;
        }

        return new Vector2(72f, 72f);
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData)
    {
        if (dragPreviewRect == null || rootCanvas == null || eventData == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
            return;

        dragPreviewRect.localPosition = localPoint;
    }

    private void DestroyDragPreview()
    {
        if (dragPreviewObject == null) return;

        Destroy(dragPreviewObject);
        dragPreviewObject = null;
        dragPreviewRect = null;
        dragPreviewIcon = null;
        dragPreviewAmountText = null;
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.75f, 0.78f, 0.82f),
            ItemRarity.Uncommon => new Color(0.35f, 0.88f, 0.48f),
            ItemRarity.Rare => new Color(0.36f, 0.62f, 1f),
            ItemRarity.Epic => new Color(0.75f, 0.43f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.72f, 0.25f),
            _ => Color.white
        };
    }

    private static string GetAmountTextValue(
        ItemDefinition item,
        int amount,
        EquipmentInstanceData equipmentInstance)
    {
        if (equipmentInstance != null && equipmentInstance.UpgradeLevel > 0)
            return "+" + equipmentInstance.UpgradeLevel;

        return item.MaxStack > 1 ? Mathf.Max(1, amount).ToString() : string.Empty;
    }

    private void LoadButton()
    {
        if (button != null) return;

        button = GetComponent<Button>();
        if (button != null) return;

        button = GetComponentInChildren<Button>(true);
    }

    private void BindButton()
    {
        if (button == null) return;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void LoadRootCanvas()
    {
        if (rootCanvas != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
            rootCanvas = parentCanvas.rootCanvas;
    }

    private void LoadCanvasGroup()
    {
        if (canvasGroup != null) return;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void LoadImageReferences()
    {
        if (iconImage != null) return;

        Image fallbackIcon = null;

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image == null) continue;
            string imageName = image.name.ToLowerInvariant();
            if (image == selectionFrame) continue;

            if (imageName == "icon")
            {
                iconImage = image;
                return;
            }

            if (fallbackIcon == null && image.transform != transform && imageName == "image")
                fallbackIcon = image;
        }

        iconImage = fallbackIcon;
    }

    private void LoadTextReferences()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI foundText in texts)
        {
            if (foundText == null) continue;

            string textName = foundText.name.ToLowerInvariant();

            if (nameText == null && (textName.Contains("name") || textName.Contains("title")))
            {
                nameText = foundText;
                continue;
            }

            if (amountText == null && (textName.Contains("amount") || textName.Contains("count") || textName.Contains("quantity")))
            {
                amountText = foundText;
                continue;
            }

            if (rarityText == null && textName.Contains("rarity"))
                rarityText = foundText;
        }

        if (amountText == null && texts.Length > 0)
            amountText = texts[0];

        Text[] legacyTexts = GetComponentsInChildren<Text>(true);

        foreach (Text foundText in legacyTexts)
        {
            if (foundText == null) continue;

            string textName = foundText.name.ToLowerInvariant();

            if (legacyNameText == null && (textName.Contains("name") || textName.Contains("title")))
            {
                legacyNameText = foundText;
                continue;
            }

            if (legacyAmountText == null && (textName.Contains("amount") || textName.Contains("count") || textName.Contains("quantity")))
            {
                legacyAmountText = foundText;
                continue;
            }

            if (legacyRarityText == null && textName.Contains("rarity"))
                legacyRarityText = foundText;
        }

        if (legacyAmountText == null && legacyTexts.Length > 0)
            legacyAmountText = legacyTexts[0];
    }

    private void LoadSelectionFrame()
    {
        if (selectionFrame != null) return;

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image == null) continue;

            string imageName = image.name.ToLowerInvariant();
            if (!imageName.Contains("select") && !imageName.Contains("frame") && !imageName.Contains("outline")) continue;

            selectionFrame = image;
            return;
        }
    }
}
