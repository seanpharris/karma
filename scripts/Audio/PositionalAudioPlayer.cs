using Godot;

namespace Karma.Audio;

// Tile-aware wrapper around AudioStreamPlayer2D. Resolves a clip path
// from AudioEventCatalog and a falloff profile from AudioFalloffRegistry,
// then spawns a one-shot AudioStreamPlayer2D at the supplied world
// position with linear distance attenuation. The player auto-frees when
// playback finishes so call sites don't have to manage lifetime.
public static class PositionalAudioPlayer
{
    public const float TilePixelSize = 32f;
    public const string SfxBus = AudioSettings.SfxBusName;

    // Spawn a one-shot positional clip for the given event id. Returns
    // the spawned player (or null when the clip is missing on disk so the
    // call site can no-op cleanly).
    public static AudioStreamPlayer2D PlayEventAt(
        Node parent,
        string eventId,
        Vector2 worldPosition,
        float volumeDb = 0f)
    {
        if (parent is null || string.IsNullOrEmpty(eventId)) return null;
        var clipPath = AudioEventCatalog.Resolve(eventId);
        if (string.IsNullOrEmpty(clipPath) || !FileAccess.FileExists(clipPath)) return null;
        var stream = ResourceLoader.Load<AudioStream>(clipPath);
        if (stream is null) return null;
        var profile = AudioFalloffRegistry.Resolve(eventId);
        var player = BuildPlayer(stream, worldPosition, profile, volumeDb);
        parent.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
        return player;
    }

    public static AudioStreamPlayer2D BuildPlayer(
        AudioStream stream,
        Vector2 worldPosition,
        AudioFalloffProfile profile,
        float volumeDb = 0f)
    {
        // AudioStreamPlayer2D doesn't expose a per-source low-pass like the 3D
        // variant, so AttenuationCutoffHz from the profile is reserved for a
        // future bus-level effect. MaxDistance + Attenuation curve is enough
        // for the prototype's falloff feel.
        return new AudioStreamPlayer2D
        {
            Name = "PositionalAudioPlayer",
            Stream = stream,
            Position = worldPosition,
            Bus = SfxBus,
            VolumeDb = volumeDb,
            MaxDistance = profile.MaxDistanceTiles * TilePixelSize,
            Attenuation = 1.0f
        };
    }

    public static Vector2 TileCenterToWorld(int tileX, int tileY)
    {
        return new Vector2(
            (tileX + 0.5f) * TilePixelSize,
            (tileY + 0.5f) * TilePixelSize);
    }
}
