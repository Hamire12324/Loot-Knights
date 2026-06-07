using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : BaseMonoBehaviour
{
    private const string DefaultGameplaySceneName = "GamePlay";

    [Header("Panels")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private CharacterCreationPanel characterCreationPanel;
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private StageSelectPanel stageSelectPanel;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = DefaultGameplaySceneName;

    public CreatedCharacterData CurrentCharacter { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribePanelEvents();
    }

    protected override void Start()
    {
        base.Start();

        LoadPanelReferences();
        SubscribePanelEvents();

        CurrentCharacter = CharacterProfileStorage.Load();

        if (StageSelectionStorage.ConsumeOpenStageSelectOnMainMenuRequest() && CurrentCharacter != null)
            ShowStageSelection();
        else if (StageSelectionStorage.ConsumeOpenLobbyOnMainMenuRequest() && CurrentCharacter != null)
            EnterLobby(CurrentCharacter);
        else
            ShowMainMenu();
    }

    protected override void OnDisable()
    {
        UnsubscribePanelEvents();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadPanelReferences();
    }

    public void ShowMainMenu()
    {
        SetPanel(mainMenuPanel, true);
        SetPanel(characterCreationPanel, false);
        SetPanel(lobbyPanel, false);
        SetPanel(stageSelectPanel, false);
    }

    public void ContinueGame()
    {
        CurrentCharacter = CharacterProfileStorage.Load();

        if (CurrentCharacter == null) return;

        EnterLobby(CurrentCharacter);
    }
    public void ShowCharacterCreation()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(characterCreationPanel, true);
        SetPanel(lobbyPanel, false);
        SetPanel(stageSelectPanel, false);
    }

    public void ConfirmCharacter(CreatedCharacterData characterData)
    {
        CharacterProfileStorage.Save(characterData);
        EnterLobby(characterData);
    }

    public void DeleteCharacter()
    {
        CharacterProfileStorage.Delete();
        CurrentCharacter = null;
        ShowMainMenu();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGameplay()
    {
        CurrentCharacter = CharacterProfileStorage.Load();

        if (CurrentCharacter == null) return;

        EnterGameplay(CurrentCharacter);
    }

    public void ShowStageSelection()
    {
        CurrentCharacter = CharacterProfileStorage.Load();

        if (CurrentCharacter == null) return;

        if (stageSelectPanel == null)
            LoadPanelReferences();

        if (stageSelectPanel == null)
        {
            Debug.LogError(transform.name + ": Missing StageSelectPanel reference.", gameObject);
            return;
        }

        SetPanel(mainMenuPanel, false);
        SetPanel(characterCreationPanel, false);
        SetPanel(lobbyPanel, false);
        SetPanel(stageSelectPanel, true);
    }

    public void SelectStageAndStart(int stageIndex)
    {
        StageSelectionStorage.SaveSelectedStageIndex(stageIndex);
        StartGameplay();
    }

    private void EnterLobby(CreatedCharacterData characterData)
    {
        CurrentCharacter = characterData;

        SetPanel(mainMenuPanel, false);
        SetPanel(characterCreationPanel, false);
        SetPanel(lobbyPanel, true);
        SetPanel(stageSelectPanel, false);
    }

    private void EnterGameplay(CreatedCharacterData characterData)
    {
        CurrentCharacter = characterData;
        LoadGameplayScene();
    }

    private void LoadGameplayScene()
    {
        string targetSceneName = string.IsNullOrWhiteSpace(gameplaySceneName)
            ? DefaultGameplaySceneName
            : gameplaySceneName.Trim();

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(transform.name + ": Gameplay scene '" + targetSceneName
                + "' is not in Build Settings.", gameObject);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void SetPanel(BaseMonoBehaviour panel, bool active)
    {
        if (panel == null) return;

        panel.SetActive(active);
    }

    private void SubscribePanelEvents()
    {
        LoadPanelReferences();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnContinueRequested -= ContinueGame;
            mainMenuPanel.OnCreateCharacterRequested -= ShowCharacterCreation;
            mainMenuPanel.OnDeleteCharacterRequested -= DeleteCharacter;
            mainMenuPanel.OnQuitRequested -= QuitGame;

            mainMenuPanel.OnContinueRequested += ContinueGame;
            mainMenuPanel.OnCreateCharacterRequested += ShowCharacterCreation;
            mainMenuPanel.OnDeleteCharacterRequested += DeleteCharacter;
            mainMenuPanel.OnQuitRequested += QuitGame;
        }

        if (characterCreationPanel != null)
        {
            characterCreationPanel.OnCharacterCreated -= ConfirmCharacter;
            characterCreationPanel.OnBackRequested -= ShowMainMenu;

            characterCreationPanel.OnCharacterCreated += ConfirmCharacter;
            characterCreationPanel.OnBackRequested += ShowMainMenu;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.OnReadyGoRequested -= StartGameplay;
            lobbyPanel.OnReadyGoRequested -= ShowStageSelection;
            lobbyPanel.OnReadyGoRequested += ShowStageSelection;
        }

        if (stageSelectPanel != null)
        {
            stageSelectPanel.OnStageSelected -= SelectStageAndStart;
            stageSelectPanel.OnBackRequested -= ContinueGame;

            stageSelectPanel.OnStageSelected += SelectStageAndStart;
            stageSelectPanel.OnBackRequested += ContinueGame;
        }
    }

    private void UnsubscribePanelEvents()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnContinueRequested -= ContinueGame;
            mainMenuPanel.OnCreateCharacterRequested -= ShowCharacterCreation;
            mainMenuPanel.OnDeleteCharacterRequested -= DeleteCharacter;
            mainMenuPanel.OnQuitRequested -= QuitGame;
        }

        if (characterCreationPanel != null)
        {
            characterCreationPanel.OnCharacterCreated -= ConfirmCharacter;
            characterCreationPanel.OnBackRequested -= ShowMainMenu;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.OnReadyGoRequested -= StartGameplay;
            lobbyPanel.OnReadyGoRequested -= ShowStageSelection;
        }

        if (stageSelectPanel != null)
        {
            stageSelectPanel.OnStageSelected -= SelectStageAndStart;
            stageSelectPanel.OnBackRequested -= ContinueGame;
        }
    }

    private void LoadPanelReferences()
    {
        if (mainMenuPanel == null)
        {
            mainMenuPanel = FindAnyObjectByType<MainMenuPanel>(FindObjectsInactive.Include);
        }

        if (characterCreationPanel == null)
        {
            characterCreationPanel = FindAnyObjectByType<CharacterCreationPanel>(FindObjectsInactive.Include);
        }

        if (lobbyPanel == null)
        {
            lobbyPanel = FindAnyObjectByType<LobbyPanel>(FindObjectsInactive.Include);
        }

        if (stageSelectPanel == null)
        {
            stageSelectPanel = FindAnyObjectByType<StageSelectPanel>(FindObjectsInactive.Include);
        }

    }
}
