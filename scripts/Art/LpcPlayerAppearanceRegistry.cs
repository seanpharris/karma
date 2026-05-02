using Godot;
using System;
using System.Collections.Generic;

namespace Karma.Art;

public static class LpcPlayerAppearanceRegistry
{
    public const string BundleDirectory = "res://assets/art/generated/lpc_npcs/";
    public const string BundleSuffix = "_32x64_8dir_4row.png";

    public static IReadOnlyList<string> ListBundleIds()
    {
        var dir = DirAccess.Open(BundleDirectory);
        if (dir is null)
            return Array.Empty<string>();

        var bundles = new List<string>();
        foreach (var fileName in dir.GetFiles())
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.EndsWith(BundleSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            bundles.Add(fileName[..^BundleSuffix.Length]);
        }

        bundles.Sort(StringComparer.Ordinal);
        return bundles;
    }

    public static string PickBundleId(string worldId, string playerId)
    {
        var bundles = ListBundleIds();
        if (bundles.Count == 0)
            return string.Empty;

        var key = $"{worldId ?? string.Empty}:{playerId ?? string.Empty}";
        var index = StableIndex(key, bundles.Count);
        return bundles[index];
    }

    public static string BuildAtlasPath(string bundleId)
    {
        return string.IsNullOrWhiteSpace(bundleId)
            ? string.Empty
            : $"{BundleDirectory}{bundleId.Trim()}{BundleSuffix}";
    }

    public static bool BundleExists(string bundleId)
    {
        var path = BuildAtlasPath(bundleId);
        return !string.IsNullOrWhiteSpace(path) && FileAccess.FileExists(path);
    }

    private static int StableIndex(string value, int count)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var hash = offset;
            foreach (var ch in value ?? string.Empty)
            {
                hash ^= ch;
                hash *= prime;
            }

            return (int)(hash % (uint)Math.Max(1, count));
        }
    }
}
