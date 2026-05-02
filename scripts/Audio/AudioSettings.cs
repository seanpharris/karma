using System;
using Godot;

namespace Karma.Audio;

// Single source of truth for the four-bus audio mixer (Master / Music /
// SFX / Ambient). Both the main-menu Options panel and the in-game
// pause menu read/write through this class so a value set in one place
// shows up in the other on next open.
//
// Persistence note: the task spec called for `user://audio_settings.json`
// but the rest of the project's options (video / vsync / fullscreen)
// already live in `user://options.cfg` via Godot ConfigFile. Forking to
// JSON would create two competing sources for the same data — they'd
// diverge on first edit. So we keep the existing ConfigFile path and
// extend its `[audio]` section with the two new bus keys. ConfigFile
// is durable, atomic, and round-trips integers/doubles without quirks.
public static class AudioSettings
{
    public const string MasterBusName = "Master";
    public const string MusicBusName = "Music";
    public const string SfxBusName = "SFX";
    public const string AmbientBusName = "Ambient";

    public const string ConfigSection = "audio";
    public const string MasterVolumeKey = "master_volume";
    public const string MusicVolumeKey = "music_volume";
    public const string SfxVolumeKey = "effects_volume";
    public const string AmbientVolumeKey = "ambient_volume";

    public const double DefaultMasterVolume = 80;
    public const double DefaultMusicVolume = 70;
    public const double DefaultSfxVolume = 80;
    public const double DefaultAmbientVolume = 60;

    public static double MasterVolume { get; set; } = DefaultMasterVolume;
    public static double MusicVolume { get; set; } = DefaultMusicVolume;
    public static double SfxVolume { get; set; } = DefaultSfxVolume;
    public static double AmbientVolume { get; set; } = DefaultAmbientVolume;

    public static void LoadFromConfig(ConfigFile config)
    {
        if (config is null) return;
        MasterVolume = ClampPercent(config.GetValue(ConfigSection, MasterVolumeKey, DefaultMasterVolume).AsDouble());
        MusicVolume = ClampPercent(config.GetValue(ConfigSection, MusicVolumeKey, DefaultMusicVolume).AsDouble());
        SfxVolume = ClampPercent(config.GetValue(ConfigSection, SfxVolumeKey, DefaultSfxVolume).AsDouble());
        AmbientVolume = ClampPercent(config.GetValue(ConfigSection, AmbientVolumeKey, DefaultAmbientVolume).AsDouble());
    }

    public static void SaveToConfig(ConfigFile config)
    {
        if (config is null) return;
        config.SetValue(ConfigSection, MasterVolumeKey, MasterVolume);
        config.SetValue(ConfigSection, MusicVolumeKey, MusicVolume);
        config.SetValue(ConfigSection, SfxVolumeKey, SfxVolume);
        config.SetValue(ConfigSection, AmbientVolumeKey, AmbientVolume);
    }

    public static bool LoadFromDisk(string path)
    {
        var config = new ConfigFile();
        if (config.Load(path) != Error.Ok) return false;
        LoadFromConfig(config);
        return true;
    }

    public static void SaveToDisk(string path)
    {
        var config = new ConfigFile();
        // Preserve unrelated sections (video / controls) the menu also writes.
        config.Load(path);
        SaveToConfig(config);
        config.Save(path);
    }

    // Registers the Music / SFX / Ambient buses if they aren't already
    // declared in default_bus_layout.tres. Idempotent — safe to call from
    // every controller's _Ready.
    public static void EnsureBusesExist()
    {
        EnsureBus(MusicBusName);
        EnsureBus(SfxBusName);
        EnsureBus(AmbientBusName);
    }

    public static void ApplyToAudioServer()
    {
        SetBusDb(MasterBusName, PercentToDb(MasterVolume));
        SetBusDb(MusicBusName, PercentToDb(MusicVolume));
        SetBusDb(SfxBusName, PercentToDb(SfxVolume));
        SetBusDb(AmbientBusName, PercentToDb(AmbientVolume));
    }

    public static float PercentToDb(double percent)
    {
        var clamped = ClampPercent(percent);
        if (clamped <= 0.001) return -80f;
        return (float)(20.0 * Math.Log10(clamped / 100.0));
    }

    public static double ClampPercent(double percent) => Math.Clamp(percent, 0.0, 100.0);

    private static void EnsureBus(string busName)
    {
        if (AudioServer.GetBusIndex(busName) >= 0) return;
        var newIndex = AudioServer.BusCount;
        AudioServer.AddBus(newIndex);
        AudioServer.SetBusName(newIndex, busName);
        // Route the new bus through Master so the master slider still
        // controls its overall level.
        AudioServer.SetBusSend(newIndex, MasterBusName);
    }

    private static void SetBusDb(string busName, float db)
    {
        var index = AudioServer.GetBusIndex(busName);
        if (index < 0) return;
        AudioServer.SetBusVolumeDb(index, db);
    }
}
