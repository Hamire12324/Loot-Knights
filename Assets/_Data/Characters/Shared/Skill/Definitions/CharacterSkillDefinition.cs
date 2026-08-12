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

    [Header("Manual Aim")]
    [Tooltip("Allow drag-to-aim input. A quick tap still uses automatic enemy targeting.")]
    [SerializeField] private bool supportsManualAim = true;
    [SerializeField, Min(0.1f)] private float manualAimRange = 6f;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float cooldown = 1f;

    [Header("Resource")]
    [SerializeField, Min(0f)] private float manaCost;

    [Header("Animation")]
    [SerializeField] private string triggerName;
    [SerializeField] private int animationIndex;
    [SerializeField] private bool executeEffectsOnAnimationHit = true;

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
    public bool SupportsManualAim => supportsManualAim;
    public float ManualAimRange => Mathf.Max(0.1f, manualAimRange);
    public float Cooldown => cooldown;
    public float ManaCost => manaCost;
    public string TriggerName => triggerName;
    public int AnimationIndex => animationIndex;
    public bool ExecuteEffectsOnAnimationHit => executeEffectsOnAnimationHit;
    public VFXDefinition CastVfx => castVfx;
    public SFXDefinition CastSfx => castSfx;
    public IReadOnlyList<CharacterSkillEffectDefinition> Effects => effects;
}
