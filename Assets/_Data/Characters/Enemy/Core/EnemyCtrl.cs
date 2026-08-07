using UnityEngine;

public class EnemyCtrl : CharacterCtrl
{
    [SerializeField] private EnemyAIController enemyAIController;
    public EnemyAIController EnemyAIController => enemyAIController;
    [SerializeField] private EnemyLevel enemyLevel;
    public EnemyLevel EnemyLevel => enemyLevel;

    public void SetFaction(Faction value)
    {
        faction = value;
    }

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

    protected override void LoadCharacterLevel()
    {
        LoadEnemyLevel();
    }

    protected virtual void LoadEnemyAIController()
    {
        if (enemyAIController != null) return;
        enemyAIController = GetComponentInChildren<EnemyAIController>();
    }

    private void LoadEnemyLevel()
    {
        if (enemyLevel == null)
            enemyLevel = GetComponentInChildren<EnemyLevel>(true);

        characterLevel = enemyLevel;
    }
}
