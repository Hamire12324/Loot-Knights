using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns the tree canvas, reset control, scrolling and node rendering.</summary>
public sealed class SkillTreeTreeAreaView : BaseMonoBehaviour
{
    [SerializeField] private Button resetButton;
    [SerializeField] private SkillTreeTextView skillPointText;
    [SerializeField] private SkillTreeTreeSwitcherView treeSwitcherView;
    private SkillTreeLayoutController layout;

    public event Action ResetRequested;
    public event Action<SkillTreeDefinition> TreeSelected;

    protected override void Awake()
    {
        base.Awake();
        BindResetButton();
        BindTreeSwitcher();
    }

    private void OnValidate() => LoadComponents();

    protected override void OnEnable() { BindResetButton(); BindTreeSwitcher(); }
    protected override void OnDisable()
    {
        if (resetButton != null) resetButton.onClick.RemoveListener(HandleReset);
        if (treeSwitcherView != null) treeSwitcherView.TreeSelected -= RaiseTreeSelected;
    }

    public void ShowTree(SkillTreeDefinition activeTree, SkillTreeDefinition primaryTree)
    {
        if (activeTree == null) return;
        GetLayout().ApplyVisibility(activeTree, primaryTree);
    }

    public void ResetScroll(SkillTreeDefinition activeTree)
    {
        if (activeTree != null) GetLayout().ResetScroll(activeTree);
    }

    public SkillTreeNodeView[] GetNodeViews(SkillTreeDefinition tree) => tree != null ? GetLayout().GetNodeViews(tree) : Array.Empty<SkillTreeNodeView>();

    public void ConfigureTreeSwitcher(SkillTreeDefinition classTree, SkillTreeDefinition elementalTree, SkillTreeDefinition activeTree)
    {
        treeSwitcherView?.Configure(classTree, elementalTree, activeTree);
    }

    public void RenderNodes(SkillTreeUiState state, SkillTreeView owner)
    {
        if (state == null)
            return;

        if (skillPointText != null)
            skillPointText.Value = $"POINTS: {state.AvailablePoints}";

        foreach (SkillTreeNodeView nodeView in GetNodeViews(state.SkillTree))
        {
            if (nodeView == null || nodeView.Definition == null) continue;
            nodeView.Bind(owner, nodeView.Definition);
            if (state.TryGetNodeState(nodeView.Definition, out SkillTreeNodeUiState nodeState)) nodeView.Render(nodeState);
        }
    }

    private SkillTreeLayoutController GetLayout() => layout ??= new SkillTreeLayoutController(transform.parent);
    protected override void LoadComponents()
    {
        base.LoadComponents();
        resetButton ??= transform.Find("ResetButton")?.GetComponent<Button>();
        skillPointText ??= SkillTreeTextView.GetOrAdd(transform.Find("SkillPointText"));
        treeSwitcherView ??= GetComponentInChildren<SkillTreeTreeSwitcherView>(true);
    }
    private void BindResetButton()
    {
        if (resetButton == null) return;
        resetButton.onClick.RemoveListener(HandleReset);
        resetButton.onClick.AddListener(HandleReset);
    }
    private void HandleReset() => ResetRequested?.Invoke();

    private void BindTreeSwitcher()
    {
        if (treeSwitcherView == null) return;
        treeSwitcherView.TreeSelected -= RaiseTreeSelected;
        treeSwitcherView.TreeSelected += RaiseTreeSelected;
    }

    private void RaiseTreeSelected(SkillTreeDefinition tree) => TreeSelected?.Invoke(tree);
}
