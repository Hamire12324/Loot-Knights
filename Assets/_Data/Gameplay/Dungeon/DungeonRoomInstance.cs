using System.Collections.Generic;
using UnityEngine;

public class DungeonRoomInstance
{
    public RoomTemplate Template { get; }
    public Vector2Int Origin { get; }
    public RectInt Bounds { get; }

    private readonly List<DungeonDoorInstance> doors = new();
    public IEnumerable<DungeonDoorInstance> Doors => doors;

    public DungeonRoomInstance(RoomTemplate template, Vector2Int origin)
    {
        Template = template;
        Origin = origin;
        Bounds = new RectInt(origin + template.MinCell, template.Size);

        if (template.Doors == null) return;

        foreach (RoomDoorTemplate door in template.Doors)
        {
            if (door == null) continue;
            doors.Add(new DungeonDoorInstance(door.Direction, origin + door.Position));
        }
    }

    public List<DungeonDoorInstance> GetAvailableDoors()
    {
        List<DungeonDoorInstance> availableDoors = new();

        foreach (DungeonDoorInstance door in doors)
        {
            if (door.IsConnected) continue;
            availableDoors.Add(door);
        }

        return availableDoors;
    }

    public bool HasAvailableDoor()
    {
        foreach (DungeonDoorInstance door in doors)
        {
            if (!door.IsConnected)
                return true;
        }

        return false;
    }

    public void SetDoorConnected(DoorDirection direction)
    {
        foreach (DungeonDoorInstance door in doors)
        {
            if (door.Direction != direction) continue;
            door.SetConnected();
            return;
        }
    }
}

public class DungeonDoorInstance
{
    public DoorDirection Direction { get; }
    public Vector2Int WorldCell { get; }
    public bool IsConnected { get; private set; }

    public DungeonDoorInstance(DoorDirection direction, Vector2Int worldCell)
    {
        Direction = direction;
        WorldCell = worldCell;
    }

    public void SetConnected()
    {
        IsConnected = true;
    }
}
