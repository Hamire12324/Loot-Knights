using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileSkillButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool isBasicAttack;
    [SerializeField, Min(0)] private int skillIndex;
    [SerializeField] private Image icon;
    [SerializeField] private UICircleGraphic cooldownFill;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text fallbackLabel;

    public void Setup(bool basicAttack, int index, Image iconImage, UICircleGraphic cooldown, TMP_Text text, TMP_Text label)
    {
        isBasicAttack = basicAttack;
        skillIndex = Mathf.Max(0, index);
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

        if (isBasicAttack)
            hero.CharacterSkillController.TryCastBasicAttack();
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

        float normalized = runtime != null ? runtime.Cooldown.Normalized : 0f;
        if (cooldownFill != null)
            cooldownFill.FillAmount = normalized;

        if (cooldownText == null) return;

        float remaining = runtime != null ? runtime.Cooldown.Remaining : 0f;
        cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    private CharacterSkillRuntime GetRuntime()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.CharacterSkillController == null) return null;

        return isBasicAttack
            ? hero.CharacterSkillController.BasicAttackRuntime
            : hero.CharacterSkillController.GetSkill(skillIndex);
    }
}
