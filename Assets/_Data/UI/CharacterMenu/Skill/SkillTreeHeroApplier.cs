using System.Collections.Generic;
using UnityEngine;

public static class SkillTreeHeroApplier
{
    public static void ApplyStats(SkillTreeDefinition skillTree)
    {
        ApplyStats(ToTreeList(skillTree));
    }

    public static void ApplyStats(IReadOnlyList<SkillTreeDefinition> skillTrees)
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.CharacterStat == null)
            return;

        List<StatModifier> modifiers = new();
        if (skillTrees != null)
        {
            foreach (SkillTreeDefinition tree in skillTrees)
            {
                if (tree == null)
                    continue;

                SkillTreeRuntime runtime = new(tree);
                modifiers.AddRange(runtime.CreateStatModifiers());
            }
        }

        hero.CharacterStat.RecalculateSkillTree(modifiers);
    }

    public static void ApplyLoadout(SkillTreeDefinition skillTree, int slotCount)
    {
        ApplyLoadout(ToTreeList(skillTree), slotCount);
    }

    public static void ApplyLoadout(IReadOnlyList<SkillTreeDefinition> skillTrees, int slotCount)
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        PlayerSkillTreeManager.Service.ApplyEquippedSkillsToHero(hero, skillTrees, slotCount);

        HeroSkillLoadoutPhotonSync loadoutSync = hero != null
            ? hero.GetComponent<HeroSkillLoadoutPhotonSync>()
            : null;

        ConfigureLoadoutSync(loadoutSync, skillTrees);
        loadoutSync?.PublishLocalLoadout();
    }

    private static void ConfigureLoadoutSync(
        HeroSkillLoadoutPhotonSync loadoutSync,
        IReadOnlyList<SkillTreeDefinition> skillTrees)
    {
        if (loadoutSync == null || skillTrees == null || skillTrees.Count == 0)
            return;

        SkillTreeDefinition primaryTree = skillTrees[0];
        List<SkillTreeDefinition> additionalTrees = new();
        for (int i = 1; i < skillTrees.Count; i++)
        {
            if (skillTrees[i] != null)
                additionalTrees.Add(skillTrees[i]);
        }

        loadoutSync.SetSkillTrees(primaryTree, additionalTrees.ToArray());
    }

    private static IReadOnlyList<SkillTreeDefinition> ToTreeList(SkillTreeDefinition skillTree)
    {
        return skillTree != null
            ? new[] { skillTree }
            : System.Array.Empty<SkillTreeDefinition>();
    }
}
