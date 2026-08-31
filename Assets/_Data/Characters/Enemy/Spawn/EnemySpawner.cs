using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : BaseMonoBehaviour
{
    private const string ElementalSkillTreeResourcePath = "SkillTrees/Common/Elemental_SkillTree";
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

    [Header("Element Shards")]
    [SerializeField] private bool dropElementShardsOnDeath = true;
    [SerializeField] private ElementalShardPickup elementalShardPrefab;
    [SerializeField, Range(0f, 1f)] private float elementalShardDropChance = 1f;
    [SerializeField, Min(0)] private int minElementalShardDrops = 1;
    [SerializeField, Min(0)] private int maxElementalShardDrops = 2;
    [SerializeField, Min(0f)] private float elementalShardScatterRadius = 0.35f;
    [SerializeField, Min(0f)] private float elementalShardPower = 1f;
    [SerializeField] private SkillTreeDefinition elementalSkillTree;

    private readonly List<PoolObj> aliveEnemies = new();
    private static readonly ElementType[] DropElements =
    {
        ElementType.Fire,
        ElementType.Frost,
        ElementType.Lightning,
        ElementType.Poison
    };

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

    /// <summary>
    /// Spawns the requested number of enemies when spawn limits allow it. Each enemy
    /// is randomly chosen by Weight from the supplied stage roster.
    /// </summary>
    public List<PoolObj> SpawnCount(
        IReadOnlyList<Vector3> positions,
        int enemyCount,
        int level,
        IReadOnlyList<StageEnemyEntry> stageEnemyEntries,
        BossEncounterConfig bossEncounter = null)
    {
        List<PoolObj> spawnedEnemies = new();

        if (positions == null || positions.Count == 0 || enemyCount <= 0)
            return spawnedEnemies;

        SetDifficultyLevel(level);
        RemoveMissingEnemies();

        int[] anchorUseCounts = new int[positions.Count];
        while (spawnedEnemies.Count < enemyCount && aliveEnemies.Count < maxAlive)
        {
            StageEnemyEntry entry = PickStageEnemy(stageEnemyEntries, difficultyLevel);
            if (entry == null || !TryPickAnchorIndex(anchorUseCounts, out int positionIndex))
                break;

            PoolObj enemy = SpawnAt(entry.Prefab, GetSpawnPositionNearAnchor(positions[positionIndex]));
            if (enemy == null)
                break;

            BossEnemy bossEnemy = enemy.GetComponent<BossEnemy>();
            if (bossEnemy == null && bossEncounter != null && bossEncounter.Enabled)
                bossEnemy = enemy.gameObject.AddComponent<BossEnemy>();

            bossEnemy?.Configure(bossEncounter);

            anchorUseCounts[positionIndex]++;
            spawnedEnemies.Add(enemy);
        }

        if (spawnedEnemies.Count < enemyCount)
        {
            Debug.LogWarning(
                $"{name}: Spawned {spawnedEnemies.Count}/{enemyCount} stage enemies. " +
                "Check Stage Enemies, Max Alive, and spawn-point limits.",
                gameObject);
        }

        return spawnedEnemies;
    }

    private static StageEnemyEntry PickStageEnemy(IReadOnlyList<StageEnemyEntry> entries, int level)
    {
        if (entries == null || entries.Count == 0)
            return null;

        List<StageEnemyEntry> candidates = new();
        int totalWeight = 0;

        foreach (StageEnemyEntry entry in entries)
        {
            if (entry == null || entry.Prefab == null || !entry.IsAllowedAtDifficulty(level))
                continue;

            candidates.Add(entry);
            totalWeight += entry.Weight;
        }

        if (candidates.Count == 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        foreach (StageEnemyEntry entry in candidates)
        {
            roll -= entry.Weight;
            if (roll < 0)
                return entry;
        }

        return candidates[0];
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

        ApplyLevelToEnemy(enemy, difficultyLevel);

        return enemy;
    }

    private void ApplyLevelToEnemy(PoolObj enemy, int level)
    {
        if (enemy == null) return;

        EnemyLevel enemyLevel = enemy.GetComponentInChildren<EnemyLevel>(true);
        if (enemyLevel != null)
        {
            enemyLevel.ApplyLevel(level);
            return;
        }

        CharacterLevel characterLevel = enemy.GetComponentInChildren<CharacterLevel>(true);
        characterLevel?.ApplyLevel(level);
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

        PoolObj poolObj = ResolvePoolObj(receiver);
        if (poolObj == null)
        {
            Debug.LogWarning(
                $"{name}: Enemy death received from {receiver.name}, but no PoolObj was found in its hierarchy.",
                receiver.gameObject);
            return;
        }

        receiver.OnDeath -= HandleEnemyDeath;
        aliveEnemies.Remove(poolObj);
        DropElementShards(receiver);

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            poolObj.ReturnToPool();
            return;
        }

        StartCoroutine(ReturnToPoolAfterDelay(poolObj));
    }

    private void DropElementShards(CharacterDamReceiver receiver)
    {
        if (!dropElementShardsOnDeath || receiver == null)
            return;

        if (Random.value > elementalShardDropChance)
            return;

        int minDrops = Mathf.Max(0, minElementalShardDrops);
        int maxDrops = Mathf.Max(minDrops, maxElementalShardDrops);
        int count = Random.Range(minDrops, maxDrops + 1);
        if (count <= 0)
            return;

        if (!TryResolveUnlockedDropElement(receiver, out ElementType element))
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, elementalShardScatterRadius);
            Vector3 position = receiver.transform.position + new Vector3(scatter.x, scatter.y, 0f);
            ElementalShardPickup.Spawn(element, elementalShardPower, position, elementalShardPrefab);
        }
    }

    private bool TryResolveUnlockedDropElement(CharacterDamReceiver receiver, out ElementType element)
    {
        element = ElementType.None;
        elementalSkillTree ??= Resources.Load<SkillTreeDefinition>(ElementalSkillTreeResourcePath);
        if (elementalSkillTree == null)
            return false;

        SkillTreeRuntime runtime = new(elementalSkillTree);
        CharacterElementalState state = receiver.GetComponentInChildren<CharacterElementalState>();
        if (state != null && state.TryGetStrongestStatus(out ElementType statusElement, out _) && runtime.HasElement(statusElement))
        {
            element = statusElement;
            return true;
        }

        ElementType[] unlockedElements = new ElementType[DropElements.Length];
        int unlockedCount = 0;
        foreach (ElementType candidate in DropElements)
        {
            if (runtime.HasElement(candidate))
                unlockedElements[unlockedCount++] = candidate;
        }

        if (unlockedCount == 0)
            return false;

        element = unlockedElements[Random.Range(0, unlockedCount)];
        return true;
    }

    private static PoolObj ResolvePoolObj(CharacterDamReceiver receiver)
    {
        if (receiver == null) return null;

        PoolObj poolObj = receiver.GetComponentInParent<PoolObj>();
        if (poolObj != null) return poolObj;

        CharacterCtrl characterCtrl = receiver.CharacterCtrl;
        if (characterCtrl != null)
            poolObj = characterCtrl.GetComponent<PoolObj>();

        return poolObj;
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
