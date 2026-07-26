using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillTreeManager : BaseSingleton<PlayerSkillTreeManager>
{
    private static PlayerSkillTreeManager editorService;
    private readonly PlayerSkillTreeStorage storage = new();

    public event Action OnChanged
    {
        add => storage.OnChanged += value;
        remove => storage.OnChanged -= value;
    }

    public static PlayerSkillTreeManager Service
    {
        get
        {
            if (HasInstance)
                return InstanceOrNull;

            PlayerSkillTreeManager existingManager = FindExistingManager();
            if (existingManager != null)
                return existingManager;

            if (!Application.isPlaying)
            {
                if (editorService != null)
                    return editorService;

                GameObject editorManagerObject = new($"[Editor] {nameof(PlayerSkillTreeManager)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                editorService = editorManagerObject.AddComponent<PlayerSkillTreeManager>();
                editorService.hideFlags = HideFlags.HideAndDontSave;
                return editorService;
            }

            GameObject managerObject = new(nameof(PlayerSkillTreeManager));
            return managerObject.AddComponent<PlayerSkillTreeManager>();
        }
    }

    protected override void Awake()
    {
        if (!Application.isPlaying)
            return;

        base.Awake();
        storage.EnsureDebugStartingPoints();
    }

    protected override void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            if (editorService == this)
                editorService = null;

            return;
        }

        base.OnDestroy();
    }

    private static PlayerSkillTreeManager FindExistingManager()
    {
        PlayerSkillTreeManager[] managers = Resources.FindObjectsOfTypeAll<PlayerSkillTreeManager>();
        foreach (PlayerSkillTreeManager manager in managers)
        {
            if (manager == null || manager.gameObject == null)
                continue;

            if (manager.gameObject.scene.IsValid())
                return manager;
        }

        return null;
    }

    public int AvailablePoints => storage.AvailablePoints;
    public int HighestRewardedLevel => storage.HighestRewardedLevel;

    public void EnsureLevelRewarded(int level)
    {
        storage.EnsureLevelRewarded(level);
    }

    public int GetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return storage.GetRank(tree, node);
    }

    public void GrantPoints(int amount)
    {
        storage.GrantPoints(amount);
    }

    public bool TrySpendPoints(int amount)
    {
        return storage.TrySpendPoints(amount);
    }

    public void SetRank(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int rank)
    {
        storage.SetRank(tree, node, rank);
    }

    public void ClearTree(SkillTreeDefinition tree)
    {
        storage.ClearTree(tree);
    }

    public int ResetTreeAndRefund(SkillTreeDefinition tree, int slotCount)
    {
        return storage.ResetTreeAndRefund(tree, slotCount);
    }

    public HeroSkillDefinition[] GetEquippedActiveSkills(SkillTreeDefinition tree, int slotCount)
    {
        return storage.GetEquippedActiveSkills(tree, slotCount);
    }

    public HeroSkillDefinition[] GetEquippedActiveSkills(IReadOnlyList<SkillTreeDefinition> trees, int slotCount)
    {
        return storage.GetEquippedActiveSkills(trees, slotCount);
    }

    public HeroSkillDefinition GetEquippedSpecialSkill(IReadOnlyList<SkillTreeDefinition> trees)
    {
        return storage.GetEquippedSpecialSkill(trees);
    }

    public int EnsureUnlockedActiveSkillsEquipped(SkillTreeDefinition tree, int slotCount)
    {
        return storage.EnsureUnlockedActiveSkillsEquipped(tree, slotCount);
    }

    public string[] GetEquippedActiveSkillNodeIds(SkillTreeDefinition tree, int slotCount)
    {
        return storage.GetEquippedActiveSkillNodeIds(tree, slotCount);
    }

    public string[] GetEquippedActiveSkillNodeIds(IReadOnlyList<SkillTreeDefinition> trees, int slotCount)
    {
        return storage.GetEquippedActiveSkillNodeIds(trees, slotCount);
    }

    public string GetEquippedSpecialSkillNodeId(IReadOnlyList<SkillTreeDefinition> trees)
    {
        return storage.GetEquippedSpecialSkillNodeId(trees);
    }

    public void ApplyEquippedSkillsToHero(HeroCtrl hero, SkillTreeDefinition tree, int slotCount)
    {
        ApplyEquippedSkillsToHero(hero, new[] { tree }, slotCount);
    }

    public void ApplyEquippedSkillsToHero(HeroCtrl hero, IReadOnlyList<SkillTreeDefinition> trees, int slotCount)
    {
        if (hero == null || hero.HeroSkillController == null)
            return;

        HeroSkillDefinition[] skills = GetEquippedActiveSkills(trees, slotCount);
        for (int i = 0; i < Mathf.Max(0, slotCount); i++)
            hero.HeroSkillController.SetEquippedSkill(i, i < skills.Length ? skills[i] : null);

        hero.HeroSkillController.SetSpecialSkill(GetEquippedSpecialSkill(trees));
    }

    public void ApplyEquippedSkillNodeIdsToHero(
        HeroCtrl hero,
        SkillTreeDefinition tree,
        IReadOnlyList<string> nodeIds,
        int slotCount)
    {
        ApplyEquippedSkillNodeIdsToHero(hero, new[] { tree }, nodeIds, slotCount);
    }

    public void ApplyEquippedSkillNodeIdsToHero(
        HeroCtrl hero,
        IReadOnlyList<SkillTreeDefinition> trees,
        IReadOnlyList<string> nodeIds,
        int slotCount)
    {
        ApplyEquippedSkillNodeIdsToHero(hero, trees, nodeIds, slotCount, string.Empty);
    }

    public void ApplyEquippedSkillNodeIdsToHero(
        HeroCtrl hero,
        IReadOnlyList<SkillTreeDefinition> trees,
        IReadOnlyList<string> nodeIds,
        int slotCount,
        string specialNodeId)
    {
        if (hero == null || hero.HeroSkillController == null)
            return;

        int safeSlotCount = Mathf.Max(0, slotCount);
        for (int i = 0; i < safeSlotCount; i++)
        {
            string encodedNodeId = nodeIds != null && i < nodeIds.Count ? nodeIds[i] : string.Empty;
            TryFindEncodedNode(trees, encodedNodeId, out SkillTreeDefinition tree, out SkillTreeNodeDefinition node);
            HeroSkillDefinition skill = ResolveSyncedRegularSkill(tree, node);

            hero.HeroSkillController.SetEquippedSkill(i, skill);
        }

        hero.HeroSkillController.SetSpecialSkill(ResolveSpecialSkill(trees, specialNodeId));
    }

    public SkillTreeDefinition FindTreeContainingNode(IReadOnlyList<SkillTreeDefinition> trees, string nodeId)
    {
        if (trees == null || string.IsNullOrWhiteSpace(nodeId))
            return null;

        foreach (SkillTreeDefinition tree in trees)
        {
            if (tree != null && tree.FindNode(nodeId) != null)
                return tree;
        }

        return null;
    }

    private static SkillTreeNodeDefinition FindEncodedNode(
        IReadOnlyList<SkillTreeDefinition> trees,
        string encodedNodeId)
    {
        return TryFindEncodedNode(trees, encodedNodeId, out _, out SkillTreeNodeDefinition node)
            ? node
            : null;
    }

    private static bool TryFindEncodedNode(
        IReadOnlyList<SkillTreeDefinition> trees,
        string encodedNodeId,
        out SkillTreeDefinition resolvedTree,
        out SkillTreeNodeDefinition resolvedNode)
    {
        resolvedTree = null;
        resolvedNode = null;

        if (!PlayerSkillTreeStorage.TryDecodeEquippedNodeId(encodedNodeId, out string treeId, out string nodeId))
            return false;

        if (trees == null)
            return false;

        foreach (SkillTreeDefinition tree in trees)
        {
            if (tree == null)
                continue;

            if (!string.IsNullOrWhiteSpace(treeId) && tree.TreeId != treeId)
                continue;

            SkillTreeNodeDefinition node = tree.FindNode(nodeId);
            if (node != null)
            {
                resolvedTree = tree;
                resolvedNode = node;
                return true;
            }
        }

        return false;
    }

    private HeroSkillDefinition ResolveSpecialSkill(IReadOnlyList<SkillTreeDefinition> trees, string encodedNodeId)
    {
        return TryFindEncodedNode(trees, encodedNodeId, out SkillTreeDefinition tree, out SkillTreeNodeDefinition node) &&
               storage.IsSpecialActiveSkill(tree, node)
            ? node.ActiveSkill
            : null;
    }

    private HeroSkillDefinition ResolveSyncedRegularSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        if (node == null || node.ActiveSkill == null)
            return null;

        if (node.Kind == SkillTreeNodeKind.SkillUpgrade)
            return node.ActiveSkill;

        return node.Kind == SkillTreeNodeKind.ActiveSkill &&
               !storage.IsSpecialActiveSkill(tree, node)
            ? node.ActiveSkill
            : null;
    }

    public bool IsSpecialActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node)
    {
        return storage.IsSpecialActiveSkill(tree, node);
    }

    public bool IsEquipped(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount)
    {
        return storage.IsEquipped(tree, node, slotCount);
    }

    public bool HasEquippedSlot(SkillTreeDefinition tree, int slotIndex)
    {
        return storage.HasEquippedSlot(tree, slotIndex);
    }

    public bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
    {
        return storage.TryEquipActiveSkill(tree, node, slotCount, out reason);
    }

    public bool TryEquipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotIndex, int slotCount, out string reason)
    {
        return storage.TryEquipActiveSkill(tree, node, slotIndex, slotCount, out reason);
    }

    public bool TryUnequipActiveSkill(SkillTreeDefinition tree, SkillTreeNodeDefinition node, int slotCount, out string reason)
    {
        return storage.TryUnequipActiveSkill(tree, node, slotCount, out reason);
    }

    public bool TryUnequipSlot(SkillTreeDefinition tree, int slotIndex, out string reason)
    {
        return storage.TryUnequipSlot(tree, slotIndex, out reason);
    }

    public void ClearAllProgress(SkillTreeDefinition tree = null)
    {
        storage.ClearAllProgress(tree);
    }
}
