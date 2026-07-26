using UnityEngine;

public sealed class SkillTreeStatApplier : CharacterAbstract
{
    [SerializeField] private SkillTreeDefinition skillTree;

    private SkillTreeRuntime runtime;
    private PlayerSkillTreeManager skillTreeManager;

    protected override void OnEnable()
    {
        base.OnEnable();
        skillTreeManager = PlayerSkillTreeManager.Service;
        skillTreeManager.OnChanged += Apply;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelChanged;
        RefreshRuntime();
        Apply();
    }

    protected override void OnDisable()
    {
        if (skillTreeManager != null)
            skillTreeManager.OnChanged -= Apply;

        skillTreeManager = null;
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelChanged;
        base.OnDisable();
    }

    public void SetSkillTree(SkillTreeDefinition tree)
    {
        if (skillTree == tree) return;

        skillTree = tree;
        RefreshRuntime();
        Apply();
    }

    public void Apply()
    {
        RefreshRuntime();
        characterCtrl?.CharacterStat?.RecalculateSkillTree(runtime.CreateStatModifiers());
    }

    private void RefreshRuntime()
    {
        runtime = new SkillTreeRuntime(skillTree);
    }

    private void HandleLevelChanged(PlayerLevelSnapshot snapshot)
    {
        (skillTreeManager != null ? skillTreeManager : PlayerSkillTreeManager.Service).EnsureLevelRewarded(snapshot.Level);
    }
}
