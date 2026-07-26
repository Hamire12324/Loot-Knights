using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHeroSkill : ButtonAbstract
{
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
    [SerializeField] private Color lockedColor = new(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color readyColor = Color.white;

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
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null) return;

        switch (mode)
        {
            case HeroSkillButtonMode.ElementAbsorb:
                hero.HeroSkillController.TryAbsorbElementConduit();
                break;
            case HeroSkillButtonMode.ElementRelease:
                if (hero.HeroSkillController.TryReleaseElementConduit())
                    RefreshStoredElementSlots();
                break;
            default:
                hero.HeroSkillController.TryCast(skillIndex);
                break;
        }
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
        CharacterSkillDefinition definition = runtime != null ? runtime.Definition : null;

        if (mode == HeroSkillButtonMode.ElementAbsorb)
        {
            LoadElementAbsorbIcon();
            ApplyExistingIcon(readyColor);
            RefreshCooldown();
            return;
        }

        if (mode == HeroSkillButtonMode.ElementRelease)
        {
            Color releaseColor = IsButtonUsable(runtime) ? readyColor : lockedColor;
            ApplyExistingIcon(releaseColor);
            RefreshCooldown();
            return;
        }

        Sprite sprite = definition != null ? definition.Icon : null;
        bool hasIcon = sprite != null;
        Color color = mode == HeroSkillButtonMode.Skill && !IsButtonUsable(runtime)
            ? lockedColor
            : readyColor;

        if (iconImages != null && iconImages.Length > 0)
        {
            foreach (Image iconImage in iconImages)
                ApplyIcon(iconImage, sprite, hasIcon, color);
        }
        else
        {
            ApplyIcon(icon, sprite, hasIcon, color);
        }

        RefreshCooldown();
    }

    private void RefreshCooldown()
    {
        CharacterSkillRuntime runtime = GetRuntime();
        float normalized = mode == HeroSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Normalized : 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = normalized;

        if (cooldownText == null) return;

        float remaining = mode == HeroSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    private CharacterSkillRuntime GetRuntime()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null) return null;

        return mode == HeroSkillButtonMode.Skill
            ? hero.HeroSkillController.GetSkill(skillIndex)
            : hero.HeroSkillController.GetSpecialSkill();
    }

    private bool IsButtonUsable(CharacterSkillRuntime runtime)
    {
        if (mode == HeroSkillButtonMode.ElementAbsorb)
            return runtime != null && runtime.IsUnlocked;

        if (mode == HeroSkillButtonMode.ElementRelease)
        {
            HeroCtrl hero = HeroCtrl.GetLocal();
            return runtime != null &&
                   runtime.IsUnlocked &&
                   hero != null &&
                   hero.HeroSkillController != null &&
                   hero.HeroSkillController.CanReleaseElementConduit();
        }

        return runtime != null && runtime.IsUnlocked;
    }

    private void LoadIcon()
    {
        if (HasValidIconImages()) return;

        List<Image> found = new();
        AddIconImage(found, icon);
        AddIconImage(found, FindChildComponentByName<Image>("Icon"));
        AddIconImage(found, FindChildComponentByName<Image>("IconSkill"));
        AddIconImage(found, FindChildComponentByName<Image>("SkillIcon"));

        iconImages = found.ToArray();

        if (icon == null && iconImages.Length > 0)
            icon = iconImages[0];
    }

    private void LoadElementAbsorbIcon()
    {
        Image namedIcon = FindChildComponentByName<Image>("Icon");
        if (namedIcon == null)
        {
            LoadIcon();
            return;
        }

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

    private static void ApplyIcon(Image target, Sprite sprite, bool hasIcon, Color color)
    {
        if (target == null) return;

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
