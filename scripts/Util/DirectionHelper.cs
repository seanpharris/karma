using Godot;

namespace Karma.Util;

public enum CardinalDirection
{
    Down = 0,
    Left = 1,
    Right = 2,
    Up = 3
}

public static class DirectionHelper
{
    public static Vector2I ToCardinalVector(Vector2 direction)
    {
        if (direction.LengthSquared() <= 0f)
        {
            return Vector2I.Zero;
        }

        return Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
            ? new Vector2I(direction.X > 0f ? 1 : -1, 0)
            : new Vector2I(0, direction.Y > 0f ? 1 : -1);
    }

    public static CardinalDirection ToCardinalDirection(Vector2 direction, CardinalDirection fallback = CardinalDirection.Down)
    {
        var vector = ToCardinalVector(direction);
        return vector == Vector2I.Zero
            ? fallback
            : ToCardinalDirection(vector, fallback);
    }

    public static CardinalDirection ToCardinalDirection(Vector2I direction, CardinalDirection fallback = CardinalDirection.Down)
    {
        return direction switch
        {
            { X: 0, Y: > 0 } => CardinalDirection.Down,
            { X: < 0, Y: 0 } => CardinalDirection.Left,
            { X: > 0, Y: 0 } => CardinalDirection.Right,
            { X: 0, Y: < 0 } => CardinalDirection.Up,
            _ => fallback
        };
    }

    public static Vector2I ToVector(CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.Left => Vector2I.Left,
            CardinalDirection.Right => Vector2I.Right,
            CardinalDirection.Up => Vector2I.Up,
            _ => Vector2I.Down
        };
    }

    public static string ToName(CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.Left => "left",
            CardinalDirection.Right => "right",
            CardinalDirection.Up => "up",
            _ => "down"
        };
    }

    public static int ToBit(CardinalDirection direction)
    {
        return 1 << (int)direction;
    }
}
