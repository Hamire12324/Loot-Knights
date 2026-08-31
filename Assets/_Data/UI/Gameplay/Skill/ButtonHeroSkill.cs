using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHeroSkill : ButtonAbstract
{
    private const float DefaultManualAimRange = 6f;
    private const float ClickSuppressionDuration = 0.15f;
    private const float CooldownTextThreshold = 0.05f;

    private static readonly string[] IconObjectNames = { "Icon", "IconSkill", "SkillIcon" };

    private enum HeroSkillButtonMode
    {
        Skill,
        ElementAbsorb,
        ElementRelease
    }

    [SerializeField] private HeroSkillButtonMode mode;
    [SerializeField, Min(0)] private int skillIndex;
    [SerializeField] private Image icon;
    [SerializeField] private Image[] iconImages;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text ultimateChargeText;
    [SerializeField] private Color lockedColor = new(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color readyColor = Color.white;

    private float suppressClickUntil;

    public bool SupportsManualAim =>
        mode != HeroSkillButtonMode.ElementAbsorb && GetRuntime()?.Definition?.SupportsManualAim == true;

    public float ManualAimRange => GetRuntime()?.Definition?.ManualAimRange ?? DefaultManualAimRange;

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    protected override void Update()
    {
        base.Update();
        Refresh();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadIcon();
        LoadCooldownOverlay();
        LoadCooldownText();
    }

    [ContextMenu("Load UI References")]
    private void LoadUIReferencesFromContextMenu()
    {
        LoadComponents();
    }

    protected override void OnClick()
    {
        if (Time.unscaledTime <= suppressClickUntil)
            return;

        HeroSkillController skillController = GetLocalSkillController();
        if (skillController == null)
            return;

        switch (mode)
        {
            case HeroSkillButtonMode.ElementAbsorb:
                skillController.TryAbsorbElementConduit();
                return;
            case HeroSkillButtonMode.ElementRelease:
                if (skillController.TryReleaseElementConduit())
                    RefreshStoredElementSlots();
                return;
            default:
                skillController.TryCast(skillIndex);
                return;
        }
    }

    public bool TryCastAtPosition(Vector2 targetPosition)
    {
        HeroSkillController skillController = GetLocalSkillController();
        if (skillController == null || !SupportsManualAim)
            return false;

        return mode == HeroSkillButtonMode.ElementRelease
            ? skillController.TryReleaseElementConduitAtPosition(targetPosition)
            : skillController.TryCastAtPosition(skillIndex, targetPosition);
    }

    public void SuppressClickForCurrentGesture()
    {
        suppressClickUntil = Time.unscaledTime + ClickSuppressionDuration;
    }

    public void SetSkillIndex(int index)
    {
        mode = HeroSkillButtonMode.Skill;
        skillIndex = Mathf.Max(0, index);
        Refresh();
    }

    public void SetSpecialSkill()
    {
        SetElementRelease();
    }

    public void SetElementAbsorb()
    {
        mode = HeroSkillButtonMode.ElementAbsorb;
        Refresh();
    }

    public void SetElementRelease()
    {
        mode = HeroSkillButtonMode.ElementRelease;
        Refresh();
    }

    private void Refresh()
    {
        LoadIcon();

        CharacterSkillRuntime runtime = GetRuntime();
        switch (mode)
        {
            case HeroSkillButtonMode.ElementAbsorb:
                LoadElementAbsorbIcon();
                ApplyExistingIcon(readyColor);
                break;
            case HeroSkillButtonMode.ElementRelease:
                ApplyExistingIcon(IsButtonUsable(runtime) ? readyColor : lockedColor);
                break;
            default:
                RefreshSkillIcon(runtime);
                RefreshUltimateCharge(runtime);
                break;
        }

        RefreshCooldown(runtime);
    }

    private void RefreshSkillIcon(CharacterSkillRuntime runtime)
    {
        Sprite sprite = runtime?.Definition?.Icon;
        Color color = IsButtonUsable(runtime) ? readyColor : lockedColor;

        if (iconImages != null && iconImages.Length > 0)
        {
            foreach (Image iconImage in iconImages)
                ApplyIcon(iconImage, sprite, color);
        }
        else
        {
            ApplyIcon(icon, sprite, color);
        }
    }

    private void RefreshCooldown(CharacterSkillRuntime runtime)
    {
        float normalized = mode == HeroSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Normalized : 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = normalized;

        if (cooldownText == null) return;

        float remaining = mode == HeroSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > CooldownTextThreshold ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    private void RefreshUltimateCharge(CharacterSkillRuntime runtime)
    {
        string resourceId = GetConsumedResourceId(runtime);
        bool shouldShow = !string.IsNullOrWhiteSpace(resourceId);
        if (!shouldShow)
        {
            if (ultimateChargeText != null)
                ultimateChargeText.gameObject.SetActive(false);
            return;
        }

        LoadUltimateChargeText();

        if (ultimateChargeText == null)
            return;

        HeroCtrl hero = HeroCtrl.GetLocal();
        int charges = hero != null ? CharacterSkillResource.GetValue(hero, resourceId) : 0;
        ultimateChargeText.text = charges.ToString();
        ultimateChargeText.gameObject.SetActive(true);
    }

    private static string GetConsumedResourceId(CharacterSkillRuntime runtime)
    {
        if (runtime?.Definition?.Effects == null)
            return null;

        foreach (CharacterSkillEffectDefinition effect in runtime.Definition.Effects)
        {
            if (effect is ICharacterSkillResourceConsumer consumer && !string.IsNullOrWhiteSpace(consumer.ResourceId))
                return consumer.ResourceId;
        }

        return null;
    }

    private CharacterSkillRuntime GetRuntime()
    {
        HeroSkillController skillController = GetLocalSkillController();
        if (skillController == null)
            return null;

        return mode == HeroSkillButtonMode.Skill
            ? skillController.GetSkill(skillIndex)
            : skillController.GetSpecialSkill();
    }

    private bool IsButtonUsable(CharacterSkillRuntime runtime)
    {
        if (runtime == null || !runtime.IsUnlocked)
            return false;

        return mode != HeroSkillButtonMode.ElementRelease || GetLocalSkillController()?.CanReleaseElementConduit() == true;
    }

    private static HeroSkillController GetLocalSkillController()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        return hero != null ? hero.HeroSkillController : null;
    }

    private void LoadIcon()
    {
        if (HasValidIconImages())
            return;

        List<Image> found = new();
        AddIconImage(found, icon);
        foreach (string iconObjectName in IconObjectNames)
            AddIconImage(found, FindChildComponentByName<Image>(iconObjectName));

        iconImages = found.ToArray();

        if (icon == null && iconImages.Length > 0)
            icon = iconImages[0];
    }

    private void LoadElementAbsorbIcon()
    {
        Image namedIcon = FindChildComponentByName<Image>("Icon");
        if (namedIcon == null)
            return;

        icon = namedIcon;
        iconImages = new[] { namedIcon };
    }

    private bool HasValidIconImages()
    {
        if (iconImages == null || iconImages.Length == 0)
            return false;

        foreach (Image iconImage in iconImages)
        {
            if (iconImage != null)
                return true;
        }

        return false;
    }

    private static void AddIconImage(List<Image> images, Image image)
    {
        if (image == null || images.Contains(image)) return;

        images.Add(image);
    }

    private static void ApplyIcon(Image target, Sprite sprite, Color color)
    {
        if (target == null)
            return;

        bool hasIcon = sprite != null;
        target.sprite = sprite;
        target.enabled = hasIcon;
        target.color = hasIcon ? color : Color.clear;
        target.preserveAspect = true;
        target.raycastTarget = false;
    }

    private void ApplyExistingIcon(Color color)
    {
        if (iconImages != null && iconImages.Length > 0)
        {
            foreach (Image iconImage in iconImages)
                ApplyExistingIcon(iconImage, color);
        }
        else
        {
            ApplyExistingIcon(icon, color);
        }
    }

    private void RefreshStoredElementSlots()
    {
        GameplayMobileSkillHud hud = GetComponentInParent<GameplayMobileSkillHud>();
        if (hud == null)
            hud = FindAnyObjectByType<GameplayMobileSkillHud>();

        hud?.RefreshElementMeterNow();
    }

    private static void ApplyExistingIcon(Image target, Color color)
    {
        if (target == null || target.sprite == null) return;

        target.enabled = true;
        target.color = color;
        target.preserveAspect = true;
        target.raycastTarget = false;
    }

    private void LoadCooldownOverlay()
    {
        if (cooldownOverlay != null) return;

        cooldownOverlay = FindChildComponentByName<Image>("CooldownOverlay");
    }

    private void LoadCooldownText()
    {
        if (cooldownText != null) return;

        cooldownText = FindChildComponentByName<TMP_Text>("CooldownText");
    }

    private void LoadUltimateChargeText()
    {
        if (ultimateChargeText != null)
            return;

        ultimateChargeText = FindChildComponentByName<TMP_Text>("UltimateChargeText");
        if (ultimateChargeText != null)
            return;

        GameObject textObject = new("UltimateChargeText");
        textObject.transform.SetParent(transform, false);
        ultimateChargeText = textObject.AddComponent<TextMeshProUGUI>();
        ultimateChargeText.alignment = TextAlignmentOptions.Center;
        ultimateChargeText.fontSize = 24f;
        ultimateChargeText.fontStyle = FontStyles.Bold;
        ultimateChargeText.color = new Color(1f, 0.95f, 0.55f, 1f);
        ultimateChargeText.raycastTarget = false;

        RectTransform rect = ultimateChargeText.rectTransform;
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(8f, 0f);
        rect.sizeDelta = new Vector2(42f, 30f);
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null || child == transform) continue;
            if (child.name != childName) continue;

            return child.GetComponent<T>();
        }

        return null;
    }
}
