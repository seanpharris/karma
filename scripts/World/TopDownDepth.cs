using Godot;

namespace Karma.World;

public static class TopDownDepth
{
    public const int TileLayerZ = -150;
    public const int StructureOffsetZ = -5;
    public const int ActorOffsetZ = 0;
    public const int ItemOffsetZ = 2;
    public const int HudOffsetZ = 10000;

    public static int CalculateZIndex(float footY, int offset = ActorOffsetZ)
    {
        return Mathf.RoundToInt(footY) + offset;
    }

    public static void Apply(Node2D node, int offset = ActorOffsetZ)
    {
        node.ZIndex = CalculateZIndex(node.GlobalPosition.Y, offset);
    }
}
