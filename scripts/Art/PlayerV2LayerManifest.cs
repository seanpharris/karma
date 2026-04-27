using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Karma.Art;

public sealed record PlayerV2LayerDefinition(
    string Id,
    string Slot,
    string Path,
    bool Required = false,
    bool IsDefault = false);

public sealed class PlayerV2LayerManifest
{
    public const string DefaultManifestPath = "res://assets/art/sprites/player_v2/player_v2_manifest.json";

    public PlayerV2LayerManifest(
        string manifestPath,
        string schema,
        int frameSize,
        int columns,
        int rows,
        IReadOnlyList<string> directions,
        IReadOnlyList<string> animations,
        IReadOnlyList<string> layerOrder,
        IReadOnlyList<PlayerV2LayerDefinition> layers,
        IReadOnlyList<string> previewStack,
        string compositePath)
    {
        ManifestPath = manifestPath;
        Schema = schema;
        FrameSize = frameSize;
        Columns = columns;
        Rows = rows;
        Directions = directions;
        Animations = animations;
        LayerOrder = layerOrder;
        Layers = layers;
        PreviewStack = previewStack;
        CompositePath = compositePath;
    }

    public string ManifestPath { get; }
    public string Schema { get; }
    public int FrameSize { get; }
    public int Columns { get; }
    public int Rows { get; }
    public IReadOnlyList<string> Directions { get; }
    public IReadOnlyList<string> Animations { get; }
    public IReadOnlyList<string> LayerOrder { get; }
    public IReadOnlyList<PlayerV2LayerDefinition> Layers { get; }
    public IReadOnlyList<string> PreviewStack { get; }
    public string CompositePath { get; }

    public static PlayerV2LayerManifest LoadDefault()
    {
        return Load(DefaultManifestPath);
    }

    public static PlayerV2LayerManifest Load(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !FileAccess.FileExists(manifestPath))
        {
            throw new InvalidOperationException($"Player v2 layer manifest does not exist: {manifestPath}");
        }

        using var document = JsonDocument.Parse(FileAccess.GetFileAsString(manifestPath));
        var root = document.RootElement;
        var layers = root.GetProperty("layers")
            .EnumerateArray()
            .Select(layer => new PlayerV2LayerDefinition(
                RequiredString(layer, "id"),
                RequiredString(layer, "slot"),
                RequiredString(layer, "path"),
                OptionalBool(layer, "required"),
                OptionalBool(layer, "default")))
            .ToArray();

        return new PlayerV2LayerManifest(
            manifestPath,
            RequiredString(root, "schema"),
            root.GetProperty("frameSize").GetInt32(),
            root.GetProperty("columns").GetInt32(),
            root.GetProperty("rows").GetInt32(),
            ReadStringArray(root, "directions"),
            ReadStringArray(root, "animations"),
            ReadStringArray(root, "layerOrder"),
            layers,
            ReadStringArray(root, "previewStack"),
            ResolveRelativePath(manifestPath, RequiredString(root, "composite")));
    }

    public Image ComposePreviewStack()
    {
        return Compose(PreviewStack);
    }

    public Image Compose(IEnumerable<string> layerIds)
    {
        var byId = Layers.ToDictionary(layer => layer.Id, StringComparer.Ordinal);
        Image target = null;
        foreach (var layerId in layerIds)
        {
            if (!byId.TryGetValue(layerId, out var layer))
            {
                throw new InvalidOperationException($"Unknown player v2 layer id: {layerId}");
            }

            var image = Image.LoadFromFile(ResolveRelativePath(ManifestPath, layer.Path));
            if (image is null || image.IsEmpty())
            {
                throw new InvalidOperationException($"Could not load player v2 layer: {layer.Path}");
            }

            if (target is null)
            {
                target = Image.CreateEmpty(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8);
                target.Fill(new Color(0f, 0f, 0f, 0f));
            }

            if (image.GetWidth() != target.GetWidth() || image.GetHeight() != target.GetHeight())
            {
                throw new InvalidOperationException($"Player v2 layer size mismatch: {layer.Id}");
            }

            target.BlendRect(image, new Rect2I(Vector2I.Zero, image.GetSize()), Vector2I.Zero);
        }

        return target ?? Image.CreateEmpty(FrameSize * Columns, FrameSize * Rows, false, Image.Format.Rgba8);
    }

    public string ResolveLayerPath(PlayerV2LayerDefinition layer)
    {
        return ResolveRelativePath(ManifestPath, layer.Path);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string name)
    {
        return root.GetProperty(name).EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
    }

    private static string RequiredString(JsonElement root, string name)
    {
        return root.GetProperty(name).GetString() ?? string.Empty;
    }

    private static bool OptionalBool(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static string ResolveRelativePath(string manifestPath, string relativePath)
    {
        if (relativePath.StartsWith("res://", StringComparison.Ordinal))
        {
            return relativePath;
        }

        var slash = manifestPath.LastIndexOf('/');
        var directory = slash >= 0 ? manifestPath[..slash] : "res://assets/art/sprites/player_v2";
        return $"{directory}/{relativePath}";
    }
}
