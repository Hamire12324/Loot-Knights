using UnityEngine;

[System.Serializable]
public class DamageData
{
    [Tooltip("Hệ số nhân sát thương")]
    public float Multiplier = 1f;

    [Tooltip("Có thể gây crit không")]
    public bool CanCrit = false;

    [Header("Hit Stun")]
    [Tooltip("Only special, elite, or boss attacks should enable this.")]
    public bool CausesHitStun = false;

    [Tooltip("How long the target loses control when this hit applies stun.")]
    public float HitStunDuration = 0.2f;

    [Tooltip("How long the target is protected from another hit stun after one is applied.")]
    public float HitStunImmunityDuration = 0.75f;

    [Tooltip("Whether this hit can cancel the target's current attack when stun applies.")]
    public bool InterruptsAttack = true;

    [Header("VFX")]
    [Tooltip("Optional VFX definition played when this damage lands.")]
    public VFXDefinition HitVfx;

    [Tooltip("Local offset added to the target hit VFX anchor.")]
    public Vector3 HitVfxOffset;

    [Header("SFX")]
    [Tooltip("Optional SFX definition played when this damage lands.")]
    public SFXDefinition HitSfx;

    [Tooltip("Local offset added to the target hit SFX position.")]
    public Vector3 HitSfxOffset;

    [Header("Element")]
    [SerializeField] private ElementType element = ElementType.None;
    public ElementType Element => element;

    [SerializeField, Min(0f)] private float elementalPower = 1f;
    public float ElementalPower => elementalPower;

    [SerializeField, Min(0f)] private float elementalStatusDuration = 4f;
    public float ElementalStatusDuration => elementalStatusDuration;

    [SerializeField] private bool consumesElementOnReaction = true;
    public bool ConsumesElementOnReaction => consumesElementOnReaction;

    public DamageData() { }

    public DamageData(float multiplier = 1f, bool canCrit = false)
    {
        Multiplier = multiplier;
        CanCrit = canCrit;
    }
}
