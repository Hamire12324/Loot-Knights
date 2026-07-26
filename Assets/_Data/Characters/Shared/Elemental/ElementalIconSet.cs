using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementalIconSet", menuName = "Loot Knights/Elemental/Icon Set")]
public sealed class ElementalIconSet : ScriptableObject
{
#pragma warning disable 0649
    [Serializable]
    private struct ElementIconEntry
    {
        public ElementType Element;
        public Sprite Sprite;
        public VFXDefinition ShardVfx;
        public Color FallbackColor;
    }

    [Serializable]
    private struct ReactionIconEntry
    {
        public ElementalReactionType Reaction;
        public Sprite Sprite;
    }
#pragma warning restore 0649

    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private Color defaultFallbackColor = new(0.42f, 0.95f, 1f, 1f);
    [SerializeField] private ElementIconEntry[] elementIcons =
    {
        new() { Element = ElementType.Fire, FallbackColor = new Color(1f, 0.28f, 0.06f, 1f) },
        new() { Element = ElementType.Frost, FallbackColor = new Color(0.35f, 0.88f, 1f, 1f) },
        new() { Element = ElementType.Lightning, FallbackColor = new Color(1f, 0.88f, 0.18f, 1f) },
        new() { Element = ElementType.Poison, FallbackColor = new Color(0.42f, 1f, 0.12f, 1f) }
    };
    [SerializeField] private ReactionIconEntry[] reactionIcons =
    {
        new() { Reaction = ElementalReactionType.Shatter },
        new() { Reaction = ElementalReactionType.Overload },
        new() { Reaction = ElementalReactionType.Superconduct },
        new() { Reaction = ElementalReactionType.Burnout },
        new() { Reaction = ElementalReactionType.Neuroshock },
        new() { Reaction = ElementalReactionType.BrittleToxin }
    };

    public Sprite FallbackSprite => fallbackSprite;

    public Sprite GetElementSprite(ElementType element)
    {
        foreach (ElementIconEntry entry in elementIcons)
        {
            if (entry.Element == element)
                return entry.Sprite;
        }

        return null;
    }

    public Color GetElementColor(ElementType element)
    {
        foreach (ElementIconEntry entry in elementIcons)
        {
            if (entry.Element == element)
                return entry.FallbackColor;
        }

        return defaultFallbackColor;
    }

    public VFXDefinition GetElementShardVfx(ElementType element)
    {
        foreach (ElementIconEntry entry in elementIcons)
        {
            if (entry.Element == element)
                return entry.ShardVfx;
        }

        return null;
    }

    public Sprite GetReactionSprite(ElementalReactionType reaction)
    {
        foreach (ReactionIconEntry entry in reactionIcons)
        {
            if (entry.Reaction == reaction)
                return entry.Sprite;
        }

        return null;
    }
}
