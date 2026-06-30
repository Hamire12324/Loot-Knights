using UnityEngine;

public sealed class SkillTreeStatApplier : CharacterAbstract
{
    [SerializeField] private SkillTreeDefinition skillTree;

    private SkillTreeRuntime runtime;

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerSkillTreeStorage.OnChanged += Apply;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelChanged;
        RefreshRuntime();
        Apply();
    }

    protected override void OnDisable()
    {
        PlayerSkillTreeStorage.OnChanged -= Apply;
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
        runtime ??= new SkillTreeRuntime(skillTree);
        if (skillTree == null)
            skillTree = Resources.Load<SkillTreeDefinition>("SkillTrees/Hero_SkillTree");

        runtime = new SkillTreeRuntime(skillTree);
    }

    private void HandleLevelChanged(PlayerLevelSnapshot snapshot)
    {
        PlayerSkillTreeStorage.EnsureLevelRewarded(snapshot.Level);
    }
}
