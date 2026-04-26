using Godot;

namespace Karma.World;

public partial class GeneratedTileMapRenderer : Node2D
{
    private const float TileSize = 32f;
    private GeneratedTileMap _tileMap;

    public void SetTileMap(GeneratedTileMap tileMap)
    {
        _tileMap = tileMap;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_tileMap is null)
        {
            return;
        }

        foreach (var tile in _tileMap.Tiles)
        {
            var rect = new Rect2(tile.X * TileSize, tile.Y * TileSize, TileSize, TileSize);
            DrawRect(rect, GetFloorColor(tile.FloorId));
            DrawRect(rect, new Color(0f, 0f, 0f, 0.08f), filled: false, width: 1f);

            if (!string.IsNullOrWhiteSpace(tile.StructureId))
            {
                DrawStructure(tile, rect);
            }
        }
    }

    private void DrawStructure(GeneratedTile tile, Rect2 rect)
    {
        if (tile.StructureId == WorldTileIds.WallMetal)
        {
            DrawRect(rect.Grow(-4f), new Color(0.18f, 0.2f, 0.24f));
            DrawRect(rect.Grow(-8f), new Color(0.34f, 0.37f, 0.42f));
            return;
        }

        if (tile.StructureId == WorldTileIds.DoorAirlock)
        {
            DrawRect(rect.Grow(-4f), new Color(0.12f, 0.15f, 0.18f));
            DrawRect(new Rect2(rect.Position + new Vector2(9f, 4f), new Vector2(14f, 24f)), new Color(0.76f, 0.54f, 0.16f));
            return;
        }

        if (tile.StructureId == WorldTileIds.OddityPile)
        {
            DrawCircle(rect.GetCenter(), 9f, new Color(0.82f, 0.3f, 0.76f));
            DrawCircle(rect.GetCenter() + new Vector2(7f, -5f), 5f, new Color(0.25f, 0.84f, 0.78f));
        }
    }

    private static Color GetFloorColor(string floorId)
    {
        return floorId switch
        {
            WorldTileIds.GroundDust => new Color(0.45f, 0.34f, 0.24f),
            WorldTileIds.PathDust => new Color(0.56f, 0.45f, 0.31f),
            WorldTileIds.ClinicFloor => new Color(0.32f, 0.34f, 0.38f),
            WorldTileIds.MarketFloor => new Color(0.36f, 0.28f, 0.22f),
            WorldTileIds.WorkshopFloor => new Color(0.25f, 0.3f, 0.32f),
            WorldTileIds.DuelRingFloor => new Color(0.42f, 0.24f, 0.28f),
            _ => new Color(0.25f, 0.4f, 0.28f)
        };
    }
}
