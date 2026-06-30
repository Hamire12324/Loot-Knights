using UnityEngine;
using System.Collections;

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
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color damageFlashColor = new(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float damageFlashDuration = 0.08f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;
    private Coroutine damageFlashCoroutine;

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
        LoadSpriteRenderers();

        if (characterCtrl.CharacterStat != null)
        {
            characterCtrl.CharacterStat.OnHealthChanged += HandleHealthChanged;
            characterCtrl.CharacterStat.OnStatChanged += OnStatChanged;
        }
    }

    protected override void OnDestroy()
    {
        if (characterCtrl.CharacterStat != null)
        {
            characterCtrl.CharacterStat.OnHealthChanged -= HandleHealthChanged;
            characterCtrl.CharacterStat.OnStatChanged -= OnStatChanged;
        }
    }

    public virtual void ReceiveDamage(float damage, Transform attacker = null, DamageData damageData = null)
    {
        if (isDead || isInvincible || characterCtrl.CharacterStat == null)
            return;

        float armor = characterCtrl.CharacterStat.Armor?.FinalValue ?? 0f;

        float finalDamage = Mathf.Max(damage - armor, 0f);

        characterCtrl.CharacterStat.SetCurrentHealth(characterCtrl.CharacterStat.CurrentHealth - finalDamage);

        if (finalDamage > 0f)
            PlayDamageFeedback();

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

    protected virtual void OnStatChanged(StatType type)
    {
        if (type != StatType.MaxHealth || characterCtrl.CharacterStat == null)
            return;

        float oldMax = characterCtrl.CharacterStat.PreviousMaxHealth;

        if (oldMax <= 0f)
            return;

        float percent = characterCtrl.CharacterStat.CurrentHealth / oldMax;

        float newMax = characterCtrl.CharacterStat.MaxHealth.FinalValue;

        characterCtrl.CharacterStat.SetCurrentHealth(newMax * percent);

        characterCtrl.CharacterStat.SetPreviousMaxHealth(newMax);
    }

    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    public void SetDead(bool value)
    {
        isDead = value;
    }

    private void PlayDamageFeedback()
    {
        if (!flashOnDamage) return;

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            LoadSpriteRenderers();

        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);

        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        SetSpriteColors(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);
        RestoreSpriteColors();
        damageFlashCoroutine = null;
    }

    private void LoadSpriteRenderers()
    {
        spriteRenderers = characterCtrl != null
            ? characterCtrl.GetComponentsInChildren<SpriteRenderer>(true)
            : GetComponentsInChildren<SpriteRenderer>(true);

        originalSpriteColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalSpriteColors[i] = spriteRenderers[i].color;
    }

    private void SetSpriteColors(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].color = color;
        }
    }

    private void RestoreSpriteColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].color = originalSpriteColors[i];
        }
    }
}
