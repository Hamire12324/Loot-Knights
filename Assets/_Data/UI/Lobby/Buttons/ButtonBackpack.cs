public class ButtonBackpack : ButtonLobbySection
{
    protected override void HandleClick(LobbyPanel panel)
    {
        panel.OpenBackpack();
    }
}
