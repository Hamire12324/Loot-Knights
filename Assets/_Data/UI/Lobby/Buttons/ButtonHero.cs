public class ButtonHero : ButtonLobbySection
{
    protected override void HandleClick(LobbyPanel panel)
    {
        panel.OpenHero();
    }
}
