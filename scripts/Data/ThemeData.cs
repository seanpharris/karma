using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Karma.World;

namespace Karma.Data;

public sealed record ThemeRelationship(string Target, string Type, int Intensity);

public sealed record ThemeNpc(
    string Id,
    string Name,
    string Role,
    string Faction,
    string Alignment,
    string LpcBundle,
    IReadOnlyList<string> AppearanceOptions,
    IReadOnlyList<ThemeRelationship> Relationships,
    IReadOnlyList<string> ExplicitTags)
{
    public IReadOnlyList<string> RoleTags
    {
        get
        {
            var tags = new List<string>();
            if (ExplicitTags is { Count: > 0 })
                tags.AddRange(ExplicitTags);

            // Generic alignment-based fallbacks. Themes that supply
            // ExplicitTags can leave these to be additive — duplicates
            // are squashed by the Distinct() at the end.
            if (Alignment == "law") tags.Add("law");
            if (Alignment == "outlaw") tags.Add("outlaw");

            // Medieval-flavoured faction → tag fallbacks. These are
            // harmless for other themes (their faction ids won't
            // match) and there to support medieval theme.json data
            // that doesn't yet declare ExplicitTags.
            if (Faction == "chapel_order") tags.Add("chapel");
            if (Faction == "wayfarers") tags.Add("wayfarer");
            if (Faction == "wild_folk") tags.Add("wild");

            // Generic role-keyword tagging.
            if (Role.Contains("tavern", StringComparison.OrdinalIgnoreCase) ||
                Role.Contains("merchant", StringComparison.OrdinalIgnoreCase) ||
                Role.Contains("trader", StringComparison.OrdinalIgnoreCase))
                tags.Add("trade");

            tags.Add("peasant");
            return tags.Distinct().ToArray();
        }
    }
}

// Per-theme data parsed from assets/themes/<id>/theme.json. Empty when
// the file is absent so themes without authored data still spin up
// (they just don't get themed greetings/gossip/appearance picks).
public sealed class ThemeData
{
    public static readonly ThemeData Empty = new();

