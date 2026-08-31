using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Binds SettingsPanel controls to GameSettingsData and renders the saved values.
/// It is deliberately not a component: SettingsPanel keeps the inspector references.
/// </summary>
internal sealed class SettingsPanelView
{
    private readonly Button closeButton;
    private readonly Slider musicSlider;
    private readonly Slider effectSlider;

    private UnityAction closeListener;
    private bool isRefreshing;

    public SettingsPanelView(
        Button closeButton,
        Slider musicSlider,
        Slider effectSlider)
    {
        this.closeButton = closeButton;
        this.musicSlider = musicSlider;
        this.effectSlider = effectSlider;
    }

    public void Bind(UnityAction onClose)
    {
        Unbind();
        closeListener = onClose;

        BindSlider(musicSlider, SetMusicVolume);
        BindSlider(effectSlider, SetEffectVolume);
        BindButton(closeButton, closeListener);
    }

    public void Unbind()
    {
        UnbindSlider(musicSlider, SetMusicVolume);
        UnbindSlider(effectSlider, SetEffectVolume);
        UnbindButton(closeButton, closeListener);
    }

    public void Refresh()
    {
        isRefreshing = true;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettingsData.MusicVolume);
        if (effectSlider != null) effectSlider.SetValueWithoutNotify(GameSettingsData.EffectVolume);
        isRefreshing = false;
    }

    private void SetMusicVolume(float value)
    {
        if (!isRefreshing)
        {
            GameSettingsData.MusicVolume = value;
        }
    }

    private void SetEffectVolume(float value)
    {
        if (!isRefreshing)
        {
            GameSettingsData.EffectVolume = value;
        }
    }

    private static void BindSlider(Slider slider, UnityAction<float> listener)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.RemoveListener(listener);
        slider.onValueChanged.AddListener(listener);
    }

    private static void UnbindSlider(Slider slider, UnityAction<float> listener)
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(listener);
        }
    }

    private static void BindButton(Button button, UnityAction listener)
    {
        if (button == null || listener == null) return;

        button.onClick.RemoveListener(listener);
        button.onClick.AddListener(listener);
    }

    private static void UnbindButton(Button button, UnityAction listener)
    {
        if (button != null && listener != null)
        {
            button.onClick.RemoveListener(listener);
        }
    }
}
