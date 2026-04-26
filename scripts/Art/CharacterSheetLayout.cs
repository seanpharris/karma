using System.Collections.Generic;
using Godot;

namespace Karma.Art;

public enum CharacterSheetDirection
{
    Front,
    FrontRight,
    Right,
    BackRight,
    Back,
    BackLeft,
    Left,
    FrontLeft
}

public static class CharacterSheetLayout
{
    public const int StandardFrameSize = 32;
    public const int StandardDirectionCount = 8;
    public const int StandardWalkFrameCount = 4;
    public const int StandardIdleRow = 0;
    public const int StandardWalkStartRow = 1;
    public const int StandardRunRow = 5;
    public const int StandardShootRow = 6;
    public const int StandardMeleeRow = 7;
    public const int StandardInteractRow = 8;

    public static IReadOnlyList<PrototypeSpriteAnimation> FourDirectionRows(
        Vector2 origin,
        int frameSize = StandardFrameSize,
        int walkFramesPerDirection = StandardWalkFrameCount)
    {
        var size = new Vector2(frameSize, frameSize);
        return new[]
        {
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownAnimation, 1f, new[] { RectAt(origin, 0, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpAnimation, 1f, new[] { RectAt(origin, 1, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleLeftAnimation, 1f, new[] { RectAt(origin, 2, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleRightAnimation, 1f, new[] { RectAt(origin, 3, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownAnimation, 6f, Row(origin, 0, 1, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpAnimation, 6f, Row(origin, 0, 2, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkLeftAnimation, 6f, Row(origin, 0, 3, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkRightAnimation, 6f, Row(origin, 0, 4, size, walkFramesPerDirection))
        };
    }

    public static IReadOnlyList<PrototypeSpriteAnimation> EightDirectionTemplate(
        Vector2 origin,
        int frameSize = StandardFrameSize,
        int walkFramesPerDirection = StandardWalkFrameCount)
    {
        var size = new Vector2(frameSize, frameSize);
        return new[]
        {
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Front, StandardIdleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Back, StandardIdleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Left, StandardIdleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Right, StandardIdleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownAnimation, 6f, DirectionColumn(origin, CharacterSheetDirection.Front, StandardWalkStartRow, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpAnimation, 6f, DirectionColumn(origin, CharacterSheetDirection.Back, StandardWalkStartRow, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkLeftAnimation, 6f, DirectionColumn(origin, CharacterSheetDirection.Left, StandardWalkStartRow, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkRightAnimation, 6f, DirectionColumn(origin, CharacterSheetDirection.Right, StandardWalkStartRow, size, walkFramesPerDirection))
        };
    }

    public static Rect2 DirectionFrame(
        Vector2 origin,
        CharacterSheetDirection direction,
        int row,
        Vector2 frameSize)
    {
        return RectAt(origin, (int)direction, row, frameSize);
    }

    public static Vector2 CalculateSheetSize(
        int frameSize = StandardFrameSize,
        int directionCount = StandardDirectionCount,
        int rowCount = StandardInteractRow + 1)
    {
        return new Vector2(directionCount * frameSize, rowCount * frameSize);
    }

    private static IReadOnlyList<Rect2> DirectionColumn(
        Vector2 origin,
        CharacterSheetDirection direction,
        int startRow,
        Vector2 size,
        int count)
    {
        var frames = new Rect2[Mathf.Max(1, count)];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = DirectionFrame(origin, direction, startRow + frame, size);
        }

        return frames;
    }

    private static IReadOnlyList<Rect2> Row(Vector2 origin, int startColumn, int row, Vector2 size, int count)
    {
        var frames = new Rect2[Mathf.Max(1, count)];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = RectAt(origin, startColumn + frame, row, size);
        }

        return frames;
    }

    private static Rect2 RectAt(Vector2 origin, int column, int row, Vector2 size)
    {
        return new Rect2(
            origin.X + (column * size.X),
            origin.Y + (row * size.Y),
            size.X,
            size.Y);
    }
}
