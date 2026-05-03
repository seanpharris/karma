using System;
using System.Collections.Generic;
using Godot;

namespace Karma.Audio;

// Background music for the gameplay scenes. When playable audio files are
// present in MusicDirectory the player walks them as a sequential
// playlist — each track plays in full, then advances; the playlist loops
// once the last track ends. When the directory is empty (or only carries
// the main-menu theme) it falls back to procedurally-synthesized
// per-Theme samples so scenes always have *something* to hear.
public partial class MusicPlayer : Node
{
    public enum Theme
    {
        // Calm exploratory pad for sandbox / Main scene.
        SandboxCalm,
        // Driving mid-tempo loop for the In-Game Event Prototype.
        EventTension,
        // Slow-building atmospheric pad for the Event Playback scenarios.
        ScenarioAmbient
    }

    public const string MusicDirectory = "res://assets/audio/music/themes/medieval/";
    // The menu controller owns these — never queue them into the gameplay playlist.
    public const string MenuPlaceholderFileName = "main_menu_theme_placeholder.wav";
    public const string TravellingOnMedievalFileName = "kaazoom-travelling-on-medieval-celtic-rpg-game-music-434717.mp3";

    [Export] public Theme MusicTheme { get; set; } = Theme.SandboxCalm;
    [Export] public float Volume { get; set; } = 0.45f;

    private AudioStreamPlayer _player;
    private AudioStreamGeneratorPlayback _playback;
    private double _time;
    private List<AudioStream> _playlist = new();
    private int _playlistIndex;

    public bool IsPlaylistMode => _playlist.Count > 0;
    public int PlaylistTrackCount => _playlist.Count;

    public override void _Ready()
    {
        _player = new AudioStreamPlayer
        {
            Name = "MusicStreamPlayer",
            Bus = AudioSettings.MusicBusName,
            VolumeDb = LinearToDb(Volume)
        };
        AddChild(_player);

        _playlist = LoadPlaylist(MusicDirectory);
        if (_playlist.Count > 0)
        {
            _player.Finished += AdvancePlaylist;
            PlayCurrentTrack();
            return;
        }

        _player.Stream = new AudioStreamGenerator
        {
            MixRate = 44100,
            BufferLength = 2.0f
        };
        _player.Play();
        _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        FillBuffer();
    }

    public override void _Process(double delta)
    {
        if (_playback is null) return;
        FillBuffer();
    }

    public void SetVolume(float volume)
    {
        Volume = Mathf.Clamp(volume, 0f, 1f);
        if (_player is not null) _player.VolumeDb = LinearToDb(Volume);
    }

    public static List<AudioStream> LoadPlaylist(string directory)
    {
        var result = new List<AudioStream>();
        foreach (var fileName in ListPlayableFiles(directory))
        {
            var path = directory + fileName;
            var stream = LoadPlayableAudio(path);
            if (stream is null) continue;
            // MP3 / OGG imports default to no-loop, but a stray `loop=true`
            // import setting would freeze the playlist on track 0 — clamp it.
            DisableSingleTrackLoop(stream);
            result.Add(stream);
        }
        return result;
    }

    public static IReadOnlyList<string> ListPlayableFiles(string directory)
    {
        var dir = DirAccess.Open(directory);
        if (dir is null) return Array.Empty<string>();
        var raw = dir.GetFiles();
        if (raw is null || raw.Length == 0) return Array.Empty<string>();
        var filtered = new List<string>();
        foreach (var name in raw)
        {
            // Menu owns its own theme; skip both the placeholder and the
            // travelling-on-medieval track so gameplay doesn't double up.
            if (string.Equals(name, MenuPlaceholderFileName, StringComparison.Ordinal)) continue;
            if (string.Equals(name, TravellingOnMedievalFileName, StringComparison.Ordinal)) continue;
            if (!IsPlayableAudioFile(name)) continue;
            filtered.Add(name);
        }
        filtered.Sort(StringComparer.Ordinal);
        return filtered;
    }

