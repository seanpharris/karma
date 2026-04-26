using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.World;

namespace Karma.Art;

public sealed record ArtAssetReference(
    string Path,
    string Source,
    string DisplayName);

public static class ArtAssetManifest
{
    public static IReadOnlyList<ArtAssetReference> GetAllReferences()
    {
        var references = new List<ArtAssetReference>();
        AddPrototypeSpriteReferences(references);
        AddStructureReferences(references);
        AddTileReferences(references);
        return references
            .OrderBy(reference => reference.Path, StringComparer.Ordinal)
            .ThenBy(reference => reference.Source, StringComparer.Ordinal)
            .ThenBy(reference => reference.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<ArtAssetReference> GetUniqueAssets()
    {
        return GetAllReferences()
            .GroupBy(reference => reference.Path)
            .Select(group => new ArtAssetReference(
                group.Key,
                "asset",
                string.Join(", ", group.Select(reference => reference.Source).Distinct().OrderBy(source => source))))
            .OrderBy(reference => reference.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<ArtAssetReference> GetMissingAssets()
    {
        return GetUniqueAssets()
            .Where(reference => !FileAccess.FileExists(reference.Path))
            .ToArray();
    }

    public static string FormatSummary()
    {
        var uniqueAssets = GetUniqueAssets();
        var missingAssets = GetMissingAssets();
        return $"{uniqueAssets.Count} atlas assets referenced, {missingAssets.Count} missing";
    }

    private static void AddPrototypeSpriteReferences(ICollection<ArtAssetReference> references)
    {
        foreach (PrototypeSpriteKind kind in Enum.GetValues(typeof(PrototypeSpriteKind)))
        {
            var definition = PrototypeSpriteCatalog.Get(kind);
            if (!definition.HasAtlasRegion)
            {
                continue;
            }

            references.Add(new ArtAssetReference(
                definition.AtlasPath,
                "prototype-sprite",
                definition.DisplayName));
        }
    }

    private static void AddStructureReferences(ICollection<ArtAssetReference> references)
    {
        foreach (var definition in StructureArtCatalog.All.Values)
        {
            if (!definition.HasAtlasRegion)
            {
                continue;
            }

            references.Add(new ArtAssetReference(
                definition.AtlasPath,
                "structure",
                definition.DisplayName));
        }
    }

    private static void AddTileReferences(ICollection<ArtAssetReference> references)
    {
        foreach (var tile in ThemeArtRegistry.GetForTheme("western-sci-fi").Tiles.Values)
        {
            if (!tile.HasAtlasRegion)
            {
                continue;
            }

            references.Add(new ArtAssetReference(
                tile.AtlasPath,
                "tile",
                tile.TileId));
        }
    }
}
