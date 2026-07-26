using UnityEngine;

public enum GameLanguage
{
    English,
    Vietnamese
}

public enum GameAspectRatio
{
    Ratio16x9,
    Ratio18x9,
    Ratio19x9,
    FullScreen
}

public static class GameSettingsData
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string EffectVolumeKey = "Settings.EffectVolume";
    private const string PromptEnabledKey = "Settings.PromptEnabled";
    private const string VibrationEnabledKey = "Settings.VibrationEnabled";
    private const string LanguageKey = "Settings.Language";
    private const string AspectRatioKey = "Settings.AspectRatio";
    private const string QualityLevelKey = "Settings.QualityLevel";

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

    public static bool PromptEnabled
    {
        get => PlayerPrefs.GetInt(PromptEnabledKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(PromptEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool VibrationEnabled
    {
        get => PlayerPrefs.GetInt(VibrationEnabledKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(VibrationEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static GameLanguage Language
    {
        get => (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
        set
        {
            PlayerPrefs.SetInt(LanguageKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static GameAspectRatio AspectRatio
    {
        get => (GameAspectRatio)Mathf.Clamp(PlayerPrefs.GetInt(AspectRatioKey, 0), 0, 3);
        set
        {
            PlayerPrefs.SetInt(AspectRatioKey, (int)value);
            PlayerPrefs.Save();
            ApplyScreen();
        }
    }

    public static int QualityLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(QualityLevelKey, GetDefaultQualityLevel()), 0, GetMaxQualityLevel());
        set
        {
            int safeLevel = Mathf.Clamp(value, 0, GetMaxQualityLevel());
            PlayerPrefs.SetInt(QualityLevelKey, safeLevel);
            PlayerPrefs.Save();
            QualitySettings.SetQualityLevel(safeLevel, true);
        }
    }

    public static void ApplyAll()
    {
        ApplyAudio();
        QualitySettings.SetQualityLevel(QualityLevel, true);
        ApplyScreen();
    }

    public static string GetLanguageLabel()
    {
        return Language == GameLanguage.Vietnamese ? "Vietnamese" : "English";
    }

    public static string GetAspectRatioLabel()
    {
        return AspectRatio switch
        {
            GameAspectRatio.Ratio18x9 => "18:9",
            GameAspectRatio.Ratio19x9 => "19:9",
            GameAspectRatio.FullScreen => "Full",
            _ => "16:9"
        };
    }

    private static void ApplyAudio()
    {
        AudioListener.volume = MusicVolume;

        if (SFXManager.HasInstance)
        {
            SFXManager.InstanceOrNull.MasterVolume = EffectVolume;
        }
    }

    private static void ApplyScreen()
    {
        if (Screen.fullScreen)
        {
            return;
        }

        float ratio = AspectRatio switch
        {
            GameAspectRatio.Ratio18x9 => 18f / 9f,
            GameAspectRatio.Ratio19x9 => 19f / 9f,
            GameAspectRatio.FullScreen => (float)Screen.currentResolution.width / Screen.currentResolution.height,
            _ => 16f / 9f
        };

        int width = Screen.width;
        int height = Mathf.Max(1, Mathf.RoundToInt(width / ratio));

        if (height > Screen.currentResolution.height)
        {
            height = Screen.currentResolution.height;
            width = Mathf.RoundToInt(height * ratio);
        }

        Screen.SetResolution(Mathf.Max(320, width), Mathf.Max(180, height), false);
    }

    private static int GetDefaultQualityLevel()
    {
        return Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, GetMaxQualityLevel());
    }

    private static int GetMaxQualityLevel()
    {
        return Mathf.Max(0, QualitySettings.names.Length - 1);
    }
}
