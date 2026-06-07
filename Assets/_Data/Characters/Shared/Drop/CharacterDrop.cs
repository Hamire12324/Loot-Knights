using UnityEngine;

public abstract class CharacterDrop : BaseMonoBehaviour
{
    [SerializeField] private CharacterDamReceiver damageReceiver;
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
    [SerializeField] private float scatterRadius = 0.35f;
    [SerializeField] private Vector2 dropOffset;

    private bool droppedThisLife;

    protected override void OnEnable()
    {
        base.OnEnable();
        droppedThisLife = false;
        SubscribeDeathEvent();
    }

    protected override void OnDisable()
    {
        UnsubscribeDeathEvent();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadDamageReceiver();
    }

    protected abstract void Drop(CharacterDamReceiver receiver);

    protected Vector3 GetDropPosition(CharacterDamReceiver receiver)
    {
        Vector3 basePosition = receiver != null ? receiver.transform.position : transform.position;
        Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);
        Vector2 offset = dropOffset + scatter;

        return basePosition + new Vector3(offset.x, offset.y, 0f);
    }

    private void LoadDamageReceiver()
    {
        if (damageReceiver != null) return;

        CharacterCtrl characterCtrl = GetComponentInParent<CharacterCtrl>(true);
        if (characterCtrl != null && characterCtrl.CharacterDamReceiver != null)
        {
            damageReceiver = characterCtrl.CharacterDamReceiver;
            return;
        }

        damageReceiver = GetComponentInParent<CharacterDamReceiver>(true);
        if (damageReceiver != null) return;

        damageReceiver = transform.root.GetComponentInChildren<CharacterDamReceiver>(true);
    }

    private void SubscribeDeathEvent()
    {
        if (damageReceiver == null)
            LoadDamageReceiver();

        if (damageReceiver == null) return;

        damageReceiver.OnDeath -= HandleDeath;
        damageReceiver.OnDeath += HandleDeath;
    }

    private void UnsubscribeDeathEvent()
    {
        if (damageReceiver == null) return;

        damageReceiver.OnDeath -= HandleDeath;
    }

    private void HandleDeath(CharacterDamReceiver receiver)
    {
        if (droppedThisLife) return;
        droppedThisLife = true;

        if (Random.value > dropChance) return;

        Drop(receiver);
    }
}
