using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : BaseMonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private PoolObj enemyPrefab;

    [Header("Budget Spawn")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new();
    [SerializeField] private int maxSpawnsPerAnchor = 4;
    [SerializeField] private float anchorSpawnSpreadRadius = 0.45f;

    [Header("Spawn Limits")]
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private float returnToPoolDelay = 0.8f;

    private readonly List<PoolObj> aliveEnemies = new();

    protected override void OnDisable()
    {
        UnbindAllDeathEvents();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        UnbindAllDeathEvents();
        base.OnDestroy();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadPoolManager();
    }

    private void LoadPoolManager()
    {
        if (poolManager != null) return;
        poolManager = PoolManager.InstanceOrNull;
    }

    public int DifficultyLevel => difficultyLevel;

    public void SetDifficultyLevel(int level)
    {
        difficultyLevel = Mathf.Max(1, level);
    }

    public int GetMinimumEnemyCost(int level, IReadOnlyList<EnemySpawnEntry> stageEnemyEntries = null)
    {
        return EnemySpawnTable.GetMinimumCost(enemyEntries, stageEnemyEntries, enemyPrefab, level);
    }

    public List<PoolObj> SpawnBudget(IReadOnlyList<Vector3> positions, int budget, int level)
    {
        return SpawnBudget(positions, budget, level, null);
    }

    public List<PoolObj> SpawnBudget(
        IReadOnlyList<Vector3> positions,
        int budget,
        int level,
        IReadOnlyList<EnemySpawnEntry> stageEnemyEntries)
    {
        List<PoolObj> spawnedEnemies = new();

        if (positions == null || positions.Count == 0 || budget <= 0)
        {
            Debug.LogWarning($"{name}: SpawnBudget skipped. positions={positions?.Count ?? 0}, budget={budget}.", gameObject);
            return spawnedEnemies;
        }

        SetDifficultyLevel(level);
        RemoveMissingEnemies();

        int[] anchorUseCounts = new int[positions.Count];
        int remainingBudget = budget;

        while (remainingBudget > 0 && aliveEnemies.Count < maxAlive)
        {
            EnemySpawnEntry entry = EnemySpawnTable.Pick(
                enemyEntries,
                stageEnemyEntries,
                enemyPrefab,
                remainingBudget,
                difficultyLevel);
            if (entry == null)
            {
                Debug.LogWarning(
                    $"{name}: No enemy option for remainingBudget={remainingBudget}, difficultyLevel={difficultyLevel}.",
                    gameObject);
                break;
            }

            if (!TryPickAnchorIndex(anchorUseCounts, out int positionIndex))
                break;

            Vector3 position = GetSpawnPositionNearAnchor(positions[positionIndex]);

            PoolObj enemy = SpawnAt(entry.Prefab, position);
            if (enemy == null)
            {
                Debug.LogWarning(
                    $"{name}: SpawnAt returned null for prefab={entry.Prefab?.name}, position={position}.",
                    gameObject);
                break;
            }

            anchorUseCounts[positionIndex]++;
            spawnedEnemies.Add(enemy);
            remainingBudget -= entry.Cost;
        }

        return spawnedEnemies;
    }

    private bool TryPickAnchorIndex(int[] anchorUseCounts, out int index)
    {
        index = -1;
        if (anchorUseCounts == null || anchorUseCounts.Length == 0) return false;

        int maxPerAnchor = Mathf.Max(1, maxSpawnsPerAnchor);
        List<int> availableIndices = new();

        for (int i = 0; i < anchorUseCounts.Length; i++)
        {
            if (anchorUseCounts[i] < maxPerAnchor)
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
            return false;

        index = availableIndices[Random.Range(0, availableIndices.Count)];
        return true;
    }

    private Vector3 GetSpawnPositionNearAnchor(Vector3 anchor)
    {
        float radius = Mathf.Max(0f, anchorSpawnSpreadRadius);
        if (radius <= 0f) return anchor;

        Vector2 offset = Random.insideUnitCircle * radius;
        return anchor + new Vector3(offset.x, offset.y, 0f);
    }

    private PoolObj SpawnAt(PoolObj prefab, Vector3 position)
    {
        RemoveMissingEnemies();

        if (aliveEnemies.Count >= maxAlive)
        {
            Debug.LogWarning($"{name}: Max alive reached. alive={aliveEnemies.Count}, maxAlive={maxAlive}.", gameObject);
            return null;
        }

        if (poolManager == null)
            LoadPoolManager();

        if (poolManager == null || prefab == null)
        {
            Debug.LogWarning(
                $"{name}: Cannot spawn enemy. poolManager={(poolManager != null ? "ok" : "null")}, prefab={(prefab != null ? prefab.name : "null")}.",
                gameObject);
            return null;
        }

        PoolObj enemy = poolManager.Spawn(prefab, position, Quaternion.identity);
        if (enemy == null)
            return null;

        aliveEnemies.Add(enemy);
        BindDeathEvent(enemy);

        return enemy;
    }

    public void ReturnAllAliveEnemies()
    {
        foreach (PoolObj enemy in aliveEnemies)
        {
            if (enemy == null || enemy.IsInPool) continue;
            enemy.ReturnToPool();
        }

        aliveEnemies.Clear();
    }

    private void BindDeathEvent(PoolObj enemy)
    {
        CharacterDamReceiver receiver = enemy.GetComponentInChildren<CharacterDamReceiver>();
        if (receiver == null) return;

        receiver.OnDeath -= HandleEnemyDeath;
        receiver.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(CharacterDamReceiver receiver)
    {
        if (this == null) return;
        if (receiver == null) return;

        PoolObj poolObj = receiver.GetComponentInParent<PoolObj>();
        if (poolObj == null) return;

        receiver.OnDeath -= HandleEnemyDeath;
        aliveEnemies.Remove(poolObj);

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            poolObj.ReturnToPool();
            return;
        }

        StartCoroutine(ReturnToPoolAfterDelay(poolObj));
    }

    private IEnumerator ReturnToPoolAfterDelay(PoolObj poolObj)
    {
        if (returnToPoolDelay > 0f)
            yield return new WaitForSeconds(returnToPoolDelay);

        poolObj?.ReturnToPool();
    }

    private void RemoveMissingEnemies()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsInPool || !enemy.gameObject.activeInHierarchy);
    }

    private void UnbindAllDeathEvents()
    {
        foreach (PoolObj enemy in aliveEnemies)
        {
            if (enemy == null) continue;

            CharacterDamReceiver receiver = enemy.GetComponentInChildren<CharacterDamReceiver>();
            if (receiver == null) continue;

            receiver.OnDeath -= HandleEnemyDeath;
        }
    }
}
