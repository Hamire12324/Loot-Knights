using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileSkillButton : MonoBehaviour, IPointerClickHandler
{
    private enum MobileSkillButtonMode
    {
        Skill,
        BasicAttack,
        ElementAbsorb,
        ElementRelease
    }

    [SerializeField] private MobileSkillButtonMode mode;
    [SerializeField] private bool isBasicAttack;
    [SerializeField] private bool isSpecialSkill;
    [SerializeField, Min(0)] private int skillIndex;
    [SerializeField] private Image icon;
    [SerializeField] private UICircleGraphic cooldownFill;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text fallbackLabel;
    [SerializeField] private TMP_Text ultimateChargeText;

    public void Setup(bool basicAttack, int index, Image iconImage, UICircleGraphic cooldown, TMP_Text text, TMP_Text label)
    {
        isBasicAttack = basicAttack;
        isSpecialSkill = false;
        mode = basicAttack ? MobileSkillButtonMode.BasicAttack : MobileSkillButtonMode.Skill;
        skillIndex = Mathf.Max(0, index);
        icon = iconImage;
        cooldownFill = cooldown;
        cooldownText = text;
        fallbackLabel = label;
    }

    public void SetupSpecial(Image iconImage, UICircleGraphic cooldown, TMP_Text text, TMP_Text label)
    {
        isBasicAttack = false;
        isSpecialSkill = true;
        mode = MobileSkillButtonMode.ElementRelease;
        skillIndex = 0;
        icon = iconImage;
        cooldownFill = cooldown;
        cooldownText = text;
        fallbackLabel = label;
    }

    public void SetupElementAbsorb(Image iconImage, UICircleGraphic cooldown, TMP_Text text, TMP_Text label)
    {
        isBasicAttack = false;
        isSpecialSkill = false;
        mode = MobileSkillButtonMode.ElementAbsorb;
        skillIndex = 0;
        icon = iconImage;
        cooldownFill = cooldown;
        cooldownText = text;
        fallbackLabel = label;
    }

    private void Update()
    {
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.CharacterSkillController == null) return;

        if (mode == MobileSkillButtonMode.BasicAttack || isBasicAttack)
        {
            CharacterSkillRuntime basicRuntime = hero.CharacterSkillController.BasicAttackRuntime;
            string basicSkillName = basicRuntime != null && basicRuntime.Definition != null
                ? basicRuntime.Definition.name
                : "null";

            Debug.Log(
                $"{nameof(MobileSkillButton)} basic attack clicked. hero={hero.name}, class={(hero.Profile != null ? hero.Profile.CharacterClass.ToString() : "no-profile")}, basicSkill={basicSkillName}.",
                hero);

            if (hero.CharacterSkillController.TryCastBasicAttack())
            {
                Debug.Log($"{nameof(MobileSkillButton)} cast basic skill {basicSkillName}.", hero);
                return;
            }

            Debug.LogWarning(
                $"{nameof(MobileSkillButton)} basic skill did not cast. basicSkill={basicSkillName}, isCasting={hero.CharacterSkillController.IsCasting}.",
                hero);
        }
        else if (mode == MobileSkillButtonMode.ElementAbsorb && hero.HeroSkillController != null)
            hero.HeroSkillController.TryAbsorbElementConduit();
        else if ((mode == MobileSkillButtonMode.ElementRelease || isSpecialSkill) && hero.HeroSkillController != null)
        {
            if (hero.HeroSkillController.TryReleaseElementConduit())
                RefreshStoredElementSlots();
        }
        else
            hero.CharacterSkillController.TryCast(skillIndex);
    }

    private void Refresh()
    {
        CharacterSkillRuntime runtime = GetRuntime();
        CharacterSkillDefinition definition = runtime != null ? runtime.Definition : null;

        if (icon != null)
        {
            icon.sprite = definition != null ? definition.Icon : null;
            icon.enabled = definition != null && definition.Icon != null;
        }

        if (fallbackLabel != null)
            fallbackLabel.enabled = icon == null || !icon.enabled;

        float normalized = mode == MobileSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Normalized : 0f;
        if (cooldownFill != null)
            cooldownFill.FillAmount = normalized;

        if (cooldownText == null)
        {
            RefreshUltimateCharge();
            return;
        }

        float remaining = mode == MobileSkillButtonMode.ElementAbsorb
            ? 0f
            : runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
        RefreshUltimateCharge();
    }

    private void RefreshUltimateCharge()
    {
        string resourceId = GetConsumedResourceId(GetRuntime());
        bool shouldShow = mode == MobileSkillButtonMode.Skill && !string.IsNullOrWhiteSpace(resourceId);
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

    private CharacterSkillRuntime GetRuntime()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.CharacterSkillController == null) return null;

        if (mode == MobileSkillButtonMode.BasicAttack || isBasicAttack)
            return hero.CharacterSkillController.BasicAttackRuntime;

        return mode == MobileSkillButtonMode.ElementAbsorb ||
               mode == MobileSkillButtonMode.ElementRelease ||
               isSpecialSkill
            ? hero.CharacterSkillController.GetSpecialSkill()
            : hero.CharacterSkillController.GetSkill(skillIndex);
    }

    private void RefreshStoredElementSlots()
    {
        GameplayMobileSkillHud hud = GetComponentInParent<GameplayMobileSkillHud>();
        if (hud == null)
            hud = FindAnyObjectByType<GameplayMobileSkillHud>();

        hud?.RefreshElementMeterNow();
    }
}
