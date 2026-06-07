using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Dungeon/Room Template")]
public class RoomTemplate : ScriptableObject
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private Vector2Int size = new(12, 8);
    [SerializeField] private RoomDoorTemplate[] doors;
    [SerializeField] private Vector2Int playerSpawn;
    [SerializeField] private Vector2Int[] enemySpawns;
    [SerializeField] private Vector2Int[] lootSpawns;

    [Header("Enemy Budget Allocation")]
    [SerializeField] private int enemyBudgetWeight = 1;
    [SerializeField] private int maxEnemyBudget = 12;
    [SerializeField] private int minEnemyCount = 1;

    public RoomType RoomType => roomType;
    public Vector2Int Size => size;
    public RoomDoorTemplate[] Doors => doors;
    public Vector2Int PlayerSpawn => playerSpawn;
    public Vector2Int[] EnemySpawns => enemySpawns;
    public Vector2Int[] LootSpawns => lootSpawns;
    public int EnemyBudgetWeight => Mathf.Max(0, enemyBudgetWeight);
    public int MaxEnemyBudget => Mathf.Max(0, maxEnemyBudget);
    public int MinEnemyCount => Mathf.Max(0, minEnemyCount);

    public Vector2Int MinCell => new(-size.x / 2, -size.y / 2);
    public Vector2Int MaxCell => MinCell + size - Vector2Int.one;

    private void OnValidate()
    {
        size.x = Mathf.Max(3, size.x);
        size.y = Mathf.Max(3, size.y);
        enemyBudgetWeight = Mathf.Max(0, enemyBudgetWeight);
        maxEnemyBudget = Mathf.Max(0, maxEnemyBudget);
        minEnemyCount = Mathf.Max(0, minEnemyCount);
    }

    public bool TryGetDoor(DoorDirection direction, out RoomDoorTemplate door)
    {
        if (doors != null)
        {
            foreach (RoomDoorTemplate candidate in doors)
            {
                if (candidate == null) continue;
                if (candidate.Direction != direction) continue;

                door = candidate;
                return true;
            }
        }

        door = null;
        return false;
    }

    public bool HasDoorAt(Vector2Int position)
    {
        if (doors == null) return false;

        foreach (RoomDoorTemplate door in doors)
        {
            if (door == null) continue;
            if (door.Position == position) return true;
        }

        return false;
    }

    public bool ContainsLocalCell(Vector2Int position)
    {
        return position.x >= MinCell.x &&
               position.x <= MaxCell.x &&
               position.y >= MinCell.y &&
               position.y <= MaxCell.y;
    }

    public bool IsEdgeCell(Vector2Int position)
    {
        if (!ContainsLocalCell(position)) return false;

        return position.x == MinCell.x ||
               position.x == MaxCell.x ||
               position.y == MinCell.y ||
               position.y == MaxCell.y;
    }
}

[Serializable]
public class RoomDoorTemplate
{
    [SerializeField] private DoorDirection direction;
    public DoorDirection Direction => direction;
    [SerializeField] private Vector2Int position;
    public Vector2Int Position => position;
}
