using UnityEngine;

public class HeroLevel : CharacterLevel
{
    [SerializeField] private bool usePlayerProgression = true;

    public HeroCtrl Hero => characterCtrl as HeroCtrl;
    public PlayerLevelSnapshot CurrentSnapshot { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribePlayerLevel();
        SubscribeAttributePoints();
        RefreshFromPlayerProgression();
    }

    protected override void OnDisable()
    {
        UnsubscribePlayerLevel();
        UnsubscribeAttributePoints();
        base.OnDisable();
    }

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<HeroCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no HeroCtrl in {gameObject.name}", gameObject);
    }

    public void RefreshFromPlayerProgression()
    {
        if (!usePlayerProgression) return;

        PlayerAttributePointStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        ApplySnapshot(PlayerExperienceStorage.Snapshot);
    }

    public void ApplySnapshot(PlayerLevelSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
        ApplyLevel(snapshot.Level);
    }

    protected override StatType[] GetAllocatedStatTypes()
    {
        return PlayerAttributePointStorage.GetSpendableStats();
    }

    protected override float GetAllocatedStatBonus(StatType statType)
    {
        return PlayerAttributePointStorage.GetBonusValue(statType);
    }

    private void SubscribePlayerLevel()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelSnapshotChanged;
    }

    private void UnsubscribePlayerLevel()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
    }

    private void SubscribeAttributePoints()
    {
        PlayerAttributePointStorage.OnPointsChanged -= HandleAttributePointsChanged;
        PlayerAttributePointStorage.OnPointsChanged += HandleAttributePointsChanged;
    }

    private void UnsubscribeAttributePoints()
    {
        PlayerAttributePointStorage.OnPointsChanged -= HandleAttributePointsChanged;
    }

    private void HandleLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        if (!usePlayerProgression) return;

        ApplySnapshot(snapshot);
    }

    private void HandleAttributePointsChanged()
    {
        if (!usePlayerProgression) return;

        ApplyAllocatedStats();
    }
}
