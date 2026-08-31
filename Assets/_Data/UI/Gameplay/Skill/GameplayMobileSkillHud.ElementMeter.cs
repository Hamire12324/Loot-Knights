using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class GameplayMobileSkillHud
{
    private const int ElementMeterSlotCount = 4;
    private const string ElementalIconSetResourcePath = "Element/ElementalIconSet";

    private static readonly Color ElementSlotOutlineColor = new(1f, 0.88f, 0.25f, 0.95f);
    private static readonly Color ElementStackTextColor = new(1f, 0.95f, 0.62f, 1f);

    [SerializeField] private ElementalIconSet elementalIconSet;

    private bool attemptedElementalIconSetLoad;

    private Transform[] elementMeterSlots;
    private Image[] elementMeterIconImages;
    private TMP_Text[] elementMeterStackTexts;
    private Outline[] elementMeterSelectionOutlines;
    private UnityAction[] elementMeterSlotClickActions;
    private RectTransform elementMeterRoot;
    private Image elementReleaseIconImage;

    private void LoadElementMeterReferences()
    {
        EnsureElementMeterCaches();

        RectTransform root = FindElementMeterRoot();
        if (root == null) return;

        for (int index = 0; index < ElementMeterSlotCount; index++)
        {
            CacheElementSlotReferences(index, root.Find($"ElementSlot_{index + 1}"));
        }
    }

    private void EnsureElementMeterCaches()
    {
        if (elementMeterSlots == null || elementMeterSlots.Length != ElementMeterSlotCount)
        {
            elementMeterSlots = new Transform[ElementMeterSlotCount];
        }

        if (elementMeterIconImages == null || elementMeterIconImages.Length != ElementMeterSlotCount)
        {
            elementMeterIconImages = new Image[ElementMeterSlotCount];
        }

        if (elementMeterStackTexts == null || elementMeterStackTexts.Length != ElementMeterSlotCount)
        {
            elementMeterStackTexts = new TMP_Text[ElementMeterSlotCount];
        }

        if (elementMeterSelectionOutlines == null || elementMeterSelectionOutlines.Length != ElementMeterSlotCount)
        {
            elementMeterSelectionOutlines = new Outline[ElementMeterSlotCount];
        }

        if (elementMeterSlotClickActions == null || elementMeterSlotClickActions.Length != ElementMeterSlotCount)
        {
            elementMeterSlotClickActions = new UnityAction[ElementMeterSlotCount];
        }
    }

    private void CacheElementSlotReferences(int index, Transform slot)
    {
        elementMeterSlots[index] = slot;
        elementMeterIconImages[index] = FindElementSlotIcon(slot);
        elementMeterStackTexts[index] ??= FindChildComponent<TMP_Text>(slot, "StackText");

        if (elementMeterSelectionOutlines[index] == null && slot != null)
        {
            elementMeterSelectionOutlines[index] = slot.GetComponent<Outline>();
        }
    }

    private void BindElementMeterSlotButtons()
    {
        LoadElementMeterReferences();

        for (int index = 0; index < ElementMeterSlotCount; index++)
        {
            Transform slot = GetElementSlot(index);
            if (slot == null) continue;

            if (slot.TryGetComponent(out Graphic graphic))
            {
                graphic.raycastTarget = true;
            }

            Button button = GetOrAddComponent<Button>(slot);
            UnityAction action = GetElementSlotClickAction(index);
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

    private UnityAction GetElementSlotClickAction(int slotIndex)
    {
        EnsureElementMeterCaches();
        return elementMeterSlotClickActions[slotIndex] ??= () => SelectElementSlot(slotIndex);
    }

    private void SelectElementSlot(int slotIndex)
    {
        ElementalConduitState state = GetLocalConduitState();
        if (state != null && state.SelectReleaseSlot(slotIndex))
        {
            RefreshElementMeter();
        }
    }

    private void LoadElementReleaseIconReference()
    {
        if (elementReleaseIconImage == null)
        {
            elementReleaseIconImage = FindReleaseButtonIcon(FindRect("Btn_Skill_ElementConduit"));
        }
    }

    public void ClearStoredElementSlots()
    {
        LoadElementMeterReferences();
        LoadElementReleaseIconReference();

        for (int index = 0; index < ElementMeterSlotCount; index++)
        {
            ClearElementSlotVisuals(index);
            SetStackText(index, 0);
            SetElementSlotSelected(index, false);
        }

        ClearElementIcon(elementReleaseIconImage);
    }

    public void RefreshElementMeterNow()
    {
        RefreshElementMeter();
    }

    private void RefreshElementMeter()
    {
        LoadElementMeterReferences();

        ElementalConduitState state = GetLocalConduitState();
        IReadOnlyList<ElementalConduitStoredElementView> storedElements = state != null
            ? state.GetStoredElements()
            : Array.Empty<ElementalConduitStoredElementView>();

        RefreshElementReleaseIcon(storedElements);

        for (int index = 0; index < ElementMeterSlotCount; index++)
        {
            if (index < storedElements.Count)
            {
                ElementalConduitStoredElementView stored = storedElements[index];
                SetElementSlotIcon(index, stored.Element);
                SetStackText(index, stored.Stacks);
                SetElementSlotSelected(index, state != null && state.IsReleaseSlotSelected(index));
            }
            else
            {
                SetElementSlotIcon(index, ElementType.None);
                SetStackText(index, 0);
                SetElementSlotSelected(index, false);
            }
        }
    }

    private void SetElementSlotSelected(int index, bool selected)
    {
        Outline outline = GetElementSlotOutline(index);
        if (outline == null) return;

        outline.enabled = selected;
        outline.effectColor = ElementSlotOutlineColor;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;
    }

    private Outline GetElementSlotOutline(int index)
    {
        if (!IsValidElementSlotIndex(index)) return null;

        Transform slot = GetElementSlot(index);
        if (slot == null) return null;

        EnsureElementMeterCaches();
        return elementMeterSelectionOutlines[index] ??= GetOrAddComponent<Outline>(slot);
    }

    private void SetElementSlotIcon(int index, ElementType element)
    {
        if (!IsValidElementSlotIndex(index)) return;

        ClearElementSlotVisuals(index);
        if (element != ElementType.None)
        {
            SetElementIcon(GetElementSlotIcon(index), GetElementIconSprite(element));
        }
    }

    private Image GetElementSlotIcon(int index)
    {
        if (!IsValidElementSlotIndex(index)) return null;

        EnsureElementMeterCaches();
        Image cachedIcon = elementMeterIconImages[index];
        if (cachedIcon != null) return cachedIcon;

        cachedIcon = FindElementSlotIcon(GetElementSlot(index));
        elementMeterIconImages[index] = cachedIcon;
        return cachedIcon;
    }

    private static void ClearElementIcon(Image icon)
    {
        SetElementIcon(icon, null);
    }

    private static void SetElementIcon(Image icon, Sprite sprite)
    {
        if (icon == null) return;

        bool isVisible = sprite != null;
        icon.sprite = sprite;
        icon.gameObject.SetActive(isVisible);
        icon.enabled = isVisible;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private Transform GetElementSlot(int index)
    {
        if (!IsValidElementSlotIndex(index)) return null;

        EnsureElementMeterCaches();
        if (elementMeterSlots[index] != null) return elementMeterSlots[index];

        RectTransform root = FindElementMeterRoot();
        if (root == null) return null;

        Transform slot = root.Find($"ElementSlot_{index + 1}");
        CacheElementSlotReferences(index, slot);
        return slot;
    }

    private void ClearElementSlotVisuals(int index)
    {
        Transform slot = GetElementSlot(index);
        if (slot == null)
        {
            ClearElementIcon(GetElementSlotIcon(index));
            return;
        }

        foreach (Image image in slot.GetComponentsInChildren<Image>(true))
        {
            if (image != null && IsElementSlotRuntimeIconImage(image))
            {
                ClearElementIcon(image);
            }
        }

        if (elementMeterIconImages != null && index < elementMeterIconImages.Length)
        {
            elementMeterIconImages[index] = FindElementSlotIcon(slot);
        }
    }

    private static bool IsElementSlotRuntimeIconImage(Image image)
    {
        if (image == null) return false;
        if (image.gameObject.name == "Icon") return true;

        Transform parent = image.transform.parent;
        return parent != null && parent.name == "IconMask" && image.gameObject.name.Contains("Icon");
    }

    private void RefreshElementReleaseIcon(IReadOnlyList<ElementalConduitStoredElementView> storedElements)
    {
        LoadElementReleaseIconReference();
        if (elementReleaseIconImage == null) return;

        if (storedElements == null || storedElements.Count == 0)
        {
            ClearElementIcon(elementReleaseIconImage);
            return;
        }

        if (TryGetUnlockedReleasePreview(out ElementalConduitReleasePayload preview))
        {
            ApplyElementReleaseIcon(preview.Reaction);
        }
        else
        {
            ClearElementIcon(elementReleaseIconImage);
        }
    }

    private void ApplyElementReleaseIcon(ElementalReactionType releaseReaction)
    {
        if (elementReleaseIconImage == null || releaseReaction == ElementalReactionType.None) return;

        Sprite releaseSprite = GetReactionIconSprite(releaseReaction);
        if (releaseSprite != null)
        {
            SetElementIcon(elementReleaseIconImage, releaseSprite);
        }
        else
        {
            ClearElementIcon(elementReleaseIconImage);
        }
    }

    private void SetStackText(int index, int stacks)
    {
        TMP_Text stackText = GetElementSlotStackText(index);
        if (stackText == null) return;

        bool hasStacks = stacks > 0;
        stackText.text = hasStacks ? $"x{stacks}" : string.Empty;
        stackText.gameObject.SetActive(hasStacks);

        if (hasStacks)
        {
            stackText.enabled = true;
            stackText.color = ElementStackTextColor;
            stackText.raycastTarget = false;
            stackText.transform.SetAsLastSibling();
        }
    }

    private TMP_Text GetElementSlotStackText(int index)
    {
        if (!IsValidElementSlotIndex(index)) return null;

        EnsureElementMeterCaches();
        return elementMeterStackTexts[index] ??= FindChildComponent<TMP_Text>(GetElementSlot(index), "StackText");
    }

    private RectTransform FindElementMeterRoot()
    {
        if (elementMeterRoot != null) return elementMeterRoot;

        if (transform.Find("ElementCoreMeter") is RectTransform directRoot)
        {
            return elementMeterRoot = directRoot;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == "ElementCoreMeter")
            {
                return elementMeterRoot = child as RectTransform;
            }
        }

        return null;
    }

    private static Image FindElementSlotIcon(Transform slot)
    {
        if (slot == null) return null;

        Transform icon = slot.Find("IconMask/Icon");
        if (icon != null && icon.TryGetComponent(out Image image)) return image;

        icon = slot.Find("Icon");
        return icon != null && icon.TryGetComponent(out image) ? image : null;
    }

    private static Image FindReleaseButtonIcon(Transform releaseButton)
    {
        if (releaseButton == null) return null;

        Transform icon = releaseButton.Find("IconMask/Icon");
        if (icon != null && icon.TryGetComponent(out Image image)) return image;

        icon = releaseButton.Find("Icon");
        if (icon != null && icon.TryGetComponent(out image)) return image;

        foreach (Transform child in releaseButton)
        {
            if (child != null && child.name != "ElementCoreMeter")
            {
                image = FindChildComponent<Image>(child, "Icon");
                if (image != null) return image;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null) return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == childName)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private static ElementalConduitState GetLocalConduitState()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        return hero != null ? hero.GetComponent<ElementalConduitState>() : null;
    }

    private static bool TryGetUnlockedReleasePreview(out ElementalConduitReleasePayload preview)
    {
        preview = default;

        HeroCtrl hero = HeroCtrl.GetLocal();
        return hero != null
               && hero.HeroSkillController != null
               && hero.HeroSkillController.TryGetElementalConduitReleasePreview(out preview);
    }

    private Sprite GetElementIconSprite(ElementType element)
    {
        LoadElementalIconSet();
        return elementalIconSet != null ? elementalIconSet.GetElementSprite(element) : null;
    }

    private Sprite GetReactionIconSprite(ElementalReactionType reaction)
    {
        LoadElementalIconSet();
        return elementalIconSet != null ? elementalIconSet.GetReactionSprite(reaction) : null;
    }

    private void LoadElementalIconSet()
    {
        if (elementalIconSet != null || attemptedElementalIconSetLoad) return;

        attemptedElementalIconSetLoad = true;
        elementalIconSet = Resources.Load<ElementalIconSet>(ElementalIconSetResourcePath);

        if (elementalIconSet == null)
        {
            Debug.LogWarning($"{nameof(GameplayMobileSkillHud)} could not load {nameof(ElementalIconSet)} at " +
                             $"Resources/{ElementalIconSetResourcePath}.", gameObject);
        }
    }

    private static bool IsValidElementSlotIndex(int index)
    {
        return index >= 0 && index < ElementMeterSlotCount;
    }
}
