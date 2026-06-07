using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonCtrl : BaseMonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;
    public DungeonGenerator DungeonGenerator => dungeonGenerator;

    [SerializeField] private DungeonStageManager stageManager;
    public DungeonStageManager StageManager => stageManager;

    [SerializeField] private DungeonLayoutBuilder layoutBuilder;
    public DungeonLayoutBuilder LayoutBuilder => layoutBuilder;

    [SerializeField] private DungeonTilemapPainter tilemapPainter;
    public DungeonTilemapPainter TilemapPainter => tilemapPainter;

    [SerializeField] private DungeonEncounterDirector encounterDirector;
    public DungeonEncounterDirector EncounterDirector => encounterDirector;

    [SerializeField] private EnemySpawner enemySpawner;
    public EnemySpawner EnemySpawner => enemySpawner;

    [SerializeField] private Tilemap floorTilemap;
    public Tilemap FloorTilemap => floorTilemap;

    [SerializeField] private Tilemap wallTilemap;
    public Tilemap WallTilemap => wallTilemap;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadDungeonGenerator();
        LoadStageManager();
        LoadLayoutBuilder();
        LoadTilemapPainter();
        LoadEncounterDirector();
        LoadEnemySpawner();
        LoadTilemaps();
    }

    private void LoadDungeonGenerator()
    {
        if (dungeonGenerator != null) return;
        dungeonGenerator = GetComponentInChildren<DungeonGenerator>(true);
    }

    private void LoadStageManager()
    {
        if (stageManager != null) return;
        stageManager = GetComponentInChildren<DungeonStageManager>(true);
    }

    private void LoadLayoutBuilder()
    {
        if (layoutBuilder != null) return;
        layoutBuilder = GetComponentInChildren<DungeonLayoutBuilder>(true);
    }

    private void LoadEnemySpawner()
    {
        if (enemySpawner != null) return;
        enemySpawner = GetComponentInChildren<EnemySpawner>(true);
    }

    private void LoadTilemapPainter()
    {
        if (tilemapPainter != null) return;
        tilemapPainter = GetComponentInChildren<DungeonTilemapPainter>(true);
    }

    private void LoadEncounterDirector()
    {
        if (encounterDirector != null) return;
        encounterDirector = GetComponentInChildren<DungeonEncounterDirector>(true);
    }

    private void LoadTilemaps()
    {
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
                wallTilemap = tilemap;
        }

        if (floorTilemap == null)
            floorTilemap = fallbackTilemap;
    }
}
