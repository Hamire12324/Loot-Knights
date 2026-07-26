using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreePresenter : MonoBehaviour
{
    [SerializeField] private SkillTreeView view;
    [SerializeField] private SkillTreeDefinition skillTree;

    private SkillTreeRuntime runtime;
    private SkillTreeNodeView selectedNodeView;
    private SkillTreeNodeDefinition pendingEquipNode;
    private PlayerSkillTreeManager skillTreeManager;

    private PlayerSkillTreeManager SkillTreeManager => skillTreeManager != null
        ? skillTreeManager
        : (skillTreeManager = PlayerSkillTreeManager.Service);

    private void Awake()
    {
        LoadView();
        if (skillTree == null && view != null)
            skillTree = view.SkillTree;

        RefreshRuntime();
    }

    private void OnEnable()
    {
        LoadView();
        SubscribeView();

        SkillTreeManager.OnChanged += Refresh;
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelChanged;
        SkillTreeManager.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeView();

        if (skillTreeManager != null)
            skillTreeManager.OnChanged -= Refresh;

        skillTreeManager = null;
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelChanged;
    }

    public void Configure(SkillTreeView skillTreeView, SkillTreeDefinition tree)
    {
        view = skillTreeView != null ? skillTreeView : view;

        if (skillTree != tree)
        {
            skillTree = tree;
            selectedNodeView = null;
            pendingEquipNode = null;
            RefreshRuntime();
        }
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
        LoadView();
        RefreshRuntime();

        if (view == null)
            return;

        view.BindNodeViews();
        EnsureSelectedNode();
        view.Render(CreateViewState());
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

        if (!runtime.TryUpgrade(node, PlayerExperienceStorage.Level, out string reason))
        {
            LogReason(reason);
            return;
        }

        SkillTreeHeroApplier.ApplyStats(GetLoadoutSkillTrees());
        SkillTreeHeroApplier.ApplyLoadout(GetLoadoutSkillTrees(), GetEquipSlotCount());
        Refresh();
    }

    private void ToggleEquipSelected()
    {
        SkillTreeNodeDefinition node = GetSelectedNode();
        int slotCount = GetEquipSlotCount();

        if (!CanEquip(node, out string reason))
        {
            LogReason(reason);
            return;
        }

        if (SkillTreeManager.IsEquipped(skillTree, node, slotCount))
        {
            if (!SkillTreeManager.TryUnequipActiveSkill(skillTree, node, slotCount, out reason))
            {
                LogReason(reason);
                return;
            }

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
        int refunded = SkillTreeManager.ResetTreeAndRefund(skillTree, slotCount);

        pendingEquipNode = null;
        RefreshRuntime();
        SkillTreeHeroApplier.ApplyStats(GetLoadoutSkillTrees());
        SkillTreeHeroApplier.ApplyLoadout(GetLoadoutSkillTrees(), slotCount);
        Refresh();

        if (refunded > 0)
            Debug.Log($"Skill tree reset. Refunded {refunded} points.", this);
        else
            Debug.Log("Skill tree reset requested, but there are no spent points to refund.", this);
    }

    private void EquipPendingNodeToSlot(int slotIndex)
    {
        if (pendingEquipNode == null)
            return;

        int slotCount = GetEquipSlotCount();
        if (!SkillTreeManager.TryEquipActiveSkill(skillTree, pendingEquipNode, slotIndex, slotCount, out string reason))
        {
            LogReason(reason);
            return;
        }

        pendingEquipNode = null;
        SkillTreeHeroApplier.ApplyLoadout(GetLoadoutSkillTrees(), slotCount);
        Refresh();
    }

    private SkillTreeViewState CreateViewState()
    {
        int slotCount = GetEquipSlotCount();
        Dictionary<SkillTreeNodeDefinition, SkillTreeNodeViewState> nodeStates = CreateNodeStates();
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

        HeroSkillDefinition[] equippedSkills = SkillTreeManager.GetEquippedActiveSkills(GetLoadoutSkillTrees(), slotCount);
        HeroSkillDefinition specialSkill = SkillTreeManager.GetEquippedSpecialSkill(GetLoadoutSkillTrees());
        bool[] occupiedSlots = new bool[slotCount];
        for (int i = 0; i < occupiedSlots.Length; i++)
            occupiedSlots[i] = i < equippedSkills.Length && equippedSkills[i] != null;

        return new SkillTreeViewState(
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

    private Dictionary<SkillTreeNodeDefinition, SkillTreeNodeViewState> CreateNodeStates()
    {
        Dictionary<SkillTreeNodeDefinition, SkillTreeNodeViewState> nodeStates = new();
        if (view == null || runtime == null)
            return nodeStates;

        foreach (SkillTreeNodeView nodeView in view.GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            SkillTreeNodeDefinition node = nodeView.Definition;
            int rank = runtime.GetRank(node);
            bool canUpgrade = runtime.CanUpgrade(node, PlayerExperienceStorage.Level, out _);

            nodeStates[node] = new SkillTreeNodeViewState(
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

        if (view == null)
            return;

        foreach (SkillTreeNodeView nodeView in view.GetNodeViews())
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
        return view != null ? view.EquipSlotCount : 4;
    }

    private IReadOnlyList<SkillTreeDefinition> GetLoadoutSkillTrees()
    {
        return view != null ? view.GetSkillTrees() : new[] { skillTree };
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

    private void LoadView()
    {
        if (view == null)
            view = GetComponent<SkillTreeView>();
    }

    private void SubscribeView()
    {
        if (view == null)
            return;

        view.NodeSelected -= SelectNode;
        view.UpgradeClicked -= UpgradeSelected;
        view.EquipClicked -= ToggleEquipSelected;
        view.ResetClicked -= ResetTree;
        view.EquipSlotClicked -= EquipPendingNodeToSlot;

        view.NodeSelected += SelectNode;
        view.UpgradeClicked += UpgradeSelected;
        view.EquipClicked += ToggleEquipSelected;
        view.ResetClicked += ResetTree;
        view.EquipSlotClicked += EquipPendingNodeToSlot;
    }

    private void UnsubscribeView()
    {
        if (view == null)
            return;

        view.NodeSelected -= SelectNode;
        view.UpgradeClicked -= UpgradeSelected;
        view.EquipClicked -= ToggleEquipSelected;
        view.ResetClicked -= ResetTree;
        view.EquipSlotClicked -= EquipPendingNodeToSlot;
    }

    private void LogReason(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
            Debug.LogWarning(reason, this);
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
