using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Settings panel lifecycle and its serialized UI references.
/// SettingsPanelView contains the control binding and rendering logic.
/// </summary>
public class SettingsPanel : BaseMonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectSlider;

    private SettingsPanelView view;
    private bool isInitialized;

    public static SettingsPanel Instance { get; private set; }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadSliders();
        LoadButtons();
        HideUnsupportedControls();
    }

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    protected override void OnDestroy()
    {
        view?.Unbind();

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
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
        if (isInitialized || !TryBecomeInstance()) return;

        // This also supports an inactive panel found by SettingsRuntimeBootstrap.
        LoadComponents();
        view = CreateView();
        view.Bind(Hide);

        GameSettingsData.ApplyAll();
        isInitialized = true;
        Refresh();
        Hide();
    }

    private bool TryBecomeInstance()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
            return true;
        }

        Destroy(gameObject);
        return false;
    }

    private SettingsPanelView CreateView()
    {
        return new SettingsPanelView(
            closeButton,
            musicSlider,
            effectSlider);
    }

    private void Refresh()
    {
        view?.Refresh();
    }

    #region Reference loading

    private void LoadSliders()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);

        musicSlider ??= FindComponentInSection(sliders, "music");
        effectSlider ??= FindComponentInSection(sliders, "effect");

        if (musicSlider == null && sliders.Length > 0) musicSlider = sliders[0];
        if (effectSlider == null && sliders.Length > 1) effectSlider = sliders[1];
    }

    private void LoadButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonName = button.name;
            if (closeButton == null && (HasNamePart(buttonName, "close") || IsCloseIconName(buttonName)))
            {
                closeButton = button;
                continue;
            }
        }
    }

    private T FindComponentInSection<T>(T[] components, string sectionName) where T : Component
    {
        foreach (T component in components)
        {
            if (component != null && IsInSection(component.transform, sectionName))
            {
                return component;
            }
        }

        return null;
    }

    private bool IsInSection(Transform source, string sectionName)
    {
        for (Transform current = source; current != null && current != transform.parent; current = current.parent)
        {
            if (HasNamePart(current.name, sectionName)) return true;
            if (current == transform) break;
        }

        return false;
    }

    private static bool HasNamePart(string value, string namePart)
    {
        return value.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsCloseIconName(string buttonName)
    {
        return string.Equals(buttonName, "x", System.StringComparison.OrdinalIgnoreCase);
    }

    private void HideUnsupportedControls()
    {
        foreach (Transform item in GetComponentsInChildren<Transform>(true))
        {
            if (item == transform) continue;

            if (HasNamePart(item.name, "prompt")
                || HasNamePart(item.name, "vibration")
                || HasNamePart(item.name, "language")
                || HasNamePart(item.name, "aspect")
                || HasNamePart(item.name, "graphic")
                || HasNamePart(item.name, "system setup"))
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
