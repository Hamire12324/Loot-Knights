using UnityEngine;

public sealed class EnemyHpSlider : SliderAbstract
{
    private EnemyDamReceiver receiver;

    protected override void OnEnable()
    {
        Bind();
    }

    protected override void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        CharacterCtrl ctrl = GetComponentInParent<CharacterCtrl>();
        receiver = ctrl != null ? ctrl.CharacterDamReceiver as EnemyDamReceiver : null;

        if (receiver != null)
            receiver.OnHpChanged += Refresh;

        Refresh(
            ctrl?.CharacterStat?.CurrentHealth ?? 0f,
            ctrl?.CharacterStat?.MaxHealth?.FinalValue ?? 0f);
    }

    private void Unbind()
    {
        if (receiver != null)
            receiver.OnHpChanged -= Refresh;

        receiver = null;
    }

    private void Refresh(float current, float max)
    {
        SetValue(max > 0f ? current / max : 0f);
    }
}
