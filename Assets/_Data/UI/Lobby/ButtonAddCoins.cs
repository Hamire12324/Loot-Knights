public class ButtonAddCoins : ButtonLobbySection
{
    private const int TestAmount = 100;

    protected override void HandleClick(LobbyPanel panel)
    {
#if !UNITY_EDITOR
        return;
#else
        PlayerCurrencyStorage.Add(CurrencyType.Coins, TestAmount);
        panel?.OpenAddCoins();
#endif
    }
}
