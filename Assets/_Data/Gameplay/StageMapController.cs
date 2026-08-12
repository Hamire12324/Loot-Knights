using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixed-map stage controller. This replaces procedural rooms and tilemap painting:
/// each stage now runs on the same hand-authored background, with optional invisible
/// world bounds and designer-placed spawn points.
/// </summary>
public class StageMapController : BaseMonoBehaviour
{
    [Header("Map artwork")]
    [Tooltip("Drag your full-map sprite onto the MapBackground child in the Hierarchy.")]
    [SerializeField] private SpriteRenderer mapBackground;
    [SerializeField] private int backgroundSortingOrder = -20;

    [Header("Playable area")]
    [Tooltip("Visible trigger marking the playable area. Movement is clamped by this controller.")]
    [SerializeField] private BoxCollider2D playableAreaCollider;
    [SerializeField, Min(0f)] private float characterEdgePadding = 0.35f;
    [SerializeField] private bool clampCameraToPlayableArea = true;
    [SerializeField] private bool clampCharactersToPlayableArea = true;

    [Header("Spawn points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Enemies")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Completion")]
    [Tooltip("Seconds to keep the battlefield visible after the final enemy is defeated before showing victory.")]
    [SerializeField, Min(0f)] private float victoryDelay = 2f;

    private readonly List<CharacterDamReceiver> livingWaveEnemies = new();
    private StageConfig activeStage;
    private int currentWaveIndex;
    private int remainingEnemiesInWave;
    private IReadOnlyList<StageEnemyEntry> currentWaveEnemyEntries;
    private bool currentWaveIsBossWave;
    private bool openingEncounterPending;
    private bool stageRunning;
    private Coroutine nextWaveCoroutine;
    private Coroutine completionCoroutine;

    public int CurrentWaveNumber { get; private set; }
    public int TotalWaveCount => activeStage == null
        ? 0
        : activeStage.Waves.Count + (activeStage.HasOpeningEnemies ? 1 : 0);

    public event System.Action<int, int> OnWaveChanged;

    protected override void Awake()
    {
        base.Awake();
        ConfigureMapPresentation();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Camera.onPreCull += ClampCameraToPlayableArea;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (clampCharactersToPlayableArea)
            ClampActiveCharacters();
    }

    protected override void OnDisable()
    {
        Camera.onPreCull -= ClampCameraToPlayableArea;
        StopActiveStage();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (mapBackground == null)
            mapBackground = GetComponentInChildren<SpriteRenderer>(true);

        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
    }

    [ContextMenu("Start Stage")]
    public void Generate()
    {
        Generate(activeStage);
    }

    /// <summary>Starts a fixed-map stage. Generate is called by StageManager.</summary>
    public void Generate(StageConfig stage)
    {
        StopActiveStage();
        activeStage = stage;
        stageRunning = true;

        ConfigureMapPresentation();
        MoveHeroToSpawn();

        if (enemySpawner == null)
        {
            Debug.LogWarning(name + ": EnemySpawner was not found; stage is completed immediately.", gameObject);
            CompleteStage();
            return;
        }

        enemySpawner.ReturnAllAliveEnemies();
        currentWaveIndex = 0;
        openingEncounterPending = stage != null && stage.HasOpeningEnemies;
        CurrentWaveNumber = 0;
        NotifyWaveChanged();

        SpawnNextWave();
    }

    private void ConfigureMapPresentation()
    {
        if (mapBackground != null)
            mapBackground.sortingOrder = backgroundSortingOrder;
    }

    private void ClampActiveCharacters()
    {
        if (playableAreaCollider == null) return;
        CharacterCtrl[] characters = FindObjectsByType<CharacterCtrl>(FindObjectsInactive.Exclude);

        foreach (CharacterCtrl character in characters)
        {
            if (character == null || !character.gameObject.activeInHierarchy)
                continue;

            Vector3 clampedPosition = ClampCharacterPosition(character.transform.position);
            if (clampedPosition == character.transform.position)
                continue;

            if (character.Rb != null)
                character.Rb.position = clampedPosition;

            character.transform.position = clampedPosition;
        }
    }

    private Vector3 ClampCharacterPosition(Vector3 position)
    {
        Bounds bounds = GetPlayableBounds(characterEdgePadding);
        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
        return position;
    }

    private void ClampCameraToPlayableArea(Camera camera)
    {
        if (!clampCameraToPlayableArea || playableAreaCollider == null || camera == null || camera != Camera.main || !camera.orthographic)
            return;

        Bounds bounds = GetPlayableBounds(0f);
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        Vector3 position = camera.transform.position;

        position.x = ClampCameraAxis(position.x, bounds.min.x, bounds.max.x, halfWidth);
        position.y = ClampCameraAxis(position.y, bounds.min.y, bounds.max.y, halfHeight);
        camera.transform.position = position;
    }

    private Bounds GetPlayableBounds(float padding)
    {
        Bounds areaBounds = playableAreaCollider.bounds;

        Vector2 usableSize = Vector2.Max(
            new Vector2(areaBounds.size.x, areaBounds.size.y) - Vector2.one * padding * 2f,
            Vector2.zero);

        return new Bounds(areaBounds.center, new Vector3(usableSize.x, usableSize.y, 0f));
    }

    private static float ClampCameraAxis(float value, float min, float max, float halfViewportSize)
    {
        float clampMin = min + halfViewportSize;
        float clampMax = max - halfViewportSize;
        return clampMin > clampMax ? (min + max) * 0.5f : Mathf.Clamp(value, clampMin, clampMax);
    }

    private void OnDrawGizmosSelected()
    {
        if (playableAreaCollider == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(playableAreaCollider.bounds.center, playableAreaCollider.bounds.size);
    }

    private void MoveHeroToSpawn()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null) return;

        Vector3 position = playerSpawnPoint != null ? playerSpawnPoint.position : transform.position;
        position.z = hero.transform.position.z;
        hero.transform.position = position;

        if (hero.Rb != null)
            hero.Rb.linearVelocity = Vector2.zero;
    }

