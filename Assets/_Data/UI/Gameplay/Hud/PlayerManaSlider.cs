using TMPro;
using UnityEngine;

public sealed class PlayerManaSlider : SliderAbstract
{
    private TMP_Text valueText;
    private HeroCtrl hero;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>(true);
    }

    protected override void OnEnable() => Rebind();
    protected override void OnDisable() => Unbind();

    protected override void Update()
    {
        if (hero != HeroCtrl.GetLocal())
            Rebind();
    }

    private void Rebind()
    {
        HeroCtrl next = HeroCtrl.GetLocal();
        if (next == hero) return;
        Unbind();
        hero = next;
        if (hero?.CharacterStat != null)
            hero.CharacterStat.OnManaChanged += RefreshCurrentMana;
        Refresh(hero?.CharacterStat?.CurrentMana ?? 0f, hero?.CharacterStat?.MaxMana?.FinalValue ?? 0f);
    }

    private void Unbind()
    {
        if (hero?.CharacterStat != null)
            hero.CharacterStat.OnManaChanged -= RefreshCurrentMana;
        hero = null;
    }

    private void Refresh(float current, float max)
    {
        SetValue(max > 0f ? current / max : 0f);
        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void RefreshCurrentMana(float current) => Refresh(current, hero?.CharacterStat?.MaxMana?.FinalValue ?? 0f);
}
