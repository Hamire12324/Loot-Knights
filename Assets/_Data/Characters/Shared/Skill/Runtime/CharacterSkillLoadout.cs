using System;
using System.Collections.Generic;

public sealed class CharacterSkillLoadout
{
    private readonly List<CharacterSkillRuntime> runtimeSkills = new();

    public CharacterSkillRuntime BasicAttackRuntime { get; private set; }
    public CharacterSkillRuntime SpecialSkillRuntime { get; private set; }
    public IReadOnlyList<CharacterSkillRuntime> RuntimeSkills => runtimeSkills;

    public CharacterSkillRuntime GetSkill(int index)
    {
        if (index < 0 || index >= runtimeSkills.Count) return null;
        return runtimeSkills[index];
    }

    public void Rebuild(
        CharacterSkillDefinition basicAttack,
        IReadOnlyList<CharacterSkillDefinition> equippedSkills,
        CharacterSkillDefinition specialSkill = null)
    {
        CharacterSkillRuntime previousBasicAttack = BasicAttackRuntime;
        CharacterSkillRuntime previousSpecialSkill = SpecialSkillRuntime;
        List<CharacterSkillRuntime> previousRuntimeSkills = new(runtimeSkills);

        BasicAttackRuntime = GetReusableRuntime(previousBasicAttack, basicAttack)
            ?? CreateRuntime(basicAttack);

        SpecialSkillRuntime = GetReusableRuntime(previousSpecialSkill, specialSkill)
            ?? CreateRuntime(specialSkill);

        runtimeSkills.Clear();

        if (equippedSkills == null) return;

        HashSet<CharacterSkillRuntime> reusedRuntimes = new();
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            CharacterSkillDefinition definition = equippedSkills[i];
            CharacterSkillRuntime sameSlotRuntime = i < previousRuntimeSkills.Count
                ? previousRuntimeSkills[i]
                : null;

            CharacterSkillRuntime runtime = GetReusableRuntime(sameSlotRuntime, definition);
            if (runtime == null || reusedRuntimes.Contains(runtime))
                runtime = FindReusableRuntime(previousRuntimeSkills, reusedRuntimes, definition);

            runtime ??= CreateRuntime(definition);

            if (runtime != null)
                reusedRuntimes.Add(runtime);

            runtimeSkills.Add(runtime);
        }
    }

    private static CharacterSkillRuntime CreateRuntime(CharacterSkillDefinition definition)
    {
        return definition != null ? new CharacterSkillRuntime(definition) : null;
    }

    private static CharacterSkillRuntime GetReusableRuntime(
        CharacterSkillRuntime runtime,
        CharacterSkillDefinition definition)
    {
        return HasSameSkillId(runtime, definition) ? runtime : null;
    }

    private static CharacterSkillRuntime FindReusableRuntime(
        IReadOnlyList<CharacterSkillRuntime> previousRuntimes,
        HashSet<CharacterSkillRuntime> reusedRuntimes,
        CharacterSkillDefinition definition)
    {
        if (definition == null || previousRuntimes == null)
            return null;

        foreach (CharacterSkillRuntime runtime in previousRuntimes)
        {
            if (runtime == null || reusedRuntimes.Contains(runtime))
                continue;

            if (HasSameSkillId(runtime, definition))
                return runtime;
        }

        return null;
    }

    private static bool HasSameSkillId(CharacterSkillRuntime runtime, CharacterSkillDefinition definition)
    {
        if (runtime == null || runtime.Definition == null || definition == null)
            return false;

        return string.Equals(
            GetSkillId(runtime.Definition),
            GetSkillId(definition),
            StringComparison.Ordinal);
    }

    private static string GetSkillId(CharacterSkillDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(definition.SkillId)
            ? definition.SkillId.Trim()
            : definition.name;
    }
}
