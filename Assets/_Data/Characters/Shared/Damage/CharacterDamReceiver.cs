using UnityEngine;

public class CharacterDamReceiver : CharacterAbstract
{
    [Header("State")]
    [SerializeField] private bool isDead = false;
    public bool IsDead => isDead;

    [SerializeField] private bool isInvincible = false;
    public bool IsInvincible => isInvincible;

    [Header("Hit Stun")]
    [SerializeField] private bool canBeHitStunned = true;

    [SerializeField] private float fallbackHitStunDuration = 0.2f;
    [SerializeField] private float fallbackHitStunImmunityDuration = 0.75f;

    private float hitStunEndTime;
    private float hitStunImmunityEndTime;

    public bool IsHitStunned => Time.time < hitStunEndTime;
    public bool IsHitStunImmune => Time.time < hitStunImmunityEndTime;

    [Header("Damage Feedback")]
    [SerializeField] private CharacterDamageFlash damageFlash;

    public delegate void OnDeathDelegate(CharacterDamReceiver self);
    public event OnDeathDelegate OnDeath;

    public delegate void OnHpChangedDelegate(float currentHp, float maxHp);
    public event OnHpChangedDelegate OnHpChanged;

    public delegate void OnHitDelegate(float damage, Transform attacker);
    public event OnHitDelegate OnHit;

    public delegate void OnHitDetailedDelegate(float damage, Transform attacker, DamageData damageData);
    public event OnHitDetailedDelegate OnHitDetailed;

    protected override void Awake()
    {
        base.Awake();

        if (characterCtrl.CharacterStat != null)
        {
            characterCtrl.CharacterStat.OnHealthChanged += HandleHealthChanged;
        }
    }
    protected override void OnDestroy()
    {
        if (characterCtrl.CharacterStat != null)
        {
            characterCtrl.CharacterStat.OnHealthChanged -= HandleHealthChanged;
        }
    }


    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadDamageFlash();
    }
    private void LoadDamageFlash()
    {
        if (damageFlash != null) return;

        damageFlash = GetComponentInChildren<CharacterDamageFlash>(true);
    }
    public virtual void ReceiveDamage(float damage, Transform attacker = null, DamageData damageData = null)
    {
        if (isDead || isInvincible || characterCtrl.CharacterStat == null)
            return;

        float armor = characterCtrl.CharacterStat.Armor?.FinalValue ?? 0f;

        float finalDamage = Mathf.Max(damage - armor, 0f);

        characterCtrl.CharacterStat.SetCurrentHealth(characterCtrl.CharacterStat.CurrentHealth - finalDamage);

        if (finalDamage > 0f) damageFlash?.Play();

        TryApplyHitStun(damageData);

        OnHit?.Invoke(finalDamage, attacker);
        OnHitDetailed?.Invoke(finalDamage, attacker, damageData);
        CharacterElementalState.ApplyElementalHit(
            characterCtrl,
            attacker != null ? attacker.GetComponentInParent<CharacterCtrl>() : null,
            finalDamage,
            damageData);

        if (characterCtrl.CharacterStat.CurrentHealth <= 0f)
        {
            Die(attacker);
        }
    }

    private void TryApplyHitStun(DamageData damageData)
    {
        if (!canBeHitStunned || damageData == null) return;
        if (!damageData.CausesHitStun || IsHitStunImmune) return;

        float stunDuration = Mathf.Max(0f, damageData.HitStunDuration);
        float immunityDuration = Mathf.Max(0f, damageData.HitStunImmunityDuration);

        if (stunDuration <= 0f)
            stunDuration = fallbackHitStunDuration;

        if (immunityDuration <= 0f)
            immunityDuration = fallbackHitStunImmunityDuration;

        hitStunEndTime = Time.time + stunDuration;
        hitStunImmunityEndTime = Time.time + stunDuration + immunityDuration;

        if (damageData.InterruptsAttack)
            characterCtrl.CharacterCombatController?.CancelAttack(force: false);
        characterCtrl.CharacterAnimation?.PlayHurt();
    }

    public virtual void Heal(float amount)
    {
        if (isDead || characterCtrl.CharacterStat == null)
            return;

        characterCtrl.CharacterStat.SetCurrentHealth(characterCtrl.CharacterStat.CurrentHealth + amount);
    }

    protected virtual void Die(Transform killer = null)
    {
        if (isDead) return;

        characterCtrl.CharacterCombatController?.CancelAttack(force: true);

        isDead = true;

        characterCtrl.CharacterAnimation?.PlayDeath();

        OnDeath?.Invoke(this);
    }

    public virtual void Revive()
    {
        if (characterCtrl.CharacterStat == null)
            return;

        characterCtrl.CharacterStat.SetCurrentHealth(characterCtrl.CharacterStat.MaxHealth?.FinalValue ?? 1f);

        isDead = false;
        hitStunEndTime = 0f;
        hitStunImmunityEndTime = 0f;

        characterCtrl.CharacterAnimation?.ResetAfterRevive();
    }

    protected virtual void HandleHealthChanged(float currentHp)
    {
        OnHpChanged?.Invoke(
            currentHp,
            characterCtrl.CharacterStat.MaxHealth?.FinalValue ?? 1f
        );
    }

    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    public void SetDead(bool value)
    {
        isDead = value;
    }
}