    public string ThemeId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, ThemeNpc> NpcRoster { get; init; } =
        new Dictionary<string, ThemeNpc>();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GreetingsPool { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GossipTemplates { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    public bool HasRoster => NpcRoster.Count > 0;

    // Resolve a theme.json path for the given theme id via the
    // ThemeRegistry, then load it. Returns Empty when the registry
    // doesn't carry a path or the file is missing — non-medieval
    // themes are allowed to ship without authored data.
    public static ThemeData LoadFor(string themeId)
    {
        var definition = ThemeRegistry.Get(themeId);
        if (string.IsNullOrWhiteSpace(definition.ThemeJsonPath))
            return Empty;
        return Load(definition.ThemeJsonPath, definition.Id);
    }

    public static ThemeData Load(string path, string themeId = "")
    {
        var fsPath = StripGodotResPrefix(path);
        if (!File.Exists(fsPath))
            return new ThemeData { ThemeId = themeId };

        using var document = JsonDocument.Parse(File.ReadAllText(fsPath));
        var root = document.RootElement;
        var roster = new Dictionary<string, ThemeNpc>();
        if (root.TryGetProperty("npc_roster", out var npcRoster))
        {
            foreach (var npc in npcRoster.EnumerateArray())
            {
                var id = ReadString(npc, "id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var options = ReadStringArray(npc, "appearance_options");
                var fallbackBundle = ReadString(npc, "lpc_bundle");
                if (options.Count == 0 && !string.IsNullOrWhiteSpace(fallbackBundle))
                    options = new[] { fallbackBundle };

                roster[id] = new ThemeNpc(
                    id,
                    ReadString(npc, "name"),
                    ReadString(npc, "role"),
                    ReadString(npc, "faction"),
                    ReadString(npc, "alignment"),
                    fallbackBundle,
                    options,
                    ReadRelationships(npc),
                    ReadStringArray(npc, "tags"));
            }
        }

        var interactions = root.TryGetProperty("interactions", out var interactionRoot)
            ? interactionRoot
            : default;

        return new ThemeData
        {
            ThemeId = themeId,
            NpcRoster = roster,
            GreetingsPool = ReadPool(interactions, "greetings_pool"),
            GossipTemplates = ReadPool(interactions, "gossip_templates")
        };
    }

    public bool TryGetNpc(string npcId, out ThemeNpc npc) =>
        NpcRoster.TryGetValue(npcId, out npc);

    public string PickAppearanceBundle(string worldId, string npcId)
    {
        if (!TryGetNpc(npcId, out var npc) || npc.AppearanceOptions.Count == 0)
            return string.Empty;
        var index = Math.Abs(HashCode.Combine(worldId, npcId)) % npc.AppearanceOptions.Count;
        return npc.AppearanceOptions[index];
    }

    public string PickGreeting(string worldId, string npcId)
    {
        if (!TryGetNpc(npcId, out var npc))
            return string.Empty;
        foreach (var tag in npc.RoleTags)
        {
            if (!GreetingsPool.TryGetValue(tag, out var pool) || pool.Count == 0) continue;
            var index = Math.Abs(HashCode.Combine(worldId, npcId, tag)) % pool.Count;
            return pool[index];
        }
        return string.Empty;
    }

    public string PickGossip(string worldId, string npcId)
    {
        if (!TryGetNpc(npcId, out var npc) || npc.Relationships.Count == 0)
            return string.Empty;

        var relation = npc.Relationships
            .OrderByDescending(r => RelationshipWeight(r))
            .ThenBy(r => r.Target)
            .First();
        var relationName = TryGetNpc(relation.Target, out var target)
            ? target.Name
            : relation.Target;
        foreach (var tag in npc.RoleTags)
        {
            if (!GossipTemplates.TryGetValue(tag, out var templates) || templates.Count == 0) continue;
            var index = Math.Abs(HashCode.Combine(worldId, npcId, relation.Target, tag)) % templates.Count;
            return templates[index].Replace("{relation_name}", relationName);
        }
        return string.Empty;
    }

    private static int RelationshipWeight(ThemeRelationship relationship)
    {
        var typeBonus = relationship.Type is "rival" or "knows_secret" or "creditor" ? 10 : 0;
        return typeBonus + relationship.Intensity;
    }

    private static string StripGodotResPrefix(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        const string prefix = "res://";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path.Substring(prefix.Length)
            : path;
    }

    private static string ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static IReadOnlyList<ThemeRelationship> ReadRelationships(JsonElement npc)
    {
        if (!npc.TryGetProperty("relationships", out var values) || values.ValueKind != JsonValueKind.Array)
            return Array.Empty<ThemeRelationship>();
        return values.EnumerateArray()
            .Select(value => new ThemeRelationship(
                ReadString(value, "target"),
                ReadString(value, "type"),
                value.TryGetProperty("intensity", out var intensity) ? intensity.GetInt32() : 1))
            .Where(value => !string.IsNullOrWhiteSpace(value.Target))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadPool(JsonElement interactions, string property)
    {
        if (interactions.ValueKind != JsonValueKind.Object ||
            !interactions.TryGetProperty(property, out var poolRoot) ||
            poolRoot.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }

        return poolRoot.EnumerateObject()
            .ToDictionary(
                prop => prop.Name,
                prop => (IReadOnlyList<string>)prop.Value.EnumerateArray()
                    .Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => value.GetString() ?? string.Empty)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray());
    }
}

// Per-process cache so each theme's JSON is parsed at most once. Lookup
// is normalized via ThemeRegistry, so case + dash/underscore variants
// resolve to the same instance.
public static class ThemeDataCatalog
{
    private static readonly Dictionary<string, ThemeData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static ThemeData Get(string themeId)
    {
        var definition = ThemeRegistry.Get(themeId);
        if (_cache.TryGetValue(definition.Id, out var cached))
            return cached;
        var loaded = ThemeData.LoadFor(definition.Id);
        _cache[definition.Id] = loaded;
        return loaded;
    }

    public static void Reset()
    {
        _cache.Clear();
    }
}