    private void SpawnNextWave()
    {
        if (!stageRunning) return;

        UnsubscribeWaveEnemies();

        if (!HasNextWave())
        {
            CompleteStage();
            return;
        }

        List<Vector3> points = GetSpawnPositions();
        if (points.Count == 0)
        {
            Debug.LogWarning(name + ": Add at least one EnemySpawnPoint before starting this stage.", gameObject);
            CompleteStage();
            return;
        }

        remainingEnemiesInWave = GetCurrentWaveEnemyCount();
        currentWaveEnemyEntries = GetCurrentWaveEnemyEntries();
        currentWaveIsBossWave = IsCurrentWaveBossWave();
        if (activeStage != null)
        {
            if (openingEncounterPending)
                openingEncounterPending = false;
            else
                currentWaveIndex++;
        }
        else
            currentWaveIndex++;

        CurrentWaveNumber = activeStage != null && activeStage.HasOpeningEnemies
            ? currentWaveIndex + 1
            : currentWaveIndex;
        NotifyWaveChanged();

        SpawnCurrentWaveBatch(points);
    }

    private void SpawnCurrentWaveBatch(IReadOnlyList<Vector3> points)
    {
        if (!stageRunning || remainingEnemiesInWave <= 0)
        {
            QueueNextWaveOrComplete();
            return;
        }

        List<PoolObj> spawned = enemySpawner.SpawnCount(
            points,
            remainingEnemiesInWave,
            activeStage != null ? activeStage.DifficultyLevel : 1,
            currentWaveEnemyEntries,
            currentWaveIsBossWave);

        remainingEnemiesInWave -= spawned.Count;

        if (spawned.Count == 0)
        {
            Debug.LogWarning(
                name + ": Unable to spawn the remaining enemies for this wave. " +
                "Check the stage roster and EnemySpawner limits.",
                gameObject);
            remainingEnemiesInWave = 0;
            QueueNextWaveOrComplete();
            return;
        }

        foreach (PoolObj enemy in spawned)
        {
            CharacterDamReceiver receiver = enemy != null
                ? enemy.GetComponentInChildren<CharacterDamReceiver>()
                : null;

            if (receiver == null) continue;
            receiver.OnDeath -= HandleWaveEnemyDeath;
            receiver.OnDeath += HandleWaveEnemyDeath;
            livingWaveEnemies.Add(receiver);
        }

        if (livingWaveEnemies.Count == 0)
            HandleCurrentWaveBatchCleared();
    }

    private List<Vector3> GetSpawnPositions()
    {
        List<Vector3> positions = new();

        if (enemySpawnPoints != null)
        {
            foreach (Transform point in enemySpawnPoints)
            {
                if (point != null)
                    positions.Add(point.position);
            }
        }

        return positions;
    }

