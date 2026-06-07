using System.Collections.Generic;
using UnityEngine;

public class DungeonLayout
{
    public DungeonRoomInstance StartRoom { get; }
    public IReadOnlyList<DungeonRoomInstance> Rooms => rooms;
    public IReadOnlyList<DungeonCorridor> Corridors => corridors;

    private readonly List<DungeonRoomInstance> rooms = new();
    private readonly List<DungeonCorridor> corridors = new();

    public DungeonLayout(DungeonRoomInstance startRoom)
    {
        StartRoom = startRoom;
        rooms.Add(startRoom);
    }

    public void AddRoom(DungeonRoomInstance room)
    {
        rooms.Add(room);
    }

    public void AddCorridor(Vector2Int startCell, Vector2Int endCell, DoorDirection direction)
    {
        corridors.Add(new DungeonCorridor(startCell, endCell, direction));
    }
}

public readonly struct DungeonCorridor
{
    public Vector2Int StartCell { get; }
    public Vector2Int EndCell { get; }
    public DoorDirection Direction { get; }

    public DungeonCorridor(Vector2Int startCell, Vector2Int endCell, DoorDirection direction)
    {
        StartCell = startCell;
        EndCell = endCell;
        Direction = direction;
    }
}

public class DungeonLayoutBuilder : DungeonAbstract
{
    [SerializeField] private RoomTemplate startRoomTemplate;
    [SerializeField] private RoomTemplate bossRoomTemplate;
    [SerializeField] private List<RoomTemplate> middleRoomTemplates = new();
    [SerializeField] private int corridorLength = 8;
    [SerializeField] private int attachRetryCount = 20;

    private readonly List<DungeonRoomInstance> generatedRooms = new();

    public void Configure(
        RoomTemplate startRoom,
        RoomTemplate bossRoom,
        List<RoomTemplate> middleRooms,
        int corridorLengthValue,
        int attachRetryCountValue)
    {
        if (startRoomTemplate == null)
            startRoomTemplate = startRoom;

        if (bossRoomTemplate == null)
            bossRoomTemplate = bossRoom;

        if ((middleRoomTemplates == null || middleRoomTemplates.Count == 0) && middleRooms != null)
            middleRoomTemplates = new List<RoomTemplate>(middleRooms);

        corridorLength = Mathf.Max(1, corridorLengthValue);
        attachRetryCount = Mathf.Max(1, attachRetryCountValue);
    }

    public DungeonLayout Build(int middleRoomCount)
    {
        generatedRooms.Clear();

        DungeonRoomInstance currentRoom = CreateRoom(startRoomTemplate, Vector2Int.zero);
        DungeonLayout layout = new(currentRoom);

        int roomCount = Mathf.Max(0, middleRoomCount);
        for (int i = 0; i < roomCount; i++)
        {
            if (!TryAttachRandomMiddleRoom(currentRoom, layout, out DungeonRoomInstance nextRoom))
                break;

            currentRoom = nextRoom;
        }

        if (bossRoomTemplate != null && !TryAttachBossRoom(layout))
            Debug.LogWarning(name + ": Boss room failed to attach.", gameObject);

        return layout;
    }

    private bool TryAttachRandomMiddleRoom(
        DungeonRoomInstance currentRoom,
        DungeonLayout layout,
        out DungeonRoomInstance nextRoom)
    {
        nextRoom = null;
        List<RoomTemplate> candidates = GetCombatRoomCandidates();

        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            RoomTemplate template = candidates[index];
            candidates.RemoveAt(index);

            if (TryAttachRoom(currentRoom, template, layout, out nextRoom))
                return true;
        }

        return false;
    }

    private List<RoomTemplate> GetCombatRoomCandidates()
    {
        List<RoomTemplate> candidates = new();

        foreach (RoomTemplate template in middleRoomTemplates)
        {
            if (template == null) continue;

            if (template.EnemySpawns == null ||
                template.EnemySpawns.Length == 0 ||
                template.EnemyBudgetWeight <= 0 ||
                template.MaxEnemyBudget <= 0)
            {
                continue;
            }

            candidates.Add(template);
        }

        if (candidates.Count == 0)
            candidates.AddRange(middleRoomTemplates);

        return candidates;
    }

    private bool TryAttachBossRoom(DungeonLayout layout)
    {
        List<DungeonRoomInstance> candidates = new(generatedRooms);

        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            DungeonRoomInstance room = candidates[index];
            candidates.RemoveAt(index);

            if (!room.HasAvailableDoor())
                continue;

            if (TryAttachRoom(room, bossRoomTemplate, layout, out _))
                return true;
        }

        return false;
    }

    private bool TryAttachRoom(
        DungeonRoomInstance currentRoom,
        RoomTemplate nextTemplate,
        DungeonLayout layout,
        out DungeonRoomInstance nextRoom)
    {
        nextRoom = null;
        if (currentRoom == null || nextTemplate == null) return false;

        List<DungeonDoorInstance> exitDoors = currentRoom.GetAvailableDoors();
        int attempts = 0;

        while (exitDoors.Count > 0 && attempts < attachRetryCount)
        {
            attempts++;

            int exitIndex = Random.Range(0, exitDoors.Count);
            DungeonDoorInstance exitDoor = exitDoors[exitIndex];
            exitDoors.RemoveAt(exitIndex);

            DoorDirection entranceDirection = exitDoor.Direction.GetOpposite();

            if (!nextTemplate.TryGetDoor(entranceDirection, out RoomDoorTemplate entranceDoor))
                continue;

            Vector2Int direction = GetDirectionOffset(exitDoor.Direction);
            Vector2Int entranceCell = exitDoor.WorldCell + direction * Mathf.Max(1, corridorLength + 1);
            Vector2Int roomOrigin = entranceCell - entranceDoor.Position;
            RectInt roomBounds = GetRoomBounds(nextTemplate, roomOrigin);

            if (OverlapsExistingRooms(roomBounds))
                continue;

            nextRoom = CreateRoom(nextTemplate, roomOrigin);
            layout.AddRoom(nextRoom);
            layout.AddCorridor(exitDoor.WorldCell, entranceCell, exitDoor.Direction);

            exitDoor.SetConnected();
            nextRoom.SetDoorConnected(entranceDirection);
            return true;
        }

        return false;
    }

    private DungeonRoomInstance CreateRoom(RoomTemplate template, Vector2Int origin)
    {
        DungeonRoomInstance room = new(template, origin);
        generatedRooms.Add(room);
        return room;
    }

    private bool OverlapsExistingRooms(RectInt candidateBounds)
    {
        foreach (DungeonRoomInstance room in generatedRooms)
        {
            if (BoundsOverlap(candidateBounds, room.Bounds))
                return true;
        }

        return false;
    }

    private bool BoundsOverlap(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax &&
               a.xMax > b.xMin &&
               a.yMin < b.yMax &&
               a.yMax > b.yMin;
    }

    private RectInt GetRoomBounds(RoomTemplate template, Vector2Int origin)
    {
        Vector2Int min = origin + template.MinCell;
        return new RectInt(min, template.Size);
    }

    private Vector2Int GetDirectionOffset(DoorDirection direction)
    {
        return direction switch
        {
            DoorDirection.North => Vector2Int.up,
            DoorDirection.East => Vector2Int.right,
            DoorDirection.South => Vector2Int.down,
            DoorDirection.West => Vector2Int.left,
            _ => Vector2Int.down
        };
    }
}
