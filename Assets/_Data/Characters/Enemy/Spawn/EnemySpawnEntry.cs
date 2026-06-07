using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    [SerializeField] private PoolObj prefab;
    [SerializeField] private int cost = 1;
    [SerializeField] private int weight = 1;
    [SerializeField] private int minDifficultyLevel = 1;
    [SerializeField] private int maxDifficultyLevel = 0;

    public PoolObj Prefab => prefab;
    public int Cost => Mathf.Max(1, cost);
    public int Weight => Mathf.Max(1, weight);

    public EnemySpawnEntry() { }

    public EnemySpawnEntry(PoolObj prefab)
    {
        this.prefab = prefab;
    }

    public bool IsAllowedAtDifficulty(int level)
    {
        if (level < Mathf.Max(1, minDifficultyLevel))
            return false;

        return maxDifficultyLevel <= 0 || level <= maxDifficultyLevel;
    }
}
