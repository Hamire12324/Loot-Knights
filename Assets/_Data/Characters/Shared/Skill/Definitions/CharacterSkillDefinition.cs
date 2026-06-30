using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterSkillDefinition : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string skillId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Cast")]
    [SerializeField] private CharacterSkillCastType castType = CharacterSkillCastType.Instant;
    [SerializeField, Min(0f)] private float castTime;
    [SerializeField, Min(0f)] private float duration;
    [SerializeField] private bool lockMovementWhileCasting = true;
    [SerializeField] private bool canBeInterrupted = true;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float cooldown = 1f;

    [Header("Animation")]
    [SerializeField] private string triggerName;
    [SerializeField] private int animationIndex;

    [Header("Cast Feedback")]
    [SerializeField] private VFXDefinition castVfx;
    [SerializeField] private SFXDefinition castSfx;

    [Header("Effects")]
    [SerializeField] private List<CharacterSkillEffectDefinition> effects = new();

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public CharacterSkillCastType CastType => castType;
    public float CastTime => castTime;
    public float Duration => duration;
    public bool LockMovementWhileCasting => lockMovementWhileCasting;
    public bool CanBeInterrupted => canBeInterrupted;
    public float Cooldown => cooldown;
    public string TriggerName => triggerName;
    public int AnimationIndex => animationIndex;
    public VFXDefinition CastVfx => castVfx;
    public SFXDefinition CastSfx => castSfx;
    public IReadOnlyList<CharacterSkillEffectDefinition> Effects => effects;
}
