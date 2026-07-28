using UnityEngine;

[System.Serializable]
public class DamageData
{
    public float Multiplier = 1f;
    public bool CanCrit;

    [Header("Hit Stun")]
    public bool CausesHitStun;
    public float HitStunDuration = 0.2f;
    public float HitStunImmunityDuration = 0.75f;
    public bool InterruptsAttack = true;

    [Header("VFX")]
    public VFXDefinition HitVfx;
    public Vector3 HitVfxOffset;

    [Header("SFX")]
    public SFXDefinition HitSfx;
    public Vector3 HitSfxOffset;

    [Header("Element")]
    [SerializeField] private ElementType element = ElementType.None;
    [SerializeField, Min(0f)] private float elementalPower = 1f;
    [SerializeField, Min(0f)] private float elementalStatusDuration = 4f;
    [SerializeField] private bool consumesElementOnReaction = true;

    public ElementType Element => element;
    public float ElementalPower => elementalPower;
    public float ElementalStatusDuration => elementalStatusDuration;
    public bool ConsumesElementOnReaction => consumesElementOnReaction;
    public DamageData(float multiplier, bool canCrit = false)
    {
        Multiplier = multiplier;
        CanCrit = canCrit;
    }

    public DamageData CloneWithElement(
        ElementType newElement,
        float? power = null,
        float? duration = null,
        bool? consumeOnReaction = null)
    {
        DamageData clone = (DamageData)MemberwiseClone();

        clone.element = newElement;
        clone.elementalPower = power ?? elementalPower;
        clone.elementalStatusDuration = duration ?? elementalStatusDuration;
        clone.consumesElementOnReaction =
            consumeOnReaction ?? consumesElementOnReaction;

        return clone;
    }
}