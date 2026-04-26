using System.Collections.Generic;
using Godot;
using Karma.Data;

namespace Karma.Art;

public enum PrototypeSpriteKind
{
    Player,
    Mara,
    Peer,
    WhoopieCushion,
    DeflatedBalloon,
    RepairKit,
    PracticeStick,
    WorkVest
}

public enum PrototypeSpriteShape
{
    Rect,
    Circle,
    Line
}

public sealed record PrototypeSpriteLayer(
    PrototypeSpriteShape Shape,
    Color Color,
    Rect2 Rect,
    Vector2 From,
    Vector2 To,
    float Radius,
    float Width);

public sealed record PrototypeSpriteDefinition(
    PrototypeSpriteKind Kind,
    string DisplayName,
    Vector2 Size,
    IReadOnlyList<PrototypeSpriteLayer> Layers);

public static class PrototypeSpriteCatalog
{
    public static PrototypeSpriteKind GetKindForItem(string itemId)
    {
        return itemId switch
        {
            StarterItems.WhoopieCushionId => PrototypeSpriteKind.WhoopieCushion,
            StarterItems.DeflatedBalloonId => PrototypeSpriteKind.DeflatedBalloon,
            StarterItems.RepairKitId => PrototypeSpriteKind.RepairKit,
            StarterItems.PracticeStickId => PrototypeSpriteKind.PracticeStick,
            StarterItems.WorkVestId => PrototypeSpriteKind.WorkVest,
            _ => PrototypeSpriteKind.WhoopieCushion
        };
    }

    public static PrototypeSpriteDefinition Get(PrototypeSpriteKind kind)
    {
        return kind switch
        {
            PrototypeSpriteKind.Player => Humanoid(
                kind,
                "Player",
                new Color(0.22f, 0.76f, 0.94f),
                new Color(0.08f, 0.19f, 0.24f),
                new Color(0.96f, 0.94f, 0.72f)),
            PrototypeSpriteKind.Mara => Humanoid(
                kind,
                "Mara Venn",
                new Color(0.94f, 0.69f, 0.28f),
                new Color(0.21f, 0.14f, 0.11f),
                new Color(0.58f, 0.93f, 0.76f)),
            PrototypeSpriteKind.Peer => Humanoid(
                kind,
                "Stranded Player",
                new Color(0.65f, 0.45f, 0.94f),
                new Color(0.18f, 0.14f, 0.28f),
                new Color(0.94f, 0.82f, 0.51f)),
            PrototypeSpriteKind.WhoopieCushion => WhoopieCushion(),
            PrototypeSpriteKind.DeflatedBalloon => DeflatedBalloon(),
            PrototypeSpriteKind.RepairKit => RepairKit(),
            PrototypeSpriteKind.PracticeStick => PracticeStick(),
            PrototypeSpriteKind.WorkVest => WorkVest(),
            _ => Humanoid(
                PrototypeSpriteKind.Player,
                "Unknown",
                Colors.White,
                Colors.Black,
                Colors.White)
        };
    }

    private static PrototypeSpriteDefinition Humanoid(
        PrototypeSpriteKind kind,
        string displayName,
        Color body,
        Color outline,
        Color accent)
    {
        return new PrototypeSpriteDefinition(
            kind,
            displayName,
            new Vector2(22f, 34f),
            new[]
            {
                Rect(outline, -8f, -13f, 16f, 24f),
                Rect(body, -7f, -12f, 14f, 22f),
                Circle(outline, 0f, -18f, 7f),
                Circle(accent, 0f, -18f, 5f),
                Rect(outline, -5f, 11f, 4f, 7f),
                Rect(outline, 1f, 11f, 4f, 7f),
                Rect(accent, -5f, 0f, 10f, 3f),
                Rect(outline, -3f, -20f, 2f, 2f),
                Rect(outline, 2f, -20f, 2f, 2f)
            });
    }

    private static PrototypeSpriteDefinition WhoopieCushion()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.WhoopieCushion,
            "Whoopie Cushion",
            new Vector2(20f, 16f),
            new[]
            {
                Circle(new Color(0.33f, 0.04f, 0.08f), 0f, 1f, 9f),
                Circle(new Color(0.94f, 0.13f, 0.22f), 0f, 0f, 7f),
                Rect(new Color(0.78f, 0.08f, 0.16f), 5f, -2f, 8f, 4f),
                Rect(new Color(1f, 0.48f, 0.56f), -3f, -5f, 4f, 2f)
            });
    }

    private static PrototypeSpriteDefinition DeflatedBalloon()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.DeflatedBalloon,
            "Deflated Balloon",
            new Vector2(24f, 12f),
            new[]
            {
                Rect(new Color(0.28f, 0.06f, 0.22f), -11f, -3f, 19f, 8f),
                Rect(new Color(0.93f, 0.62f, 0.9f), -10f, -4f, 18f, 7f),
                Line(new Color(0.36f, 0.2f, 0.32f), new Vector2(6f, 1f), new Vector2(12f, 4f), 2f),
                Rect(new Color(1f, 0.85f, 0.98f), -8f, -3f, 5f, 2f)
            });
    }

    private static PrototypeSpriteDefinition RepairKit()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.RepairKit,
            "Repair Kit",
            new Vector2(22f, 18f),
            new[]
            {
                Rect(new Color(0.04f, 0.2f, 0.18f), -10f, -7f, 20f, 14f),
                Rect(new Color(0.1f, 0.72f, 0.6f), -8f, -5f, 16f, 10f),
                Rect(new Color(0.86f, 0.96f, 0.9f), -2f, -6f, 4f, 12f),
                Rect(new Color(0.86f, 0.96f, 0.9f), -6f, -2f, 12f, 4f)
            });
    }

    private static PrototypeSpriteDefinition PracticeStick()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.PracticeStick,
            "Practice Stick",
            new Vector2(28f, 10f),
            new[]
            {
                Line(new Color(0.22f, 0.12f, 0.05f), new Vector2(-13f, 3f), new Vector2(13f, -3f), 5f),
                Line(new Color(0.58f, 0.35f, 0.15f), new Vector2(-12f, 2f), new Vector2(12f, -4f), 3f),
                Rect(new Color(0.82f, 0.66f, 0.34f), -3f, -1f, 5f, 3f)
            });
    }

    private static PrototypeSpriteDefinition WorkVest()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.WorkVest,
            "Work Vest",
            new Vector2(22f, 26f),
            new[]
            {
                Rect(new Color(0.27f, 0.12f, 0.03f), -9f, -11f, 18f, 22f),
                Rect(new Color(0.94f, 0.52f, 0.16f), -7f, -10f, 14f, 20f),
                Rect(new Color(0.18f, 0.12f, 0.08f), -1f, -10f, 2f, 20f),
                Rect(new Color(0.98f, 0.9f, 0.34f), -6f, -4f, 4f, 2f),
                Rect(new Color(0.98f, 0.9f, 0.34f), 2f, -4f, 4f, 2f)
            });
    }

    private static PrototypeSpriteLayer Rect(Color color, float x, float y, float width, float height)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Rect,
            color,
            new Rect2(x, y, width, height),
            Vector2.Zero,
            Vector2.Zero,
            0f,
            1f);
    }

    private static PrototypeSpriteLayer Circle(Color color, float x, float y, float radius)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Circle,
            color,
            new Rect2(),
            new Vector2(x, y),
            Vector2.Zero,
            radius,
            1f);
    }

    private static PrototypeSpriteLayer Line(Color color, Vector2 from, Vector2 to, float width)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Line,
            color,
            new Rect2(),
            from,
            to,
            0f,
            width);
    }
}
