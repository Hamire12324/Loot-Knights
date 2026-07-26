using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsRuntimeBootstrap : BaseMonoBehaviour
{
    [Header("References")]
    [SerializeField] private SettingsPanel settingsPanel;

    [Header("Options")]
    [SerializeField] private bool createEventSystemIfMissing = true;
    [SerializeField] private bool bindButtonsByName = true;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSettingsPanel();
    }

    protected override void Awake()
    {
        base.Awake();
        SetupScene();
    }

    private void LoadSettingsPanel()
    {
        if (settingsPanel != null) return;

        settingsPanel = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settingsPanel != null) return;

        Transform existingRoot = FindExistingSettingsRoot();
        if (existingRoot == null) return;

        settingsPanel = existingRoot.GetComponent<SettingsPanel>();
        if (settingsPanel == null)
        {
            settingsPanel = existingRoot.gameObject.AddComponent<SettingsPanel>();
        }
    }

    private void SetupScene()
    {
        GameSettingsData.ApplyAll();

        if (createEventSystemIfMissing)
        {
            EnsureEventSystem();
        }

        LoadSettingsPanel();
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsRuntimeBootstrap: Missing SettingsPanel in scene. Create a SettingsPanel object or add the SettingsPanel script to your settings UI root.");
            return;
        }

        settingsPanel.InitializeFromBootstrap();

        if (bindButtonsByName)
        {
            BindExistingSettingsButtons(settingsPanel);
        }
    }

    private Transform FindExistingSettingsRoot()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
        {
            if (item == null) continue;

            string lowerName = item.name.ToLowerInvariant();
            if (lowerName == "settingspanel"
                || lowerName.Contains("settings canvas")
                || lowerName.Contains("settingspanel"))
            {
                return item;
            }
        }

        return null;
    }

    private void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BindExistingSettingsButtons(SettingsPanel panel)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (!button.name.ToLowerInvariant().Contains("setting")) continue;

            button.onClick.RemoveListener(panel.Show);
            button.onClick.AddListener(panel.Show);
        }
    }
}
