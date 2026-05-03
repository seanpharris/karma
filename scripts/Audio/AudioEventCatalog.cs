using System.Collections.Generic;
using System.Linq;
using Karma.Data;

namespace Karma.Audio;

// Maps logical event ids (door_opened, karma_break, purchase_complete, etc.)
// to audio clip paths so HUD/world systems can request playback by id rather
// than hardcoding paths. Built-ins are seeded from BuiltInClips; runtime
// registrations override them. Mirrors the StructureArtCatalog pattern.
//
// HudController and positional world helpers resolve through this catalog so
// cue swaps stay data-shaped instead of spreading clip paths through gameplay.
public static class AudioEventCatalog
{
    public const string ClipsRoot = "res://assets/audio/sfx/";

    private static readonly Dictionary<string, string> BuiltInClips = new()
    {
        // HUD/UI feedback edges.
        { "karma_break", ClipsRoot + "karma_break_stinger.wav" },
        { "contraband_detected", ClipsRoot + "contraband_alarm.wav" },
        { "purchase_complete", ClipsRoot + "purchase_chime.wav" },
        { "item_purchased", ClipsRoot + "purchase_chime.wav" },
        { "item_sold", ClipsRoot + "purchase_chime.wav" },
        { "currency_transferred", ClipsRoot + "purchase_chime.wav" },
        { "weapon_reloaded", ClipsRoot + "reload_click.wav" },
        { "supply_drop_spawned", ClipsRoot + "supply_drop_horn.wav" },
        { "supply_drop_claimed", ClipsRoot + "interact_pop.wav" },
        { "quest_completed", ClipsRoot + "purchase_chime.wav" },
        { "quest_started", ClipsRoot + "interact_pop.wav" },
        { "quest_step_advanced", ClipsRoot + "interact_pop.wav" },
        { "dialogue_started", ClipsRoot + "interact_pop.wav" },
        { "dialogue_advanced", ClipsRoot + "interact_pop.wav" },
        { "dialogue_choice_selected", ClipsRoot + "interact_pop.wav" },
        { "dialogue_closed", ClipsRoot + "interact_pop.wav" },
        // World interaction edges.
        { "door_opened", ClipsRoot + "door_open.wav" },
        { "structure_interacted", ClipsRoot + "interact_pop.wav" },
        { "container_scavenged", ClipsRoot + "interact_pop.wav" },
        { "restroom_used", ClipsRoot + "interact_pop.wav" },
        { "item_picked_up", ClipsRoot + "interact_pop.wav" },
        { "item_transferred", ClipsRoot + "interact_pop.wav" },
        { "item_placed", ClipsRoot + "interact_pop.wav" },
        { "item_used", ClipsRoot + "interact_pop.wav" },
        { "item_used_heal", ClipsRoot + "clinic_revive_chime.wav" },
        { "item_used_food", ClipsRoot + "interact_pop.wav" },
        { "drug_used", ClipsRoot + "grunt_pain.wav" },
        { "item_repaired", ClipsRoot + "interact_pop.wav" },
        { "item_crafted", ClipsRoot + "interact_pop.wav" },
        { "mount_bag_transfer", ClipsRoot + "interact_pop.wav" },
        { "player_mounted", ClipsRoot + "footstep_wood.wav" },
        { "player_dismounted", ClipsRoot + "footstep_dirt.wav" },
        { "clinic_revive", ClipsRoot + "clinic_revive_chime.wav" },
        // Combat edges.
        { "player_attacked", ClipsRoot + "hit_thud.wav" },
        { "wanted_bounty_claimed", ClipsRoot + "bounty_paid.wav" },
        // Locomotion + body cues.
        { "footstep_dirt", ClipsRoot + "footstep_dirt.wav" },
        { "footstep_stone", ClipsRoot + "footstep_stone.wav" },
        { "footstep_wood", ClipsRoot + "footstep_wood.wav" },
        { "player_moved", ClipsRoot + "footstep_dirt.wav" },
        { "grunt_pain", ClipsRoot + "grunt_pain.wav" },
        { "grunt_attack", ClipsRoot + "grunt_attack.wav" },
        // Combat cue variants.
        { "sword_swing", ClipsRoot + "sword_swing.wav" },
        { "sword_hit", ClipsRoot + "sword_hit.wav" },
        // Equipment cues. These are intentionally item-specific so new clips can
        // replace the shared placeholders without changing gameplay events.
        { "item_equipped_practice_stick", ClipsRoot + "sword_swing.wav" },
        { "item_equipped_work_vest", ClipsRoot + "interact_pop.wav" },
        { "item_equipped_stun_baton", ClipsRoot + "reload_click.wav" },
        { "item_equipped_electro_pistol", ClipsRoot + "reload_click.wav" },
        { "item_equipped_smg_11", ClipsRoot + "reload_click.wav" },
        { "item_equipped_shotgun_mk1", ClipsRoot + "reload_click.wav" },
        { "item_equipped_rifle_27", ClipsRoot + "reload_click.wav" },
        { "item_equipped_sniper_x9", ClipsRoot + "reload_click.wav" },
        { "item_equipped_plasma_cutter", ClipsRoot + "sword_swing.wav" },
        { "item_equipped_flame_thrower", ClipsRoot + "reload_click.wav" },
        { "item_equipped_grenade_launcher", ClipsRoot + "reload_click.wav" },
        { "item_equipped_railgun", ClipsRoot + "reload_click.wav" },
        { "item_equipped_impact_mine", ClipsRoot + "reload_click.wav" },
        { "item_equipped_emp_grenade", ClipsRoot + "reload_click.wav" },
        { "item_equipped_portable_shield", ClipsRoot + "interact_pop.wav" },
        { "item_equipped_backpack_brown", ClipsRoot + "interact_pop.wav" },
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

    public static string EquipmentCueIdForItem(GameItem item)
    {
        if (item is null || item.Slot == EquipmentSlot.None || string.IsNullOrWhiteSpace(item.Id))
            return string.Empty;

        return $"item_equipped_{item.Id}";
    }

    public static string EquipmentCueIdForItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !StarterItems.TryGetById(itemId, out var item))
            return string.Empty;

        return EquipmentCueIdForItem(item);
    }

    public static string ResolveEquipmentCue(GameItem item)
    {
        var cueId = EquipmentCueIdForItem(item);
        return string.IsNullOrWhiteSpace(cueId) ? string.Empty : Resolve(cueId);
    }

    public static string ItemUseCueIdForItem(GameItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Id))
            return string.Empty;

        if (item.Id == StarterItems.RepairKitId || item.Id == StarterItems.MediPatchId)
            return "item_used_heal";
        if (item.Id == StarterItems.RationPackId)
            return "item_used_food";
        if (item.Tags is not null && item.Tags.Contains("drug"))
            return "drug_used";

        return "item_used";
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
