using UnityEngine;

public abstract class ButtonLobbySection : ButtonAbstract
{
    [SerializeField] private LobbyPanel lobbyPanel;

    public void SetLobbyPanel(LobbyPanel panel)
    {
        lobbyPanel = panel;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadLobbyPanel();
    }

    protected override void OnClick()
    {
        HandleClick(lobbyPanel);
    }

    protected abstract void HandleClick(LobbyPanel panel);

    protected virtual void LoadLobbyPanel()
    {
        if (lobbyPanel != null) return;

        lobbyPanel = GetComponentInParent<LobbyPanel>();

        if (lobbyPanel == null)
        {
            lobbyPanel = FindAnyObjectByType<LobbyPanel>(FindObjectsInactive.Include);
        }
    }
}
