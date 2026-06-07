public enum DoorDirection
{
    North,
    East,
    South,
    West
}

public static class DoorDirectionExtensions
{
    public static DoorDirection GetOpposite(this DoorDirection direction)
    {
        return direction switch
        {
            DoorDirection.North => DoorDirection.South,
            DoorDirection.East => DoorDirection.West,
            DoorDirection.South => DoorDirection.North,
            DoorDirection.West => DoorDirection.East,
            _ => DoorDirection.South
        };
    }
}
