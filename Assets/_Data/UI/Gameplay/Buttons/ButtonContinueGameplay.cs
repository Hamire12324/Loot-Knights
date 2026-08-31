using UnityEngine;

/// <summary>Attach to Btn_Continue in PauseMenuPanel.</summary>
public class ButtonContinueGameplay : ButtonAbstract
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
            gameplayPanel.ResumeGame();
            return;
        }

        Transform pauseMenu = transform.parent;
        while (pauseMenu != null && pauseMenu.name != "PauseMenuPanel")
        {
            pauseMenu = pauseMenu.parent;
        }

        if (pauseMenu != null)
        {
            pauseMenu.gameObject.SetActive(false);
        }
    }
}
