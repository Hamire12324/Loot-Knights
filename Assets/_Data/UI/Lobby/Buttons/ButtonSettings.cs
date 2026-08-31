using UnityEngine;

public class ButtonSettings : ButtonLobbySection
{
    [SerializeField] private LobbyMenuPanel lobbyMenuPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (lobbyMenuPanel != null) return;

        lobbyMenuPanel = FindAnyObjectByType<LobbyMenuPanel>(FindObjectsInactive.Include);
    }

    protected override void HandleClick(LobbyPanel panel)
    {
        if (lobbyMenuPanel == null)
        {
            LoadComponents();
        }

        if (lobbyMenuPanel == null)
        {
            Debug.LogWarning("ButtonSettings: Missing LobbyMenuPanel.", gameObject);
            return;
        }

        lobbyMenuPanel.Show();
    }
}
