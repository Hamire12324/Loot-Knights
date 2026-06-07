using UnityEngine;

public enum StagePageDirection
{
    Previous,
    Next
}

public class ButtonStagePageArrow : ButtonAbstract
{
    [SerializeField] private StageSelectPanel stageSelectPanel;
    [SerializeField] private StagePageDirection direction = StagePageDirection.Next;

    public StagePageDirection Direction => direction;

    public void SetStageSelectPanel(StageSelectPanel panel)
    {
        stageSelectPanel = panel;
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadStageSelectPanel();
    }

    protected override void OnClick()
    {
        if (stageSelectPanel == null)
            LoadStageSelectPanel();

        if (stageSelectPanel == null)
        {
            Debug.LogError(transform.name + ": Missing StageSelectPanel reference.", gameObject);
            return;
        }

        if (direction == StagePageDirection.Previous)
            stageSelectPanel.PreviousPage();
        else
            stageSelectPanel.NextPage();
    }

    private void LoadStageSelectPanel()
    {
        if (stageSelectPanel != null) return;

        stageSelectPanel = GetComponentInParent<StageSelectPanel>();

        if (stageSelectPanel == null)
        {
            stageSelectPanel = FindAnyObjectByType<StageSelectPanel>(FindObjectsInactive.Include);
        }
    }
}
