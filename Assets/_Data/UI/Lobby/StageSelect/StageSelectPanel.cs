using System;
using UnityEngine;
using UnityEngine.UI;
public class StageSelectPanel : BaseMonoBehaviour
{
    private const string StagesResourcesPath = "Stages";

    public event Action<int> OnStageSelected;
    public event Action OnBackRequested;

    [Header("Stage data")]
    [SerializeField] private StageConfig[] stages;
    [SerializeField] private int stagesPerPage = 14;
    [SerializeField] private bool useSerializedUnlockProgress;
    [SerializeField] private int serializedHighestUnlockedStageIndex;
    [SerializeField] private int currentPageIndex;

    [Header("Stage buttons")]
    [SerializeField] private Transform stageButtonRoot;
    [SerializeField] private StageSelectButton stageButtonPrefab;
    [SerializeField] private StageSelectButton[] stageButtons;

    [Header("Navigation")]
    [SerializeField] private ButtonStagePageArrow[] pageArrows;
    [SerializeField] private Button backButton;

    private StageSelectGridView gridView;
    private StagePaginationView paginationView;

    public int CurrentPageIndex => currentPageIndex;
    public int StageCount => stages?.Length ?? 0;
    public int PageSize
    {
        get
        {
            int buttonCount = gridView?.ButtonCount ?? stageButtons?.Length ?? 0;
            return buttonCount == 0
                ? 0
                : stagesPerPage > 0 ? Mathf.Min(stagesPerPage, buttonCount) : buttonCount;
        }
    }

    public int PageCount => PageSize == 0
        ? 1
        : Mathf.Max(1, Mathf.CeilToInt((float)Mathf.Max(1, StageCount) / PageSize));

    public bool CanGoPrevious => currentPageIndex > 0;
    public bool CanGoNext => currentPageIndex < PageCount - 1;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadStagesFromResources();
        LoadStageButtonReferences();
        LoadBackButton();
        InitializeViews();
    }

    private void OnValidate()
    {
        LoadStageButtonReferences();
        LoadBackButton();
    }

    protected override void Start()
    {
        base.Start();
        BindBackButton();
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

    protected virtual void LoadStagesFromResources()
    {
        StageConfig[] loadedStages = Resources.LoadAll<StageConfig>(StagesResourcesPath);
        Array.Sort(loadedStages, (left, right) => left.StageNumber.CompareTo(right.StageNumber));
        stages = loadedStages;
    }

    private void RefreshPage()
    {
        int pageSize = PageSize;
        if (pageSize == 0)
        {
            paginationView?.Refresh(false, false);
            return;
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, PageCount - 1);
        gridView?.ShowPage(currentPageIndex, pageSize, StageCount, IsStageUnlocked);
        paginationView?.Refresh(CanGoPrevious, CanGoNext);
    }

    private void InitializeViews()
    {
        gridView = new StageSelectGridView(stageButtonRoot, stageButtonPrefab, stageButtons);
        gridView.Initialize(this, stagesPerPage > 0 ? Mathf.Min(StageCount, stagesPerPage) : StageCount);

        if (pageArrows == null || pageArrows.Length == 0)
            pageArrows = GetComponentsInChildren<ButtonStagePageArrow>(true);

        paginationView = new StagePaginationView(pageArrows);
        paginationView.Initialize(this);
    }

    private void LoadStageButtonReferences()
    {
        if (stageButtonRoot == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.Contains("StageGrid", StringComparison.OrdinalIgnoreCase))
                {
                    stageButtonRoot = child;
                    break;
                }
            }
        }

        stageButtonRoot ??= transform;

        if (stageButtons == null || stageButtons.Length == 0)
            stageButtons = stageButtonRoot.GetComponentsInChildren<StageSelectButton>(true);

        if (stageButtonPrefab == null && stageButtons.Length > 0)
            stageButtonPrefab = stageButtons[0];
    }

    private bool IsStageUnlocked(int stageIndex)
    {
        int highestUnlockedStageIndex = StageSelectionStorage.LoadHighestUnlockedStageIndex();
        if (useSerializedUnlockProgress)
        {
            highestUnlockedStageIndex = Mathf.Max(highestUnlockedStageIndex, serializedHighestUnlockedStageIndex);
        }

        return stageIndex <= Mathf.Max(0, highestUnlockedStageIndex);
    }

    private void LoadBackButton()
    {
        if (backButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                backButton = button;
                return;
            }
        }
    }

    private void BindBackButton()
    {
        LoadBackButton();
        if (backButton == null) return;

        backButton.onClick.RemoveListener(Back);
        backButton.onClick.AddListener(Back);
    }
}
