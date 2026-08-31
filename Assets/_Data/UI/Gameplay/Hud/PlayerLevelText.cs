public sealed class PlayerLevelText : TextAbstract
{
    private HeroCtrl hero;

    protected override void OnEnable()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelSnapshotChanged;
        Rebind();
    }

    protected override void OnDisable()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
        hero = null;
    }

    protected override void Update()
    {
        if (hero != HeroCtrl.GetLocal())
            Rebind();
    }

    private void Rebind()
    {
        hero = HeroCtrl.GetLocal();
        Refresh(PlayerExperienceStorage.Snapshot);
    }

    private void HandleLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        Refresh(snapshot);
    }

    private void Refresh(PlayerLevelSnapshot snapshot)
    {
        if (text == null || hero == null) return;

        text.text = snapshot.Level.ToString();
    }
}
