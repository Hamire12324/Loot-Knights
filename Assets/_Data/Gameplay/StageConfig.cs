using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Stage/Stage Config")]
public class StageConfig : ScriptableObject
{
    [SerializeField] private string stageName = "Stage 1";
    [SerializeField] private int stageNumber = 1;
    [SerializeField] private int difficultyLevel = 1;

    [Header("Stage Enemies")]
    [Tooltip("Enemy pool for this entire stage. Each spawn is picked randomly using Weight.")]
    [SerializeField] private List<StageEnemyEntry> enemyRoster = new();

    [Header("Stage Flow")]
    [Tooltip("Enemies spawned immediately when the stage starts.")]
    [SerializeField] private StageWaveConfig openingEnemies;
    [Tooltip("Every wave uses the Stage Enemies pool unless it has an override list.")]
    [SerializeField] private List<StageWaveConfig> waves = new();

    [Header("Rewards")]
    [SerializeField] private int coinReward = 100;
    [SerializeField] private int diamondReward = 0;
    [SerializeField] private int experienceReward = 100;

    [Header("Item Loot")]
    [SerializeField] private List<ItemDropEntry> itemDrops = new();

    public string StageName => stageName;
    public int StageNumber => Mathf.Max(1, stageNumber);
    public int DifficultyLevel => Mathf.Max(1, difficultyLevel);
    public IReadOnlyList<StageEnemyEntry> EnemyRoster => enemyRoster;
    public StageWaveConfig OpeningEnemies => openingEnemies;
    public IReadOnlyList<StageWaveConfig> Waves => waves;
    public bool HasOpeningEnemies => openingEnemies != null && openingEnemies.EnemyCount > 0;
    public int CoinReward => Mathf.Max(0, coinReward);
    public int DiamondReward => Mathf.Max(0, diamondReward);
    public int ExperienceReward => Mathf.Max(0, experienceReward);
    public IReadOnlyList<ItemDropEntry> ItemDrops => itemDrops;

    private void OnValidate()
    {
        stageNumber = Mathf.Max(1, stageNumber);
        difficultyLevel = Mathf.Max(1, difficultyLevel);
        coinReward = Mathf.Max(0, coinReward);
        diamondReward = Mathf.Max(0, diamondReward);
        experienceReward = Mathf.Max(0, experienceReward);
        enemyRoster ??= new List<StageEnemyEntry>();
        itemDrops ??= new List<ItemDropEntry>();
        waves ??= new List<StageWaveConfig>();
    }
}

[System.Serializable]
public class StageWaveConfig
{
    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float delayBeforeWave = 1f;
    [Tooltip("Marks every enemy in this wave as a boss. Boss waves are normally configured with Enemy Count = 1.")]
    [SerializeField] private bool isBossWave;
    [Tooltip("Leave empty to use Stage Enemies. Fill this only for a special wave, such as a boss wave.")]
    [SerializeField] private List<StageEnemyEntry> enemyOverrides = new();

    public int EnemyCount => Mathf.Max(1, enemyCount);
    public float DelayBeforeWave => Mathf.Max(0f, delayBeforeWave);
    public bool IsBossWave => isBossWave;
    public bool HasEnemyOverrides => enemyOverrides != null && enemyOverrides.Count > 0;
    public IReadOnlyList<StageEnemyEntry> EnemyOverrides => enemyOverrides;
}

[System.Serializable]
public class StageEnemyEntry
{
    [SerializeField] private PoolObj prefab;
    [SerializeField, Min(1)] private int weight = 1;
    [SerializeField, Min(1)] private int minDifficultyLevel = 1;
    [SerializeField, Min(0)] private int maxDifficultyLevel;

    public PoolObj Prefab => prefab;
    public int Weight => Mathf.Max(1, weight);

    public bool IsAllowedAtDifficulty(int level)
    {
        if (level < Mathf.Max(1, minDifficultyLevel))
            return false;

        return maxDifficultyLevel <= 0 || level <= maxDifficultyLevel;
    }
}
