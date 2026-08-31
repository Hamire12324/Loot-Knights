using UnityEngine;

/// <summary>Attach to Btn_Lobby in PauseMenuPanel to leave gameplay for the player's lobby.</summary>
public class ButtonReturnToLobby : ButtonAbstract
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
