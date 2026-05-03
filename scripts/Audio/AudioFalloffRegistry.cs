using System.Collections.Generic;

namespace Karma.Audio;

// Per-event falloff profile for positional audio. Lets each event id
// dictate how far it carries (`MaxDistanceTiles`) and how aggressively
// the high-end is rolled off with distance (`AttenuationCutoffHz`).
// Defaults to 8 tiles / 5 kHz cutoff per the task spec; specific events
// override (karma_break carries 16 tiles, purchase_complete only 4).
public readonly record struct AudioFalloffProfile(
    float MaxDistanceTiles,
    float AttenuationCutoffHz)
{
    public static AudioFalloffProfile Default => new(8f, 5000f);
}

public static class AudioFalloffRegistry
{
    private static readonly Dictionary<string, AudioFalloffProfile> BuiltIn = new()
    {
        // Loud, "the whole town hears it" cues.
        { "karma_break", new AudioFalloffProfile(16f, 7000f) },
        { "supply_drop_spawned", new AudioFalloffProfile(20f, 7000f) },
        { "wanted_bounty_claimed", new AudioFalloffProfile(12f, 6000f) },
        // Mid-range world cues.
        { "door_opened", new AudioFalloffProfile(8f, 5000f) },
        { "structure_interacted", new AudioFalloffProfile(6f, 5000f) },
        // Quiet personal cues — only nearby players should hear.
        { "purchase_complete", new AudioFalloffProfile(4f, 4500f) },
        { "weapon_reloaded", new AudioFalloffProfile(5f, 5000f) },
        { "footstep_dirt", new AudioFalloffProfile(4f, 4000f) },
        { "footstep_stone", new AudioFalloffProfile(4f, 5500f) },
        { "footstep_wood", new AudioFalloffProfile(4f, 4500f) },
        { "interact_pop", new AudioFalloffProfile(3f, 5500f) },
    };

    private static readonly Dictionary<string, AudioFalloffProfile> _runtimeOverrides = new();

    public static AudioFalloffProfile Resolve(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return AudioFalloffProfile.Default;
        if (_runtimeOverrides.TryGetValue(eventId, out var overrideProfile)) return overrideProfile;
        if (BuiltIn.TryGetValue(eventId, out var profile)) return profile;

        // Substring fallback so compound ids ("door_opened_chapel") inherit
        // their canonical profile without needing a per-variant entry.
        string bestKey = null;
        foreach (var key in BuiltIn.Keys)
        {
            if (!eventId.Contains(key)) continue;
            if (bestKey is null || key.Length > bestKey.Length) bestKey = key;
        }
        return bestKey is null ? AudioFalloffProfile.Default : BuiltIn[bestKey];
    }

    public static void Register(string eventId, AudioFalloffProfile profile)
    {
        if (string.IsNullOrEmpty(eventId)) return;
        _runtimeOverrides[eventId] = profile;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }
}
