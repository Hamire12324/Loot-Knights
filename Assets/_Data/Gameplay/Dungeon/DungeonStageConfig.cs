using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Dungeon/Stage Config")]
public class DungeonStageConfig : ScriptableObject
{
    [SerializeField] private string stageName = "Stage 1";
    [SerializeField] private int stageNumber = 1;
    [SerializeField] private int difficultyLevel = 1;

    [Header("Layout")]
    [SerializeField] private int middleRoomCount = 4;

    [Header("Enemy Budget")]
    [SerializeField] private int totalEnemyBudget = 40;
    [SerializeField] private int bossRoomBudget = 12;

    [Header("Rewards")]
    [SerializeField] private int coinReward = 100;
    [SerializeField] private int diamondReward = 0;
    [SerializeField] private int experienceReward = 100;

    [Header("Item Loot")]
    [SerializeField] private List<ItemDropEntry> itemDrops = new();

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new();

    public string StageName => stageName;
    public int StageNumber => Mathf.Max(1, stageNumber);
    public int DifficultyLevel => Mathf.Max(1, difficultyLevel);
    public int MiddleRoomCount => Mathf.Max(0, middleRoomCount);
    public int TotalEnemyBudget => Mathf.Max(0, totalEnemyBudget);
    public int BossRoomBudget => Mathf.Max(0, bossRoomBudget);
    public int CoinReward => Mathf.Max(0, coinReward);
    public int DiamondReward => Mathf.Max(0, diamondReward);
    public int ExperienceReward => Mathf.Max(0, experienceReward);
    public IReadOnlyList<ItemDropEntry> ItemDrops => itemDrops;
    public IReadOnlyList<EnemySpawnEntry> EnemyEntries => enemyEntries;

    private void OnValidate()
    {
        stageNumber = Mathf.Max(1, stageNumber);
        difficultyLevel = Mathf.Max(1, difficultyLevel);
        middleRoomCount = Mathf.Max(0, middleRoomCount);
        totalEnemyBudget = Mathf.Max(0, totalEnemyBudget);
        bossRoomBudget = Mathf.Max(0, bossRoomBudget);
        coinReward = Mathf.Max(0, coinReward);
        diamondReward = Mathf.Max(0, diamondReward);
        experienceReward = Mathf.Max(0, experienceReward);
        itemDrops ??= new List<ItemDropEntry>();
    }
}
