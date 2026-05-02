using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Karma.Net;

namespace Karma.Audio;

// Loops a single ambient bed at a time. When the player's snapshot
// indicates they're inside a structure, swap to that structure's
// category bed (tavern / chapel / smithy / market / clinic). When
// outside, fall back to the outdoor bed. Crossfade is a 1.5s linear
// equal-duration fade — only one bed is audible after the swap.
//
// Bed audio paths follow `assets/audio/ambient/<bed_id>.ogg`. Missing
// files no-op silently so the manager is safe to attach before clips
// land.
public partial class AmbientBedManager : Node
{
    public const string BedRoot = "res://assets/audio/ambient/";
    public const float CrossfadeSeconds = 1.5f;
    public const double PollIntervalSeconds = 1.0;

    public const string OutdoorBedId = "outdoor";

    private static readonly Dictionary<string, string> CategoryToBed = new(StringComparer.OrdinalIgnoreCase)
    {
        { "tavern", "tavern" },
        { "chapel", "chapel" },
        { "smithy", "smithy" },
        { "market", "market" },
        { "clinic", "clinic" },
    };

    [Export] public float BedVolume { get; set; } = 0.5f;

    private Karma.Net.PrototypeServerSession _session;
    private AudioStreamPlayer _activePlayer;
    private AudioStreamPlayer _previousPlayer;
    private string _activeBedId = string.Empty;
    private double _pollAccumulator;

    public string ActiveBedId => _activeBedId;

    public override void _Ready()
    {
        AudioSettings.EnsureBusesExist();
        _session = GetNodeOrNull<Karma.Net.PrototypeServerSession>("/root/PrototypeServerSession");
        // No session in headless test runs — manager is dormant but still safe.
    }

    public override void _Process(double delta)
    {
        _pollAccumulator += delta;
        if (_pollAccumulator < PollIntervalSeconds) return;
        _pollAccumulator = 0;
        SwapToBedFor(_session?.LastLocalSnapshot);
    }

    public void SwapToBedFor(ClientInterestSnapshot snapshot)
    {
        var nextBedId = PickBedId(snapshot, snapshot?.PlayerId ?? string.Empty);
        if (string.Equals(nextBedId, _activeBedId, StringComparison.Ordinal)) return;
        Crossfade(nextBedId);
    }

    // Pure picker — surfaced so the smoke test can exercise the
    // outdoor → interior → outdoor transitions without spinning up audio.
    public static string PickBedId(ClientInterestSnapshot snapshot, string localPlayerId)
    {
        if (snapshot is null || snapshot.Players is null) return OutdoorBedId;
        if (string.IsNullOrEmpty(localPlayerId)) return OutdoorBedId;
        var local = snapshot.Players.FirstOrDefault(p => p.Id == localPlayerId);
        if (local is null) return OutdoorBedId;
        return PickBedIdFromInterior(local.InsideStructureId, snapshot.Structures);
    }

    // Test-friendly overload: takes only the inputs the picker actually
    // depends on so a smoke test can drive it without constructing a
    // full ClientInterestSnapshot.
    public static string PickBedIdFromInterior(
        string insideStructureId,
        IReadOnlyList<WorldStructureSnapshot> structures)
    {
        if (string.IsNullOrEmpty(insideStructureId)) return OutdoorBedId;
        var structure = structures?.FirstOrDefault(s => s.EntityId == insideStructureId);
        if (structure is null) return OutdoorBedId;
        return CategoryToBedId(structure.Category);
    }

    public static string CategoryToBedId(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return OutdoorBedId;
        return CategoryToBed.TryGetValue(category, out var bed) ? bed : OutdoorBedId;
    }

    public static string ResolveBedPath(string bedId)
    {
        if (string.IsNullOrWhiteSpace(bedId)) return string.Empty;
        return $"{BedRoot}{bedId}.ogg";
    }

    private void Crossfade(string nextBedId)
    {
        var path = ResolveBedPath(nextBedId);
        if (string.IsNullOrEmpty(path) || !FileAccess.FileExists(path))
        {
            // No clip on disk — fade out the current bed and remember the target so
            // the manager doesn't hammer the missing-file check every poll.
            _activeBedId = nextBedId;
            FadeOutActive();
            return;
        }
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream is null)
        {
            _activeBedId = nextBedId;
            return;
        }

        // Free anything already mid-fade so we don't stack three players.
        _previousPlayer?.QueueFree();
        _previousPlayer = _activePlayer;

        _activePlayer = new AudioStreamPlayer
        {
            Name = $"AmbientBed_{nextBedId}",
            Stream = stream,
            Bus = AudioSettings.AmbientBusName,
            VolumeDb = AudioSettings.PercentToDb(0.001) // start silent, fade in
        };
        AddChild(_activePlayer);
        _activePlayer.Play();
        _activeBedId = nextBedId;

        var fadeIn = CreateTween();
        fadeIn.TweenProperty(_activePlayer, "volume_db", AudioSettings.PercentToDb(BedVolume * 100.0), CrossfadeSeconds);
        if (_previousPlayer is not null)
        {
            var fadeOut = CreateTween();
            fadeOut.TweenProperty(_previousPlayer, "volume_db", -80f, CrossfadeSeconds);
            var capturedPrev = _previousPlayer;
            fadeOut.Finished += () => capturedPrev?.QueueFree();
            _previousPlayer = null;
        }
    }

    private void FadeOutActive()
    {
        if (_activePlayer is null) return;
        var fadeOut = CreateTween();
        fadeOut.TweenProperty(_activePlayer, "volume_db", -80f, CrossfadeSeconds);
        var captured = _activePlayer;
        fadeOut.Finished += () => captured?.QueueFree();
        _activePlayer = null;
    }
}
