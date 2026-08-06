using UnityEngine;
using System.Collections;

public class EnemyDamReceiver : CharacterDamReceiver
{
    [Header("Enemy Despawn Fallback")]
    [SerializeField] private float fallbackReturnToPoolDelay = 1.2f;

    private PoolObj poolObj;
    private EnemyBlockBehaviour blockBehaviour;
    private Coroutine fallbackReturnCoroutine;

    public override void ReceiveDamage(float damage, Transform attacker = null, DamageData damageData = null)
    {
        if (blockBehaviour == null)
            blockBehaviour = GetComponentInParent<EnemyBlockBehaviour>();

        if (blockBehaviour != null && blockBehaviour.IsBlocking)
            damage *= blockBehaviour.DamageMultiplier;

        base.ReceiveDamage(damage, attacker, damageData);
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadPoolObj();
    }

    protected override void OnDisable()
    {
        StopFallbackReturnCoroutine();
        base.OnDisable();
    }

    protected override void Die(Transform killer = null)
    {
        bool wasDead = IsDead;

        base.Die(killer);

        if (!wasDead && IsDead)
            ScheduleFallbackReturnToPool();
    }

    public override void Revive()
    {
        StopFallbackReturnCoroutine();
        base.Revive();
    }

    private void ScheduleFallbackReturnToPool()
    {
        StopFallbackReturnCoroutine();
        fallbackReturnCoroutine = StartCoroutine(ReturnToPoolFallback());
    }

    private IEnumerator ReturnToPoolFallback()
    {
        float delay = Mathf.Max(0f, fallbackReturnToPoolDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        fallbackReturnCoroutine = null;

        if (!IsDead || !gameObject.activeInHierarchy)
            yield break;

        if (poolObj == null)
            LoadPoolObj();

        if (poolObj != null)
        {
            if (!poolObj.IsInPool)
                poolObj.ReturnToPool();

            yield break;
        }

        CharacterCtrl ctrl = CharacterCtrl;
        if (ctrl != null)
            ctrl.gameObject.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void StopFallbackReturnCoroutine()
    {
        if (fallbackReturnCoroutine == null) return;

        StopCoroutine(fallbackReturnCoroutine);
        fallbackReturnCoroutine = null;
    }

    private void LoadPoolObj()
    {
        if (poolObj != null) return;

        poolObj = GetComponentInParent<PoolObj>();
        if (poolObj != null) return;

        if (CharacterCtrl != null)
            poolObj = CharacterCtrl.GetComponent<PoolObj>();
    }

}
