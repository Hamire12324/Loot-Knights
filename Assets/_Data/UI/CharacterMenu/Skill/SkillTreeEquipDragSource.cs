using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTreeEquipDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private SkillTreeView owner;
    [SerializeField] private Image iconImage;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 dragPreviewSize = new(72f, 72f);
    [SerializeField] private float draggingAlpha = 0.55f;

    private GameObject dragPreviewObject;
    private RectTransform dragPreviewRect;
    private bool completedByDrop;
    private bool dragActive;

    public static SkillTreeEquipDragSource DraggingSource { get; private set; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        LoadComponents();
        if (owner == null || !owner.TryBeginEquipDrag())
            return;

        DraggingSource = this;
        completedByDrop = false;
        dragActive = true;
        SetDraggingVisual(true);
        CreateDragPreview();
        UpdateDragPreviewPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DraggingSource != this)
            return;

        UpdateDragPreviewPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragActive)
            return;

        dragActive = false;

        if (DraggingSource == this)
            DraggingSource = null;

        DestroyDragPreview();
        SetDraggingVisual(false);

        if (!completedByDrop)
            owner?.CancelEquipDrag();
    }

    public void MarkDropped()
    {
        completedByDrop = true;
    }

    private void LoadComponents()
    {
        if (owner == null)
            owner = GetComponentInParent<SkillTreeView>(true);

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null)
                iconImage = icon.GetComponent<Image>();
        }

        if (rootCanvas == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
                rootCanvas = parentCanvas.rootCanvas;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void SetDraggingVisual(bool dragging)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = dragging ? draggingAlpha : 1f;
    }

    private void CreateDragPreview()
    {
        DestroyDragPreview();
        if (rootCanvas == null || iconImage == null || iconImage.sprite == null)
            return;

        dragPreviewObject = new GameObject("SkillEquipDragPreview", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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

        Image previewImage = dragPreviewObject.GetComponent<Image>();
        previewImage.sprite = iconImage.sprite;
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;
        previewImage.color = Color.white;
    }

    private Vector2 GetDragPreviewSize()
    {
        if (dragPreviewSize.x > 0f && dragPreviewSize.y > 0f)
            return dragPreviewSize;

        if (iconImage != null && iconImage.rectTransform.rect.width > 0f && iconImage.rectTransform.rect.height > 0f)
            return iconImage.rectTransform.rect.size;

        return new Vector2(72f, 72f);
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData)
    {
        if (dragPreviewRect == null || rootCanvas == null || eventData == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
            dragPreviewRect.localPosition = localPoint;
    }

    private void DestroyDragPreview()
    {
        if (dragPreviewObject == null)
            return;

        Destroy(dragPreviewObject);
        dragPreviewObject = null;
        dragPreviewRect = null;
    }
}
