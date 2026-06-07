public class ButtonAddDiamonds : ButtonLobbySection
{
    private const int TestAmount = 10;

    protected override void HandleClick(LobbyPanel panel)
    {
#if !UNITY_EDITOR
        return;
#else
        PlayerCurrencyStorage.Add(CurrencyType.Diamonds, TestAmount);
        panel?.OpenAddDiamonds();
#endif
    }
}
