public class AttributePointsText : TextAbstract
{
    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerAttributePointStorage.OnPointsChanged += Refresh;
        Refresh();
    }

    protected override void OnDisable()
    {
        PlayerAttributePointStorage.OnPointsChanged -= Refresh;
        base.OnDisable();
    }

    public void Refresh()
    {
        PlayerAttributePointStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);

        if (text != null)
            text.text = $"Point: {PlayerAttributePointStorage.AvailablePoints:N0}";
    }
}
