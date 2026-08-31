using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : BaseMonoBehaviour
{
    private const string DefaultGameplaySceneName = "GamePlay";

    private enum Screen
    {
        MainMenu,
        CharacterCreation,
        CharacterSelection,
        Lobby,
        StageSelection
    }

    [Header("Panels")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private CharacterCreationPanel characterCreationPanel;
    [SerializeField] private CharacterSelectionPanel characterSelectionPanel;
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
        ShowScreen(Screen.MainMenu);
    }

    public void ContinueGame()
    {
        if (!CharacterProfileStorage.HasCharacter()) return;
        ShowCharacterSelection();
    }

    public void PlayGame()
    {
        if (CharacterProfileStorage.HasCharacter())
            ShowCharacterSelection();
        else
            ShowCharacterCreation();
    }
    public void ShowCharacterCreation()
    {
        if (!CharacterProfileStorage.CanCreateCharacter())
        {
            ShowCharacterSelection();
            return;
        }

        ShowScreen(Screen.CharacterCreation);
    }

    public void ConfirmCharacter(CreatedCharacterData characterData)
    {
        if (!CharacterProfileStorage.CanCreateCharacter())
        {
            ShowCharacterSelection();
            return;
        }

        SaveCurrentCharacterProgress();
        CharacterProfileStorage.Save(characterData);
        EnterLobby(characterData);
    }

    public void DeleteCharacter()
    {
        ShowCharacterSelection();
    }

    public void ShowCharacterSelection()
    {
        if (characterSelectionPanel == null) return;

        ShowScreen(Screen.CharacterSelection);
    }

    public void ReturnToLobby()
    {
        CurrentCharacter = CharacterProfileStorage.Load();
        if (CurrentCharacter == null)
        {
            ShowMainMenu();
            return;
        }

        ShowScreen(Screen.Lobby);
    }

    private void SelectCharacter(CreatedCharacterData characterData)
    {
        if (characterData == null) return;

        SaveCurrentCharacterProgress();
        CharacterProfileStorage.Select(characterData);
        EnterLobby(characterData);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGameplay()
    {
        CurrentCharacter = CharacterProfileStorage.Load();

        if (CurrentCharacter == null) return;

        LoadGameplayScene();
    }

    public void ShowStageSelection()
    {
        CurrentCharacter = CharacterProfileStorage.Load();

        if (CurrentCharacter == null) return;

        if (stageSelectPanel == null)
        {
            Debug.LogError(transform.name + ": Missing StageSelectPanel reference.", gameObject);
            return;
        }

        ShowScreen(Screen.StageSelection);
    }

    public void SelectStageAndStart(int stageIndex)
    {
        StageSelectionStorage.SaveSelectedStageIndex(stageIndex);
        StartGameplay();
    }

    private void EnterLobby(CreatedCharacterData characterData)
    {
        CurrentCharacter = characterData;
        ReloadCurrentCharacterProgress();
        ShowScreen(Screen.Lobby);
    }

    private static void SaveCurrentCharacterProgress()
    {
        if (PlayerInventoryManager.InstanceOrNull != null)
            PlayerInventoryManager.InstanceOrNull.SaveNow();

        if (PlayerEquipmentManager.InstanceOrNull != null)
            PlayerEquipmentManager.InstanceOrNull.Save();
    }

    private static void ReloadCurrentCharacterProgress()
    {
        if (PlayerInventoryManager.InstanceOrNull != null)
            PlayerInventoryManager.InstanceOrNull.Reload();

        if (PlayerEquipmentManager.InstanceOrNull != null)
            PlayerEquipmentManager.InstanceOrNull.Reload();
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

    private void ShowScreen(Screen screen)
    {
        SetPanel(mainMenuPanel, screen == Screen.MainMenu);
        SetPanel(characterCreationPanel, screen == Screen.CharacterCreation);
        SetPanel(characterSelectionPanel, screen == Screen.CharacterSelection);
        SetPanel(lobbyPanel, screen == Screen.Lobby);
        SetPanel(stageSelectPanel, screen == Screen.StageSelection);
    }

    private static void SetPanel(BaseMonoBehaviour panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private void SubscribePanelEvents()
    {
        LoadPanelReferences();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnCreateCharacterRequested -= PlayGame;
            mainMenuPanel.OnDeleteCharacterRequested -= DeleteCharacter;
            mainMenuPanel.OnQuitRequested -= QuitGame;

            mainMenuPanel.OnCreateCharacterRequested += PlayGame;
            mainMenuPanel.OnDeleteCharacterRequested += DeleteCharacter;
            mainMenuPanel.OnQuitRequested += QuitGame;
        }

        if (characterCreationPanel != null)
        {
            characterCreationPanel.OnCharacterCreated -= ConfirmCharacter;
            characterCreationPanel.OnBackRequested -= ShowCharacterSelection;

            characterCreationPanel.OnCharacterCreated += ConfirmCharacter;
            characterCreationPanel.OnBackRequested += ShowCharacterSelection;
        }

        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.OnCharacterSelected -= SelectCharacter;
            characterSelectionPanel.OnCreateCharacterRequested -= ShowCharacterCreation;
            characterSelectionPanel.OnBackRequested -= ShowMainMenu;

            characterSelectionPanel.OnCharacterSelected += SelectCharacter;
            characterSelectionPanel.OnCreateCharacterRequested += ShowCharacterCreation;
            characterSelectionPanel.OnBackRequested += ShowMainMenu;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.OnReadyGoRequested -= ShowStageSelection;
            lobbyPanel.OnReadyGoRequested += ShowStageSelection;
        }

        if (stageSelectPanel != null)
        {
            stageSelectPanel.OnStageSelected -= SelectStageAndStart;
            stageSelectPanel.OnBackRequested -= ReturnToLobby;

            stageSelectPanel.OnStageSelected += SelectStageAndStart;
            stageSelectPanel.OnBackRequested += ReturnToLobby;
        }
    }

    private void UnsubscribePanelEvents()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnCreateCharacterRequested -= PlayGame;
            mainMenuPanel.OnDeleteCharacterRequested -= DeleteCharacter;
            mainMenuPanel.OnQuitRequested -= QuitGame;
        }

        if (characterCreationPanel != null)
        {
            characterCreationPanel.OnCharacterCreated -= ConfirmCharacter;
            characterCreationPanel.OnBackRequested -= ShowCharacterSelection;
        }

        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.OnCharacterSelected -= SelectCharacter;
            characterSelectionPanel.OnCreateCharacterRequested -= ShowCharacterCreation;
            characterSelectionPanel.OnBackRequested -= ShowMainMenu;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.OnReadyGoRequested -= ShowStageSelection;
        }

        if (stageSelectPanel != null)
        {
            stageSelectPanel.OnStageSelected -= SelectStageAndStart;
            stageSelectPanel.OnBackRequested -= ReturnToLobby;
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

        if (characterSelectionPanel == null)
        {
            characterSelectionPanel = FindAnyObjectByType<CharacterSelectionPanel>(FindObjectsInactive.Include);
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
