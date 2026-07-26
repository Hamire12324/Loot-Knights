public class ButtonSettings : ButtonLobbySection
{
    protected override void HandleClick(LobbyPanel panel)
    {
        if (panel != null)
        {
            panel.OpenSettings();
            return;
        }

        if (SettingsPanel.Instance != null)
        {
            SettingsPanel.Instance.Show();
        }
    }
}
