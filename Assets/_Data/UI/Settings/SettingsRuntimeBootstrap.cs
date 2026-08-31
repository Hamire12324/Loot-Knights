using UnityEngine;
using UnityEngine.EventSystems;

public class SettingsRuntimeBootstrap : BaseMonoBehaviour
{
    [Header("References")]
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private LobbyMenuPanel lobbyMenuPanel;

    [SerializeField] private bool createEventSystemIfMissing = true;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSettingsPanel();
        LoadLobbyMenuPanel();
        EnsureLobbySettingsButton();
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

    }

    private void LoadLobbyMenuPanel()
    {
        if (lobbyMenuPanel != null) return;

        lobbyMenuPanel = Object.FindAnyObjectByType<LobbyMenuPanel>(FindObjectsInactive.Include);
        if (lobbyMenuPanel != null) return;

        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name.Trim() != "LobbyMenuPanel") continue;

            lobbyMenuPanel = item.gameObject.AddComponent<LobbyMenuPanel>();
            return;
        }
    }

    private static void EnsureLobbySettingsButton()
    {
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name.Trim() != "Btn_Settings") continue;
            if (item.GetComponent<ButtonSettings>() == null)
            {
                item.gameObject.AddComponent<ButtonSettings>();
            }

            return;
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

}
