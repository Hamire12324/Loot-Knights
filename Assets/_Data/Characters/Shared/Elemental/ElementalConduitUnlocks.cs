public static class ElementalConduitUnlocks
{
    public static bool IsElementAvailable(
        SkillTreeRuntime runtime,
        ElementType element,
        bool requireElementUnlocks)
    {
        if (element == ElementType.None)
            return false;

        return !requireElementUnlocks ||
               runtime == null ||
               !runtime.HasAnyElementUnlockNodes() ||
               runtime.HasElement(element);
    }

    public static bool IsReactionUnlocked(
        SkillTreeRuntime runtime,
        ElementalReactionType reaction,
        bool requireReactionUnlocks)
    {
        if (reaction == ElementalReactionType.None)
            return false;

        return !requireReactionUnlocks ||
               runtime == null ||
               !runtime.HasAnyReactionUnlockNodes() ||
               runtime.HasReaction(reaction);
    }
}
