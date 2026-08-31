using UnityEngine;

/// <summary>Attach to the HUD gear button to open or close PauseMenuPanel.</summary>
public class ButtonPauseMenu : ButtonAbstract
{
    [SerializeField] private GameplayPanel gameplayPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        gameplayPanel ??= FindAnyObjectByType<GameplayPanel>();
    }

    protected override void OnClick()
    {
        if (gameplayPanel != null) gameplayPanel.TogglePauseMenu();
    }
}
