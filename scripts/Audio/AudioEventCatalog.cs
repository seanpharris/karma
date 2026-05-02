using System.Collections.Generic;

namespace Karma.Audio;

// Maps logical event ids (door_opened, karma_break, purchase_complete, etc.)
// to audio clip paths so HUD/world systems can request playback by id rather
// than hardcoding paths. Built-ins are seeded from BuiltInClips; runtime
// registrations override them. Mirrors the StructureArtCatalog pattern.
//
// This catalog is the seam — nothing plays audio yet. Once SOUND_NEEDED.md
// deliveries land we can wire AudioStreamPlayer at the call sites.
public static class AudioEventCatalog
{
    public const string ClipsRoot = "res://assets/audio/sfx/";

    private static readonly Dictionary<string, string> BuiltInClips = new()
    {
        // HUD/UI feedback edges.
        { "karma_break", ClipsRoot + "karma_break_stinger.wav" },
        { "contraband_detected", ClipsRoot + "contraband_alarm.wav" },
        { "purchase_complete", ClipsRoot + "purchase_chime.wav" },
        { "weapon_reloaded", ClipsRoot + "reload_click.wav" },
        { "supply_drop_spawned", ClipsRoot + "supply_drop_horn.wav" },
        // World interaction edges.
        { "door_opened", ClipsRoot + "door_open.wav" },
        { "structure_interacted", ClipsRoot + "interact_pop.wav" },
        { "clinic_revive", ClipsRoot + "clinic_revive_chime.wav" },
        // Combat edges.
        { "player_attacked", ClipsRoot + "hit_thud.wav" },
        { "wanted_bounty_claimed", ClipsRoot + "bounty_paid.wav" },
        // Locomotion + body cues.
        { "footstep_dirt", ClipsRoot + "footstep_dirt.wav" },
        { "footstep_stone", ClipsRoot + "footstep_stone.wav" },
        { "footstep_wood", ClipsRoot + "footstep_wood.wav" },
        { "grunt_pain", ClipsRoot + "grunt_pain.wav" },
        { "grunt_attack", ClipsRoot + "grunt_attack.wav" },
        // Combat cue variants.
        { "sword_swing", ClipsRoot + "sword_swing.wav" },
        { "sword_hit", ClipsRoot + "sword_hit.wav" },
    };

    private static readonly Dictionary<string, string> _runtimeOverrides = new();

    public static IReadOnlyDictionary<string, string> All
    {
        get
        {
            var merged = new Dictionary<string, string>(BuiltInClips);
            foreach (var (key, value) in _runtimeOverrides)
                merged[key] = value;
            return merged;
        }
    }

    // Register or override a clip for an event id at runtime.
    public static void Register(string eventId, string clipPath)
    {
        if (string.IsNullOrEmpty(eventId)) return;
        _runtimeOverrides[eventId] = clipPath ?? string.Empty;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }

    // Returns the clip path for an event id, or empty string when no mapping
    // is registered. Substring-style match scan picks longer ids first so
    // "wanted_bounty_claimed" doesn't accidentally match a generic "wanted"
    // entry, mirroring how HudController.ResolveEventIconName scans.
    public static string Resolve(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return string.Empty;
        if (_runtimeOverrides.TryGetValue(eventId, out var overridePath))
            return overridePath;
        if (BuiltInClips.TryGetValue(eventId, out var directPath))
            return directPath;

        // Fallback: scan registered ids for a substring match. Prefer longer
        // keys so the most specific entry wins.
        string bestKey = null;
        foreach (var key in BuiltInClips.Keys)
        {
            if (!eventId.Contains(key)) continue;
            if (bestKey is null || key.Length > bestKey.Length) bestKey = key;
        }
        foreach (var key in _runtimeOverrides.Keys)
        {
            if (!eventId.Contains(key)) continue;
            if (bestKey is null || key.Length > bestKey.Length) bestKey = key;
        }
        if (bestKey is null) return string.Empty;
        return _runtimeOverrides.TryGetValue(bestKey, out var ov)
            ? ov
            : BuiltInClips[bestKey];
    }
}
