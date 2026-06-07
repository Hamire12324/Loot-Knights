using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageCompletePanel : BaseMonoBehaviour
{
    private const string NextStageLabel = "NEXT";
    private const string FinalStageLabel = "STAGE SELECT";
    private const string HomeLabel = "HOME";

    public event Action OnNextStageRequested;
    public event Action OnRestartStageRequested;
    public event Action OnMainMenuRequested;

    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button restartStageButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TMP_Text nextStageButtonText;
    [SerializeField] private TMP_Text mainMenuButtonText;
    [SerializeField] private TMP_Text rewardCoinsText;
    [SerializeField] private TMP_Text rewardDiamondsText;
    [SerializeField] private TMP_Text rewardExperienceText;
    [SerializeField] private TMP_Text levelResultText;

    protected override void Start()
    {
        base.Start();
        BindButtons();
    }

    public void Show(
        DungeonStageConfig completedStage,
        int completedStageIndex,
        bool hasNextStage,
        PlayerLevelRewardResult? experienceResult = null)
    {
        LoadComponents();
        BindButtons();
        RefreshRewardTexts(completedStage, experienceResult);
        RefreshButtonState(hasNextStage);

        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
    }

    public void RequestNextStage()
    {
        OnNextStageRequested?.Invoke();
    }

    public void RequestRestartStage()
    {
        OnRestartStageRequested?.Invoke();
    }

    public void RequestMainMenu()
    {
        OnMainMenuRequested?.Invoke();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadButtons();
        LoadButtonTexts();
        LoadRewardTexts();
    }

    private void LoadButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button foundButton in buttons)
        {
            if (foundButton == null) continue;

            string buttonName = foundButton.name.ToLowerInvariant();

            if (nextStageButton == null && buttonName.Contains("next"))
            {
                nextStageButton = foundButton;
                continue;
            }

            if (restartStageButton == null &&
                (buttonName.Contains("restart") || buttonName.Contains("retry") || buttonName.Contains("again")))
            {
                restartStageButton = foundButton;
                continue;
            }

            if (mainMenuButton == null &&
                (buttonName.Contains("stageselect") ||
                 buttonName.Contains("stage_select") ||
                 buttonName.Contains("select") ||
                 buttonName.Contains("mainmenu") ||
                 buttonName.Contains("main_menu") ||
                 buttonName.Contains("home") ||
                 buttonName.Contains("menu")))
            {
                mainMenuButton = foundButton;
            }
        }
    }

    private void BindButtons()
    {
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveListener(RequestNextStage);
            nextStageButton.onClick.AddListener(RequestNextStage);
        }

        if (restartStageButton != null)
        {
            restartStageButton.onClick.RemoveListener(RequestRestartStage);
            restartStageButton.onClick.AddListener(RequestRestartStage);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(RequestMainMenu);
            mainMenuButton.onClick.AddListener(RequestMainMenu);
        }
    }

    private void LoadButtonTexts()
    {
        if (nextStageButtonText == null)
            nextStageButtonText = GetButtonText(nextStageButton);

        if (mainMenuButtonText == null)
            mainMenuButtonText = GetButtonText(mainMenuButton);
    }

    private void LoadRewardTexts()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text foundText in texts)
        {
            if (foundText == null) continue;

            string textName = foundText.name.ToLowerInvariant();

            if (rewardCoinsText == null && textName.Contains("coin"))
            {
                rewardCoinsText = foundText;
                continue;
            }

            if (rewardDiamondsText == null && (textName.Contains("diamond")))
            {
                rewardDiamondsText = foundText;
                continue;
            }

            if (rewardExperienceText == null &&
                (textName.Contains("experience") || textName.Contains("exp") || textName.Contains("xp")))
            {
                rewardExperienceText = foundText;
                continue;
            }

            if (levelResultText == null && textName.Contains("level"))
            {
                levelResultText = foundText;
            }
        }
    }

    private void RefreshRewardTexts(
        DungeonStageConfig completedStage,
        PlayerLevelRewardResult? experienceResult)
    {
        int coins = completedStage != null ? completedStage.CoinReward : 0;
        int diamonds = completedStage != null ? completedStage.DiamondReward : 0;
        int experience = completedStage != null ? completedStage.ExperienceReward : 0;

        SetText(rewardCoinsText, coins.ToString("N0"));
        SetText(rewardDiamondsText, diamonds.ToString("N0"));
        SetText(rewardExperienceText, experience.ToString("N0"));
        SetText(levelResultText, GetLevelResultText(experienceResult));
    }

    private string GetLevelResultText(PlayerLevelRewardResult? experienceResult)
    {
        if (!experienceResult.HasValue)
            return "Lv. " + PlayerExperienceStorage.Level;

        PlayerLevelRewardResult result = experienceResult.Value;
        return result.LeveledUp
            ? "LEVEL UP! " + result.Before.Level + " -> " + result.After.Level
            : "Lv. " + result.After.Level;
    }

    private void RefreshButtonState(bool hasNextStage)
    {
        if (nextStageButton != null)
            nextStageButton.interactable = true;

        SetText(nextStageButtonText, hasNextStage ? NextStageLabel : FinalStageLabel);
        SetText(mainMenuButtonText, HomeLabel);
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null) return;

        targetText.text = value;
    }

    private TMP_Text GetButtonText(Button targetButton)
    {
        return targetButton != null
            ? targetButton.GetComponentInChildren<TMP_Text>(true)
            : null;
    }
}
