public class ButtonStartGameplay : ButtonLobbySection
{
    protected override void HandleClick(LobbyPanel panel)
    {
        panel.ReadyGo();
    }
}
