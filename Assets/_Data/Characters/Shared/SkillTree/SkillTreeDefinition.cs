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
    [SerializeField, HideInInspector] private float contentScale = 1f;
    [SerializeField] private Vector2 contentScale2D = Vector2.one;
    [SerializeField] private float nodeSize = 86f;
    [SerializeField] private float iconSize = 64f;
    [SerializeField] private float nodeIconFramePadding = 24f;
    [SerializeField] private bool useMajorMinorNodeSizes = true;
    [SerializeField] private float majorNodeSize = 76f;
    [SerializeField] private float majorIconSize = 56f;
    [SerializeField] private float minorNodeSize = 52f;
    [SerializeField] private float minorIconSize = 34f;
    [SerializeField] private bool autoExpandScrollContent = true;
    [SerializeField] private Vector2 scrollContentPadding = new(160f, 160f);
    [SerializeField] private bool horizontalScroll;
    [SerializeField] private bool startTreeAtTop = true;

    public bool OverrideBuilderSettings => overrideBuilderSettings;
    public Vector2 ContentSize => contentSize;
    public Vector2 ContentOffset => contentOffset;
    public float ContentScale => Mathf.Max(0.01f, contentScale);
    public Vector2 ContentScale2D
    {
        get
        {
            Vector2 fallback = new(contentScale, contentScale);
            Vector2 scale = contentScale2D.x > 0f && contentScale2D.y > 0f ? contentScale2D : fallback;
            return new Vector2(Mathf.Max(0.01f, scale.x), Mathf.Max(0.01f, scale.y));
        }
    }
    public bool AutoExpandScrollContent => autoExpandScrollContent;
    public float NodeSize => Mathf.Max(1f, nodeSize);
    public float IconSize => Mathf.Max(1f, iconSize);
    public float NodeIconFramePadding => Mathf.Max(0f, nodeIconFramePadding);
    public bool UseMajorMinorNodeSizes => useMajorMinorNodeSizes;
    public float MajorNodeSize => Mathf.Max(1f, majorNodeSize);
    public float MajorIconSize => Mathf.Max(1f, majorIconSize);
    public float MinorNodeSize => Mathf.Max(1f, minorNodeSize);
    public float MinorIconSize => Mathf.Max(1f, minorIconSize);
    public Vector2 ScrollContentPadding => scrollContentPadding;
    public bool HorizontalScroll => horizontalScroll;
    public bool StartTreeAtTop => startTreeAtTop;

    public void Validate()
    {
        contentSize = new Vector2(Mathf.Max(1f, contentSize.x), Mathf.Max(1f, contentSize.y));
        contentScale = Mathf.Max(0.01f, contentScale);
        if (contentScale2D.x <= 0f || contentScale2D.y <= 0f)
            contentScale2D = new Vector2(contentScale, contentScale);
        contentScale2D = new Vector2(Mathf.Max(0.01f, contentScale2D.x), Mathf.Max(0.01f, contentScale2D.y));
        nodeSize = Mathf.Max(1f, nodeSize);
        iconSize = Mathf.Max(1f, iconSize);
        nodeIconFramePadding = Mathf.Max(0f, nodeIconFramePadding);
        majorNodeSize = Mathf.Max(1f, majorNodeSize);
        majorIconSize = Mathf.Max(1f, majorIconSize);
        minorNodeSize = Mathf.Max(1f, minorNodeSize);
        minorIconSize = Mathf.Max(1f, minorIconSize);
        scrollContentPadding = new Vector2(Mathf.Max(0f, scrollContentPadding.x), Mathf.Max(0f, scrollContentPadding.y));
    }
}
