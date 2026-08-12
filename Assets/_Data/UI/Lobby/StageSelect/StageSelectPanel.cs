using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectPanel : BaseMonoBehaviour
{
    private const string StagesResourcesPath = "Stages";

    public event Action<int> OnStageSelected;
    public event Action OnBackRequested;

    [SerializeField] private List<StageConfig> stages = new();
    [SerializeField] private int stagesPerPage = 14;
    [SerializeField] private bool useSerializedUnlockProgress;
    [SerializeField] private int serializedHighestUnlockedStageIndex;
    [SerializeField] private int currentPageIndex;
    [SerializeField] private Transform stageButtonRoot;
    [SerializeField] private StageSelectButton stageButtonPrefab;
    [SerializeField] private StageSelectButton[] stageButtons;
    [SerializeField] private ButtonStagePageArrow[] pageArrows;
    [SerializeField] private Button backButton;

    public int CurrentPageIndex => currentPageIndex;
    public int StageCount => stages == null ? 0 : stages.Count;
    public int PageSize
    {
        get
        {
            int buttonCount = stageButtons == null ? 0 : stageButtons.Length;
            if (buttonCount <= 0) return 0;
            if (stagesPerPage <= 0) return buttonCount;

            return Mathf.Clamp(stagesPerPage, 1, buttonCount);
        }
    }
    public int PageCount => PageSize <= 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt((float)Mathf.Max(1, StageCount) / PageSize));
    public bool CanGoPrevious => currentPageIndex > 0;
    public bool CanGoNext => currentPageIndex < PageCount - 1;

    protected override void Start()
    {
        base.Start();
        BindBackButton();
        RefreshPage();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshPage();
    }

    public void SelectStage(int stageIndex)
    {
        int safeStageIndex = Mathf.Max(0, stageIndex);
        if (safeStageIndex >= StageCount) return;

        OnStageSelected?.Invoke(safeStageIndex);
    }

    public void Back()
    {
        OnBackRequested?.Invoke();
    }

    public void PreviousPage()
    {
        SetPage(currentPageIndex - 1);
    }

    public void NextPage()
    {
        SetPage(currentPageIndex + 1);
    }

    public void SetPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, PageCount - 1);
        RefreshPage();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadStagesFromResources();
        LoadStageButtonRoot();
        LoadStageButtons();
        LoadPageArrows();
        LoadBackButton();
        SetupStageButtons();
        SetupPageArrows();
        RefreshPage();
    }

    private void LoadStageButtons()
    {
        Transform searchRoot = LoadStageButtonRoot();
        stageButtons = searchRoot.GetComponentsInChildren<StageSelectButton>(true);

        if (stageButtonPrefab == null && stageButtons.Length > 0)
            stageButtonPrefab = stageButtons[0];

        int requiredButtonCount = stagesPerPage <= 0
            ? StageCount
            : Mathf.Min(StageCount, stagesPerPage);

        if (stageButtonPrefab == null || requiredButtonCount <= stageButtons.Length)
            return;

        List<StageSelectButton> buttons = new(stageButtons);

        while (buttons.Count < requiredButtonCount)
        {
            StageSelectButton stageButton = Instantiate(stageButtonPrefab, stageButtonRoot);
            stageButton.name = $"StageButton_{buttons.Count + 1:D2}";
            buttons.Add(stageButton);
        }

        stageButtons = buttons.ToArray();
    }

    protected virtual void LoadStagesFromResources()
    {
        StageConfig[] loadedStages = Resources.LoadAll<StageConfig>(StagesResourcesPath);
        Array.Sort(loadedStages, (left, right) => left.StageNumber.CompareTo(right.StageNumber));
        stages = new List<StageConfig>(loadedStages);

    }

    private void LoadPageArrows()
    {
        if (pageArrows != null && pageArrows.Length > 0) return;

        pageArrows = GetComponentsInChildren<ButtonStagePageArrow>(true);
    }

    private void LoadBackButton()
    {
        if (backButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button foundButton in buttons)
            {
                if (foundButton == null) continue;

                string buttonName = foundButton.name.ToLowerInvariant();
                if (buttonName.Contains("back"))
                {
                    backButton = foundButton;
                    break;
                }
            }
        }
    }

    private Transform LoadStageButtonRoot()
    {
        if (stageButtonRoot != null)
            return stageButtonRoot;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null || child == transform) continue;

            if (child.name.Contains("StageGrid"))
            {
                stageButtonRoot = child;
                return stageButtonRoot;
            }
        }

        stageButtonRoot = transform;
        return stageButtonRoot;
    }

    private void SetupStageButtons()
    {
        LoadStageButtons();

        if (stageButtons == null) return;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            StageSelectButton stageButton = stageButtons[i];
            if (stageButton == null) continue;

            stageButton.SetStageSelectPanel(this);

            if (!stageButton.HasStageIndex)
                stageButton.SetStageIndex(i);
        }
    }

    private void SetupPageArrows()
    {
        LoadPageArrows();

        if (pageArrows == null) return;

        foreach (ButtonStagePageArrow pageArrow in pageArrows)
        {
            if (pageArrow == null) continue;

            pageArrow.SetStageSelectPanel(this);
        }
    }

    private void RefreshPage()
    {
        LoadStageButtons();
        SetupStageButtons();

        int pageSize = PageSize;
        if (pageSize <= 0) return;

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, PageCount - 1);
        int firstStageIndex = currentPageIndex * pageSize;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            StageSelectButton stageButton = stageButtons[i];
            if (stageButton == null) continue;

            if (i >= pageSize)
            {
                stageButton.SetAvailable(false);
                continue;
            }

            int stageIndex = firstStageIndex + i;
            bool exists = stageIndex < StageCount;

            stageButton.SetAvailable(exists);
            if (!exists) continue;

            stageButton.SetStageIndex(stageIndex);
            stageButton.SetUnlocked(IsStageUnlocked(stageIndex));
        }

        RefreshPageArrows();
    }

    private bool IsStageUnlocked(int stageIndex)
    {
        int safeStageIndex = Mathf.Max(0, stageIndex);
        int highestUnlockedStageIndex = StageSelectionStorage.LoadHighestUnlockedStageIndex();

        if (useSerializedUnlockProgress)
            highestUnlockedStageIndex = Mathf.Max(
                highestUnlockedStageIndex,
                Mathf.Max(0, serializedHighestUnlockedStageIndex));

        return safeStageIndex <= highestUnlockedStageIndex;
    }

    private void RefreshPageArrows()
    {
        LoadPageArrows();

        if (pageArrows == null) return;

        foreach (ButtonStagePageArrow pageArrow in pageArrows)
        {
            if (pageArrow == null) continue;

            bool interactable = pageArrow.Direction == StagePageDirection.Previous
                ? CanGoPrevious
                : CanGoNext;

            pageArrow.SetInteractable(interactable);
        }
    }

    private void BindBackButton()
    {
        LoadBackButton();

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Back);
            backButton.onClick.AddListener(Back);
        }
    }
}
