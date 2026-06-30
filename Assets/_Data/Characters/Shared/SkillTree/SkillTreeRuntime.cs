using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeRuntime
{
    private readonly SkillTreeDefinition tree;

    public SkillTreeRuntime(SkillTreeDefinition tree)
    {
        this.tree = tree;
    }

    public bool CanUpgrade(SkillTreeNodeDefinition node, int playerLevel, out string reason)
    {
        reason = string.Empty;

        if (tree == null)
        {
            reason = "Missing skill tree.";
            return false;
        }

        if (node == null)
        {
            reason = "Missing node.";
            return false;
        }

        int currentRank = GetRank(node);
        if (currentRank >= node.MaxRank)
        {
            reason = "Node is already max rank.";
            return false;
        }

        if (playerLevel < node.RequiredPlayerLevel)
        {
            reason = $"Requires player level {node.RequiredPlayerLevel}.";
            return false;
        }

        if (PlayerSkillTreeStorage.AvailablePoints < node.PointCost)
        {
            reason = "Not enough skill points.";
            return false;
        }

        foreach (SkillTreePrerequisite prerequisite in node.Prerequisites)
        {
            if (prerequisite == null || prerequisite.Node == null) continue;

            int prerequisiteRank = GetRank(prerequisite.Node);
            if (prerequisiteRank >= prerequisite.RequiredRank) continue;

            reason = $"Requires {prerequisite.Node.DisplayName} rank {prerequisite.RequiredRank}.";
            return false;
        }

        return true;
    }

    public bool TryUpgrade(SkillTreeNodeDefinition node, int playerLevel, out string reason)
    {
        if (!CanUpgrade(node, playerLevel, out reason))
            return false;

        if (!PlayerSkillTreeStorage.TrySpendPoints(node.PointCost))
        {
            reason = "Not enough skill points.";
            return false;
        }

        PlayerSkillTreeStorage.SetRank(tree, node, GetRank(node) + 1);
        reason = string.Empty;
        return true;
    }

    public int GetRank(SkillTreeNodeDefinition node)
    {
        return PlayerSkillTreeStorage.GetRank(tree, node);
    }

    public List<StatModifier> CreateStatModifiers()
    {
        List<StatModifier> modifiers = new();
        if (tree == null) return modifiers;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null) continue;

            int rank = GetRank(node);
            if (rank <= 0) continue;

            modifiers.AddRange(node.CreateStatModifiers(rank));
        }

        return modifiers;
    }

    public List<HeroSkillDefinition> GetUnlockedActiveSkills()
    {
        List<HeroSkillDefinition> skills = new();
        if (tree == null) return skills;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null ||
                node.Kind != SkillTreeNodeKind.ActiveSkill ||
                node.ActiveSkill == null ||
                GetRank(node) <= 0)
            {
                continue;
            }

            skills.Add(node.ActiveSkill);
        }

        return skills;
    }

    public bool HasElement(ElementType element)
    {
        if (tree == null || element == ElementType.None)
            return false;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null ||
                node.Kind != SkillTreeNodeKind.ElementUnlock ||
                node.Element != element)
            {
                continue;
            }

            if (GetRank(node) > 0)
                return true;
        }

        return false;
    }

    public bool HasReaction(ElementalReactionType reaction)
    {
        if (tree == null || reaction == ElementalReactionType.None)
            return false;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null ||
                node.Kind != SkillTreeNodeKind.ElementReaction ||
                node.Reaction != reaction)
            {
                continue;
            }

            if (GetRank(node) > 0)
                return true;
        }

        return false;
    }
}
