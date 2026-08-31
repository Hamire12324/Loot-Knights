/// <summary>Shared click-forwarding behaviour for every UI Back button.</summary>
public class ButtonBack : ButtonAbstract
{
    protected override void OnClick()
    {
        NotifyClicked();
    }
}
