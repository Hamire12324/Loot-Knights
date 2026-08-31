using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeUiState
{
    private readonly Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState> nodeStates;
    private readonly HeroSkillDefinition[] equippedSkills;
    private readonly bool[] occupiedEquipSlots;

    public SkillTreeUiState(
        SkillTreeDefinition skillTree,
        int availablePoints,
        SkillTreeNodeDefinition selectedNode,
        int selectedRank,
        bool selectedCanUpgrade,
        string selectedUpgradeReason,
        bool selectedIsSpecialSkill,
        bool selectedIsUnlockedSpecialSkill,
        bool selectedIsUnlockedActiveSkill,
        bool selectedIsEquipped,
        bool selectingEquipSlot,
        bool hasPendingEquip,
        HeroSkillDefinition specialSkill,
        HeroSkillDefinition[] equippedSkills,
        bool[] occupiedEquipSlots,
        Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState> nodeStates)
    {
        SkillTree = skillTree;
        AvailablePoints = availablePoints;
        SelectedNode = selectedNode;
        SelectedRank = selectedRank;
        SelectedCanUpgrade = selectedCanUpgrade;
        SelectedUpgradeReason = selectedUpgradeReason;
        SelectedIsSpecialSkill = selectedIsSpecialSkill;
        SelectedIsUnlockedSpecialSkill = selectedIsUnlockedSpecialSkill;
        SelectedIsUnlockedActiveSkill = selectedIsUnlockedActiveSkill;
        SelectedIsEquipped = selectedIsEquipped;
        SelectingEquipSlot = selectingEquipSlot;
        HasPendingEquip = hasPendingEquip;
        SpecialSkill = specialSkill;
        this.equippedSkills = equippedSkills ?? new HeroSkillDefinition[0];
        this.occupiedEquipSlots = occupiedEquipSlots ?? new bool[0];
        this.nodeStates = nodeStates ?? new Dictionary<SkillTreeNodeDefinition, SkillTreeNodeUiState>();
    }

    public SkillTreeDefinition SkillTree { get; }
    public int AvailablePoints { get; }
    public SkillTreeNodeDefinition SelectedNode { get; }
    public int SelectedRank { get; }
    public bool SelectedCanUpgrade { get; }
    public string SelectedUpgradeReason { get; }
    public bool SelectedIsSpecialSkill { get; }
    public bool SelectedIsUnlockedSpecialSkill { get; }
    public bool SelectedIsUnlockedActiveSkill { get; }
    public bool SelectedIsEquipped { get; }
    public bool SelectingEquipSlot { get; }
    public bool HasPendingEquip { get; }
    public HeroSkillDefinition SpecialSkill { get; }
    public int EquipSlotCount => equippedSkills.Length;

    public bool TryGetNodeState(SkillTreeNodeDefinition node, out SkillTreeNodeUiState state)
    {
        if (node != null && nodeStates.TryGetValue(node, out state))
            return true;

        state = null;
        return false;
    }

    public HeroSkillDefinition GetEquippedSkill(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < equippedSkills.Length
            ? equippedSkills[slotIndex]
            : null;
    }

    public bool IsEquipSlotOccupied(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < occupiedEquipSlots.Length && occupiedEquipSlots[slotIndex];
    }
}

public sealed class SkillTreeNodeUiState
{
    public SkillTreeNodeUiState(
        SkillTreeNodeDefinition definition,
        Sprite icon,
        int rank,
        bool canUpgrade,
        bool selected)
    {
        Definition = definition;
        Icon = icon;
        Rank = rank;
        CanUpgrade = canUpgrade;
        Selected = selected;
    }

    public SkillTreeNodeDefinition Definition { get; }
    public Sprite Icon { get; }
    public int Rank { get; }
    public bool CanUpgrade { get; }
    public bool Selected { get; }

    public int MaxRank => Definition != null ? Definition.MaxRank : 0;
    public int PointCost => Definition != null ? Definition.PointCost : 0;
    public bool IsMaxed => Definition != null && Rank >= Definition.MaxRank;
    public bool IsLocked => Rank <= 0 && !CanUpgrade;
}
