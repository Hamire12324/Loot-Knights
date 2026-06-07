using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonTilemapPainter : DungeonAbstract
{
    [SerializeField] private Tilemap floorTilemap;
    public Tilemap FloorTilemap => floorTilemap;

    [SerializeField] private Tilemap wallTilemap;
    public Tilemap WallTilemap => wallTilemap;

    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    private readonly HashSet<Vector3Int> floorCells = new();
    private readonly HashSet<Vector3Int> wallCells = new();

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadTilemaps();
    }

    public void Configure(Tilemap floor, Tilemap wall, TileBase floorTileAsset, TileBase wallTileAsset)
    {
        if (floorTilemap == null)
            floorTilemap = floor;

        if (wallTilemap == null)
            wallTilemap = wall;

        if (floorTile == null)
            floorTile = floorTileAsset;

        if (wallTile == null)
            wallTile = wallTileAsset;
    }

    public bool CanPaintFloor()
    {
        return floorTilemap != null && floorTile != null;
    }

    public bool CanPaintWalls()
    {
        return wallTilemap != null && wallTile != null;
    }

    public bool HasFloorCell(Vector3Int cell)
    {
        return floorCells.Contains(cell);
    }

    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        return floorTilemap != null ? floorTilemap.GetCellCenterWorld(cell) : Vector3.zero;
    }

    public int GetWallLayerMask()
    {
        return wallTilemap != null ? 1 << wallTilemap.gameObject.layer : 0;
    }

    public void Clear()
    {
        if (floorTilemap != null)
        {
            foreach (Vector3Int cell in floorCells)
                floorTilemap.SetTile(cell, null);
        }

        if (wallTilemap != null)
        {
            foreach (Vector3Int cell in wallCells)
                wallTilemap.SetTile(cell, null);
        }

        floorCells.Clear();
        wallCells.Clear();
    }

    public void PaintRoom(DungeonRoomInstance room)
    {
        RectInt bounds = room.Bounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int worldCell = new(x, y);
                Vector2Int localCell = worldCell - room.Origin;

                if (CanPaintWalls() && IsRoomEdge(bounds, worldCell) && !room.Template.HasDoorAt(localCell))
                    PaintWall(ToVector3Int(worldCell));
                else
                    PaintFloor(ToVector3Int(worldCell));
            }
        }
    }

    public void PaintCorridor(Vector2Int startCell, Vector2Int endCell, DoorDirection direction, int corridorWidth)
    {
        List<Vector2Int> path = GetCellPath(startCell, endCell);
        if (path.Count == 0) return;

        Vector2Int normal = GetNormal(direction);
        int width = Mathf.Max(1, corridorWidth);
        int left = -(width / 2);
        int right = width % 2 == 0 ? (width / 2) - 1 : width / 2;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int center = path[i];

            for (int offset = left; offset <= right; offset++)
                PaintFloor(ToVector3Int(center + normal * offset));

            if (i == 0 || i == path.Count - 1)
                continue;

            PaintWall(ToVector3Int(center + normal * (left - 1)));
            PaintWall(ToVector3Int(center + normal * (right + 1)));
        }
    }

    public void SealUnconnectedDoors(IEnumerable<DungeonRoomInstance> rooms)
    {
        foreach (DungeonRoomInstance room in rooms)
        {
            foreach (DungeonDoorInstance door in room.Doors)
            {
                if (door.IsConnected) continue;
                SealDoor(door.WorldCell);
            }
        }
    }

    public void PaintFloor(Vector3Int cell)
    {
        if (!CanPaintFloor()) return;

        floorTilemap.SetTile(cell, floorTile);
        floorCells.Add(cell);

        if (wallTilemap != null && wallCells.Remove(cell))
            wallTilemap.SetTile(cell, null);
    }

    public void PaintWall(Vector3Int cell)
    {
        if (!CanPaintWalls()) return;
        if (floorCells.Contains(cell)) return;

        wallTilemap.SetTile(cell, wallTile);
        wallTilemap.SetColliderType(cell, Tile.ColliderType.Grid);
        wallCells.Add(cell);
    }

    private void SealDoor(Vector2Int worldCell)
    {
        Vector3Int cell = ToVector3Int(worldCell);

        if (floorCells.Remove(cell) && floorTilemap != null)
            floorTilemap.SetTile(cell, null);

        PaintWall(cell);
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
                wallTilemap = tilemap;
        }

        if (floorTilemap == null)
            floorTilemap = fallbackTilemap;
    }

    private bool IsRoomEdge(RectInt bounds, Vector2Int cell)
    {
        return cell.x == bounds.xMin ||
               cell.x == bounds.xMax - 1 ||
               cell.y == bounds.yMin ||
               cell.y == bounds.yMax - 1;
    }

    private List<Vector2Int> GetCellPath(Vector2Int startCell, Vector2Int endCell)
    {
        List<Vector2Int> path = new() { startCell };
        Vector2Int current = startCell;

        while (current != endCell)
        {
            if (current.x != endCell.x)
                current.x += current.x < endCell.x ? 1 : -1;
            else if (current.y != endCell.y)
                current.y += current.y < endCell.y ? 1 : -1;

            path.Add(current);
        }

        return path;
    }

    private Vector2Int GetNormal(DoorDirection direction)
    {
        return direction == DoorDirection.East || direction == DoorDirection.West
            ? Vector2Int.up
            : Vector2Int.right;
    }

    private Vector3Int ToVector3Int(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }
}