    public static bool IsPlayableAudioFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        var lower = fileName.ToLowerInvariant();
        return lower.EndsWith(".mp3") || lower.EndsWith(".ogg") || lower.EndsWith(".wav");
    }

    public static AudioStream LoadPlayableAudio(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
            return null;

        if (ResourceLoader.Exists(path))
        {
            var imported = ResourceLoader.Load<AudioStream>(path);
            if (imported is not null)
                return imported;
        }

        var lower = path.ToLowerInvariant();
        if (lower.EndsWith(".mp3"))
        {
            var data = FileAccess.GetFileAsBytes(path);
            return data is null || data.Length == 0
                ? null
                : new AudioStreamMP3 { Data = data };
        }

        return null;
    }

    private void PlayCurrentTrack()
    {
        if (_playlist.Count == 0) return;
        if (_playlistIndex < 0 || _playlistIndex >= _playlist.Count) _playlistIndex = 0;
        _player.Stream = _playlist[_playlistIndex];
        _player.Play();
    }

    private void AdvancePlaylist()
    {
        if (_playlist.Count == 0) return;
        _playlistIndex = (_playlistIndex + 1) % _playlist.Count;
        PlayCurrentTrack();
    }

    private static void DisableSingleTrackLoop(AudioStream stream)
    {
        switch (stream)
        {
            case AudioStreamMP3 mp3:
                mp3.Loop = false;
                break;
            case AudioStreamOggVorbis ogg:
                ogg.Loop = false;
                break;
            case AudioStreamWav wav:
                wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
                break;
        }
    }

    private static float LinearToDb(float volume)
    {
        if (volume <= 0.0001f) return -80f;
        return Mathf.LinearToDb(volume);
    }

    private void FillBuffer()
    {
        if (_playback is null) return;
        const double sampleRate = 44100.0;
        var frames = _playback.GetFramesAvailable();
        for (var i = 0; i < frames; i++)
        {
            var sample = GenerateSample(MusicTheme, _time);
            _playback.PushFrame(new Vector2(sample, sample));
            _time += 1.0 / sampleRate;
        }
    }

    public static float GenerateSample(Theme theme, double time)
    {
        return theme switch
        {
            Theme.SandboxCalm => SandboxCalmSample(time),
            Theme.EventTension => EventTensionSample(time),
            Theme.ScenarioAmbient => ScenarioAmbientSample(time),
            _ => 0f
        };
    }

    private static float SandboxCalmSample(double time)
    {
        // 96 BPM, minor pentatonic over a slow root. Calm sandbox vibe.
        const double bpm = 96.0;
        const int beats = 16;
        var beatLength = 60.0 / bpm;
        var beat = (int)(time / beatLength) % beats;
        var within = time - Math.Floor(time / beatLength) * beatLength;
        int[] melody = { 0, 3, 5, 0, 3, 5, 7, 5, 0, 3, 5, 7, 10, 7, 5, 3 };
        int[] bass = { 0, 0, 0, 0, 5, 5, 5, 5, 7, 7, 7, 7, 5, 5, 0, 0 };
        var lead = Sine(NoteFrequency(196.0, melody[beat]) * time)
                   * Envelope(within, beatLength * 0.78) * 0.22;
        var pad = Sine(NoteFrequency(98.0, bass[beat]) * time)
                  * Envelope(within, beatLength * 1.6) * 0.18;
        var sub = Sine(NoteFrequency(49.0, bass[beat]) * time) * 0.07;
        return (float)Math.Clamp(lead + pad + sub, -1.0, 1.0);
    }

    private static float EventTensionSample(double time)
    {
        // 128 BPM, walking bass + sparse high stab. Drives event-prototype urgency.
        const double bpm = 128.0;
        const int beats = 8;
        var beatLength = 60.0 / bpm;
        var beat = (int)(time / beatLength) % beats;
        var within = time - Math.Floor(time / beatLength) * beatLength;
        int[] bassLine = { 0, 5, 7, 0, 3, 7, 8, 5 };
        int[] stab = { 12, 0, 0, 12, 15, 0, 17, 15 };
        var bass = Saw(NoteFrequency(110.0, bassLine[beat] - 12) * time)
                   * Envelope(within, beatLength * 0.85) * 0.26;
        var stabWave = stab[beat] == 0
            ? 0.0
            : Triangle(NoteFrequency(220.0, stab[beat]) * time)
                * Envelope(within, beatLength * 0.45) * 0.18;
        var pulse = within < 0.01
            ? Math.Sin(2.0 * Math.PI * 2200.0 * time) * (1.0 - within / 0.01) * 0.05
            : 0.0;
        return (float)Math.Clamp(bass + stabWave + pulse, -1.0, 1.0);
    }

    private static float ScenarioAmbientSample(double time)
    {
        // Slow detuned pad over evolving root; no obvious beat. Atmospheric.
        var slow = 1.0 + 0.4 * Math.Sin(time * 0.07);
        var rootShift = (int)(time / 12.0) % 4;
        int[] roots = { 0, 5, 7, 3 };
        var root = NoteFrequency(110.0, roots[rootShift]);
        var pad1 = Sine(root * 0.5 * time) * 0.18;
        var pad2 = Sine(root * 0.5 * time * 1.005) * 0.16;
        var pad3 = Sine(root * 1.0 * time * 0.997) * 0.12 * slow;
        var shimmer = Triangle(root * 4.0 * time) * 0.05 * slow;
        return (float)Math.Clamp(pad1 + pad2 + pad3 + shimmer, -1.0, 1.0);
    }

    public static double NoteFrequency(double root, int semitone)
    {
        return root * Math.Pow(2.0, semitone / 12.0);
    }

    private static double Sine(double phase)
    {
        return Math.Sin(2.0 * Math.PI * phase);
    }

    private static double Triangle(double phase)
    {
        return 2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0;
    }

    private static double Saw(double phase)
    {
        return 2.0 * (phase - Math.Floor(phase + 0.5));
    }

    private static double Envelope(double time, double duration)
    {
        var attack = Math.Min(1.0, time / 0.025);
        var release = Math.Min(1.0, Math.Max(0.0, (duration - time) / 0.10));
        return Math.Min(attack, release);
    }
}
