using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTree", menuName = "Loot Knights/Skill Tree/Tree")]
public sealed class SkillTreeDefinition : ScriptableObject
{
    [SerializeField] private string treeId;
    [SerializeField] private string displayName;
    [SerializeField] private List<SkillTreeNodeDefinition> nodes = new();
    [SerializeField] private SkillTreeViewSettings viewSettings = new();

    public string TreeId => string.IsNullOrWhiteSpace(treeId) ? name : treeId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<SkillTreeNodeDefinition> Nodes => nodes;
    public SkillTreeViewSettings ViewSettings => viewSettings;

    public SkillTreeNodeDefinition FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;
        return nodes.Find(node => node != null && node.NodeId == nodeId);
    }

    private void OnValidate()
    {
        viewSettings ??= new SkillTreeViewSettings();
        viewSettings.Validate();
    }
}

[System.Serializable]
public sealed class SkillTreeViewSettings
{
    [SerializeField] private bool overrideBuilderSettings;
    [SerializeField] private Vector2 contentSize = new(1100f, 860f);
    [SerializeField] private Vector2 contentOffset = Vector2.zero;
    [SerializeField, Range(0.75f, 2.25f)] private float contentScale = 1f;
    [SerializeField] private bool autoExpandScrollContent = true;
    [SerializeField] private Vector2 scrollContentPadding = new(160f, 160f);
    [SerializeField] private bool horizontalScroll;
    [SerializeField] private bool startTreeAtTop = true;

    public bool OverrideBuilderSettings => overrideBuilderSettings;
    public Vector2 ContentSize => contentSize;
    public Vector2 ContentOffset => contentOffset;
    public float ContentScale => Mathf.Max(0.01f, contentScale);
    public bool AutoExpandScrollContent => autoExpandScrollContent;
    public Vector2 ScrollContentPadding => scrollContentPadding;
    public bool HorizontalScroll => horizontalScroll;
    public bool StartTreeAtTop => startTreeAtTop;

    public void Validate()
    {
        contentSize = new Vector2(Mathf.Max(1f, contentSize.x), Mathf.Max(1f, contentSize.y));
        contentScale = Mathf.Max(0.01f, contentScale);
        scrollContentPadding = new Vector2(Mathf.Max(0f, scrollContentPadding.x), Mathf.Max(0f, scrollContentPadding.y));
    }
}
