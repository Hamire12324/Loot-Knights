using TMPro;
using UnityEngine;

public sealed class PlayerHpSlider : SliderAbstract
{
    private TMP_Text valueText;
    private HeroCtrl hero;
    private CharacterDamReceiver receiver;

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
        receiver = hero != null ? hero.CharacterDamReceiver : null;
        if (receiver != null)
            receiver.OnHpChanged += Refresh;
        Refresh(hero?.CharacterStat?.CurrentHealth ?? 0f, hero?.CharacterStat?.MaxHealth?.FinalValue ?? 0f);
    }

    private void Unbind()
    {
        if (receiver != null)
            receiver.OnHpChanged -= Refresh;
        receiver = null;
        hero = null;
    }

    private void Refresh(float current, float max)
    {
        SetValue(max > 0f ? current / max : 0f);
        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
