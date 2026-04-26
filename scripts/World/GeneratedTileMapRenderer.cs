using Godot;

namespace Karma.World;

public partial class GeneratedTileMapRenderer : Node2D
{
    private const float TileSize = 32f;
    private GeneratedTileMap _tileMap;
    private ThemeArtSet _artSet = ThemeArtRegistry.GetForTheme("western-sci-fi");

    public void SetTileMap(GeneratedTileMap tileMap, ThemeArtSet artSet)
    {
        _tileMap = tileMap;
        _artSet = artSet;
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
            DrawRect(rect, _artSet.GetTile(tile.FloorId).PlaceholderColor);
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
            DrawRect(rect.Grow(-8f), _artSet.GetTile(tile.StructureId).PlaceholderColor);
            return;
        }

        if (tile.StructureId == WorldTileIds.DoorAirlock)
        {
            DrawRect(rect.Grow(-4f), new Color(0.12f, 0.15f, 0.18f));
            DrawRect(new Rect2(rect.Position + new Vector2(9f, 4f), new Vector2(14f, 24f)), _artSet.GetTile(tile.StructureId).PlaceholderColor);
            return;
        }

        if (tile.StructureId == WorldTileIds.OddityPile)
        {
            DrawCircle(rect.GetCenter(), 9f, _artSet.GetTile(tile.StructureId).PlaceholderColor);
            DrawCircle(rect.GetCenter() + new Vector2(7f, -5f), 5f, new Color(0.25f, 0.84f, 0.78f));
        }
    }
}
