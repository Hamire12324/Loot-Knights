using System;
using UnityEngine;

[Serializable]
public sealed class SkillTreePrerequisite
{
    [SerializeField] private SkillTreeNodeDefinition node;
    [SerializeField, Min(1)] private int requiredRank = 1;

    public SkillTreeNodeDefinition Node => node;
    public int RequiredRank => Mathf.Max(1, requiredRank);
}
