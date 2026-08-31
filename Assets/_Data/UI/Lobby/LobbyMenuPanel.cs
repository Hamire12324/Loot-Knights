using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu displayed from the Lobby gear button. It deliberately does not pause time
/// and must not use the gameplay pause-menu button behaviours.
/// </summary>
public sealed class LobbyMenuPanel : BaseMonoBehaviour
{
    private SettingsPanel settingsPanel;
    private GameFlowManager gameFlowManager;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BindButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string name = button.name.ToLowerInvariant();
            if (name.Contains("continue"))
                Bind(button, Hide);
            else if (name.Contains("opensettings") || name.Contains("setting"))
                Bind(button, OpenSettings);
            else if (name.Contains("returntomainmenu") || name.Contains("mainmenu"))
                Bind(button, ReturnToMainMenu);
            else if (name.Contains("quit"))
                Bind(button, QuitGame);
        }
    }

    private void OpenSettings()
    {
        settingsPanel ??= FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        settingsPanel?.Show();
    }

    private void ReturnToMainMenu()
    {
        Hide();
        gameFlowManager ??= FindAnyObjectByType<GameFlowManager>(FindObjectsInactive.Include);
        gameFlowManager?.ShowMainMenu();
    }

    private static void QuitGame()
    {
        Application.Quit();
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
