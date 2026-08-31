using System;
using UnityEngine;
using UnityEngine.UI;

public class StageDefeatPanel : BaseMonoBehaviour
{
    private static readonly string[] RetryButtonNameParts = { "retry", "restart", "again" };
    private static readonly string[] StageSelectButtonNameParts = { "stageselect", "stage_select", "select" };
    private static readonly string[] MainMenuButtonNameParts = { "home", "mainmenu", "main_menu", "menu" };

    public event Action OnRetryStageRequested;
    public event Action OnStageSelectRequested;
    public event Action OnMainMenuRequested;

    [SerializeField] private Button retryStageButton;
    [SerializeField] private Button stageSelectButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Start()
    {
        base.Start();
        BindButtons();
    }

    public void Show()
    {
        LoadComponents();
        BindButtons();

        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
    }

    public void RequestRetryStage()
    {
        OnRetryStageRequested?.Invoke();
    }

    public void RequestStageSelect()
    {
        OnStageSelectRequested?.Invoke();
    }

    public void RequestMainMenu()
    {
        OnMainMenuRequested?.Invoke();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadButtons();
    }

    private void LoadButtons()
    {
        foreach (Button foundButton in GetComponentsInChildren<Button>(true))
        {
            if (foundButton == null)
                continue;

            string buttonName = foundButton.name;
            if (retryStageButton == null && HasAnyNamePart(buttonName, RetryButtonNameParts))
            {
                retryStageButton = foundButton;
                continue;
            }

            if (stageSelectButton == null && HasAnyNamePart(buttonName, StageSelectButtonNameParts))
            {
                stageSelectButton = foundButton;
                continue;
            }

            if (mainMenuButton == null && HasAnyNamePart(buttonName, MainMenuButtonNameParts))
                mainMenuButton = foundButton;
        }
    }

    private void BindButtons()
    {
        BindButton(retryStageButton, RequestRetryStage);
        BindButton(stageSelectButton, RequestStageSelect);
        BindButton(mainMenuButton, RequestMainMenu);
    }

    private static void BindButton(Button targetButton, UnityEngine.Events.UnityAction action)
    {
        if (targetButton == null)
            return;

        targetButton.onClick.RemoveListener(action);
        targetButton.onClick.AddListener(action);
    }

    private static bool HasAnyNamePart(string value, string[] nameParts)
    {
        foreach (string namePart in nameParts)
        {
            if (value.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
