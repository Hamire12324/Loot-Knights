using UnityEngine;

[RequireComponent(typeof(EnemyCtrl))]
public class EnemyPoolObj : PoolObj
{
    [SerializeField] private EnemyCtrl enemyCtrl;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadEnemyCtrl();
    }

    private void LoadEnemyCtrl()
    {
        if (enemyCtrl != null) return;
        enemyCtrl = GetComponent<EnemyCtrl>();
    }

    public override void OnSpawnedFromPool()
    {
        base.OnSpawnedFromPool();

        if (enemyCtrl == null)
            LoadEnemyCtrl();

        enemyCtrl.CharacterDamReceiver?.SetDead(false);
        enemyCtrl.CharacterDamReceiver?.SetInvincible(false);
        enemyCtrl.CharacterStat?.SetCurrentHealth(
            enemyCtrl.CharacterStat.MaxHealth?.FinalValue ?? 1f
        );

        if (enemyCtrl.Rb != null)
            enemyCtrl.Rb.linearVelocity = Vector2.zero;

        if (enemyCtrl.EnemyAIController != null)
            enemyCtrl.EnemyAIController.enabled = true;
    }

    public override void OnReturnedToPool()
    {
        base.OnReturnedToPool();

        if (enemyCtrl == null)
            LoadEnemyCtrl();

        enemyCtrl.CharacterCombatController?.CancelAttack(force: true);

        if (enemyCtrl.Rb != null)
            enemyCtrl.Rb.linearVelocity = Vector2.zero;

        if (enemyCtrl.EnemyAIController != null)
            enemyCtrl.EnemyAIController.enabled = false;
    }
}
