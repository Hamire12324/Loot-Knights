using System;
using UnityEngine;
using UnityEngine.UI;

public class StageDefeatPanel : BaseMonoBehaviour
{
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
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button foundButton in buttons)
        {
            if (foundButton == null) continue;

            string buttonName = foundButton.name.ToLowerInvariant();

            if (retryStageButton == null &&
                (buttonName.Contains("retry") || buttonName.Contains("restart") || buttonName.Contains("again")))
            {
                retryStageButton = foundButton;
                continue;
            }

            if (stageSelectButton == null &&
                (buttonName.Contains("stageselect") ||
                 buttonName.Contains("stage_select") ||
                 buttonName.Contains("select")))
            {
                stageSelectButton = foundButton;
                continue;
            }

            if (mainMenuButton == null &&
                (buttonName.Contains("home") ||
                 buttonName.Contains("mainmenu") ||
                 buttonName.Contains("main_menu") ||
                 buttonName.Contains("menu")))
            {
                mainMenuButton = foundButton;
            }
        }
    }

    private void BindButtons()
    {
        if (retryStageButton != null)
        {
            retryStageButton.onClick.RemoveListener(RequestRetryStage);
            retryStageButton.onClick.AddListener(RequestRetryStage);
        }

        if (stageSelectButton != null)
        {
            stageSelectButton.onClick.RemoveListener(RequestStageSelect);
            stageSelectButton.onClick.AddListener(RequestStageSelect);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(RequestMainMenu);
            mainMenuButton.onClick.AddListener(RequestMainMenu);
        }
    }
}
