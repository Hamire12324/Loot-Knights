using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DungeonStageManager : DungeonAbstract
{
    private const string MainMenuSceneName = "MainMenu";

    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private StageCompletePanel stageCompletePanel;
    [SerializeField] private StageDefeatPanel stageDefeatPanel;
    [SerializeField] private GameplayPanel gameplayPanel;
    [SerializeField] private List<DungeonStageConfig> stages = new();
    [SerializeField] private int currentStageIndex;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private float defeatPanelDelay = 2f;
    private bool currentStageRewardGranted;
    private bool currentStageCompleted;
    private bool currentStageFailed;
    private CharacterDamReceiver currentHeroDeathReceiver;
    private Coroutine showDefeatPanelCoroutine;

    public DungeonStageConfig CurrentStage =>
        stages.Count == 0 ? null : stages[Mathf.Clamp(currentStageIndex, 0, stages.Count - 1)];
    public int CurrentStageIndex => Mathf.Clamp(currentStageIndex, 0, Mathf.Max(0, stages.Count - 1));
    public bool HasNextStage => stages.Count > 0 && CurrentStageIndex < stages.Count - 1;

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeStageCompletePanel();
        SubscribeStageDefeatPanel();
    }

    protected override void Start()
    {
        base.Start();

        currentStageIndex = Mathf.Clamp(
            StageSelectionStorage.LoadSelectedStageIndex(),
            0,
            Mathf.Max(0, stages.Count - 1));

        if (generateOnStart)
            StartCurrentStage();
    }

    protected override void OnDisable()
    {
        StopShowDefeatPanelCoroutine();
        UnsubscribeHeroDeath();
        UnsubscribeStageDefeatPanel();
        UnsubscribeStageCompletePanel();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadDungeonGenerator();
        LoadStageCompletePanel();
        LoadStageDefeatPanel();
        LoadGameplayPanel();
    }

    public void StartCurrentStage()
    {
        SetGameplayInteractionEnabled(true);
        currentStageRewardGranted = false;
        currentStageCompleted = false;
        currentStageFailed = false;
        StopShowDefeatPanelCoroutine();
        stageCompletePanel?.Hide();
        stageDefeatPanel?.Hide();
        ClearActivePickupsForNewStage();
        PrepareHeroForStage();
        SubscribeHeroDeath();

        DungeonStageConfig stage = CurrentStage;

        if (dungeonGenerator == null)
            LoadDungeonGenerator();

        if (dungeonGenerator == null)
            return;

        if (stage == null)
        {
            dungeonGenerator.Generate();
            return;
        }

        dungeonGenerator.Generate(stage);
    }

    public void CompleteStage()
    {
        if (currentStageCompleted || currentStageFailed) return;

        currentStageCompleted = true;
        UnsubscribeHeroDeath();
        PlayerLevelRewardResult? experienceResult = GrantCurrentStageReward();

        if (HasNextStage)
            StageSelectionStorage.UnlockStage(CurrentStageIndex + 1);

        if (stageCompletePanel == null)
            LoadStageCompletePanel();

        if (stageCompletePanel == null)
        {
            Debug.LogWarning(transform.name + ": StageCompletePanel not found. Completing stage without victory UI.", gameObject);

            if (HasNextStage)
                GoToNextStage();
            else
                ReturnToStageSelect();

            return;
        }

        SetGameplayInteractionEnabled(false);
        SubscribeStageCompletePanel();
        stageCompletePanel.Show(CurrentStage, CurrentStageIndex, HasNextStage, experienceResult);
    }

    private PlayerLevelRewardResult? GrantCurrentStageReward()
    {
        if (currentStageRewardGranted) return null;

        DungeonStageConfig stage = CurrentStage;
        if (stage == null) return null;

        PlayerCurrencyStorage.Add(CurrencyType.Coins, stage.CoinReward);
        PlayerCurrencyStorage.Add(CurrencyType.Diamonds, stage.DiamondReward);
        PlayerLevelRewardResult experienceResult = PlayerExperienceStorage.Add(stage.ExperienceReward);

        currentStageRewardGranted = true;
        return experienceResult;
    }

    public void GoToNextStage()
    {
        if (!HasNextStage)
        {
            ReturnToStageSelect();
            return;
        }

        SetGameplayInteractionEnabled(true);

        if (stages.Count > 0)
            currentStageIndex = Mathf.Min(currentStageIndex + 1, stages.Count - 1);

        StageSelectionStorage.SaveSelectedStageIndex(currentStageIndex);
        StartCurrentStage();
    }

    public void RestartRun()
    {
        SetGameplayInteractionEnabled(true);
        currentStageIndex = 0;
        StageSelectionStorage.SaveSelectedStageIndex(currentStageIndex);
        StartCurrentStage();
    }

    public void SetStageIndex(int stageIndex)
    {
        SetGameplayInteractionEnabled(true);
        currentStageIndex = Mathf.Clamp(stageIndex, 0, Mathf.Max(0, stages.Count - 1));
        StageSelectionStorage.SaveSelectedStageIndex(currentStageIndex);
        StartCurrentStage();
    }

    public void RestartCurrentStage()
    {
        SetGameplayInteractionEnabled(true);
        StageSelectionStorage.SaveSelectedStageIndex(currentStageIndex);
        StartCurrentStage();
    }

    public void ReturnToStageSelect()
    {
        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.LogError(transform.name + ": Main menu scene '" + MainMenuSceneName
                + "' is not in Build Settings.", gameObject);
            return;
        }

        StageSelectionStorage.RequestOpenStageSelectOnMainMenu();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void LoadDungeonGenerator()
    {
        if (dungeonGenerator != null) return;

        if (dungeonCtrl != null && dungeonCtrl.DungeonGenerator != null)
        {
            dungeonGenerator = dungeonCtrl.DungeonGenerator;
            return;
        }

        dungeonGenerator = GetComponent<DungeonGenerator>();
        if (dungeonGenerator != null) return;

        dungeonGenerator = GetComponentInChildren<DungeonGenerator>(true);
        if (dungeonGenerator != null) return;

        dungeonGenerator = FindAnyObjectByType<DungeonGenerator>(FindObjectsInactive.Include);
    }

    private void LoadStageCompletePanel()
    {
        if (stageCompletePanel != null) return;

        stageCompletePanel = FindAnyObjectByType<StageCompletePanel>(FindObjectsInactive.Include);
    }

    private void LoadStageDefeatPanel()
    {
        if (stageDefeatPanel != null) return;

        stageDefeatPanel = FindAnyObjectByType<StageDefeatPanel>(FindObjectsInactive.Include);
    }

    private void LoadGameplayPanel()
    {
        if (gameplayPanel != null) return;

        gameplayPanel = FindAnyObjectByType<GameplayPanel>(FindObjectsInactive.Include);
    }

    private void SubscribeStageCompletePanel()
    {
        if (stageCompletePanel == null)
            LoadStageCompletePanel();

        if (stageCompletePanel == null) return;

        stageCompletePanel.OnNextStageRequested -= HandleNextStageRequested;
        stageCompletePanel.OnRestartStageRequested -= HandleRestartStageRequested;
        stageCompletePanel.OnMainMenuRequested -= HandleMainMenuRequested;

        stageCompletePanel.OnNextStageRequested += HandleNextStageRequested;
        stageCompletePanel.OnRestartStageRequested += HandleRestartStageRequested;
        stageCompletePanel.OnMainMenuRequested += HandleMainMenuRequested;
    }

    private void UnsubscribeStageCompletePanel()
    {
        if (stageCompletePanel == null) return;

        stageCompletePanel.OnNextStageRequested -= HandleNextStageRequested;
        stageCompletePanel.OnRestartStageRequested -= HandleRestartStageRequested;
        stageCompletePanel.OnMainMenuRequested -= HandleMainMenuRequested;
    }

    private void SubscribeStageDefeatPanel()
    {
        if (stageDefeatPanel == null)
            LoadStageDefeatPanel();

        if (stageDefeatPanel == null) return;

        stageDefeatPanel.OnRetryStageRequested -= HandleRetryAfterDefeatRequested;
        stageDefeatPanel.OnStageSelectRequested -= HandleStageSelectAfterDefeatRequested;
        stageDefeatPanel.OnMainMenuRequested -= HandleMainMenuRequested;

        stageDefeatPanel.OnRetryStageRequested += HandleRetryAfterDefeatRequested;
        stageDefeatPanel.OnStageSelectRequested += HandleStageSelectAfterDefeatRequested;
        stageDefeatPanel.OnMainMenuRequested += HandleMainMenuRequested;
    }

    private void UnsubscribeStageDefeatPanel()
    {
        if (stageDefeatPanel == null) return;

        stageDefeatPanel.OnRetryStageRequested -= HandleRetryAfterDefeatRequested;
        stageDefeatPanel.OnStageSelectRequested -= HandleStageSelectAfterDefeatRequested;
        stageDefeatPanel.OnMainMenuRequested -= HandleMainMenuRequested;
    }

    private void SubscribeHeroDeath()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        CharacterDamReceiver receiver = hero != null ? hero.CharacterDamReceiver : null;

        if (currentHeroDeathReceiver == receiver)
            return;

        UnsubscribeHeroDeath();

        currentHeroDeathReceiver = receiver;

        if (currentHeroDeathReceiver == null) return;

        currentHeroDeathReceiver.OnDeath -= HandleHeroDeath;
        currentHeroDeathReceiver.OnDeath += HandleHeroDeath;
    }

    private void UnsubscribeHeroDeath()
    {
        if (currentHeroDeathReceiver == null) return;

        currentHeroDeathReceiver.OnDeath -= HandleHeroDeath;
        currentHeroDeathReceiver = null;
    }

    private void HandleNextStageRequested()
    {
        stageCompletePanel?.Hide();

        if (!HasNextStage)
            ReturnToStageSelect();
        else
            GoToNextStage();
    }

    private void HandleRestartStageRequested()
    {
        stageCompletePanel?.Hide();
        RestartCurrentStage();
    }

    private void HandleRetryAfterDefeatRequested()
    {
        stageDefeatPanel?.Hide();
        RestartCurrentStage();
    }

    private void HandleStageSelectAfterDefeatRequested()
    {
        ReturnToStageSelect();
    }

    private void HandleMainMenuRequested()
    {
        ReturnToLobby();
    }

    private void HandleHeroDeath(CharacterDamReceiver receiver)
    {
        if (currentStageCompleted || currentStageFailed)
            return;

        currentStageFailed = true;
        UnsubscribeHeroDeath();
        SetGameplayInteractionEnabled(false);

        stageCompletePanel?.Hide();
        StopShowDefeatPanelCoroutine();
        showDefeatPanelCoroutine = StartCoroutine(ShowDefeatPanelAfterDelay());
    }

    private IEnumerator ShowDefeatPanelAfterDelay()
    {
        float delay = Mathf.Max(0f, defeatPanelDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        showDefeatPanelCoroutine = null;

        if (!currentStageFailed || currentStageCompleted)
            yield break;

        if (stageDefeatPanel == null)
            LoadStageDefeatPanel();

        SubscribeStageDefeatPanel();

        if (stageDefeatPanel != null)
            stageDefeatPanel.Show();
        else
            Debug.LogWarning(transform.name + ": Hero died but StageDefeatPanel was not found.", gameObject);
    }

    private void StopShowDefeatPanelCoroutine()
    {
        if (showDefeatPanelCoroutine == null) return;

        StopCoroutine(showDefeatPanelCoroutine);
        showDefeatPanelCoroutine = null;
    }

    private void ReturnToLobby()
    {
        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.LogError(transform.name + ": Main menu scene '" + MainMenuSceneName
                + "' is not in Build Settings.", gameObject);
            return;
        }

        StageSelectionStorage.RequestOpenLobbyOnMainMenu();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void SetGameplayInteractionEnabled(bool enabled)
    {
        if (gameplayPanel == null)
            LoadGameplayPanel();

        SetGameplayPanelActive(enabled);

        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null) return;

        HeroMovement heroMovement = hero.CharacterMovement as HeroMovement;
        heroMovement?.SetInputEnabled(enabled);

        PlayerInput playerInput = hero.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            if (enabled)
                playerInput.ActivateInput();
            else
                playerInput.DeactivateInput();
        }

        if (!enabled)
        {
            hero.CharacterCombatController?.CancelAttack(force: true);

            if (hero.Rb != null)
                hero.Rb.linearVelocity = Vector2.zero;
        }
    }

    private void PrepareHeroForStage()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null) return;

        hero.CharacterDamReceiver?.Revive();
        hero.CharacterCombatController?.CancelAttack(force: true);

        if (hero.Rb != null)
            hero.Rb.linearVelocity = Vector2.zero;
    }

    private void ClearActivePickupsForNewStage()
    {
        ReturnActivePickupsToPool(FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude));
        ReturnActivePickupsToPool(FindObjectsByType<CurrencyPickup>(FindObjectsInactive.Exclude));
    }

    private static void ReturnActivePickupsToPool<T>(T[] pickups) where T : PoolObj
    {
        if (pickups == null) return;

        foreach (T pickup in pickups)
        {
            if (pickup == null || pickup.IsInPool) continue;
            pickup.ReturnToPool();
        }
    }

    private void SetGameplayPanelActive(bool active)
    {
        if (gameplayPanel == null) return;

        if (!active && IsBlockingOverlayInsideGameplayPanel())
        {
            Debug.LogWarning(
                transform.name + ": End-state panel is inside GameplayPanel, so GameplayPanel cannot be disabled without hiding the result UI.",
                gameObject);
            return;
        }

        gameplayPanel.SetActive(active);
    }

    private bool IsBlockingOverlayInsideGameplayPanel()
    {
        if (gameplayPanel == null) return false;

        return (stageCompletePanel != null && stageCompletePanel.transform.IsChildOf(gameplayPanel.transform)) ||
               (stageDefeatPanel != null && stageDefeatPanel.transform.IsChildOf(gameplayPanel.transform));
    }

}
