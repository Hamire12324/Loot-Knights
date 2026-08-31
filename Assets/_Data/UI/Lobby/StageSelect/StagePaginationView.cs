using UnityEngine;

public sealed class StagePaginationView
{
    private readonly ButtonStagePageArrow[] arrows;

    public StagePaginationView(ButtonStagePageArrow[] arrows)
    {
        this.arrows = arrows;
    }

    public void Initialize(StageSelectPanel panel)
    {
        foreach (ButtonStagePageArrow arrow in arrows)
            arrow?.SetStageSelectPanel(panel);
    }

    public void Refresh(bool canGoPrevious, bool canGoNext)
    {
        foreach (ButtonStagePageArrow arrow in arrows)
        {
            if (arrow == null) continue;
            arrow.SetInteractable(arrow.Direction == StagePageDirection.Previous ? canGoPrevious : canGoNext);
        }
    }
}
