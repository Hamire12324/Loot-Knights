using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSkillSlotUI : BaseMonoBehaviour
{
    [SerializeField] private bool isSpecialSkill;
    [SerializeField, Min(0)] private int skillIndex;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text ultimateChargeText;
    [SerializeField] private Sprite emptyIcon;
    [SerializeField] private Color lockedColor = new(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color readyColor = Color.white;

    private CharacterSkillRuntime runtime;

    protected override void OnEnable()
    {
        base.OnEnable();
        BindRuntime();
        Refresh();
    }

    protected override void OnDisable()
    {
        UnbindRuntime();
        base.OnDisable();
    }

    protected override void Update()
    {
        base.Update();
        RebindIfRuntimeChanged();
        RefreshCooldown();
        RefreshUltimateCharge();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadIcon();
        LoadCooldownOverlay();
        LoadCooldownText();
    }

    public void SetSkillIndex(int index)
    {
        isSpecialSkill = false;
        skillIndex = Mathf.Max(0, index);
        BindRuntime();
        Refresh();
    }

    public void SetSpecialSkillSlot()
    {
        isSpecialSkill = true;
        BindRuntime();
        Refresh();
    }

    private void BindRuntime()
    {
        UnbindRuntime();

        runtime = GetCurrentRuntime();

        if (runtime != null)
            runtime.OnChanged += HandleRuntimeChanged;
    }

    private void RebindIfRuntimeChanged()
    {
        CharacterSkillRuntime currentRuntime = GetCurrentRuntime();
        if (currentRuntime == runtime)
            return;

        BindRuntime();
        Refresh();
    }

    private CharacterSkillRuntime GetCurrentRuntime()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null)
            return null;

        return isSpecialSkill
            ? hero.HeroSkillController.GetSpecialSkill()
            : hero.HeroSkillController.GetSkill(skillIndex);
    }

    private void UnbindRuntime()
    {
        if (runtime != null)
            runtime.OnChanged -= HandleRuntimeChanged;

        runtime = null;
    }

    private void HandleRuntimeChanged(CharacterSkillRuntime changedRuntime)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (icon != null)
        {
            icon.sprite = runtime != null && runtime.Definition != null && runtime.Definition.Icon != null
                ? runtime.Definition.Icon
                : emptyIcon;

            icon.color = runtime != null && runtime.IsUnlocked ? readyColor : lockedColor;
        }

        RefreshCooldown();
    }

    private void RefreshCooldown()
    {
        float normalized = runtime != null ? runtime.Cooldown.Normalized : 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = normalized;

        if (cooldownText == null) return;

        float remaining = runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    private void RefreshUltimateCharge()
    {
        bool shouldShow = !isSpecialSkill && skillIndex == 3;
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
        int charges = hero != null ? ArcherUltimateCharge.GetCharges(hero) : 0;
        ultimateChargeText.text = charges.ToString();
        ultimateChargeText.gameObject.SetActive(true);
    }

    private void LoadIcon()
    {
        if (icon != null) return;

        Transform child = transform.Find("Icon");
        if (child != null)
            icon = child.GetComponent<Image>();
    }

    private void LoadCooldownOverlay()
    {
        if (cooldownOverlay != null) return;

        Transform child = transform.Find("CooldownOverlay");
        if (child != null)
            cooldownOverlay = child.GetComponent<Image>();
    }

    private void LoadCooldownText()
    {
        if (cooldownText != null) return;

        cooldownText = GetComponentInChildren<TMP_Text>(true);
    }

    private void LoadUltimateChargeText()
    {
        if (ultimateChargeText != null)
            return;

        Transform child = transform.Find("UltimateChargeText");
        if (child != null)
            ultimateChargeText = child.GetComponent<TMP_Text>();

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
}
