using System;

public class CharacterSkillRuntime
{
    private readonly CharacterSkillDefinition definition;
    private readonly CharacterSkillCooldown cooldown = new();

    public CharacterSkillDefinition Definition => definition;
    public CharacterSkillCooldown Cooldown => cooldown;
    public bool IsUnlocked { get; private set; } = true;

    public event Action<CharacterSkillRuntime> OnChanged;

    public CharacterSkillRuntime(CharacterSkillDefinition definition)
    {
        this.definition = definition;
    }

    public virtual bool CanCast(CharacterSkillController controller)
    {
        if (definition == null) return false;
        if (!IsUnlocked) return false;
        if (!cooldown.IsReady) return false;
        if (controller == null || controller.IsCasting) return false;

        CharacterCtrl caster = controller.CharacterCtrl;
        if (caster == null) return false;
        if (caster.CharacterStat != null && caster.CharacterStat.CurrentMana < definition.ManaCost) return false;
        if (caster.CharacterDamReceiver != null && caster.CharacterDamReceiver.IsDead) return false;
        if (caster.CharacterDamReceiver != null && caster.CharacterDamReceiver.IsHitStunned) return false;

        return true;
    }

    public void StartCooldown(float cooldownDuration)
    {
        cooldown.Start(cooldownDuration);
        NotifyChanged();
    }

    public void ReduceCooldown(float seconds)
    {
        if (cooldown.Reduce(seconds))
            NotifyChanged();
    }

    public void SetUnlocked(bool value)
    {
        if (IsUnlocked == value) return;

        IsUnlocked = value;
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }
}
