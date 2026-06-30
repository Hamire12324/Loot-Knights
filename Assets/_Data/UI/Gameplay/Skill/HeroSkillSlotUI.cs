using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSkillSlotUI : BaseMonoBehaviour
{
    [SerializeField, Min(0)] private int skillIndex;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
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
        RefreshCooldown();
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
        skillIndex = Mathf.Max(0, index);
        BindRuntime();
        Refresh();
    }

    private void BindRuntime()
    {
        UnbindRuntime();

        HeroCtrl hero = HeroCtrl.GetLocal();
        runtime = hero != null && hero.HeroSkillController != null
            ? hero.HeroSkillController.GetSkill(skillIndex)
            : null;

        if (runtime != null)
            runtime.OnChanged += HandleRuntimeChanged;
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
}
