using System;
using UnityEngine;

public static class PlayerSkillTreeStorage
{
    private const string AvailablePointsKey = "LootKnights.SkillTree.Available";
    private const string HighestRewardedLevelKey = "LootKnights.SkillTree.HighestRewardedLevel";
    private const string RankKeyPrefix = "LootKnights.SkillTree.Rank.";
    private const string EquippedNodeKeyPrefix = "LootKnights.SkillTree.Equipped.";

    public const int PointsPerLevel = 1;

    public static event Action OnChanged;

    public static int AvailablePoints => Mathf.Max(0, PlayerPrefs.GetInt(AvailablePointsKey, 0));
    public static int HighestRewardedLevel => Mathf.Max(1, PlayerPrefs.GetInt(HighestRewardedLevelKey, 1));

    public static void EnsureLevelRewarded(int level)
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

    public static int GetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        if (tree == null || node == null) return 0;
        return Mathf.Max(0, PlayerPrefs.GetInt(GetRankKey(tree, node), 0));
    }

    public static void GrantPoints(int amount)
    {
        if (amount <= 0) return;

        PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints + amount);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public static bool TrySpendPoints(int amount)
    {
        if (amount <= 0 || AvailablePoints < amount)
            return false;

        PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints - amount);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public static void SetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int rank)
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

    public static void ClearTree(SkillTreeDefinition tree)
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

    public static int ResetTreeAndRefund(SkillTreeDefinition tree, int slotCount)
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
            PlayerPrefs.DeleteKey(GetEquippedNodeKey(tree, i));

        if (refund > 0)
            PlayerPrefs.SetInt(AvailablePointsKey, AvailablePoints + refund);

        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return refund;
    }

    public static HeroSkillDefinition[] GetEquippedActiveSkills(SkillTreeDefinition tree, int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        HeroSkillDefinition[] skills = new HeroSkillDefinition[safeSlotCount];
        if (tree == null) return skills;

        for (int i = 0; i < safeSlotCount; i++)
        {
            SkillTreeNodeDefinition node = FindNode(tree, PlayerPrefs.GetString(GetEquippedNodeKey(tree, i), string.Empty));
            if (IsUnlockedActiveSkill(tree, node))
                skills[i] = node.ActiveSkill;
        }

        return skills;
    }

    public static bool IsEquipped(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount)
    {
        if (tree == null || node == null) return false;

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            if (PlayerPrefs.GetString(GetEquippedNodeKey(tree, i), string.Empty) == node.NodeId)
                return true;
        }

        return false;
    }

    public static bool HasEquippedSlot(SkillTreeDefinition tree, int slotIndex)
    {
        if (tree == null || slotIndex < 0) return false;
        return PlayerPrefs.HasKey(GetEquippedNodeKey(tree, slotIndex));
    }

    public static bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
    {
        reason = string.Empty;

        int safeSlotCount = Mathf.Max(1, slotCount);
        int existingSlot = GetEquippedSlotIndex(tree, node, safeSlotCount);
        if (existingSlot >= 0)
        {
            reason = "Skill is already equipped.";
            return false;
        }

        int targetSlot = 0;
        for (int i = 0; i < safeSlotCount; i++)
        {
            SkillTreeNodeDefinition equippedNode = FindNode(tree, PlayerPrefs.GetString(GetEquippedNodeKey(tree, i), string.Empty));
            if (IsUnlockedActiveSkill(tree, equippedNode))
                continue;

            targetSlot = i;
            break;
        }

        return TryEquipActiveSkill(tree, node, targetSlot, safeSlotCount, out reason);
    }

    public static bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotIndex, int slotCount, out string reason)
    {
        reason = string.Empty;

        if (tree == null)
        {
            reason = "Missing skill tree.";
            return false;
        }

        if (!IsUnlockedActiveSkill(tree, node))
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

        PlayerPrefs.SetString(GetEquippedNodeKey(tree, safeSlotIndex), node.NodeId);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public static bool TryUnequipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
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

    public static bool TryUnequipSlot(SkillTreeDefinition tree, int slotIndex, out string reason)
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

        string key = GetEquippedNodeKey(tree, slotIndex);
        if (!PlayerPrefs.HasKey(key))
        {
            reason = "Equip slot is already empty.";
            return false;
        }

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    public static void ClearAllProgress(SkillTreeDefinition tree = null)
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

    private static bool IsUnlockedActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return tree != null &&
               node != null &&
               node.Kind == SkillTreeNodeKind.ActiveSkill &&
               node.ActiveSkill != null &&
               GetRank(tree, node) > 0;
    }

    private static int GetEquippedSlotIndex(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount)
    {
        if (tree == null || node == null)
            return -1;

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            if (PlayerPrefs.GetString(GetEquippedNodeKey(tree, i), string.Empty) == node.NodeId)
                return i;
        }

        return -1;
    }
}
