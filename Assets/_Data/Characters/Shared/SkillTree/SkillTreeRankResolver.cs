using UnityEngine;

public static class SkillTreeRankResolver
{
    public static int GetRank(CharacterCtrl character, string nodeId)
    {
        if (character == null || string.IsNullOrWhiteSpace(nodeId))
            return 0;

        HeroSkillLoadoutPhotonSync loadoutSync = character.GetComponent<HeroSkillLoadoutPhotonSync>();
        SkillTreeDefinition tree = loadoutSync != null
            ? loadoutSync.FindSkillTreeContainingNode(nodeId)
            : null;

        SkillTreeNodeDefinition node = tree != null ? tree.FindNode(nodeId) : null;
        return tree != null && node != null
            ? PlayerSkillTreeManager.Service.GetRank(tree, node)
            : 0;
    }
}
