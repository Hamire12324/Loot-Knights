using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMobileSkillHud : BaseMonoBehaviour
{
    private const int ElementMeterSlotCount = 4;

    [SerializeField] private ElementalIconSet elementalIconSet;

    private Transform[] elementMeterSlots;
    private Image[] elementMeterIconImages;
    private TMP_Text[] elementMeterStackTexts;
    private Outline[] elementMeterSelectionOutlines;
    private Image elementReleaseIconImage;

    protected override void Start()
    {
        base.Start();

        BindExistingButtons();
        LoadElementMeterReferences();
        BindElementMeterSlotButtons();
        LoadElementReleaseIconReference();
        RefreshElementMeter();
    }

    protected override void Update()
    {
        base.Update();

        if (Application.isPlaying)
            RefreshElementMeter();
    }

    private void BindExistingButtons()
    {
        BindAttackButton(FindRect("Btn_Attack_Basic", "Btn_Attack"));
        BindSkillButton(FindRect("Btn_Skill_GroundWave", "Btn_Skill"), 0);
        BindSkillButton(FindRect("Btn_Skill_IronGuard", "Btn_Skill_ShieldBash", "Btn_Skill (1)"), 1);
        BindSkillButton(FindRect("Btn_Skill_Whirlwind", "Btn_Skill (2)"), 2);
        BindSkillButton(FindRect("Btn_Skill_ChargeStrike", "Btn_Skill (3)"), 3);
        BindElementButton(FindRect("Btn_Skill_ElementConduit"), release: true);
        BindElementButton(FindRect("Btn_ElementAbsorb"), release: false);
        BindAddAllElementsButton(FindRect("Btn_AddAllElements", "Button_AddAllElements"));
    }

    private static void BindAttackButton(RectTransform rect)
    {
        if (rect == null)
            return;

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        if (rect.GetComponent<ButtonAttack>() == null)
            rect.gameObject.AddComponent<ButtonAttack>();
    }

    private static void BindSkillButton(RectTransform rect, int skillIndex)
    {
        if (rect == null)
            return;

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        ButtonHeroSkill buttonHeroSkill = rect.GetComponent<ButtonHeroSkill>();
        if (buttonHeroSkill == null)
            buttonHeroSkill = rect.gameObject.AddComponent<ButtonHeroSkill>();

        buttonHeroSkill.SetSkillIndex(skillIndex);
        ConfigureAimInput(buttonHeroSkill);
    }

    private static void BindElementButton(RectTransform rect, bool release)
    {
        if (rect == null)
            return;

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        ButtonHeroSkill buttonHeroSkill = rect.GetComponent<ButtonHeroSkill>();
        if (buttonHeroSkill == null)
            buttonHeroSkill = rect.gameObject.AddComponent<ButtonHeroSkill>();

        if (release)
            buttonHeroSkill.SetElementRelease();
        else
            buttonHeroSkill.SetElementAbsorb();

        ConfigureAimInput(buttonHeroSkill);
    }

    private static void ConfigureAimInput(ButtonHeroSkill skillButton)
    {
        if (skillButton == null)
            return;

        MobileSkillAimInput aimInput = skillButton.GetComponent<MobileSkillAimInput>();
        if (aimInput == null)
            aimInput = skillButton.gameObject.AddComponent<MobileSkillAimInput>();

        aimInput.SetSkillButton(skillButton);
    }

    private static void BindAddAllElementsButton(RectTransform rect)
    {
        if (rect == null)
            return;

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        if (rect.GetComponent<ButtonAddAllElements>() == null)
            rect.gameObject.AddComponent<ButtonAddAllElements>();
    }

    private void LoadElementMeterReferences()
    {
        elementMeterSlots ??= new Transform[ElementMeterSlotCount];
        elementMeterIconImages ??= new Image[ElementMeterSlotCount];
        elementMeterStackTexts ??= new TMP_Text[ElementMeterSlotCount];
        elementMeterSelectionOutlines ??= new Outline[ElementMeterSlotCount];

        RectTransform root = FindElementMeterRoot();
        if (root == null)
            return;

        for (int i = 0; i < ElementMeterSlotCount; i++)
        {
            Transform slot = root.Find($"ElementSlot_{i + 1}");
            elementMeterSlots[i] = slot;

            elementMeterIconImages[i] = FindElementSlotIcon(slot);

            if (elementMeterStackTexts[i] == null)
                elementMeterStackTexts[i] = FindChildComponent<TMP_Text>(slot, "StackText");

            if (elementMeterSelectionOutlines[i] == null && slot != null)
                elementMeterSelectionOutlines[i] = slot.GetComponent<Outline>();
        }
    }

    private void BindElementMeterSlotButtons()
    {
        LoadElementMeterReferences();

        for (int i = 0; i < ElementMeterSlotCount; i++)
        {
            Transform slot = GetElementSlot(i);
            if (slot == null)
                continue;

            if (slot.TryGetComponent(out Graphic graphic))
                graphic.raycastTarget = true;

            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.gameObject.AddComponent<Button>();

            int slotIndex = i;
            button.onClick.RemoveListener(() => SelectElementSlot(slotIndex));
            button.onClick.AddListener(() => SelectElementSlot(slotIndex));
        }
    }

    private void SelectElementSlot(int slotIndex)
    {
        ElementalConduitState state = GetLocalConduitState();
        if (state == null)
            return;

        if (state.SelectReleaseSlot(slotIndex))
            RefreshElementMeter();
    }

    private void LoadElementReleaseIconReference()
    {
        if (elementReleaseIconImage != null)
            return;

        RectTransform releaseButton = FindRect("Btn_Skill_ElementConduit");
        elementReleaseIconImage = FindReleaseButtonIcon(releaseButton);
    }

    public void ClearStoredElementSlots()
    {
        LoadElementMeterReferences();
        LoadElementReleaseIconReference();

        for (int i = 0; i < ElementMeterSlotCount; i++)
        {
            ClearElementSlotVisuals(i);
            SetStackText(i, 0);
            SetElementSlotSelected(i, false);
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

        RefreshElementReleaseIcon(state, storedElements);

        for (int i = 0; i < ElementMeterSlotCount; i++)
        {
            if (i < storedElements.Count)
            {
                ElementalConduitStoredElementView stored = storedElements[i];
                SetElementSlotIcon(i, stored.Element);
                SetStackText(i, stored.Stacks);
                SetElementSlotSelected(i, state != null && state.IsReleaseSlotSelected(i));
            }
            else
            {
                SetElementSlotIcon(i, ElementType.None);
                SetStackText(i, 0);
                SetElementSlotSelected(i, false);
            }
        }
    }

    private void SetElementSlotSelected(int index, bool selected)
    {
        if (index < 0 || index >= ElementMeterSlotCount)
            return;

        Transform slot = GetElementSlot(index);
        if (slot == null)
            return;

        if (elementMeterSelectionOutlines == null || elementMeterSelectionOutlines.Length != ElementMeterSlotCount)
            elementMeterSelectionOutlines = new Outline[ElementMeterSlotCount];

        Outline outline = elementMeterSelectionOutlines[index];
        if (outline == null)
        {
            outline = slot.GetComponent<Outline>();
            if (outline == null)
                outline = slot.gameObject.AddComponent<Outline>();

            elementMeterSelectionOutlines[index] = outline;
        }

        outline.enabled = selected;
        outline.effectColor = new Color(1f, 0.88f, 0.25f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;
    }

    private void SetElementSlotIcon(int index, ElementType element)
    {
        if (index < 0 || index >= ElementMeterSlotCount)
        {
            return;
        }

        if (element == ElementType.None)
        {
            ClearElementSlotVisuals(index);
            return;
        }

        ClearElementSlotVisuals(index);

        Image icon = GetElementSlotIcon(index);
        if (icon == null)
            return;

        Sprite sprite = GetElementIconSprite(element);
        if (sprite == null)
            return;

        icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
        icon.enabled = sprite != null;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private Image GetElementSlotIcon(int index)
    {
        if (index < 0 || index >= ElementMeterSlotCount)
        {
            return null;
        }

        if (elementMeterIconImages == null || elementMeterIconImages.Length != ElementMeterSlotCount)
            elementMeterIconImages = new Image[ElementMeterSlotCount];

        Image cachedIcon = elementMeterIconImages[index];
        if (cachedIcon != null)
            return cachedIcon;

        Transform slot = GetElementSlot(index);
        cachedIcon = FindElementSlotIcon(slot);
        elementMeterIconImages[index] = cachedIcon;
        return cachedIcon;
    }

    private static void ClearElementIcon(Image icon)
    {
        if (icon == null)
            return;

        icon.sprite = null;
        icon.gameObject.SetActive(false);
        icon.enabled = false;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private Transform GetElementSlot(int index)
    {
        if (index < 0 || index >= ElementMeterSlotCount)
            return null;

        if (elementMeterSlots != null &&
            index < elementMeterSlots.Length &&
            elementMeterSlots[index] != null)
        {
            return elementMeterSlots[index];
        }

        RectTransform root = FindElementMeterRoot();
        if (root == null)
            return null;

        elementMeterSlots ??= new Transform[ElementMeterSlotCount];
        Transform slot = root.Find($"ElementSlot_{index + 1}");
        elementMeterSlots[index] = slot;
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
            if (image == null || !IsElementSlotRuntimeIconImage(image))
                continue;

            ClearElementIcon(image);
        }

        if (elementMeterIconImages != null && index < elementMeterIconImages.Length)
            elementMeterIconImages[index] = FindElementSlotIcon(slot);
    }

    private static bool IsElementSlotRuntimeIconImage(Image image)
    {
        if (image == null)
            return false;

        if (image.gameObject.name == "Icon")
            return true;

        Transform parent = image.transform.parent;
        return parent != null && parent.name == "IconMask" && image.gameObject.name.Contains("Icon");
    }

    private void RefreshElementReleaseIcon(
        ElementalConduitState state,
        IReadOnlyList<ElementalConduitStoredElementView> storedElements)
    {
        LoadElementReleaseIconReference();

        if (elementReleaseIconImage == null)
            return;

        if (storedElements == null || storedElements.Count == 0)
        {
            ClearElementIcon(elementReleaseIconImage);
            return;
        }

        if (TryGetUnlockedReleasePreview(out ElementalConduitReleasePayload preview))
        {
            ApplyElementReleaseIcon(preview.Reaction);
            return;
        }

        ClearElementIcon(elementReleaseIconImage);
    }

    private void ApplyElementReleaseIcon(ElementalReactionType releaseReaction)
    {
        if (elementReleaseIconImage == null || releaseReaction == ElementalReactionType.None)
            return;

        Sprite releaseSprite = GetReactionIconSprite(releaseReaction);
        Color releaseColor = Color.white;

        if (releaseSprite == null)
        {
            ClearElementIcon(elementReleaseIconImage);
            return;
        }

        elementReleaseIconImage.sprite = releaseSprite;
        elementReleaseIconImage.gameObject.SetActive(true);
        elementReleaseIconImage.enabled = true;
        elementReleaseIconImage.color = releaseColor;
        elementReleaseIconImage.preserveAspect = true;
        elementReleaseIconImage.raycastTarget = false;
    }

    private void SetStackText(int index, int stacks)
    {
        if (index < 0 || index >= ElementMeterSlotCount)
        {
            return;
        }

        if (elementMeterStackTexts == null || elementMeterStackTexts.Length != ElementMeterSlotCount)
            elementMeterStackTexts = new TMP_Text[ElementMeterSlotCount];

        if (elementMeterStackTexts[index] == null)
            elementMeterStackTexts[index] = FindChildComponent<TMP_Text>(GetElementSlot(index), "StackText");

        if (elementMeterStackTexts[index] == null)
            return;

        TMP_Text stackText = elementMeterStackTexts[index];
        stackText.text = stacks > 0 ? $"x{stacks}" : "";
        stackText.gameObject.SetActive(stacks > 0);

        if (stacks > 0)
        {
            stackText.enabled = true;
            stackText.color = new Color(1f, 0.95f, 0.62f, 1f);
            stackText.raycastTarget = false;
            stackText.transform.SetAsLastSibling();
        }
    }

    private RectTransform FindElementMeterRoot()
    {
        Transform direct = transform.Find("ElementCoreMeter");
        if (direct is RectTransform directRect)
            return directRect;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == "ElementCoreMeter")
                return child as RectTransform;
        }

        return null;
    }

    private RectTransform FindRect(params string[] objectNames)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            foreach (string objectName in objectNames)
            {
                if (child.name == objectName)
                    return child as RectTransform;
            }
        }

        return null;
    }

    private static Image FindElementSlotIcon(Transform slot)
    {
        if (slot == null)
            return null;

        Transform icon = slot.Find("IconMask/Icon");
        if (icon != null && icon.TryGetComponent(out Image image))
            return image;

        icon = slot.Find("Icon");
        if (icon != null && icon.TryGetComponent(out image))
            return image;

        return null;
    }

    private static Image FindReleaseButtonIcon(Transform releaseButton)
    {
        if (releaseButton == null)
            return null;

        Transform icon = releaseButton.Find("IconMask/Icon");
        if (icon != null && icon.TryGetComponent(out Image image))
            return image;

        icon = releaseButton.Find("Icon");
        if (icon != null && icon.TryGetComponent(out image))
            return image;

        foreach (Transform child in releaseButton)
        {
            if (child == null || child.name == "ElementCoreMeter")
                continue;

            image = FindChildComponent<Image>(child, "Icon");
            if (image != null)
                return image;
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == null || child.name != childName)
                continue;

            return child.GetComponent<T>();
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
        return hero != null &&
               hero.HeroSkillController != null &&
               hero.HeroSkillController.TryGetElementalConduitReleasePreview(out preview);
    }

    private Sprite GetElementIconSprite(ElementType element)
    {
        return elementalIconSet != null ? elementalIconSet.GetElementSprite(element) : null;
    }

    private Sprite GetReactionIconSprite(ElementalReactionType reaction)
    {
        return elementalIconSet != null ? elementalIconSet.GetReactionSprite(reaction) : null;
    }

}

public sealed class ButtonAddAllElements : ButtonAbstract
{
    protected override void OnClick()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null)
            return;

        if (!hero.HeroSkillController.AddAllElementConduitForTesting())
            return;

        GameplayMobileSkillHud hud = GetComponentInParent<GameplayMobileSkillHud>();
        if (hud == null)
            hud = FindAnyObjectByType<GameplayMobileSkillHud>();

        hud?.RefreshElementMeterNow();
    }
}
