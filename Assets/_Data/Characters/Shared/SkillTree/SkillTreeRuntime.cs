using System.Collections.Generic;

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

        if (PlayerSkillTreeManager.Service.AvailablePoints < node.PointCost)
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

        if (!PlayerSkillTreeManager.Service.TrySpendPoints(node.PointCost))
        {
            reason = "Not enough skill points.";
            return false;
        }

        PlayerSkillTreeManager.Service.SetRank(tree, node, GetRank(node) + 1);
        reason = string.Empty;
        return true;
    }

    public int GetRank(SkillTreeNodeDefinition node)
    {
        return PlayerSkillTreeManager.Service.GetRank(tree, node);
    }

    public int GetRank(string nodeId)
    {
        return GetRank(tree != null ? tree.FindNode(nodeId) : null);
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
                IsSpecialActiveSkillNode(node) ||
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

    public List<ElementType> GetUnlockedElements()
    {
        List<ElementType> elements = new();
        if (tree == null) return elements;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null ||
                node.Kind != SkillTreeNodeKind.ElementUnlock ||
                node.Element == ElementType.None ||
                GetRank(node) <= 0 ||
                elements.Contains(node.Element))
            {
                continue;
            }

            elements.Add(node.Element);
        }

        return elements;
    }

    public bool HasAnyElementUnlockNodes()
    {
        if (tree == null) return false;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null && node.Kind == SkillTreeNodeKind.ElementUnlock)
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

    public bool HasAnyReactionUnlockNodes()
    {
        if (tree == null) return false;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null && node.Kind == SkillTreeNodeKind.ElementReaction)
                return true;
        }

        return false;
    }

    private bool IsSpecialActiveSkillNode(SkillTreeNodeDefinition node)
    {
        return tree != null &&
               node != null &&
               node.Kind == SkillTreeNodeKind.ActiveSkill &&
               node.ActiveSkill != null &&
               IsElementalSkillTree();
    }

    private bool IsElementalSkillTree()
    {
        if (tree == null)
            return false;

        if (!string.IsNullOrWhiteSpace(tree.TreeId) &&
            tree.TreeId.IndexOf("element", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null &&
                (node.Element != ElementType.None ||
                 node.Reaction != ElementalReactionType.None ||
                 node.Kind == SkillTreeNodeKind.ElementUnlock ||
                 node.Kind == SkillTreeNodeKind.ElementReaction))
            {
                return true;
            }
        }

        return false;
    }
}
