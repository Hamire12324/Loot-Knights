using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectPanel : BaseMonoBehaviour
{
    public event Action<int> OnStageSelected;
    public event Action OnBackRequested;

    [SerializeField] private int totalStageCount = 3;
    [SerializeField] private int stagesPerPage = 14;
    [SerializeField] private bool useSerializedUnlockProgress;
    [SerializeField] private int serializedHighestUnlockedStageIndex;
    [SerializeField] private int currentPageIndex;
    [SerializeField] private Transform stageButtonRoot;
    [SerializeField] private StageSelectButton[] stageButtons;
    [SerializeField] private ButtonStagePageArrow[] pageArrows;
    [SerializeField] private Button backButton;

    public int CurrentPageIndex => currentPageIndex;
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
    public int PageCount => PageSize <= 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt((float)Mathf.Max(1, totalStageCount) / PageSize));
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
        if (safeStageIndex >= totalStageCount) return;

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
        Button[] buttons = searchRoot.GetComponentsInChildren<Button>(true);
        List<StageSelectButton> foundStageButtons = new();

        foreach (Button foundButton in buttons)
        {
            if (!IsStageButton(foundButton)) continue;

            StageSelectButton stageButton = foundButton.GetComponent<StageSelectButton>();
            if (stageButton == null)
                stageButton = foundButton.gameObject.AddComponent<StageSelectButton>();

            foundStageButtons.Add(stageButton);
        }

        stageButtons = foundStageButtons.ToArray();
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

            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("stagegrid") ||
                childName.Contains("stage_grid") ||
                childName.Contains("stagebuttons") ||
                childName.Contains("stage_buttons") ||
                childName.Contains("grid"))
            {
                stageButtonRoot = child;
                return stageButtonRoot;
            }
        }

        stageButtonRoot = transform;
        return stageButtonRoot;
    }

    private bool IsStageButton(Button foundButton)
    {
        if (foundButton == null) return false;
        if (foundButton == backButton) return false;
        if (foundButton.GetComponent<ButtonStagePageArrow>() != null) return false;

        string buttonName = foundButton.name.ToLowerInvariant();
        if (buttonName.Contains("arrow") ||
            buttonName.Contains("left") ||
            buttonName.Contains("right") ||
            buttonName.Contains("back"))
        {
            return false;
        }

        return true;
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
            bool exists = stageIndex < totalStageCount;

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
