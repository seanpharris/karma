using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Karma.Data;

namespace Karma.Art;

public sealed record PlayerV2LayerDefinition(
    string Id,
    string Slot,
    string Path,
    bool Required = false,
    bool IsDefault = false);

public sealed record PlayerV2Appearance(IReadOnlyDictionary<string, string> SelectedLayerIdsBySlot)
{
    public static PlayerV2Appearance Empty { get; } = new(new Dictionary<string, string>());

    public PlayerV2Appearance WithLayer(string slot, string layerId)
    {
        var selected = new Dictionary<string, string>(SelectedLayerIdsBySlot, StringComparer.Ordinal)
        {
            [slot] = layerId
        };
        return new PlayerV2Appearance(selected);
    }

    public string GetLayerIdForSlot(string slot)
    {
        return SelectedLayerIdsBySlot.TryGetValue(slot, out var layerId)
            ? layerId
            : string.Empty;
    }
}

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

    public PlayerV2Appearance CreateDefaultAppearance()
    {
        var selected = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in LayerOrder)
        {
            var slotLayers = Layers.Where(layer => layer.Slot == slot).ToArray();
            var selectedLayer = slotLayers.FirstOrDefault(layer => layer.IsDefault) ??
                                slotLayers.FirstOrDefault(layer => layer.Required) ??
                                slotLayers.FirstOrDefault();
            if (selectedLayer is not null)
            {
                selected[slot] = selectedLayer.Id;
            }
        }

        return new PlayerV2Appearance(selected);
    }

    public PlayerV2Appearance CreateAppearance(PlayerAppearanceSelection selection)
    {
        return CreateAppearance(selection.ToLayerIdsBySlot());
    }

    public PlayerV2Appearance CreateAppearance(IReadOnlyDictionary<string, string> layerIdsBySlot)
    {
        var appearance = CreateDefaultAppearance();
        foreach (var (slot, layerId) in layerIdsBySlot)
        {
            appearance = appearance.WithLayer(slot, layerId);
        }

        ValidateAppearance(appearance);
        return appearance;
    }

    public IReadOnlyList<string> GetLayerStack(PlayerV2Appearance appearance)
    {
        ValidateAppearance(appearance);
        var orderedLayerIds = new List<string>();
        foreach (var slot in LayerOrder)
        {
            var layerId = appearance.GetLayerIdForSlot(slot);
            if (!string.IsNullOrWhiteSpace(layerId))
            {
                orderedLayerIds.Add(layerId);
            }
        }

        return orderedLayerIds;
    }

    public Image ComposePreviewStack()
    {
        return Compose(PreviewStack);
    }

    public Image Compose(PlayerV2Appearance appearance)
    {
        return Compose(GetLayerStack(appearance));
    }

    public string ExportAppearanceComposite(PlayerAppearanceSelection selection, string cacheRoot = "user://player_v2/composites")
    {
        return ExportAppearanceComposite(CreateAppearance(selection), cacheRoot);
    }

    public string ExportAppearanceComposite(PlayerV2Appearance appearance, string cacheRoot = "user://player_v2/composites")
    {
        var layerStack = GetLayerStack(appearance);
        var outputPath = BuildCompositePath(layerStack, cacheRoot);
        var globalCacheRoot = ProjectSettings.GlobalizePath(cacheRoot);
        DirAccess.MakeDirRecursiveAbsolute(globalCacheRoot);
        var image = Compose(layerStack);
        var error = image.SavePng(outputPath);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Could not export player v2 appearance composite to {outputPath}: {error}");
        }

        return outputPath;
    }

    public string BuildCompositePath(PlayerV2Appearance appearance, string cacheRoot = "user://player_v2/composites")
    {
        return BuildCompositePath(GetLayerStack(appearance), cacheRoot);
    }

    public static string BuildCompositePath(IEnumerable<string> layerIds, string cacheRoot = "user://player_v2/composites")
    {
        var key = string.Join("__", layerIds.Select(SanitizeCacheToken));
        return $"{cacheRoot.TrimEnd('/')}/player_v2__{key}.png";
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

    private void ValidateAppearance(PlayerV2Appearance appearance)
    {
        var byId = Layers.ToDictionary(layer => layer.Id, StringComparer.Ordinal);
        foreach (var (slot, layerId) in appearance.SelectedLayerIdsBySlot)
        {
            if (!LayerOrder.Contains(slot, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Unknown player v2 layer slot: {slot}");
            }

            if (!byId.TryGetValue(layerId, out var layer))
            {
                throw new InvalidOperationException($"Unknown player v2 layer id: {layerId}");
            }

            if (!string.Equals(layer.Slot, slot, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Player v2 layer {layerId} belongs to slot {layer.Slot}, not {slot}");
            }
        }
    }

    private static string SanitizeCacheToken(string value)
    {
        var characters = value
            .Where(character => char.IsLetterOrDigit(character) || character == '_' || character == '-')
            .ToArray();
        return characters.Length == 0
            ? "layer"
            : new string(characters);
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
