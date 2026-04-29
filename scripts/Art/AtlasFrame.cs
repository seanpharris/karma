using Godot;

namespace Karma.Art;

public enum AtlasFrameAnchor
{
    Center,
    FootprintBottom,
    PropBottom
}

public readonly record struct AtlasFrame(
    string AtlasPath,
    Rect2 SourceRegion,
    Vector2 DisplaySize,
    AtlasFrameAnchor Anchor = AtlasFrameAnchor.Center)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(AtlasPath) &&
                           SourceRegion.Size.X > 0f &&
                           SourceRegion.Size.Y > 0f &&
                           DisplaySize.X > 0f &&
                           DisplaySize.Y > 0f;

    public AtlasTexture ToTexture(Texture2D texture)
    {
        return new AtlasTexture
        {
            Atlas = texture,
            Region = SourceRegion
        };
    }

    public Vector2 CalculateScale()
    {
        if (SourceRegion.Size.X <= 0f || SourceRegion.Size.Y <= 0f)
        {
            return Vector2.One;
        }

        return new Vector2(
            DisplaySize.X / SourceRegion.Size.X,
            DisplaySize.Y / SourceRegion.Size.Y);
    }

    public Vector2 CalculateOffset()
    {
        return Anchor switch
        {
            AtlasFrameAnchor.FootprintBottom => new Vector2(0f, -DisplaySize.Y * 0.5f),
            AtlasFrameAnchor.PropBottom => new Vector2(0f, -DisplaySize.Y * 0.3f),
            _ => Vector2.Zero
        };
    }
}

public static class AtlasFrames
{
    public static AtlasFrame FromPrototype(PrototypeSpriteDefinition definition)
    {
        return new AtlasFrame(
            definition.AtlasPath,
            definition.AtlasRegion,
            definition.Size,
            IsHumanoid(definition.Kind) ? AtlasFrameAnchor.FootprintBottom : AtlasFrameAnchor.PropBottom);
    }

    public static AtlasFrame FromStructure(StructureSpriteDefinition definition)
    {
        return new AtlasFrame(
            definition.AtlasPath,
            definition.AtlasRegion,
            definition.Size,
            AtlasFrameAnchor.FootprintBottom);
    }

    private static bool IsHumanoid(PrototypeSpriteKind kind)
    {
        return kind is PrototypeSpriteKind.Player or PrototypeSpriteKind.Mara or PrototypeSpriteKind.Peer or PrototypeSpriteKind.PixellabTrialNpc or PrototypeSpriteKind.Dallen;
    }
}