    private void HandleWaveEnemyDeath(CharacterDamReceiver receiver)
    {
        if (receiver != null)
            receiver.OnDeath -= HandleWaveEnemyDeath;

        livingWaveEnemies.Remove(receiver);

        if (stageRunning && livingWaveEnemies.Count == 0)
            HandleCurrentWaveBatchCleared();
    }

    private void HandleCurrentWaveBatchCleared()
    {
        if (!stageRunning) return;

        if (remainingEnemiesInWave > 0)
        {
            List<Vector3> points = GetSpawnPositions();
            if (points.Count == 0)
            {
                Debug.LogWarning(name + ": Add at least one EnemySpawnPoint before spawning reinforcements.", gameObject);
                CompleteStage();
                return;
            }

            SpawnCurrentWaveBatch(points);
            return;
        }

        QueueNextWaveOrComplete();
    }

    private void QueueNextWaveOrComplete()
    {
        if (!stageRunning) return;

        if (!HasNextWave())
        {
            CompleteStage();
            return;
        }

        if (nextWaveCoroutine == null)
            nextWaveCoroutine = StartCoroutine(SpawnWaveAfterDelay());
    }

    private IEnumerator SpawnWaveAfterDelay()
    {
        yield return new WaitForSeconds(GetNextWaveDelay());
        nextWaveCoroutine = null;
        SpawnNextWave();
    }

    private void CompleteStage()
    {
        if (!stageRunning) return;

        stageRunning = false;
        completionCoroutine = StartCoroutine(CompleteStageAfterDelay());
    }

    private IEnumerator CompleteStageAfterDelay()
    {
        float delay = Mathf.Max(0f, victoryDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        completionCoroutine = null;
        StageManager manager = FindAnyObjectByType<StageManager>(FindObjectsInactive.Include);
        manager?.CompleteStage();
    }

    private void StopActiveStage()
    {
        stageRunning = false;
        currentWaveIndex = 0;
        remainingEnemiesInWave = 0;
        currentWaveEnemyEntries = null;
        currentWaveIsBossWave = false;
        openingEncounterPending = false;
        CurrentWaveNumber = 0;
        NotifyWaveChanged();

        if (nextWaveCoroutine != null)
        {
            StopCoroutine(nextWaveCoroutine);
            nextWaveCoroutine = null;
        }

        if (completionCoroutine != null)
        {
            StopCoroutine(completionCoroutine);
            completionCoroutine = null;
        }

        UnsubscribeWaveEnemies();
    }

    private void UnsubscribeWaveEnemies()
    {
        foreach (CharacterDamReceiver receiver in livingWaveEnemies)
        {
            if (receiver != null)
                receiver.OnDeath -= HandleWaveEnemyDeath;
        }

        livingWaveEnemies.Clear();
    }

    private bool HasNextWave()
    {
        if (activeStage != null)
            return openingEncounterPending || currentWaveIndex < activeStage.Waves.Count;

        return false;
    }

    private int GetCurrentWaveEnemyCount()
    {
        if (activeStage != null)
            return openingEncounterPending
                ? activeStage.OpeningEnemies.EnemyCount
                : activeStage.Waves[currentWaveIndex].EnemyCount;

        return 0;
    }

    private float GetNextWaveDelay()
    {
        if (activeStage != null && !openingEncounterPending && currentWaveIndex < activeStage.Waves.Count)
            return activeStage.Waves[currentWaveIndex].DelayBeforeWave;

        return 0f;
    }

    private IReadOnlyList<StageEnemyEntry> GetCurrentWaveEnemyEntries()
    {
        StageWaveConfig wave = null;

        if (activeStage != null)
            wave = openingEncounterPending ? activeStage.OpeningEnemies : activeStage.Waves[currentWaveIndex];
        if (wave != null && wave.HasEnemyOverrides)
            return wave.EnemyOverrides;

        return activeStage != null ? activeStage.EnemyRoster : null;
    }

    private bool IsCurrentWaveBossWave()
    {
        if (activeStage == null)
            return false;

        StageWaveConfig wave = openingEncounterPending
            ? activeStage.OpeningEnemies
            : activeStage.Waves[currentWaveIndex];
        return wave != null && wave.IsBossWave;
    }

    private void NotifyWaveChanged()
    {
        OnWaveChanged?.Invoke(CurrentWaveNumber, TotalWaveCount);
    }
}
