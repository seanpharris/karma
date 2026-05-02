using System.Collections.Generic;

namespace Karma.Audio;

// Three interchangeable voice "slots" so different player characters
// can be assigned different vocal personalities (deep / mid / high, or
// any other casting choice). Each slot holds the same set of barks.
public enum VoiceSlot
{
    Voice1,
    Voice2,
    Voice3
}

// Catalog of short player vocal stingers ("barks"): laugh, sigh,
// taunt, ouch, ready, surrender. Mirrors AudioEventCatalog's shape:
// resolves a (slot, barkId) pair to a clip path under
// `assets/audio/voice/<slot>/<bark>.ogg`. Built-in entries cover
// every (slot × barkId) combination so the resolver is total. The
// player-side play sites should still File.Exists-check the returned
// path — clips ship later, the catalog is the seam.
public static class VoiceBarkCatalog
{
    public const string VoiceRoot = "res://assets/audio/voice/";

    // Bark id constants — keep in one place so call sites don't drift.
    public const string Ouch = "ouch";
    public const string Ready = "ready";
    public const string Laugh = "laugh";
    public const string Sigh = "sigh";
    public const string Taunt = "taunt";
    public const string Surrender = "surrender";

    public static IReadOnlyList<string> BuiltInBarkIds { get; } = new[]
    {
        Ouch, Ready, Laugh, Sigh, Taunt, Surrender
    };

    private static readonly Dictionary<string, string> _runtimeOverrides = new();

    public static string Resolve(VoiceSlot slot, string barkId)
    {
        if (string.IsNullOrEmpty(barkId)) return string.Empty;
        var key = ComposeKey(slot, barkId);
        if (_runtimeOverrides.TryGetValue(key, out var overridePath))
            return overridePath;
        // Built-in path is purely conventional — no map lookup needed.
        return $"{VoiceRoot}{SlotFolder(slot)}/{barkId}.ogg";
    }

    public static void Register(VoiceSlot slot, string barkId, string clipPath)
    {
        if (string.IsNullOrEmpty(barkId)) return;
        _runtimeOverrides[ComposeKey(slot, barkId)] = clipPath ?? string.Empty;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }

    public static string SlotFolder(VoiceSlot slot) => slot switch
    {
        VoiceSlot.Voice1 => "voice1",
        VoiceSlot.Voice2 => "voice2",
        VoiceSlot.Voice3 => "voice3",
        _ => "voice1"
    };

    // For the karma_break / match_started / posse_formed wiring contract.
    public static string BarkForEventId(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return string.Empty;
        if (eventId.Contains("karma_break")) return Ouch;
        if (eventId.Contains("match_started")) return Ready;
        if (eventId.Contains("posse_formed") || eventId.Contains("posse_accepted")) return Laugh;
        return string.Empty;
    }

    private static string ComposeKey(VoiceSlot slot, string barkId) => $"{(int)slot}::{barkId}";
}
