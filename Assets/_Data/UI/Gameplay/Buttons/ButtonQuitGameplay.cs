using UnityEngine;

/// <summary>Attach to Btn_QuitGame in PauseMenuPanel.</summary>
public class ButtonQuitGameplay : ButtonAbstract
{
    [SerializeField] private GameplayPanel gameplayPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        gameplayPanel ??= FindAnyObjectByType<GameplayPanel>();
    }

    protected override void OnClick()
    {
        if (gameplayPanel != null)
        {
            gameplayPanel.QuitGame();
            return;
        }

        Application.Quit();
    }
}
