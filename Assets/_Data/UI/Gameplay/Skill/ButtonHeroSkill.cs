using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHeroSkill : ButtonAbstract
{
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

        hero.HeroSkillController.TryCast(skillIndex);
    }

    public void SetSkillIndex(int index)
    {
        skillIndex = Mathf.Max(0, index);
        Refresh();
    }

    private void Refresh()
    {
        LoadIcon();

        CharacterSkillRuntime runtime = GetRuntime();
        CharacterSkillDefinition definition = runtime != null ? runtime.Definition : null;
        Sprite sprite = definition != null ? definition.Icon : null;
        bool hasIcon = sprite != null;
        Color color = runtime != null && runtime.IsUnlocked ? readyColor : lockedColor;

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
        float normalized = runtime != null ? runtime.Cooldown.Normalized : 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = normalized;

        if (cooldownText == null) return;

        float remaining = runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    private CharacterSkillRuntime GetRuntime()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null) return null;

        return hero.HeroSkillController.GetSkill(skillIndex);
    }

    private void LoadIcon()
    {
        if (iconImages != null && iconImages.Length > 0) return;

        List<Image> found = new();
        AddIconImage(found, icon);
        AddIconImage(found, FindChildComponentByName<Image>("Icon"));
        AddIconImage(found, FindChildComponentByName<Image>("IconSkill"));
        AddIconImage(found, FindChildComponentByName<Image>("SkillIcon"));

        iconImages = found.ToArray();

        if (icon == null && iconImages.Length > 0)
            icon = iconImages[0];
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
