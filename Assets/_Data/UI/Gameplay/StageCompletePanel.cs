using System;
using System.Collections;
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
    [SerializeField] private Text legacyNextStageButtonText;
    [SerializeField] private Text legacyMainMenuButtonText;
    [SerializeField] private TMP_Text rewardCoinsText;
    [SerializeField] private TMP_Text rewardDiamondsText;
    [SerializeField] private TMP_Text rewardExperienceText;
    [SerializeField] private TMP_Text levelResultText;
    [SerializeField] private Text legacyRewardCoinsText;
    [SerializeField] private Text legacyRewardDiamondsText;
    [SerializeField] private Text legacyRewardExperienceText;
    [SerializeField] private Text legacyLevelResultText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private float experienceFillDuration = 0.6f;

    private Coroutine experienceFillCoroutine;

    protected override void Start()
    {
        base.Start();
        BindButtons();
    }

    public void Show(
        StageConfig completedStage,
        int completedStageIndex,
        bool hasNextStage,
        PlayerLevelRewardResult? experienceResult = null)
    {
        LoadComponents();
        BindButtons();
        RefreshButtonState(hasNextStage);

        SetActive(true);
        RefreshRewardTexts(completedStage, experienceResult);
    }

    public void Hide()
    {
        StopExperienceFillCoroutine();
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
        LoadLegacyRewardTexts();
        LoadExperienceSlider();
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

        if (legacyNextStageButtonText == null)
            legacyNextStageButtonText = GetLegacyButtonText(nextStageButton);

        if (legacyMainMenuButtonText == null)
            legacyMainMenuButtonText = GetLegacyButtonText(mainMenuButton);
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

    private void LoadLegacyRewardTexts()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);

        foreach (Text foundText in texts)
        {
            if (foundText == null) continue;

            string textName = foundText.name.ToLowerInvariant();

            if (legacyRewardCoinsText == null && textName.Contains("coin"))
            {
                legacyRewardCoinsText = foundText;
                continue;
            }

            if (legacyRewardDiamondsText == null && textName.Contains("diamond"))
            {
                legacyRewardDiamondsText = foundText;
                continue;
            }

            if (legacyRewardExperienceText == null &&
                (textName.Contains("experience") || textName.Contains("exp") || textName.Contains("xp")))
            {
                legacyRewardExperienceText = foundText;
                continue;
            }

            if (legacyLevelResultText == null &&
                (textName.Contains("level") || textName.Contains("lvl")))
            {
                legacyLevelResultText = foundText;
            }
        }
    }

    private void LoadExperienceSlider()
    {
        if (experienceSlider != null) return;

        Slider[] sliders = GetComponentsInChildren<Slider>(true);

        foreach (Slider foundSlider in sliders)
        {
            if (foundSlider == null) continue;

            string sliderName = foundSlider.name.ToLowerInvariant();
            if (sliderName.Contains("experience") ||
                sliderName.Contains("exp") ||
                sliderName.Contains("xp"))
            {
                experienceSlider = foundSlider;
                return;
            }
        }
    }

    private void RefreshRewardTexts(
        StageConfig completedStage,
        PlayerLevelRewardResult? experienceResult)
    {
        int coins = completedStage != null ? completedStage.CoinReward : 0;
        int diamonds = completedStage != null ? completedStage.DiamondReward : 0;
        int experience = completedStage != null ? completedStage.ExperienceReward : 0;

        SetText(rewardCoinsText, coins.ToString("N0"));
        SetText(rewardDiamondsText, diamonds.ToString("N0"));
        SetText(rewardExperienceText, experience.ToString("N0"));
        SetText(legacyRewardCoinsText, coins.ToString("N0"));
        SetText(legacyRewardDiamondsText, diamonds.ToString("N0"));
        SetText(legacyRewardExperienceText, experience.ToString("N0"));
        SetText(levelResultText, GetLevelResultText(experienceResult));
        SetText(legacyLevelResultText, GetLevelResultText(experienceResult));
        RefreshExperienceSlider(experienceResult);
    }

    private string GetLevelResultText(PlayerLevelRewardResult? experienceResult)
    {
        if (!experienceResult.HasValue)
            return "Lv:" + PlayerExperienceStorage.Level;

        PlayerLevelRewardResult result = experienceResult.Value;
        return "Lv:" + result.After.Level;
    }

    private void RefreshExperienceSlider(PlayerLevelRewardResult? experienceResult)
    {
        if (experienceSlider == null) return;

        experienceSlider.minValue = 0f;
        experienceSlider.maxValue = 1f;
        experienceSlider.wholeNumbers = false;

        if (experienceFillCoroutine != null)
            StopExperienceFillCoroutine();

        if (!experienceResult.HasValue || !gameObject.activeInHierarchy || experienceFillDuration <= 0f)
        {
            PlayerLevelSnapshot snapshot = experienceResult.HasValue
                ? experienceResult.Value.After
                : PlayerExperienceStorage.Snapshot;
            experienceSlider.value = snapshot.Progress01;
            return;
        }

        experienceFillCoroutine = StartCoroutine(AnimateExperienceSlider(experienceResult.Value));
    }

    private void StopExperienceFillCoroutine()
    {
        if (experienceFillCoroutine == null) return;

        StopCoroutine(experienceFillCoroutine);
        experienceFillCoroutine = null;
    }

    private IEnumerator AnimateExperienceSlider(PlayerLevelRewardResult result)
    {
        PlayerLevelSnapshot before = result.Before;
        PlayerLevelSnapshot after = result.After;

        experienceSlider.value = before.Progress01;

        if (!result.LeveledUp)
        {
            yield return AnimateSliderValue(before.Progress01, after.Progress01);
            experienceFillCoroutine = null;
            yield break;
        }

        yield return AnimateSliderValue(before.Progress01, 1f);

        for (int level = before.Level + 1; level < after.Level; level++)
        {
            experienceSlider.value = 0f;
            yield return AnimateSliderValue(0f, 1f);
        }

        experienceSlider.value = 0f;
        yield return AnimateSliderValue(0f, after.Progress01);
        experienceFillCoroutine = null;
    }

    private IEnumerator AnimateSliderValue(float from, float to)
    {
        float duration = Mathf.Max(0.01f, experienceFillDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            experienceSlider.value = Mathf.Lerp(from, to, progress);
            yield return null;
        }

        experienceSlider.value = to;
    }

    private void RefreshButtonState(bool hasNextStage)
    {
        if (nextStageButton != null)
            nextStageButton.interactable = true;

        SetText(nextStageButtonText, hasNextStage ? NextStageLabel : FinalStageLabel);
        SetText(mainMenuButtonText, HomeLabel);
        SetText(legacyNextStageButtonText, hasNextStage ? NextStageLabel : FinalStageLabel);
        SetText(legacyMainMenuButtonText, HomeLabel);
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null) return;

        targetText.text = value;
    }

    private void SetText(Text targetText, string value)
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

    private Text GetLegacyButtonText(Button targetButton)
    {
        return targetButton != null
            ? targetButton.GetComponentInChildren<Text>(true)
            : null;
    }
}
