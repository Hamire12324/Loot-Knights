using UnityEngine;

/// <summary>References the systems required by a fixed background stage.</summary>
public class StageMapRoot : BaseMonoBehaviour
{
    [SerializeField] private StageMapController stageMapController;
    public StageMapController StageMapController => stageMapController;

    [SerializeField] private StageManager stageManager;
    public StageManager StageManager => stageManager;

    [SerializeField] private EnemySpawner enemySpawner;
    public EnemySpawner EnemySpawner => enemySpawner;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        stageMapController ??= GetComponentInChildren<StageMapController>(true);
        stageManager ??= GetComponentInChildren<StageManager>(true);
        enemySpawner ??= FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
    }
}
