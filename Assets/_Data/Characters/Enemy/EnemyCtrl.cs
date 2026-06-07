using UnityEngine;

public class EnemyCtrl : CharacterCtrl
{
    [SerializeField] private EnemyAIController enemyAIController;
    public EnemyAIController EnemyAIController => enemyAIController;

    protected override void ResetValue()
    {
        base.ResetValue();

        this.faction = Faction.Enemy;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadEnemyAIController();
    }

    protected virtual void LoadEnemyAIController()
    {
        if (enemyAIController != null) return;
        enemyAIController = GetComponentInChildren<EnemyAIController>();
    }
}
