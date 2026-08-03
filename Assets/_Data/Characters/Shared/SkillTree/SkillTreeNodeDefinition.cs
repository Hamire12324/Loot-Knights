using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeNode", menuName = "Loot Knights/Skill Tree/Node")]
public sealed class SkillTreeNodeDefinition : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string nodeId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Tree")]
    [SerializeField] private SkillTreeNodeKind kind;
    [SerializeField] private SkillTreeBranch branch;
    [SerializeField] private Vector2 treePosition;
    [SerializeField, Min(1)] private int maxRank = 1;
    [SerializeField, Min(1)] private int pointCost = 1;
    [SerializeField, Min(1)] private int requiredPlayerLevel = 1;
    [SerializeField] private List<SkillTreePrerequisite> prerequisites = new();

    [Header("Stats")]
    [SerializeField] private bool scaleStatModifiersByRank = true;
    [SerializeField] private List<StatModifierData> statModifiers = new();

    [Header("Skill Modifiers")]
    [SerializeField] private List<SkillModifierData> skillModifiers = new();

    [Header("Skill Unlock")]
    [SerializeField] private HeroSkillDefinition activeSkill;
    [SerializeField] private int preferredEquipSlot = -1;

    [Header("Element Unlock")]
    [SerializeField] private ElementType element = ElementType.None;
    [SerializeField] private ElementalReactionType reaction = ElementalReactionType.None;

    public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? name : nodeId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public SkillTreeNodeKind Kind => kind;
    public SkillTreeBranch Branch => branch;
    public Vector2 TreePosition => treePosition;
    public int MaxRank => Mathf.Max(1, maxRank);
    public int PointCost => Mathf.Max(1, pointCost);
    public int RequiredPlayerLevel => Mathf.Max(1, requiredPlayerLevel);
    public IReadOnlyList<SkillTreePrerequisite> Prerequisites => prerequisites;
    public IReadOnlyList<SkillModifierData> SkillModifiers => skillModifiers ??= new List<SkillModifierData>();
    public HeroSkillDefinition ActiveSkill => activeSkill;
    public int PreferredEquipSlot => preferredEquipSlot;
    public ElementType Element => element;
    public ElementalReactionType Reaction => reaction;

    public IEnumerable<StatModifier> CreateStatModifiers(int rank)
    {
        int safeRank = Mathf.Clamp(rank, 0, MaxRank);
        if (safeRank <= 0)
            yield break;

        foreach (StatModifierData modifier in statModifiers)
        {
            if (modifier == null || modifier.StatType == StatType.None) continue;

            float amount = scaleStatModifiersByRank
                ? modifier.Amount * safeRank
                : modifier.Amount;

            yield return new StatModifier(
                modifier.StatType,
                modifier.ModifierType,
                amount,
                this);
        }
    }

    private void OnValidate()
    {
        maxRank = Mathf.Max(1, maxRank);
        pointCost = Mathf.Max(1, pointCost);
        requiredPlayerLevel = Mathf.Max(1, requiredPlayerLevel);
        skillModifiers ??= new List<SkillModifierData>();
    }
}
