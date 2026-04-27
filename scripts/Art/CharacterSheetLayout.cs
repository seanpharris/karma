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
    public const float StandardWalkAnimationSpeed = 8f;
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
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownAnimation, StandardWalkAnimationSpeed, Row(origin, 0, 1, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpAnimation, StandardWalkAnimationSpeed, Row(origin, 0, 2, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkLeftAnimation, StandardWalkAnimationSpeed, Row(origin, 0, 3, size, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkRightAnimation, StandardWalkAnimationSpeed, Row(origin, 0, 4, size, walkFramesPerDirection))
        };
    }

    public static IReadOnlyList<PrototypeSpriteAnimation> EightDirectionTemplate(
        Vector2 origin,
        int frameSize = StandardFrameSize,
        int walkFramesPerDirection = StandardWalkFrameCount)
    {
        return EightDirectionRows(origin, frameSize, StandardIdleRow, StandardWalkStartRow, walkFramesPerDirection);
    }

    public static IReadOnlyList<PrototypeSpriteAnimation> EightDirectionFourRowWalkTemplate(
        Vector2 origin,
        int frameSize = 64)
    {
        return EightDirectionRows(origin, frameSize, idleRow: 0, walkStartRow: 1, walkFramesPerDirection: 3);
    }

    public static IReadOnlyList<PrototypeSpriteAnimation> EightDirectionFourRowPreviewTemplate(
        Vector2 origin,
        int frameSize = 64)
    {
        // The preview sheet rows are idle plus three movement rows. Front-facing
        // rows still avoid the fourth row from the original full-sheet prompt, but
        // right/back/back-diagonal rows are patched with strict no-tool walk strips
        // so movement has visible stepping and no random wrench/tool pop.
        var size = new Vector2(frameSize, frameSize);
        return new[]
        {
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Front, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.FrontRight, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Right, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.BackRight, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Back, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.BackLeft, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Left, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.FrontLeft, 0, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Front, size, startRow: 1, count: 2)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.FrontRight, size, startRow: 1, count: 2)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Right, size, startRow: 1, count: 3)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.BackRight, size, startRow: 1, count: 3)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Back, size, startRow: 1, count: 3)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.BackLeft, size, startRow: 1, count: 3)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Left, size, startRow: 1, count: 3)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.FrontLeft, size, startRow: 1, count: 2))
        };
    }

    private static IReadOnlyList<PrototypeSpriteAnimation> EightDirectionRows(
        Vector2 origin,
        int frameSize,
        int idleRow,
        int walkStartRow,
        int walkFramesPerDirection)
    {
        var size = new Vector2(frameSize, frameSize);
        return new[]
        {
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Front, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.FrontRight, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Right, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpRightAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.BackRight, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Back, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleUpLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.BackLeft, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.Left, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.IdleDownLeftAnimation, 1f, new[] { DirectionFrame(origin, CharacterSheetDirection.FrontLeft, idleRow, size) }),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Front, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.FrontRight, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Right, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpRightAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.BackRight, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Back, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkUpLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.BackLeft, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.Left, size, walkStartRow, walkFramesPerDirection)),
            new PrototypeSpriteAnimation(PrototypeCharacterSprite.WalkDownLeftAnimation, StandardWalkAnimationSpeed, SmoothDirectionWalk(origin, CharacterSheetDirection.FrontLeft, size, walkStartRow, walkFramesPerDirection))
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

    private static IReadOnlyList<Rect2> SmoothDirectionWalk(
        Vector2 origin,
        CharacterSheetDirection direction,
        Vector2 size,
        int startRow,
        int count)
    {
        var sourceFrames = DirectionColumn(origin, direction, startRow, size, count);
        if (sourceFrames.Count < 4)
        {
            return sourceFrames;
        }

        return new[]
        {
            sourceFrames[0],
            sourceFrames[1],
            DirectionFrame(origin, direction, Mathf.Max(0, startRow - 1), size),
            sourceFrames[2],
            sourceFrames[3],
            sourceFrames[2],
            sourceFrames[1]
        };
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
