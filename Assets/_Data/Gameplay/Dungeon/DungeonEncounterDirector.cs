using System.Collections.Generic;
using UnityEngine;

public class DungeonEncounterDirector : DungeonAbstract
{
    [SerializeField] private DungeonTilemapPainter tilemapPainter;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private bool spawnEnemiesFromRoomTemplates = true;
    [SerializeField] private bool clearEnemiesOnGenerate = true;
    [SerializeField] private bool skipStartRoomEnemies = true;
    [SerializeField] private bool completeStageWhenBossRoomCleared = true;
    [SerializeField] private bool debugEnemySpawn = true;

    private readonly List<CharacterDamReceiver> trackedBossRoomEnemies = new();
    private int aliveBossRoomEnemies;
    private bool stageCompleteNotified;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadTilemapPainter();
        LoadEnemySpawner();
    }

    public void Configure(DungeonCtrl ctrl, DungeonTilemapPainter painter, EnemySpawner spawner)
    {
        if (dungeonCtrl == null)
            dungeonCtrl = ctrl;

        if (tilemapPainter == null)
            tilemapPainter = painter;

        if (enemySpawner == null)
            enemySpawner = spawner;
    }

    public void PrepareForGenerate()
    {
        ResetBossRoomTracking();

        if (enemySpawner == null)
            LoadEnemySpawner();

        if (enemySpawner == null) return;

        if (clearEnemiesOnGenerate)
            enemySpawner.ReturnAllAliveEnemies();
    }

    public void SpawnEnemies(
        IReadOnlyList<DungeonRoomInstance> generatedRooms,
        DungeonStageConfig activeStage,
        int difficultyLevel)
    {
        if (!spawnEnemiesFromRoomTemplates) return;

        if (enemySpawner == null)
            LoadEnemySpawner();

        if (tilemapPainter == null)
            LoadTilemapPainter();

        if (enemySpawner == null || tilemapPainter == null) return;

        int level = Mathf.Max(1, difficultyLevel);
        enemySpawner.SetDifficultyLevel(level);

        Dictionary<DungeonRoomInstance, int> roomBudgets = AllocateEnemyBudgets(generatedRooms, activeStage, level);
        Dictionary<DungeonRoomInstance, int> spawnedCounts = new();

        foreach (KeyValuePair<DungeonRoomInstance, int> roomBudget in roomBudgets)
            spawnedCounts[roomBudget.Key] = SpawnEnemiesForRoom(roomBudget.Key, roomBudget.Value, activeStage, level);

        if (debugEnemySpawn)
        {
            Debug.Log(
                $"{name}: Enemy spawn finished. Stage={(activeStage != null ? activeStage.StageName : "None")} " +
                $"level={level}, generatedRooms={generatedRooms.Count}, roomsWithBudget={roomBudgets.Count}.",
                gameObject);

            LogDungeonRoomInstanceSummary(generatedRooms, roomBudgets, spawnedCounts);
        }
    }

    private int SpawnEnemiesForRoom(
        DungeonRoomInstance room,
        int budget,
        DungeonStageConfig activeStage,
        int level)
    {
        if (room == null || budget <= 0) return 0;

        Vector2Int[] spawnCells = room.Template.EnemySpawns;
        if (spawnCells == null || spawnCells.Length == 0)
        {
            LogEnemySpawnDebug(room, $"Skipped: no enemy spawn cells. budget={budget}");
            return 0;
        }

        List<Vector3> spawnPositions = new();

        foreach (Vector2Int localCell in spawnCells)
        {
            if (TryGetEnemySpawnPosition(room, localCell, out Vector3 spawnPosition))
                spawnPositions.Add(spawnPosition);
        }

        if (spawnPositions.Count == 0)
        {
            LogEnemySpawnDebug(room, $"Skipped: no valid enemy spawn positions. budget={budget}, configuredCells={spawnCells.Length}");
            return 0;
        }

        List<PoolObj> spawnedEnemies = enemySpawner.SpawnBudget(
            spawnPositions,
            budget,
            level,
            GetStageEnemyEntries(activeStage));

        foreach (PoolObj enemy in spawnedEnemies)
            ConfigureSpawnedEnemy(enemy);

        if (completeStageWhenBossRoomCleared && room.Template.RoomType == RoomType.Boss)
            TrackBossRoomEnemies(spawnedEnemies);

        LogEnemySpawnDebug(
            room,
            $"budget={budget}, validPositions={spawnPositions.Count}, spawned={spawnedEnemies.Count}, level={level}");

        return spawnedEnemies.Count;
    }

    private void TrackBossRoomEnemies(List<PoolObj> spawnedEnemies)
    {
        if (spawnedEnemies == null || spawnedEnemies.Count == 0) return;

        foreach (PoolObj enemy in spawnedEnemies)
        {
            CharacterDamReceiver receiver = enemy != null
                ? enemy.GetComponentInChildren<CharacterDamReceiver>()
                : null;

            if (receiver == null) continue;
            if (trackedBossRoomEnemies.Contains(receiver)) continue;

            receiver.OnDeath -= HandleBossRoomEnemyDeath;
            receiver.OnDeath += HandleBossRoomEnemyDeath;

            trackedBossRoomEnemies.Add(receiver);
            aliveBossRoomEnemies++;
        }
    }

    private void HandleBossRoomEnemyDeath(CharacterDamReceiver receiver)
    {
        if (receiver != null)
            receiver.OnDeath -= HandleBossRoomEnemyDeath;

        trackedBossRoomEnemies.Remove(receiver);
        aliveBossRoomEnemies = Mathf.Max(0, aliveBossRoomEnemies - 1);

        if (stageCompleteNotified || aliveBossRoomEnemies > 0)
            return;

        stageCompleteNotified = true;
        CompleteStage();
    }

    private void CompleteStage()
    {
        DungeonStageManager stageManager = dungeonCtrl != null
            ? dungeonCtrl.StageManager
            : null;

        if (stageManager == null)
            stageManager = GetComponentInParent<DungeonStageManager>();

        if (stageManager == null)
            stageManager = FindAnyObjectByType<DungeonStageManager>(FindObjectsInactive.Include);

        if (stageManager == null)
        {
            Debug.LogWarning(name + ": Boss room cleared but DungeonStageManager was not found.", gameObject);
            return;
        }

        stageManager.CompleteStage();
    }

    private void ResetBossRoomTracking()
    {
        foreach (CharacterDamReceiver receiver in trackedBossRoomEnemies)
        {
            if (receiver == null) continue;
            receiver.OnDeath -= HandleBossRoomEnemyDeath;
        }

        trackedBossRoomEnemies.Clear();
        aliveBossRoomEnemies = 0;
        stageCompleteNotified = false;
    }

    private bool TryGetEnemySpawnPosition(
        DungeonRoomInstance room,
        Vector2Int localCell,
        out Vector3 position)
    {
        position = Vector3.zero;

        if (!room.Template.ContainsLocalCell(localCell))
        {
            Debug.LogWarning(room.Template.name + ": Enemy spawn " + localCell + " is outside room bounds.", room.Template);
            return false;
        }

        if (room.Template.IsEdgeCell(localCell))
        {
            Debug.LogWarning(room.Template.name + ": Enemy spawn " + localCell + " is on a wall/door edge.", room.Template);
            return false;
        }

        Vector2Int worldCell = room.Origin + localCell;
        Vector3Int tileCell = ToVector3Int(worldCell);

        if (!tilemapPainter.HasFloorCell(tileCell))
        {
            Debug.LogWarning(room.Template.name + ": Enemy spawn " + localCell + " has no floor tile.", room.Template);
            return false;
        }

        position = tilemapPainter.GetCellCenterWorld(tileCell);
        return true;
    }

    private void ConfigureSpawnedEnemy(PoolObj enemy)
    {
        if (enemy == null || tilemapPainter == null) return;

        EnemyMovement movement = enemy.GetComponentInChildren<EnemyMovement>();
        if (movement == null) return;

        int wallLayerMask = tilemapPainter.GetWallLayerMask();
        if (wallLayerMask != 0)
            movement.ConfigureObstacleLayer(wallLayerMask);
    }

    private IReadOnlyList<EnemySpawnEntry> GetStageEnemyEntries(DungeonStageConfig activeStage)
    {
        return activeStage != null ? activeStage.EnemyEntries : null;
    }

    private Dictionary<DungeonRoomInstance, int> AllocateEnemyBudgets(
        IReadOnlyList<DungeonRoomInstance> generatedRooms,
        DungeonStageConfig activeStage,
        int level)
    {
        Dictionary<DungeonRoomInstance, int> budgets = new();
        List<DungeonRoomInstance> standardRooms = new();
        List<DungeonRoomInstance> bossRooms = new();

        foreach (DungeonRoomInstance room in generatedRooms)
        {
            if (!CanRoomSpawnEnemies(room, out string skipReason))
            {
                LogEnemySpawnDebug(room, "Budget skipped: " + skipReason);
                continue;
            }

            if (room.Template.RoomType == RoomType.Boss)
                bossRooms.Add(room);
            else
                standardRooms.Add(room);
        }

        int minEnemyCost = enemySpawner != null
            ? enemySpawner.GetMinimumEnemyCost(level, GetStageEnemyEntries(activeStage))
            : 0;

        if (debugEnemySpawn)
        {
            Debug.Log(
                $"{name}: Allocating enemy budgets. standardRooms={standardRooms.Count}, bossRooms={bossRooms.Count}, " +
                $"minEnemyCost={minEnemyCost}, totalEnemyBudget={(activeStage != null ? activeStage.TotalEnemyBudget : GetFallbackTotalEnemyBudget(standardRooms))}, " +
                $"bossRoomBudget={(activeStage != null ? activeStage.BossRoomBudget : GetFallbackTotalEnemyBudget(bossRooms))}.",
                gameObject);
        }

        int standardBudget = activeStage != null
            ? activeStage.TotalEnemyBudget
            : GetFallbackTotalEnemyBudget(standardRooms);

        DistributeBudget(standardRooms, standardBudget, budgets, minEnemyCost);

        int bossBudget = activeStage != null
            ? activeStage.BossRoomBudget
            : GetFallbackTotalEnemyBudget(bossRooms);

        DistributeBudget(bossRooms, bossBudget, budgets, minEnemyCost);

        return budgets;
    }

    private bool CanRoomSpawnEnemies(DungeonRoomInstance room, out string reason)
    {
        reason = string.Empty;

        if (room == null || room.Template == null)
        {
            reason = "room/template null";
            return false;
        }

        if (skipStartRoomEnemies && room.Template.RoomType == RoomType.Start)
        {
            reason = "start room";
            return false;
        }

        if (room.Template.EnemySpawns == null || room.Template.EnemySpawns.Length == 0)
        {
            reason = "no enemy spawn cells";
            return false;
        }

        if (room.Template.EnemyBudgetWeight <= 0)
        {
            reason = "enemyBudgetWeight <= 0";
            return false;
        }

        if (room.Template.MaxEnemyBudget <= 0)
        {
            reason = "maxEnemyBudget <= 0";
            return false;
        }

        return true;
    }

    private int GetFallbackTotalEnemyBudget(List<DungeonRoomInstance> rooms)
    {
        int total = 0;

        foreach (DungeonRoomInstance room in rooms)
            total += room.Template.MaxEnemyBudget;

        return total;
    }

    private void DistributeBudget(
        List<DungeonRoomInstance> rooms,
        int totalBudget,
        Dictionary<DungeonRoomInstance, int> budgets,
        int minEnemyCost)
    {
        if (rooms == null || rooms.Count == 0 || totalBudget <= 0) return;

        List<DungeonRoomInstance> remainingRooms = new(rooms);
        int remainingBudget = AllocateMinimumBudgets(remainingRooms, totalBudget, budgets, minEnemyCost);

        while (remainingRooms.Count > 0 && remainingBudget > 0)
        {
            int totalWeight = 0;

            foreach (DungeonRoomInstance room in remainingRooms)
                totalWeight += room.Template.EnemyBudgetWeight;

            if (totalWeight <= 0) break;

            bool allocatedAny = false;
            int passBudget = remainingBudget;

            for (int i = remainingRooms.Count - 1; i >= 0 && remainingBudget > 0; i--)
            {
                DungeonRoomInstance room = remainingRooms[i];
                budgets.TryGetValue(room, out int currentBudget);

                int remainingCapacity = room.Template.MaxEnemyBudget - currentBudget;
                if (remainingCapacity <= 0)
                {
                    remainingRooms.RemoveAt(i);
                    continue;
                }

                int share = Mathf.FloorToInt((float)passBudget * room.Template.EnemyBudgetWeight / totalWeight);
                share = Mathf.Max(1, share);

                int allocation = Mathf.Min(share, remainingCapacity, remainingBudget);
                budgets[room] = currentBudget + allocation;
                remainingBudget -= allocation;
                allocatedAny = true;

                if (budgets[room] >= room.Template.MaxEnemyBudget)
                    remainingRooms.RemoveAt(i);
            }

            if (!allocatedAny)
                break;
        }
    }

    private int AllocateMinimumBudgets(
        List<DungeonRoomInstance> rooms,
        int totalBudget,
        Dictionary<DungeonRoomInstance, int> budgets,
        int minEnemyCost)
    {
        if (minEnemyCost <= 0) return totalBudget;

        int remainingBudget = totalBudget;

        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            DungeonRoomInstance room = rooms[i];
            int minCount = Mathf.Min(room.Template.MinEnemyCount, room.Template.EnemySpawns.Length);

            if (minCount <= 0)
                continue;

            int minimumBudget = Mathf.Min(minEnemyCost * minCount, room.Template.MaxEnemyBudget);

            if (remainingBudget < minimumBudget)
                continue;

            budgets[room] = minimumBudget;
            remainingBudget -= minimumBudget;

            if (minimumBudget >= room.Template.MaxEnemyBudget)
                rooms.RemoveAt(i);
        }

        return remainingBudget;
    }

    private void LogEnemySpawnDebug(DungeonRoomInstance room, string message)
    {
        if (!debugEnemySpawn) return;

        string roomName = room?.Template != null ? room.Template.name : "UnknownRoom";
        string roomTypeName = room?.Template != null ? room.Template.RoomType.ToString() : "UnknownType";
        Vector2Int origin = room != null ? room.Origin : Vector2Int.zero;

        Debug.Log($"{name}: [{roomName}/{roomTypeName} at {origin}] {message}", gameObject);
    }

    private void LogDungeonRoomInstanceSummary(
        IReadOnlyList<DungeonRoomInstance> generatedRooms,
        Dictionary<DungeonRoomInstance, int> roomBudgets,
        Dictionary<DungeonRoomInstance, int> spawnedCounts)
    {
        if (!debugEnemySpawn) return;

        Debug.Log($"{name}: Generated room summary begin", gameObject);

        for (int i = 0; i < generatedRooms.Count; i++)
        {
            DungeonRoomInstance room = generatedRooms[i];
            RoomTemplate template = room.Template;
            int spawnCellCount = template.EnemySpawns != null ? template.EnemySpawns.Length : 0;
            roomBudgets.TryGetValue(room, out int budget);
            spawnedCounts.TryGetValue(room, out int spawned);

            Debug.Log(
                $"{name}: Room #{i:00} uses {template.name} | type={template.RoomType} | origin={room.Origin} | " +
                $"spawnCells={spawnCellCount} | weight={template.EnemyBudgetWeight} | maxBudget={template.MaxEnemyBudget} | " +
                $"minEnemies={template.MinEnemyCount} | allocatedBudget={budget} | spawned={spawned}",
                gameObject);
        }

        Debug.Log($"{name}: Generated room summary end", gameObject);
    }

    private void LoadTilemapPainter()
    {
        if (tilemapPainter != null) return;

        if (dungeonCtrl != null && dungeonCtrl.TilemapPainter != null)
        {
            tilemapPainter = dungeonCtrl.TilemapPainter;
            return;
        }

        tilemapPainter = GetComponentInChildren<DungeonTilemapPainter>(true);
    }

    private void LoadEnemySpawner()
    {
        if (enemySpawner != null) return;

        if (dungeonCtrl != null && dungeonCtrl.EnemySpawner != null)
        {
            enemySpawner = dungeonCtrl.EnemySpawner;
            return;
        }

        enemySpawner = GetComponentInChildren<EnemySpawner>(true);
        if (enemySpawner != null) return;

        enemySpawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
    }

    private Vector3Int ToVector3Int(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }
}
