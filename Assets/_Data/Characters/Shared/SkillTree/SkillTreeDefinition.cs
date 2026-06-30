using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTree", menuName = "Loot Knights/Skill Tree/Tree")]
public sealed class SkillTreeDefinition : ScriptableObject
{
    [SerializeField] private string treeId;
    [SerializeField] private string displayName;
    [SerializeField] private List<SkillTreeNodeDefinition> nodes = new();

    public string TreeId => string.IsNullOrWhiteSpace(treeId) ? name : treeId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<SkillTreeNodeDefinition> Nodes => nodes;

    public SkillTreeNodeDefinition FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;
        return nodes.Find(node => node != null && node.NodeId == nodeId);
    }
}
