using UnityEngine;

/// <summary>Legacy name. Attach ButtonReturnToLobby to the lobby button instead.</summary>
public class ButtonReturnToMainMenu : ButtonAbstract
{
    [SerializeField] private GameplayPanel gameplayPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        gameplayPanel ??= FindAnyObjectByType<GameplayPanel>();
    }

    protected override void OnClick()
    {
        if (gameplayPanel != null) gameplayPanel.ReturnToLobby();
    }
}
