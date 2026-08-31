using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreePresenter : BaseMonoBehaviour
{
    [SerializeField] private SkillTreeView skillTreeView;
    [SerializeField] private SkillTreeDefinition skillTree;

    private SkillTreeRuntime runtime;
    private SkillTreeNodeView selectedNodeView;
    private SkillTreeNodeDefinition pendingEquipNode;
    private PlayerSkillTreeManager skillTreeManager;

    private PlayerSkillTreeManager SkillTreeManager => skillTreeManager != null
        ? skillTreeManager
        : (skillTreeManager = PlayerSkillTreeManager.Service);

    protected override void OnEnable()
    {
        SubscribeView();

        SkillTreeManager.OnChanged += Refresh;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelChanged;
        SkillTreeManager.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        Refresh();
    }

    protected override void OnDisable()
    {
        UnsubscribeView();

        if (skillTreeManager != null)
            skillTreeManager.OnChanged -= Refresh;

        skillTreeManager = null;
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelChanged;
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSkillTreeView();
    }
    private void SubscribeView()
    {
        if (skillTreeView == null)
            return;

        skillTreeView.NodeSelected -= SelectNode;
        skillTreeView.UpgradeClicked -= UpgradeSelected;
        skillTreeView.EquipClicked -= ToggleEquipSelected;
        skillTreeView.ResetClicked -= ResetTree;
        skillTreeView.EquipSlotClicked -= EquipPendingNodeToSlot;

        skillTreeView.NodeSelected += SelectNode;
        skillTreeView.UpgradeClicked += UpgradeSelected;
        skillTreeView.EquipClicked += ToggleEquipSelected;
        skillTreeView.ResetClicked += ResetTree;
        skillTreeView.EquipSlotClicked += EquipPendingNodeToSlot;
    }

    private void UnsubscribeView()
    {
        if (skillTreeView == null)
            return;

        skillTreeView.NodeSelected -= SelectNode;
        skillTreeView.UpgradeClicked -= UpgradeSelected;
        skillTreeView.EquipClicked -= ToggleEquipSelected;
        skillTreeView.ResetClicked -= ResetTree;
        skillTreeView.EquipSlotClicked -= EquipPendingNodeToSlot;
    }

    public void SetSkillTree(SkillTreeDefinition tree)
    {
        if (skillTree == tree)
            return;

        skillTree = tree;
        selectedNodeView = null;
        pendingEquipNode = null;
        RefreshRuntime();
        Refresh();
    }

    public void Refresh()
    {
        skillTreeView.BindNodeViews();
        EnsureSelectedNode();
        skillTreeView.Render(CreateViewState());
    }

    private void SelectNode(SkillTreeNodeView nodeView)
    {
        if (selectedNodeView != nodeView)
            pendingEquipNode = null;

        selectedNodeView = nodeView;
        Refresh();
    }

    private void UpgradeSelected()
    {
        SkillTreeNodeDefinition node = GetSelectedNode();
        if (node == null || runtime == null)
            return;

        if (!runtime.TryUpgrade(node, PlayerExperienceStorage.Level, out _))
            return;

        IReadOnlyList<SkillTreeDefinition> trees = GetLoadoutSkillTrees();
        SkillTreeHeroApplier.ApplyStats(trees);
        SkillTreeHeroApplier.ApplyLoadout(trees, GetEquipSlotCount());
        Refresh();
    }

    private void ToggleEquipSelected()
    {
        SkillTreeNodeDefinition node = GetSelectedNode();
        int slotCount = GetEquipSlotCount();

        if (!CanEquip(node, out _))
            return;

        if (SkillTreeManager.IsEquipped(skillTree, node, slotCount))
        {
            if (!SkillTreeManager.TryUnequipActiveSkill(skillTree, node, slotCount, out _))
                return;

            pendingEquipNode = null;
            SkillTreeHeroApplier.ApplyLoadout(GetLoadoutSkillTrees(), slotCount);
            Refresh();
            return;
        }

        pendingEquipNode = pendingEquipNode == node ? null : node;
        Refresh();
    }

    private void ResetTree()
    {
        if (skillTree == null)
            return;

        int slotCount = GetEquipSlotCount();
        SkillTreeManager.ResetTreeAndRefund(skillTree, slotCount);

        pendingEquipNode = null;
        IReadOnlyList<SkillTreeDefinition> trees = GetLoadoutSkillTrees();
        SkillTreeHeroApplier.ApplyStats(trees);
        SkillTreeHeroApplier.ApplyLoadout(trees, slotCount);
        Refresh();

    }

    private void EquipPendingNodeToSlot(int slotIndex)
    {
        if (pendingEquipNode == null)
            return;

        int slotCount = GetEquipSlotCount();
        if (!SkillTreeManager.TryEquipActiveSkill(skillTree, pendingEquipNode, slotIndex, slotCount, out _))
            return;

        pendingEquipNode = null;
        SkillTreeHeroApplier.ApplyLoadout(GetLoadoutSkillTrees(), slotCount);
        Refresh();
    }

    private SkillTreeUiState CreateViewState()
    {
        int slotCount = GetEquipSlotCount();
        Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState> nodeStates = CreateNodeStates();
        SkillTreeNodeDefinition selectedNode = GetSelectedNode();
        int selectedRank = selectedNode != null && runtime != null ? runtime.GetRank(selectedNode) : 0;
        string selectedReason = string.Empty;
        bool selectedCanUpgrade = selectedNode != null &&
                                  runtime != null &&
                                  runtime.CanUpgrade(selectedNode, PlayerExperienceStorage.Level, out selectedReason);

        bool selectedIsSpecialSkill = SkillTreeManager.IsSpecialActiveSkill(skillTree, selectedNode);
        bool selectedIsUnlockedSpecialSkill = selectedIsSpecialSkill && selectedRank > 0;
        bool selectedIsUnlockedActiveSkill = selectedNode != null &&
                                             selectedNode.Kind == SkillTreeNodeKind.ActiveSkill &&
                                             selectedNode.ActiveSkill != null &&
                                             selectedRank > 0 &&
                                             !selectedIsSpecialSkill;

        IReadOnlyList<SkillTreeDefinition> trees = GetLoadoutSkillTrees();
        HeroSkillDefinition[] equippedSkills = SkillTreeManager.GetEquippedActiveSkills(trees, slotCount);
        HeroSkillDefinition specialSkill = SkillTreeManager.GetEquippedSpecialSkill(trees);
        bool[] occupiedSlots = new bool[slotCount];
        for (int i = 0; i < occupiedSlots.Length; i++)
            occupiedSlots[i] = i < equippedSkills.Length && equippedSkills[i] != null;

        return new SkillTreeUiState(
            skillTree,
            SkillTreeManager.AvailablePoints,
            selectedNode,
            selectedRank,
            selectedCanUpgrade,
            selectedReason,
            selectedIsSpecialSkill,
            selectedIsUnlockedSpecialSkill,
            selectedIsUnlockedActiveSkill,
            selectedIsUnlockedActiveSkill && SkillTreeManager.IsEquipped(skillTree, selectedNode, slotCount),
            selectedIsUnlockedActiveSkill && pendingEquipNode == selectedNode,
            pendingEquipNode != null,
            specialSkill,
            equippedSkills,
            occupiedSlots,
            nodeStates);
    }

    private Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState> CreateNodeStates()
    {
        Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState> nodeStates = new();
        if (skillTreeView == null || runtime == null)
            return nodeStates;

        foreach (SkillTreeNodeView nodeView in skillTreeView.GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            SkillTreeNodeDefinition node = nodeView.Definition;
            int rank = runtime.GetRank(node);
            bool canUpgrade = runtime.CanUpgrade(node, PlayerExperienceStorage.Level, out _);

            nodeStates[node] = new SkillTreeNodeUiState(
                node,
                GetNodeIcon(node),
                rank,
                canUpgrade,
                nodeView == selectedNodeView);
        }

        return nodeStates;
    }

    private void EnsureSelectedNode()
    {
        if (selectedNodeView != null && selectedNodeView.Definition != null)
            return;

        if (skillTreeView == null)
            return;

        foreach (SkillTreeNodeView nodeView in skillTreeView.GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            selectedNodeView = nodeView;
            return;
        }
    }

    private SkillTreeNodeDefinition GetSelectedNode()
    {
        return selectedNodeView != null ? selectedNodeView.Definition : null;
    }

    private int GetEquipSlotCount()
    {
        return skillTreeView.EquipSlotCount;
    }

    private IReadOnlyList<SkillTreeDefinition> GetLoadoutSkillTrees()
    {
        return skillTreeView != null ? skillTreeView.GetSkillTrees() : new[] { skillTree };
    }

    private void RefreshRuntime()
    {
        runtime = new SkillTreeRuntime(skillTree);
    }

    private bool CanEquip(SkillTreeNodeDefinition node, out string reason)
    {
        reason = string.Empty;

        if (runtime == null)
        {
            reason = "Missing skill tree runtime.";
            return false;
        }

        if (node == null ||
            node.Kind != SkillTreeNodeKind.ActiveSkill ||
            node.ActiveSkill == null)
        {
            reason = "Select an active skill first.";
            return false;
        }

        if (SkillTreeManager.IsSpecialActiveSkill(skillTree, node))
        {
            reason = "Element skills unlock into the core slot automatically.";
            return false;
        }

        if (runtime.GetRank(node) <= 0)
        {
            reason = "Unlock this active skill first.";
            return false;
        }

        return true;
    }

    private void HandleLevelChanged(PlayerLevelSnapshot snapshot)
    {
        SkillTreeManager.EnsureLevelRewarded(snapshot.Level);
        Refresh();
    }

    private void LoadSkillTreeView()
    {
        if (skillTreeView == null)
            skillTreeView = GetComponent<SkillTreeView>();
    }
    private static Sprite GetNodeIcon(SkillTreeNodeDefinition node)
    {
        if (node == null)
            return null;

        if (node.Icon != null)
            return node.Icon;

        return node.ActiveSkill != null ? node.ActiveSkill.Icon : null;
    }
}
