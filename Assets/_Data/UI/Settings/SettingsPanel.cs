using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : BaseMonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button languagePreviousButton;
    [SerializeField] private Button languageNextButton;
    [SerializeField] private Button aspectPreviousButton;
    [SerializeField] private Button aspectNextButton;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle promptToggle;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle lowQualityToggle;
    [SerializeField] private Toggle mediumQualityToggle;
    [SerializeField] private Toggle highQualityToggle;
    [SerializeField] private Toggle maxQualityToggle;

    [Header("Texts")]
    [SerializeField] private TMP_Text languageValueText;
    [SerializeField] private TMP_Text aspectValueText;

    private Toggle[] qualityToggles;
    private bool isRefreshing;
    private bool isInitialized;

    public static SettingsPanel Instance { get; private set; }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSliders();
        LoadToggles();
        LoadButtons();
        LoadTexts();
    }

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show()
    {
        Initialize();
        Refresh();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Hide();
            return;
        }

        Show();
    }

    public void InitializeFromBootstrap()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BindExistingUi();
        GameSettingsData.ApplyAll();
        Refresh();
        isInitialized = true;
        Hide();
    }

    private void BindExistingUi()
    {
        BindSliders();
        BindToggles();
        BindCycleControls();
        BindCloseButton();
    }

    private void BindSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (effectSlider != null)
        {
            effectSlider.minValue = 0f;
            effectSlider.maxValue = 1f;
            effectSlider.onValueChanged.RemoveListener(SetEffectVolume);
            effectSlider.onValueChanged.AddListener(SetEffectVolume);
        }
    }

    private void BindToggles()
    {
        if (promptToggle != null)
        {
            promptToggle.onValueChanged.RemoveListener(SetPrompt);
            promptToggle.onValueChanged.AddListener(SetPrompt);
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveListener(SetVibration);
            vibrationToggle.onValueChanged.AddListener(SetVibration);
        }

        qualityToggles = new[] { lowQualityToggle, mediumQualityToggle, highQualityToggle, maxQualityToggle };
        for (int i = 0; i < qualityToggles.Length; i++)
        {
            Toggle toggle = qualityToggles[i];
            if (toggle == null) continue;

            int level = GetQualityLevelForToggle(i);
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn) SetQuality(level);
            });
        }
    }

    private void BindCycleControls()
    {
        if (languagePreviousButton != null)
        {
            languagePreviousButton.onClick.RemoveListener(PreviousLanguage);
            languagePreviousButton.onClick.AddListener(PreviousLanguage);
        }

        if (languageNextButton != null)
        {
            languageNextButton.onClick.RemoveListener(NextLanguage);
            languageNextButton.onClick.AddListener(NextLanguage);
        }

        if (aspectPreviousButton != null)
        {
            aspectPreviousButton.onClick.RemoveListener(PreviousAspectRatio);
            aspectPreviousButton.onClick.AddListener(PreviousAspectRatio);
        }

        if (aspectNextButton != null)
        {
            aspectNextButton.onClick.RemoveListener(NextAspectRatio);
            aspectNextButton.onClick.AddListener(NextAspectRatio);
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null) return;

        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);
    }

    private void Refresh()
    {
        if (!isInitialized) return;

        isRefreshing = true;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettingsData.MusicVolume);
        if (effectSlider != null) effectSlider.SetValueWithoutNotify(GameSettingsData.EffectVolume);
        if (promptToggle != null) promptToggle.SetIsOnWithoutNotify(GameSettingsData.PromptEnabled);
        if (vibrationToggle != null) vibrationToggle.SetIsOnWithoutNotify(GameSettingsData.VibrationEnabled);
        if (languageValueText != null) languageValueText.text = GameSettingsData.GetLanguageLabel();
        if (aspectValueText != null) aspectValueText.text = GameSettingsData.GetAspectRatioLabel();

        RefreshQualityToggles();
        isRefreshing = false;
    }

    private void RefreshQualityToggles()
    {
        if (qualityToggles == null) return;

        int savedQuality = GameSettingsData.QualityLevel;
        for (int i = 0; i < qualityToggles.Length; i++)
        {
            if (qualityToggles[i] == null) continue;

            int level = GetQualityLevelForToggle(i);
            qualityToggles[i].SetIsOnWithoutNotify(savedQuality == level);
        }
    }

    private int GetQualityLevelForToggle(int index)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        return index switch
        {
            0 => 0,
            1 => Mathf.Clamp(1, 0, max),
            2 => Mathf.Clamp(2, 0, max),
            _ => max
        };
    }

    private void SetMusicVolume(float value)
    {
        if (isRefreshing) return;
        GameSettingsData.MusicVolume = value;
    }

    private void SetEffectVolume(float value)
    {
        if (isRefreshing) return;
        GameSettingsData.EffectVolume = value;
    }

    private void SetPrompt(bool value)
    {
        if (isRefreshing) return;
        GameSettingsData.PromptEnabled = value;
        Refresh();
    }

    private void SetVibration(bool value)
    {
        if (isRefreshing) return;
        GameSettingsData.VibrationEnabled = value;
        Refresh();
    }

    private void PreviousLanguage()
    {
        GameSettingsData.Language = GameSettingsData.Language == GameLanguage.English
            ? GameLanguage.Vietnamese
            : GameLanguage.English;
        Refresh();
    }

    private void NextLanguage()
    {
        PreviousLanguage();
    }

    private void PreviousAspectRatio()
    {
        int current = (int)GameSettingsData.AspectRatio;
        current = (current + 3) % 4;
        GameSettingsData.AspectRatio = (GameAspectRatio)current;
        Refresh();
    }

    private void NextAspectRatio()
    {
        int current = (int)GameSettingsData.AspectRatio;
        current = (current + 1) % 4;
        GameSettingsData.AspectRatio = (GameAspectRatio)current;
        Refresh();
    }

    private void SetQuality(int level)
    {
        if (isRefreshing) return;
        GameSettingsData.QualityLevel = level;
        RefreshQualityToggles();
    }

    private void LoadSliders()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);

        if (musicSlider == null) musicSlider = FindComponentNearName(sliders, "music");
        if (effectSlider == null) effectSlider = FindComponentNearName(sliders, "effect");

        if (musicSlider == null && sliders.Length > 0) musicSlider = sliders[0];
        if (effectSlider == null && sliders.Length > 1) effectSlider = sliders[1];
    }

    private void LoadToggles()
    {
        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);

        if (promptToggle == null) promptToggle = FindComponentNearName(toggles, "prompt");
        if (vibrationToggle == null) vibrationToggle = FindComponentNearName(toggles, "vibration");

        Toggle[] graphicToggles = FindQualityToggles(toggles);
        if (lowQualityToggle == null && graphicToggles.Length > 0) lowQualityToggle = graphicToggles[0];
        if (mediumQualityToggle == null && graphicToggles.Length > 1) mediumQualityToggle = graphicToggles[1];
        if (highQualityToggle == null && graphicToggles.Length > 2) highQualityToggle = graphicToggles[2];
        if (maxQualityToggle == null && graphicToggles.Length > 3) maxQualityToggle = graphicToggles[3];
    }

    private void LoadButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null) continue;

            string lowerName = button.name.ToLowerInvariant();
            if (closeButton == null && (lowerName.Contains("close") || lowerName == "x"))
            {
                closeButton = button;
                continue;
            }

            TryAssignCycleButton(button, lowerName);
        }
    }

    private void LoadTexts()
    {
        if (languageValueText == null) languageValueText = FindValueText(FindChildTransform("language"));
        if (aspectValueText == null) aspectValueText = FindValueText(FindChildTransform("aspect"));
    }

    private void TryAssignCycleButton(Button button, string lowerName)
    {
        Transform current = button.transform;
        bool isLanguageButton = false;
        bool isAspectButton = false;

        while (current != null && current != transform.parent)
        {
            string currentName = current.name.ToLowerInvariant();
            isLanguageButton |= currentName.Contains("language");
            isAspectButton |= currentName.Contains("aspect");

            if (current == transform) break;
            current = current.parent;
        }

        bool isPrevious = lowerName.Contains("previous") || lowerName.Contains("prev") || lowerName.Contains("left");
        bool isNext = lowerName.Contains("next") || lowerName.Contains("right");

        if (isLanguageButton && isPrevious && languagePreviousButton == null) languagePreviousButton = button;
        if (isLanguageButton && isNext && languageNextButton == null) languageNextButton = button;
        if (isAspectButton && isPrevious && aspectPreviousButton == null) aspectPreviousButton = button;
        if (isAspectButton && isNext && aspectNextButton == null) aspectNextButton = button;
    }

    private T FindComponentNearName<T>(T[] components, string namePart) where T : Component
    {
        foreach (T component in components)
        {
            if (component == null) continue;

            Transform current = component.transform;
            while (current != null && current != transform.parent)
            {
                if (current.name.ToLowerInvariant().Contains(namePart))
                {
                    return component;
                }

                if (current == transform) break;
                current = current.parent;
            }
        }

        return null;
    }

    private Toggle[] FindQualityToggles(Toggle[] allToggles)
    {
        Transform graphicRoot = FindChildTransform("graphic");
        if (graphicRoot != null)
        {
            Toggle[] graphicToggles = graphicRoot.GetComponentsInChildren<Toggle>(true);
            if (graphicToggles.Length > 0)
            {
                return graphicToggles;
            }
        }

        return allToggles;
    }

    private Transform FindChildTransform(string namePart)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == null || child == transform) continue;
            if (child.name.ToLowerInvariant().Contains(namePart)) return child;
        }

        return null;
    }

    private TMP_Text FindValueText(Transform root)
    {
        if (root == null) return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = texts.Length - 1; i >= 0; i--)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;

            string lowerName = text.name.ToLowerInvariant();
            string lowerText = text.text.ToLowerInvariant();
            if (lowerName.Contains("value")
                || lowerText.Contains("english")
                || lowerText.Contains("vietnamese")
                || lowerText.Contains("16:9")
                || lowerText.Contains("18:9")
                || lowerText.Contains("19:9")
                || lowerText.Contains("full"))
            {
                return text;
            }
        }

        return texts.Length > 0 ? texts[texts.Length - 1] : null;
    }

}
