using System;
using System.Collections.Generic;
using Godot;

namespace Karma.Art;

public sealed record ItemArtEntry(
    string ThemeId,
    string ItemId,
    string IconPath,
    bool HasIcon);

public static class ItemArtRegistry
{
    public const string ThemeItemsRoot = "res://assets/art/themes/";
    public const string ItemsFolderName = "items";

    private static readonly Dictionary<string, ItemArtEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static ItemArtEntry Get(string themeId, string itemId)
    {
        var normalizedThemeId = NormalizeThemeId(themeId);
        var normalizedItemId = NormalizeItemId(itemId);
        var cacheKey = $"{normalizedThemeId}::{normalizedItemId}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        foreach (var path in CandidateIconPaths(normalizedThemeId, normalizedItemId))
        {
            if (!FileAccess.FileExists(path))
            {
                continue;
            }

            var hit = new ItemArtEntry(normalizedThemeId, normalizedItemId, path, HasIcon: true);
            _cache[cacheKey] = hit;
            return hit;
        }

        var fallback = new ItemArtEntry(normalizedThemeId, normalizedItemId, string.Empty, HasIcon: false);
        _cache[cacheKey] = fallback;
        return fallback;
    }

    public static IReadOnlyList<ItemArtEntry> ListThemeIcons(string themeId)
    {
        var normalizedThemeId = NormalizeThemeId(themeId);
        var directoryPath = ThemeItemDirectory(normalizedThemeId);
        var directory = DirAccess.Open(directoryPath);
        if (directory is null)
        {
            return Array.Empty<ItemArtEntry>();
        }

        var entries = new List<ItemArtEntry>();
        foreach (var fileName in directory.GetFiles())
        {
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var itemId = fileName[..^4];
            entries.Add(new ItemArtEntry(
                normalizedThemeId,
                itemId,
                $"{directoryPath}/{fileName}",
                HasIcon: true));
        }

        entries.Sort((a, b) => string.Compare(a.ItemId, b.ItemId, StringComparison.Ordinal));
        return entries;
    }

    public static void ResetCache()
    {
        _cache.Clear();
    }

    private static IEnumerable<string> CandidateIconPaths(string themeId, string itemId)
    {
        var directory = ThemeItemDirectory(themeId);
        yield return $"{directory}/{itemId}.png";

        var compactItemId = itemId.Replace("_", string.Empty, StringComparison.Ordinal);
        if (!string.Equals(compactItemId, itemId, StringComparison.Ordinal))
        {
            yield return $"{directory}/{compactItemId}.png";
        }
    }

    private static string ThemeItemDirectory(string themeId) =>
        $"{ThemeItemsRoot}{themeId}/{ItemsFolderName}";

    private static string NormalizeThemeId(string themeId) =>
        string.IsNullOrWhiteSpace(themeId)
            ? "medieval"
            : themeId.Trim().Replace('-', '_').ToLowerInvariant();

    private static string NormalizeItemId(string itemId) =>
        string.IsNullOrWhiteSpace(itemId)
            ? string.Empty
            : itemId.Trim().Replace('-', '_').ToLowerInvariant();
}
