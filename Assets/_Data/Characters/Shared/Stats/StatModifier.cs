using System;
using UnityEngine;

[Serializable]
public class StatModifier
{
    [SerializeField] private StatType statType;
    public StatType StatType => statType;

    [SerializeField] private ModifierType modifierType;
    public ModifierType ModifierType => modifierType;

    [SerializeField] private float amount;
    public float Amount => amount;
    [SerializeField] private bool isEnabled = true;
    public bool IsEnabled => isEnabled;
    [SerializeField] private UnityEngine.Object source;
    public UnityEngine.Object Source => source;

    [SerializeField] private float duration;
    public float Duration => duration;

    [SerializeField] private int stack = 1;
    public int Stack => stack;

    [SerializeField] private float startTime;
    public float StartTime => startTime;
    public StatModifier(StatType statType, ModifierType modifierType, float value, 
        UnityEngine.Object source = null, float duration = -1f, int stack = 1)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.amount = value;
        this.source = source;
        this.duration = duration;
        this.stack = Mathf.Max(1, stack);
        this.isEnabled = true;
        this.startTime = Time.time;
    }
    public bool HasExpired()
    {
        if (duration < 0f) return false;
        return Time.time >= startTime + duration;
    }
    public void AddStack(int count = 1)
    {
        stack += count;
    }
    public bool RemoveStack(int count = 1)
    {
        stack -= count;
        if (stack <= 0)
        {
            stack = 0;
            return false;
        }
        return true;
    }
    public float GetEffectiveValue()
    {
        if (!isEnabled) return 0f;
        return amount * stack;
    }
    public void Enable()
    {
        isEnabled = true;
    }
    public void Disable()
    {
        isEnabled = false;
    }
}