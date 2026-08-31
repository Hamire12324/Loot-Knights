using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayPanel : BaseMonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    [Header("Pause menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private bool hidePauseMenuOnStart = true;

    private bool isPaused;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        if (hidePauseMenuOnStart)
        {
            ResumeGame();
        }
    }

    protected override void OnDestroy()
    {
        Time.timeScale = 1f;
        base.OnDestroy();
    }

    public void TogglePauseMenu()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetPauseMenuActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetPauseMenuActive(false);
    }

    public void OpenSettings()
    {
        SettingsPanel settingsPanel = SettingsPanel.Instance;
        if (settingsPanel == null)
        {
            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        }

        if (settingsPanel == null)
        {
            Debug.LogWarning("GameplayPanel: Missing GameSettingsPanel (SettingsPanel component).", gameObject);
            return;
        }

        settingsPanel.Show();
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.LogError("GameplayPanel: Scene 'MainMenu' is not in Build Settings.", gameObject);
            return;
        }

        StageSelectionStorage.RequestOpenLobbyOnMainMenu();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
    private void SetPauseMenuActive(bool active)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(active);
        }
    }

    private static bool HasNamePart(string value, string namePart)
    {
        return value.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
