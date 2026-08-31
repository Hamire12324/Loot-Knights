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

    private static readonly string[] RestartButtonNameParts = { "restart", "retry", "again" };
    private static readonly string[] MainMenuButtonNameParts =
    {
        "stageselect", "stage_select", "select", "mainmenu", "main_menu", "home", "menu"
    };
    private static readonly string[] ExperienceNameParts = { "experience", "exp", "xp" };

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
        foreach (Button foundButton in GetComponentsInChildren<Button>(true))
        {
            if (foundButton == null)
                continue;

            string buttonName = foundButton.name;
            if (nextStageButton == null && HasNamePart(buttonName, "next"))
            {
                nextStageButton = foundButton;
                continue;
            }

            if (restartStageButton == null && HasAnyNamePart(buttonName, RestartButtonNameParts))
            {
                restartStageButton = foundButton;
                continue;
            }

            if (mainMenuButton == null && HasAnyNamePart(buttonName, MainMenuButtonNameParts))
                mainMenuButton = foundButton;
        }
    }

    private void BindButtons()
    {
        BindButton(nextStageButton, RequestNextStage);
        BindButton(restartStageButton, RequestRestartStage);
        BindButton(mainMenuButton, RequestMainMenu);
    }

    private static void BindButton(Button targetButton, UnityEngine.Events.UnityAction action)
    {
        if (targetButton == null)
            return;

        targetButton.onClick.RemoveListener(action);
        targetButton.onClick.AddListener(action);
    }

    private void LoadButtonTexts()
    {
        nextStageButtonText ??= GetButtonText<TMP_Text>(nextStageButton);
        mainMenuButtonText ??= GetButtonText<TMP_Text>(mainMenuButton);
        legacyNextStageButtonText ??= GetButtonText<Text>(nextStageButton);
        legacyMainMenuButtonText ??= GetButtonText<Text>(mainMenuButton);
    }

    private void LoadRewardTexts()
    {
        LoadRewardTextReferences(
            GetComponentsInChildren<TMP_Text>(true),
            ref rewardCoinsText,
            ref rewardDiamondsText,
            ref rewardExperienceText,
            ref levelResultText,
            allowShortLevelName: false);
    }

    private void LoadLegacyRewardTexts()
    {
        LoadRewardTextReferences(
            GetComponentsInChildren<Text>(true),
            ref legacyRewardCoinsText,
            ref legacyRewardDiamondsText,
            ref legacyRewardExperienceText,
            ref legacyLevelResultText,
            allowShortLevelName: true);
    }

    private static void LoadRewardTextReferences<T>(
        T[] textComponents,
        ref T coinsText,
        ref T diamondsText,
        ref T experienceText,
        ref T levelText,
        bool allowShortLevelName)
        where T : Component
    {
        foreach (T foundText in textComponents)
        {
            if (foundText == null)
                continue;

            string textName = foundText.name;
            if (coinsText == null && HasNamePart(textName, "coin"))
            {
                coinsText = foundText;
                continue;
            }

            if (diamondsText == null && HasNamePart(textName, "diamond"))
            {
                diamondsText = foundText;
                continue;
            }

            if (experienceText == null && HasAnyNamePart(textName, ExperienceNameParts))
            {
                experienceText = foundText;
                continue;
            }

            if (levelText == null &&
                (HasNamePart(textName, "level") || (allowShortLevelName && HasNamePart(textName, "lvl"))))
                levelText = foundText;
        }
    }

    private void LoadExperienceSlider()
    {
        if (experienceSlider != null)
            return;

        foreach (Slider foundSlider in GetComponentsInChildren<Slider>(true))
        {
            if (foundSlider != null && HasAnyNamePart(foundSlider.name, ExperienceNameParts))
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

        string coinsValue = coins.ToString("N0");
        string diamondsValue = diamonds.ToString("N0");
        string experienceValue = experience.ToString("N0");
        string levelValue = GetLevelResultText(experienceResult);

        SetText(rewardCoinsText, coinsValue);
        SetText(legacyRewardCoinsText, coinsValue);
        SetText(rewardDiamondsText, diamondsValue);
        SetText(legacyRewardDiamondsText, diamondsValue);
        SetText(rewardExperienceText, experienceValue);
        SetText(legacyRewardExperienceText, experienceValue);
        SetText(levelResultText, levelValue);
        SetText(legacyLevelResultText, levelValue);
        RefreshExperienceSlider(experienceResult);
    }

    private static string GetLevelResultText(PlayerLevelRewardResult? experienceResult)
    {
        return experienceResult.HasValue
            ? "Lv:" + experienceResult.Value.After.Level
            : "Lv:" + PlayerExperienceStorage.Level;
    }

    private void RefreshExperienceSlider(PlayerLevelRewardResult? experienceResult)
    {
        if (experienceSlider == null)
            return;

        experienceSlider.minValue = 0f;
        experienceSlider.maxValue = 1f;
        experienceSlider.wholeNumbers = false;

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
        if (experienceFillCoroutine == null)
            return;

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
            experienceSlider.value = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        experienceSlider.value = to;
    }

    private void RefreshButtonState(bool hasNextStage)
    {
        if (nextStageButton != null)
            nextStageButton.interactable = true;

        string nextStageLabel = hasNextStage ? NextStageLabel : FinalStageLabel;
        SetText(nextStageButtonText, nextStageLabel);
        SetText(mainMenuButtonText, HomeLabel);
        SetText(legacyNextStageButtonText, nextStageLabel);
        SetText(legacyMainMenuButtonText, HomeLabel);
    }

    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
            targetText.text = value;
    }

    private static void SetText(Text targetText, string value)
    {
        if (targetText != null)
            targetText.text = value;
    }

    private static T GetButtonText<T>(Button targetButton) where T : Component
    {
        return targetButton != null
            ? targetButton.GetComponentInChildren<T>(true)
            : null;
    }

    private static bool HasNamePart(string value, string namePart)
    {
        return value.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool HasAnyNamePart(string value, string[] nameParts)
    {
        foreach (string namePart in nameParts)
        {
            if (HasNamePart(value, namePart))
                return true;
        }

        return false;
    }
}
