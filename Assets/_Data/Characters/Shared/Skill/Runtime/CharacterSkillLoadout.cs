using System.Collections.Generic;

public sealed class CharacterSkillLoadout
{
    private readonly List<CharacterSkillRuntime> runtimeSkills = new();

    public CharacterSkillRuntime BasicAttackRuntime { get; private set; }
    public IReadOnlyList<CharacterSkillRuntime> RuntimeSkills => runtimeSkills;

    public CharacterSkillRuntime GetSkill(int index)
    {
        if (index < 0 || index >= runtimeSkills.Count) return null;
        return runtimeSkills[index];
    }

    public void Rebuild(CharacterSkillDefinition basicAttack,
        IReadOnlyList<CharacterSkillDefinition> equippedSkills)
    {
        BasicAttackRuntime = basicAttack != null
            ? new CharacterSkillRuntime(basicAttack)
            : null;

        runtimeSkills.Clear();

        if (equippedSkills == null) return;

        foreach (CharacterSkillDefinition definition in equippedSkills)
            runtimeSkills.Add(definition != null ? new CharacterSkillRuntime(definition) : null);
    }
}
