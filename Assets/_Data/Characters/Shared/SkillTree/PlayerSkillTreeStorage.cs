using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillTreeStorage
{
    private const string AvailablePointsKey = "LootKnights.SkillTree.Available";
    private const string HighestRewardedLevelKey = "LootKnights.SkillTree.HighestRewardedLevel";
    private const string RankKeyPrefix = "LootKnights.SkillTree.Rank.";
    private const string EquippedNodeKeyPrefix = "LootKnights.SkillTree.Equipped.";
    private const string GlobalEquippedTreeKeyPrefix = "LootKnights.SkillTree.Equipped.Global.Tree.";
    private const string GlobalEquippedNodeKeyPrefix = "LootKnights.SkillTree.Equipped.Global.Node.";
    private const int DebugMinimumStartingPoints = 99;

    public const int PointsPerLevel = 1;

    public event Action OnChanged;

    public int AvailablePoints => Mathf.Max(0, PlayerPrefs.GetInt(AvailablePointsKey, 0));
    public int HighestRewardedLevel => Mathf.Max(1, PlayerPrefs.GetInt(HighestRewardedLevelKey, 1));

    public void EnsureDebugStartingPoints()
    {
        if (AvailablePoints >= DebugMinimumStartingPoints)
            return;

        PlayerPrefs.SetInt(AvailablePointsKey, DebugMinimumStartingPoints);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public void EnsureLevelRewarded(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        int highestRewardedLevel = HighestRewardedLevel;

        if (safeLevel <= highestRewardedLevel) return;

        int gainedLevels = safeLevel - highestRewardedLevel;
        PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints + gainedLevels * PointsPerLevel);
        PlayerPrefs.SetInt(HighestRewardedLevelKey, safeLevel);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public int GetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        if (tree == null || node == null) return 0;
        return Mathf.Max(0, PlayerPrefs.GetInt(GetRankKey(tree, node), 0));
    }

    public void GrantPoints(int amount)
    {
        if (amount <= 0) return;

        PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints + amount);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public bool TrySpendPoints(int amount)
    {
        if (amount <= 0 || AvailablePoints < amount)
            return false;

        PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints - amount);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public void SetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int rank)
    {
        if (tree == null || node == null) return;

        int safeRank = Mathf.Clamp(rank, 0, node.MaxRank);
        string key = GetRankKey(tree, node);

        if (safeRank <= 0)
            PlayerPrefs.DeleteKey(key);
        else
            PlayerPrefs.SetInt(key, safeRank);

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public void ClearTree(SkillTreeDefinition tree)
    {
        if (tree == null) return;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null) continue;
            PlayerPrefs.DeleteKey(GetRankKey(tree, node));
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public int ResetTreeAndRefund(SkillTreeDefinition tree, int slotCount)
    {
        if (tree == null) return 0;

        int refund = 0;
        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node == null) continue;

            int rank = GetRank(tree, node);
            if (rank > 0)
                refund += rank * Mathf.Max(0, node.PointCost);

            PlayerPrefs.DeleteKey(GetRankKey(tree, node));
        }

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            string globalTreeKey = GetGlobalEquippedTreeKey(i);
            string globalNodeKey = GetGlobalEquippedNodeKey(i);
            if (PlayerPrefs.GetString(globalTreeKey, string.Empty) == tree.TreeId)
            {
                PlayerPrefs.DeleteKey(globalTreeKey);
                PlayerPrefs.DeleteKey(globalNodeKey);
            }

            PlayerPrefs.DeleteKey(GetEquippedNodeKey(tree, i));
        }

        if (refund > 0)
            PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints + refund);

        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return refund;
    }

    public HeroSkillDefinition[] GetEquippedActiveSkills(SkillTreeDefinition tree, int slotCount)
    {
        return GetEquippedActiveSkills(ToTreeList(tree), slotCount, tree);
    }

    public HeroSkillDefinition[] GetEquippedActiveSkills(IReadOnlyList<SkillTreeDefinition> trees, int slotCount)
    {
        return GetEquippedActiveSkills(trees, slotCount, visibleTree: null);
    }

    public HeroSkillDefinition GetEquippedSpecialSkill(IReadOnlyList<SkillTreeDefinition> trees)
    {
        return TryGetUnlockedSpecialSkillNode(trees, out _, out SkillTreeNodeDefinition node)
            ? node.ActiveSkill
            : null;
    }

    public string GetEquippedSpecialSkillNodeId(IReadOnlyList<SkillTreeDefinition> trees)
    {
        return TryGetUnlockedSpecialSkillNode(trees, out SkillTreeDefinition tree, out SkillTreeNodeDefinition node)
            ? EncodeEquippedNodeId(tree, node)
            : string.Empty;
    }

    public bool IsSpecialActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return IsSpecialActiveSkillNode(tree, node);
    }

    public HeroSkillDefinition ResolveEquippedRegularSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        SkillTreeNodeDefinition resolvedNode = ResolveEquippedRegularSkillNode(tree, node);
        return resolvedNode != null ? resolvedNode.ActiveSkill : null;
    }

    private HeroSkillDefinition[] GetEquippedActiveSkills(
        IReadOnlyList<SkillTreeDefinition> trees,
        int slotCount,
        SkillTreeDefinition visibleTree)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        HeroSkillDefinition[] skills = new HeroSkillDefinition[safeSlotCount];

        for (int i = 0; i < safeSlotCount; i++)
            skills[i] = GetEquippedSkill(trees, i, visibleTree);

        return skills;
    }

    public int EnsureUnlockedActiveSkillsEquipped(SkillTreeDefinition tree, int slotCount)
    {
        if (tree == null)
            return 0;

        int safeSlotCount = Mathf.Max(1, slotCount);
        if (HasAnyValidEquippedActiveSkill(tree, safeSlotCount))
            return 0;

        bool[] occupiedSlots = new bool[safeSlotCount];
        for (int i = 0; i < safeSlotCount; i++)
            occupiedSlots[i] = IsAnyRegularGlobalSlotOccupied(i) || PlayerPrefs.HasKey(GetEquippedNodeKey(tree, i));

        int equippedCount = 0;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (!IsUnlockedRegularActiveSkill(tree, node))
                continue;

            int slotIndex = GetAutoEquipSlot(node, occupiedSlots);
            if (slotIndex < 0)
                break;

            if (!TryEquipActiveSkill(tree, node, slotIndex, safeSlotCount, out _))
                continue;

            occupiedSlots[slotIndex] = true;
            equippedCount++;
        }

        return equippedCount;
    }

    public string[] GetEquippedActiveSkillNodeIds(SkillTreeDefinition tree, int slotCount)
    {
        return GetEquippedActiveSkillNodeIds(ToTreeList(tree), slotCount);
    }

    public string[] GetEquippedActiveSkillNodeIds(IReadOnlyList<SkillTreeDefinition> trees, int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        string[] nodeIds = new string[safeSlotCount];

        for (int i = 0; i < safeSlotCount; i++)
        {
            if (TryGetEquippedNode(trees, i, out SkillTreeDefinition tree, out SkillTreeNodeDefinition node) &&
                IsUnlockedRegularActiveSkill(tree, node))
            {
                SkillTreeNodeDefinition resolvedNode = ResolveEquippedRegularSkillNode(tree, node);
                nodeIds[i] = EncodeEquippedNodeId(tree, resolvedNode != null ? resolvedNode : node);
            }
        }

        return nodeIds;
    }

    public bool IsEquipped(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount)
    {
        if (IsSpecialActiveSkillNode(tree, node))
            return false;

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            if (TryGetEquippedNode(ToTreeList(tree), i, out SkillTreeDefinition equippedTree, out SkillTreeNodeDefinition equippedNode) &&
                equippedTree == tree &&
                equippedNode == node &&
                IsUnlockedRegularActiveSkill(equippedTree, equippedNode))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasEquippedSlot(SkillTreeDefinition tree, int slotIndex)
    {
        if (tree == null || slotIndex < 0) return false;
        return TryGetEquippedNode(ToTreeList(tree), slotIndex, out SkillTreeDefinition equippedTree, out SkillTreeNodeDefinition node) &&
               equippedTree == tree &&
               IsUnlockedRegularActiveSkill(equippedTree, node);
    }

    public bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
    {
        reason = string.Empty;

        if (IsSpecialActiveSkillNode(tree, node))
        {
            reason = "Element skills use their own core slot.";
            return false;
        }

        int safeSlotCount = Mathf.Max(1, slotCount);
        int existingSlot = GetEquippedSlotIndex(tree, node, safeSlotCount);
        if (existingSlot >= 0)
        {
            reason = "Skill is already equipped.";
            return false;
        }

        int targetSlot = GetPreferredOrFirstOpenSlot(tree, node, safeSlotCount);

        return TryEquipActiveSkill(tree, node, targetSlot, safeSlotCount, out reason);
    }

    public bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotIndex, int slotCount, out string reason)
    {
        reason = string.Empty;

        if (tree == null)
        {
            reason = "Missing skill tree.";
            return false;
        }

        if (IsSpecialActiveSkillNode(tree, node))
        {
            reason = "Element skills use their own core slot.";
            return false;
        }

        if (!IsUnlockedRegularActiveSkill(tree, node))
        {
            reason = "Unlock this active skill first.";
            return false;
        }

        int safeSlotCount = Mathf.Max(1, slotCount);
        int safeSlotIndex = Mathf.Clamp(slotIndex, 0, safeSlotCount - 1);
        int existingSlot = GetEquippedSlotIndex(tree, node, safeSlotCount);
        if (existingSlot == safeSlotIndex)
        {
            reason = "Skill is already equipped in this slot.";
            return false;
        }

        if (existingSlot >= 0)
            PlayerPrefs.DeleteKey(GetEquippedNodeKey(tree, existingSlot));

        PlayerPrefs.DeleteKey(GetEquippedNodeKey(tree, safeSlotIndex));
        PlayerPrefs.SetString(GetGlobalEquippedTreeKey(safeSlotIndex), tree.TreeId);
        PlayerPrefs.SetString(GetGlobalEquippedNodeKey(safeSlotIndex), node.NodeId);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public bool TryUnequipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
    {
        reason = string.Empty;

        int slotIndex = GetEquippedSlotIndex(tree, node, slotCount);
        if (slotIndex < 0)
        {
            reason = "Skill is not equipped.";
            return false;
        }

        return TryUnequipSlot(tree, slotIndex, out reason);
    }

    public bool TryUnequipSlot(SkillTreeDefinition tree, int slotIndex, out string reason)
    {
        reason = string.Empty;

        if (tree == null)
        {
            reason = "Missing skill tree.";
            return false;
        }

        if (slotIndex < 0)
        {
            reason = "Invalid equip slot.";
            return false;
        }

        string globalTreeKey = GetGlobalEquippedTreeKey(slotIndex);
        string globalNodeKey = GetGlobalEquippedNodeKey(slotIndex);
        if (PlayerPrefs.GetString(globalTreeKey, string.Empty) == tree.TreeId)
        {
            PlayerPrefs.DeleteKey(globalTreeKey);
            PlayerPrefs.DeleteKey(globalNodeKey);
            PlayerPrefs.DeleteKey(GetEquippedNodeKey(tree, slotIndex));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
            return true;
        }

        string legacyKey = GetEquippedNodeKey(tree, slotIndex);
        if (!PlayerPrefs.HasKey(legacyKey))
        {
            reason = "Equip slot is already empty.";
            return false;
        }

        PlayerPrefs.DeleteKey(legacyKey);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public void ClearAllProgress(SkillTreeDefinition tree = null)
    {
        PlayerPrefs.DeleteKey(AvailablePointsKey);
        PlayerPrefs.DeleteKey(HighestRewardedLevelKey);

        if (tree != null)
        {
            foreach (SkillTreeNodeDefinition node in tree.Nodes)
            {
                if (node == null) continue;
                PlayerPrefs.DeleteKey(GetRankKey(tree, node));
            }
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    private static string GetRankKey(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return RankKeyPrefix + tree.TreeId + "." + node.NodeId;
    }

    private static string GetEquippedNodeKey(SkillTreeDefinition tree, int slotIndex)
    {
        return EquippedNodeKeyPrefix + tree.TreeId + "." + Mathf.Max(0, slotIndex);
    }

    private static string GetGlobalEquippedTreeKey(int slotIndex)
    {
        return GlobalEquippedTreeKeyPrefix + Mathf.Max(0, slotIndex);
    }

    private static string GetGlobalEquippedNodeKey(int slotIndex)
    {
        return GlobalEquippedNodeKeyPrefix + Mathf.Max(0, slotIndex);
    }

    private static bool IsAnyRegularGlobalSlotOccupied(int slotIndex)
    {
        string treeId = PlayerPrefs.GetString(GetGlobalEquippedTreeKey(slotIndex), string.Empty);
        string nodeId = PlayerPrefs.GetString(GetGlobalEquippedNodeKey(slotIndex), string.Empty);
        if (string.IsNullOrWhiteSpace(treeId) || string.IsNullOrWhiteSpace(nodeId))
            return false;

        return !LooksLikeSpecialSkillReference(treeId, nodeId);
    }

    private static bool LooksLikeSpecialSkillReference(string treeId, string nodeId)
    {
        return !string.IsNullOrWhiteSpace(treeId) &&
               treeId.IndexOf("element", StringComparison.OrdinalIgnoreCase) >= 0 &&
               !string.IsNullOrWhiteSpace(nodeId);
    }

    private static SkillTreeNodeDefinition FindNode(SkillTreeDefinition tree, string nodeId)
    {
        if (tree == null || string.IsNullOrWhiteSpace(nodeId))
            return null;

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null && node.NodeId == nodeId)
                return node;
        }

        return null;
    }

    private static List<SkillTreeDefinition> ToTreeList(SkillTreeDefinition tree)
    {
        List<SkillTreeDefinition> trees = new();
        if (tree != null)
            trees.Add(tree);

        return trees;
    }

    private HeroSkillDefinition GetEquippedSkill(
        IReadOnlyList<SkillTreeDefinition> trees,
        int slotIndex,
        SkillTreeDefinition visibleTree)
    {
        if (!TryGetEquippedNode(trees, slotIndex, out SkillTreeDefinition tree, out SkillTreeNodeDefinition node))
            return null;

        if (visibleTree != null && tree != visibleTree)
            return null;

        return ResolveEquippedRegularSkill(tree, node);
    }

    private SkillTreeNodeDefinition ResolveEquippedRegularSkillNode(
        SkillTreeDefinition tree,
        SkillTreeNodeDefinition baseNode)
    {
        if (!IsUnlockedRegularActiveSkill(tree, baseNode))
            return null;

        SkillTreeNodeDefinition resolvedNode = baseNode;
        foreach (SkillTreeNodeDefinition candidate in tree.Nodes)
        {
            if (!IsUnlockedSkillEvolution(tree, candidate, baseNode))
                continue;

            resolvedNode = candidate;
        }

        return resolvedNode;
    }

    private bool IsUnlockedSkillEvolution(
        SkillTreeDefinition tree,
        SkillTreeNodeDefinition candidate,
        SkillTreeNodeDefinition baseNode)
    {
        return tree != null &&
               candidate != null &&
               baseNode != null &&
               candidate.Kind == SkillTreeNodeKind.SkillUpgrade &&
               candidate.ActiveSkill != null &&
               GetRank(tree, candidate) > 0 &&
               DependsOn(candidate, baseNode, new HashSet<SkillTreeNodeDefinition>());
    }

    private static bool DependsOn(
        SkillTreeNodeDefinition candidate,
        SkillTreeNodeDefinition target,
        HashSet<SkillTreeNodeDefinition> visited)
    {
        if (candidate == null || target == null || visited == null || !visited.Add(candidate))
            return false;

        foreach (SkillTreePrerequisite prerequisite in candidate.Prerequisites)
        {
            SkillTreeNodeDefinition prerequisiteNode = prerequisite != null ? prerequisite.Node : null;
            if (prerequisiteNode == null)
                continue;

            if (prerequisiteNode == target ||
                DependsOn(prerequisiteNode, target, visited))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetEquippedNode(
        IReadOnlyList<SkillTreeDefinition> trees,
        int slotIndex,
        out SkillTreeDefinition tree,
        out SkillTreeNodeDefinition node)
    {
        tree = null;
        node = null;

        if (slotIndex < 0)
            return false;

        string globalTreeId = PlayerPrefs.GetString(GetGlobalEquippedTreeKey(slotIndex), string.Empty);
        string globalNodeId = PlayerPrefs.GetString(GetGlobalEquippedNodeKey(slotIndex), string.Empty);
        if (!string.IsNullOrWhiteSpace(globalTreeId) && !string.IsNullOrWhiteSpace(globalNodeId))
        {
            tree = FindTree(trees, globalTreeId);
            node = tree != null ? tree.FindNode(globalNodeId) : null;
            if (tree != null && node != null)
            {
                if (IsUnlockedRegularActiveSkill(tree, node))
                    return true;

                PlayerPrefs.DeleteKey(GetGlobalEquippedTreeKey(slotIndex));
                PlayerPrefs.DeleteKey(GetGlobalEquippedNodeKey(slotIndex));
                PlayerPrefs.Save();
            }
        }

        if (trees == null)
            return false;

        foreach (SkillTreeDefinition candidateTree in trees)
        {
            if (candidateTree == null)
                continue;

            string legacyNodeId = PlayerPrefs.GetString(GetEquippedNodeKey(candidateTree, slotIndex), string.Empty);
            SkillTreeNodeDefinition legacyNode = FindNode(candidateTree, legacyNodeId);
            if (!IsUnlockedRegularActiveSkill(candidateTree, legacyNode))
                continue;

            tree = candidateTree;
            node = legacyNode;
            return true;
        }

        return false;
    }

    private static SkillTreeDefinition FindTree(IReadOnlyList<SkillTreeDefinition> trees, string treeId)
    {
        if (trees == null || string.IsNullOrWhiteSpace(treeId))
            return null;

        foreach (SkillTreeDefinition tree in trees)
        {
            if (tree != null && tree.TreeId == treeId)
                return tree;
        }

        return null;
    }

    public static string EncodeEquippedNodeId(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        if (tree == null || node == null)
            return string.Empty;

        return tree.TreeId + "|" + node.NodeId;
    }

    public static bool TryDecodeEquippedNodeId(
        string value,
        out string treeId,
        out string nodeId)
    {
        treeId = string.Empty;
        nodeId = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        int splitIndex = value.IndexOf('|');
        if (splitIndex < 0)
        {
            nodeId = value;
            return true;
        }

        treeId = value[..splitIndex];
        nodeId = value[(splitIndex + 1)..];
        return !string.IsNullOrWhiteSpace(nodeId);
    }

    private bool IsUnlockedActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return tree != null &&
               node != null &&
               node.Kind == SkillTreeNodeKind.ActiveSkill &&
               node.ActiveSkill != null &&
               GetRank(tree, node) > 0;
    }

    private bool IsUnlockedRegularActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return IsUnlockedActiveSkill(tree, node) &&
               !IsSpecialActiveSkillNode(tree, node);
    }

    private bool TryGetUnlockedSpecialSkillNode(
        IReadOnlyList<SkillTreeDefinition> trees,
        out SkillTreeDefinition tree,
        out SkillTreeNodeDefinition node)
    {
        tree = null;
        node = null;

        if (trees == null)
            return false;

        foreach (SkillTreeDefinition candidateTree in trees)
        {
            if (candidateTree == null)
                continue;

            foreach (SkillTreeNodeDefinition candidateNode in candidateTree.Nodes)
            {
                if (!IsSpecialActiveSkillNode(candidateTree, candidateNode) ||
                    !IsUnlockedActiveSkill(candidateTree, candidateNode))
                {
                    continue;
                }

                tree = candidateTree;
                node = candidateNode;
                return true;
            }
        }

        return false;
    }

    private static bool IsSpecialActiveSkillNode(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return tree != null &&
               node != null &&
               node.Kind == SkillTreeNodeKind.ActiveSkill &&
               node.ActiveSkill != null &&
               IsElementalSkillTree(tree);
    }

    private static bool IsElementalSkillTree(SkillTreeDefinition tree)
    {
        if (tree == null)
            return false;

        if (!string.IsNullOrWhiteSpace(tree.TreeId) &&
            tree.TreeId.IndexOf("element", StringComparison.OrdinalIgnoreCase) >= 0)
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

    private int GetEquippedSlotIndex(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount)
    {
        if (tree == null || node == null)
            return -1;

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            if (TryGetEquippedNode(ToTreeList(tree), i, out SkillTreeDefinition equippedTree, out SkillTreeNodeDefinition equippedNode) &&
                equippedTree == tree &&
                equippedNode == node)
            {
                return i;
            }
        }

        return -1;
    }

    private bool HasAnyValidEquippedActiveSkill(SkillTreeDefinition tree, int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            if (TryGetEquippedNode(ToTreeList(tree), i, out SkillTreeDefinition equippedTree, out SkillTreeNodeDefinition node) &&
                equippedTree == tree &&
                IsUnlockedRegularActiveSkill(tree, node))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetAutoEquipSlot(SkillTreeNodeDefinition node, bool[] occupiedSlots)
    {
        if (occupiedSlots == null || occupiedSlots.Length == 0)
            return -1;

        int preferredSlot = node != null ? node.PreferredEquipSlot : -1;
        if (preferredSlot >= 0 && preferredSlot < occupiedSlots.Length && !occupiedSlots[preferredSlot])
            return preferredSlot;

        for (int i = 0; i < occupiedSlots.Length; i++)
        {
            if (!occupiedSlots[i])
                return i;
        }

        return -1;
    }

    private int GetPreferredOrFirstOpenSlot(
        SkillTreeDefinition tree,
        SkillTreeNodeDefinition node,
        int slotCount)
    {
        int safeSlotCount = Mathf.Max(1, slotCount);
        int preferredSlot = node != null ? node.PreferredEquipSlot : -1;
        if (preferredSlot >= 0 &&
            preferredSlot < safeSlotCount &&
            !IsSlotOccupied(tree, preferredSlot))
        {
            return preferredSlot;
        }

        for (int i = 0; i < safeSlotCount; i++)
        {
            if (!IsSlotOccupied(tree, i))
                return i;
        }

        return Mathf.Clamp(preferredSlot, 0, safeSlotCount - 1);
    }

    private bool IsSlotOccupied(SkillTreeDefinition tree, int slotIndex)
    {
        string globalTreeId = PlayerPrefs.GetString(GetGlobalEquippedTreeKey(slotIndex), string.Empty);
        string globalNodeId = PlayerPrefs.GetString(GetGlobalEquippedNodeKey(slotIndex), string.Empty);
        if (tree != null && globalTreeId == tree.TreeId && !string.IsNullOrWhiteSpace(globalNodeId))
        {
            SkillTreeNodeDefinition globalNode = tree.FindNode(globalNodeId);
            if (IsUnlockedRegularActiveSkill(tree, globalNode))
                return true;

            PlayerPrefs.DeleteKey(GetGlobalEquippedTreeKey(slotIndex));
            PlayerPrefs.DeleteKey(GetGlobalEquippedNodeKey(slotIndex));
            PlayerPrefs.Save();
            return false;
        }

        if (IsAnyRegularGlobalSlotOccupied(slotIndex))
            return true;

        if (tree == null)
            return false;

        SkillTreeNodeDefinition equippedNode = FindNode(
            tree,
            PlayerPrefs.GetString(GetEquippedNodeKey(tree, slotIndex), string.Empty));

        return IsUnlockedRegularActiveSkill(tree, equippedNode);
    }
}
