using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : DungeonAbstract
{
    [Header("Controller")]
    [SerializeField] private DungeonLayoutBuilder layoutBuilder;
    [SerializeField] private DungeonTilemapPainter tilemapPainter;
    [SerializeField] private DungeonEncounterDirector encounterDirector;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    [Header("Rooms")]
    [SerializeField] private RoomTemplate startRoomTemplate;
    [SerializeField] private RoomTemplate bossRoomTemplate;
    [SerializeField] private List<RoomTemplate> middleRoomTemplates = new();

    [Header("Generation")]
    [SerializeField] private int middleRoomCount = 4;
    [SerializeField] private int corridorLength = 8;
    [SerializeField] private int corridorWidth = 4;
    [SerializeField] private int attachRetryCount = 20;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Runtime")]
    [SerializeField] private Transform player;

    [Header("Enemy Spawn")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Difficulty")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private DungeonStageConfig stageConfig;

    private readonly List<DungeonRoomInstance> generatedRooms = new();
    private DungeonStageConfig activeStage;

    protected override void Start()
    {
        base.Start();

        if (HasActiveStageManager())
            return;

        if (generateOnStart)
            Generate();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadLayoutBuilder();
        LoadTilemapPainter();
        LoadEncounterDirector();
        LoadTilemaps();
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        activeStage = stageConfig;
        GenerateInternal();
    }

    public void Generate(DungeonStageConfig stage)
    {
        activeStage = stage;
        ApplyStage(stage);
        GenerateInternal();
    }

    private void GenerateInternal()
    {
        if (!CanGenerate()) return;

        Random.State previousState = Random.state;

        if (useRandomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);

        Random.InitState(seed);
        PrepareEnemySpawnerForGenerate();
        ClearDungeon();

        DungeonLayout layout = layoutBuilder.Build(GetMiddleRoomCount());
        PaintLayout(layout);
        MovePlayerToStart(layout.StartRoom);

        SealUnconnectedDoors();
        SpawnEnemiesFromRoomTemplates();
        Random.state = previousState;
    }

    [ContextMenu("Clear Dungeon")]
    public void ClearDungeon()
    {
        generatedRooms.Clear();
        tilemapPainter?.Clear();
    }

    public void SetDifficultyLevel(int level)
    {
        difficultyLevel = Mathf.Max(1, level);
        activeStage = null;
        stageConfig = null;
    }

    public void IncreaseDifficulty(int amount = 1)
    {
        difficultyLevel = Mathf.Max(1, difficultyLevel + amount);
        activeStage = null;
        stageConfig = null;
    }

    [ContextMenu("Generate Next Difficulty")]
    public void GenerateNextDifficulty()
    {
        IncreaseDifficulty();
        Generate();
    }

    public void SetStage(DungeonStageConfig stage)
    {
        stageConfig = stage;
        activeStage = stage;
        ApplyStage(stage);
    }

    private void PaintLayout(DungeonLayout layout)
    {
        foreach (DungeonRoomInstance room in layout.Rooms)
        {
            generatedRooms.Add(room);
            tilemapPainter.PaintRoom(room);
        }

        foreach (DungeonCorridor corridor in layout.Corridors)
            tilemapPainter.PaintCorridor(corridor.StartCell, corridor.EndCell, corridor.Direction, corridorWidth);
    }

    private void SealUnconnectedDoors()
    {
        tilemapPainter.SealUnconnectedDoors(generatedRooms);
    }

    private void MovePlayerToStart(DungeonRoomInstance startRoom)
    {
        if (player == null)
        {
            HeroCtrl hero = HeroCtrl.GetLocal();
            if (hero != null) player = hero.transform;
        }

        if (player == null || startRoom == null) return;

        Vector2Int spawnCell = startRoom.Origin + startRoom.Template.PlayerSpawn;
        player.position = tilemapPainter.GetCellCenterWorld(ToVector3Int(spawnCell));
    }

    private bool CanGenerate()
    {
        if (layoutBuilder == null)
        {
            Debug.LogWarning(name + ": Missing Dungeon Layout Builder.", gameObject);
            return false;
        }

        if (tilemapPainter == null)
        {
            Debug.LogWarning(name + ": Missing Dungeon Tilemap Painter.", gameObject);
            return false;
        }

        if (!tilemapPainter.CanPaintFloor())
        {
            Debug.LogWarning(name + ": Missing Floor Tilemap.", gameObject);
            return false;
        }

        if (floorTile == null)
        {
            Debug.LogWarning(name + ": Missing Floor Tile.", gameObject);
            return false;
        }

        if (startRoomTemplate == null)
        {
            Debug.LogWarning(name + ": Missing Start Room Template.", gameObject);
            return false;
        }

        if (wallTilemap == null)
            Debug.LogWarning(name + ": Missing Wall Tilemap. Rooms and corridors will be drawn without walls.", gameObject);

        if (wallTile == null)
            Debug.LogWarning(name + ": Missing Wall Tile. Rooms and corridors will be drawn without walls.", gameObject);

        if (bossRoomTemplate == null)
            Debug.LogWarning(name + ": Missing Boss Room Template. Only start and middle rooms will be generated.", gameObject);

        return true;
    }

    private void ApplyStage(DungeonStageConfig stage)
    {
        if (stage == null) return;

        difficultyLevel = stage.DifficultyLevel;
        middleRoomCount = stage.MiddleRoomCount;
    }

    private int GetDifficultyLevel()
    {
        return activeStage != null ? activeStage.DifficultyLevel : Mathf.Max(1, difficultyLevel);
    }

    private int GetMiddleRoomCount()
    {
        return activeStage != null ? activeStage.MiddleRoomCount : Mathf.Max(0, middleRoomCount);
    }

    private void PrepareEnemySpawnerForGenerate()
    {
        encounterDirector?.PrepareForGenerate();
    }

    private void SpawnEnemiesFromRoomTemplates()
    {
        encounterDirector?.SpawnEnemies(generatedRooms, activeStage, GetDifficultyLevel());
    }

    private Vector3Int ToVector3Int(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }

    private void LoadTilemaps()
    {
        if (dungeonCtrl != null)
        {
            if (floorTilemap == null)
                floorTilemap = dungeonCtrl.FloorTilemap;

            if (wallTilemap == null)
                wallTilemap = dungeonCtrl.WallTilemap;
        }

        if (floorTilemap != null && wallTilemap != null) return;

        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);
        Tilemap fallbackTilemap = null;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (fallbackTilemap == null)
                fallbackTilemap = tilemap;

            string tilemapName = tilemap.name.ToLowerInvariant();

            if (floorTilemap == null &&
                (tilemapName.Contains("floor") ||
                 tilemapName.Contains("ground") ||
                 tilemapName.Contains("corridor") ||
                 tilemapName.Contains("path")))
            {
                floorTilemap = tilemap;
                continue;
            }

            if (wallTilemap == null && tilemapName.Contains("wall"))
            {
                wallTilemap = tilemap;
            }
        }

        if (floorTilemap == null)
            floorTilemap = fallbackTilemap;

        tilemapPainter?.Configure(floorTilemap, wallTilemap, floorTile, wallTile);
    }

    private void LoadTilemapPainter()
    {
        if (tilemapPainter != null)
        {
            tilemapPainter.Configure(floorTilemap, wallTilemap, floorTile, wallTile);
            return;
        }

        if (dungeonCtrl != null && dungeonCtrl.TilemapPainter != null)
            tilemapPainter = dungeonCtrl.TilemapPainter;

        if (tilemapPainter == null)
            tilemapPainter = GetComponentInChildren<DungeonTilemapPainter>(true);

        if (tilemapPainter != null)
            tilemapPainter.Configure(floorTilemap, wallTilemap, floorTile, wallTile);
    }

    private void LoadLayoutBuilder()
    {
        if (layoutBuilder != null)
        {
            layoutBuilder.Configure(startRoomTemplate, bossRoomTemplate, middleRoomTemplates, corridorLength, attachRetryCount);
            return;
        }

        if (dungeonCtrl != null && dungeonCtrl.LayoutBuilder != null)
            layoutBuilder = dungeonCtrl.LayoutBuilder;

        if (layoutBuilder == null)
            layoutBuilder = GetComponentInChildren<DungeonLayoutBuilder>(true);

        if (layoutBuilder != null)
            layoutBuilder.Configure(startRoomTemplate, bossRoomTemplate, middleRoomTemplates, corridorLength, attachRetryCount);
    }

    private void LoadEncounterDirector()
    {
        if (encounterDirector != null)
        {
            encounterDirector.Configure(dungeonCtrl, tilemapPainter, enemySpawner);
            return;
        }

        if (dungeonCtrl != null && dungeonCtrl.EncounterDirector != null)
            encounterDirector = dungeonCtrl.EncounterDirector;

        if (encounterDirector == null)
            encounterDirector = GetComponentInChildren<DungeonEncounterDirector>(true);

        if (encounterDirector != null)
            encounterDirector.Configure(dungeonCtrl, tilemapPainter, enemySpawner);
    }

    private bool HasActiveStageManager()
    {
        if (dungeonCtrl != null && dungeonCtrl.StageManager != null && dungeonCtrl.StageManager.enabled)
            return true;

        DungeonStageManager stageManager = GetComponent<DungeonStageManager>();
        if (stageManager != null && stageManager.enabled)
            return true;

        stageManager = GetComponentInParent<DungeonStageManager>();
        if (stageManager != null && stageManager.enabled)
            return true;

        stageManager = GetComponentInChildren<DungeonStageManager>(true);
        return stageManager != null && stageManager.enabled;
    }

}
