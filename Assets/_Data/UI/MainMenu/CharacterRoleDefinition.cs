using System;
using UnityEngine;

[Serializable]
public class CharacterRoleDefinition
{
    public string RoleName = "Knight";
    [TextArea] public string Description = "Description words here";
    public CharacterClass CharacterClass = CharacterClass.Knight;
    public Sprite Portrait;
    public Sprite FullBodySprite;
    public Sprite[] SkillIcons;

    [Range(0f, 1f)] public float Attack = 0.8f;
    [Range(0f, 1f)] public float Defence = 0.6f;
    [Range(0f, 1f)] public float Vitality = 0.5f;
    [Range(0f, 1f)] public float Speed = 0.5f;
}
