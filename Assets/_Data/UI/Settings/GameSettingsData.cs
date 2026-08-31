using UnityEngine;

public static class GameSettingsData
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string EffectVolumeKey = "Settings.EffectVolume";

    public static float MusicVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f));
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyAudio();
        }
    }

    public static float EffectVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectVolumeKey, 0.8f));
        set
        {
            PlayerPrefs.SetFloat(EffectVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyAudio();
        }
    }

    public static void ApplyAll()
    {
        ApplyAudio();
    }

    private static void ApplyAudio()
    {
        AudioListener.volume = MusicVolume;

        if (SFXManager.HasInstance)
        {
            SFXManager.InstanceOrNull.MasterVolume = EffectVolume;
        }
    }

}
