using System;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SkillTreePresenter))]
public sealed class SkillTreeView : BaseMonoBehaviour
{
    [SerializeField] private SkillTreeDefinition skillTree;
    public SkillTreeDefinition ActiveSkillTree => skillTree;
    [SerializeField] private SkillTreeDefinition classSkillTree;
    public SkillTreeDefinition ClassSkillTree => classSkillTree;
    [SerializeField] private SkillTreeDefinition elementalSkillTree;
    public SkillTreeDefinition ElementalSkillTree => elementalSkillTree;
    [SerializeField] private SkillTreePresenter skillTreePresenter;
    [SerializeField] private SkillTreeTreeAreaView treeAreaView;
    [SerializeField] private SkillTreeDetailPanelView detailPanelView;
    [SerializeField] private SkillTreeEquipPanelView equipPanelView;

    private SkillTreeUiState skillTreeUiState;
    private readonly List<SkillTreeDefinition> skillTreeBuffer = new();

    public event Action<SkillTreeNodeView> NodeSelected;
    public event Action UpgradeClicked;
    public event Action EquipClicked;
    public event Action ResetClicked;
    public event Action<int> EquipSlotClicked;

    public int EquipSlotCount => 4;
    protected override void OnEnable()
    {
        SubscribeChildren();
    }

    protected override void OnDisable()
    {
        UnsubscribeChildren();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        skillTreePresenter ??= GetComponent<SkillTreePresenter>();
        treeAreaView ??= GetComponentInChildren<SkillTreeTreeAreaView>(true);
        detailPanelView ??= GetComponentInChildren<SkillTreeDetailPanelView>(true);
        equipPanelView ??= GetComponentInChildren<SkillTreeEquipPanelView>(true);
    }
    public void SetSkillTrees(SkillTreeDefinition primaryTree, SkillTreeDefinition secondaryTree)
    {
        if (primaryTree != null)
            classSkillTree = primaryTree;

        if (classSkillTree == null)
            classSkillTree = skillTree;

        elementalSkillTree = secondaryTree;

        if (!IsConfiguredSkillTree(skillTree))
            skillTree = classSkillTree != null ? classSkillTree : elementalSkillTree;

        SetSkillTree(skillTree);
    }
    public void SetSkillTree(SkillTreeDefinition tree)
    {
        if (tree == null)
            return;

        skillTree = tree;
        ConfigureChildren();
        treeAreaView?.ResetScroll(tree);
        skillTreePresenter?.SetSkillTree(tree);
        skillTreePresenter?.Refresh();
    }
    public bool TryBeginEquipDrag()
    {
        if (skillTreeUiState == null ||
            !skillTreeUiState.SelectedIsUnlockedActiveSkill ||
            skillTreeUiState.SelectedIsEquipped)
            return false;

        if (!skillTreeUiState.SelectingEquipSlot)
            EquipClicked?.Invoke();

        return true;
    }

    public bool CompleteEquipDrag(int slotIndex)
    {
        if (skillTreeUiState == null || !skillTreeUiState.HasPendingEquip)
            return false;

        EquipSlotClicked?.Invoke(slotIndex);
        return true;
    }

    public void CancelEquipDrag()
    {
        if (skillTreeUiState != null && skillTreeUiState.SelectingEquipSlot)
            EquipClicked?.Invoke();
    }
    public void BindNodeViews()
    {
        foreach (SkillTreeNodeView nodeView in GetNodeViews())
        {
            if (nodeView == null || nodeView.Definition == null)
                continue;

            nodeView.Bind(this, nodeView.Definition);
        }
    }

    public SkillTreeNodeView[] GetNodeViews()
    {
        return treeAreaView != null ? treeAreaView.GetNodeViews(skillTree) 
            : Array.Empty<SkillTreeNodeView>();
    }

    public void Render(SkillTreeUiState state)
    {
        skillTreeUiState = state;
        if (state == null || state.SkillTree == null)
        {
            detailPanelView?.Clear();
            equipPanelView?.Render(state, SkillTreeLayoutController.IsElementalSkillTree(skillTree));
            return;
        }

        treeAreaView?.RenderNodes(state, this);
        detailPanelView?.Render(state);
        equipPanelView?.Render(state, SkillTreeLayoutController.IsElementalSkillTree(state.SkillTree));
    }

    private void ConfigureChildren()
    {
        treeAreaView?.ShowTree(skillTree, ClassSkillTree);
        treeAreaView?.ConfigureTreeSwitcher(ClassSkillTree, ElementalSkillTree, skillTree);
    }
    private void SubscribeChildren()
    {
        LoadComponents();
        if (treeAreaView != null) { treeAreaView.ResetRequested -= RaiseResetClicked; treeAreaView.ResetRequested += RaiseResetClicked; }
        if (detailPanelView != null) { detailPanelView.UpgradeRequested -= RaiseUpgradeClicked; detailPanelView.UpgradeRequested += RaiseUpgradeClicked; detailPanelView.EquipRequested -= RaiseEquipClicked; detailPanelView.EquipRequested += RaiseEquipClicked; }
        if (equipPanelView != null) { equipPanelView.SlotSelected -= HandleEquipSlotSelected; equipPanelView.SlotSelected += HandleEquipSlotSelected; equipPanelView.SlotDropped -= HandleSlotDropped; equipPanelView.SlotDropped += HandleSlotDropped; }
        if (treeAreaView != null) { treeAreaView.TreeSelected -= SetSkillTree; treeAreaView.TreeSelected += SetSkillTree; }
    }

    private void UnsubscribeChildren()
    {
        if (treeAreaView != null) treeAreaView.ResetRequested -= RaiseResetClicked;
        if (detailPanelView != null) { detailPanelView.UpgradeRequested -= RaiseUpgradeClicked; detailPanelView.EquipRequested -= RaiseEquipClicked; }
        if (equipPanelView != null) { equipPanelView.SlotSelected -= HandleEquipSlotSelected; equipPanelView.SlotDropped -= HandleSlotDropped; }
        if (treeAreaView != null) treeAreaView.TreeSelected -= SetSkillTree;
    }

    private void RaiseUpgradeClicked() => UpgradeClicked?.Invoke();
    private void RaiseEquipClicked() => EquipClicked?.Invoke();
    private void RaiseResetClicked() => ResetClicked?.Invoke();
    private void HandleEquipSlotSelected(int slotIndex) => EquipSlotClicked?.Invoke(slotIndex);
    private void HandleSlotDropped(int slotIndex)
    {
        if (CompleteEquipDrag(slotIndex)) SkillTreeEquipDragSource.DraggingSource?.MarkDropped();
    }

    private bool IsConfiguredSkillTree(SkillTreeDefinition tree) => tree != null && (tree == classSkillTree || tree == elementalSkillTree);
    public IReadOnlyList<SkillTreeDefinition> GetSkillTrees()
    {
        skillTreeBuffer.Clear();
        AddSkillTree(classSkillTree);
        AddSkillTree(elementalSkillTree);

        if (skillTreeBuffer.Count == 0)
            AddSkillTree(skillTree);

        return skillTreeBuffer;
    }
    private void AddSkillTree(SkillTreeDefinition tree)
    {
        if (tree != null && !skillTreeBuffer.Contains(tree))
            skillTreeBuffer.Add(tree);
    }
    public void Refresh()
    {
        skillTreePresenter?.Refresh();
    }

    public void Select(SkillTreeNodeView nodeView)
    {
        NodeSelected?.Invoke(nodeView);
    }
}
