using System;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Karma.Net;
using Karma.Voice;

namespace Karma.Audio;

public partial class NpcTextToSpeechController : Node
{
    private const int VoicePlaybackMixRate = 48000;
    private const float VoicePlaybackBufferLengthSeconds = 2.0f;
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private PrototypeServerSession _serverSession;
    private AudioStreamPlayer _fallbackPlayer;
    private AudioStreamGeneratorPlayback _voicePlayback;
    private Vector2[] _voiceFrames = Array.Empty<Vector2>();
    private int _voiceFrameIndex;
    private int _voicePlaybackCapacityFrames;
    private string _lastSpokenEventKey = string.Empty;
    private bool _warnedUnavailable;
    private string _resolvedProfileRequest = string.Empty;
    private VoiceboxResolvedProfile _resolvedProfile;

    [Export]
    public bool Enabled { get; set; } = true;

    [Export]
    public int Volume { get; set; } = 85;

    [Export]
    public int Rate { get; set; } = -1;

    [Export]
    public bool AlwaysPlayInEngineFallback { get; set; } = true;

    [Export]
    public bool UseVoiceboxApi { get; set; } = true;

    [Export]
    public string VoiceboxBaseUrl { get; set; } = "http://127.0.0.1:17493";

    [Export]
    public string VoiceboxProfile { get; set; } = "Deep Voice";

    [Export]
    public int VoiceboxLeadTrimMs { get; set; } = 120;

    public override void _Ready()
    {
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (_serverSession is null)
        {
            GD.PushWarning("NPC TTS is disabled because PrototypeServerSession was not found.");
            return;
        }
        _fallbackPlayer = new AudioStreamPlayer
        {
            Name = "NpcTtsFallbackPlayer",
            VolumeDb = PercentToDb(Volume),
            Bus = "Master"
        };
        AddChild(_fallbackPlayer);

        StopAnySystemTts();

        _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
    }

    public override void _ExitTree()
    {
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged -= OnLocalSnapshotChanged;
        }

        StopAnySystemTts();
    }

    public override void _Process(double delta)
    {
        PumpVoicePlayback();
    }

    public static string BuildUtterance(string npcName, string line)
    {
        var cleanedLine = CleanForSpeech(line);
        if (string.IsNullOrWhiteSpace(cleanedLine))
        {
            return string.Empty;
        }

        return cleanedLine;
    }

    public static string CleanForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var collapsed = Whitespace.Replace(text, " ").Trim();
        return collapsed.Length <= 320
            ? collapsed
            : collapsed[..317] + "...";
    }

    private void OnLocalSnapshotChanged(string _)
    {
        if (!Enabled || _serverSession?.LastLocalSnapshot is not { } snapshot)
        {
            return;
        }

        foreach (var serverEvent in snapshot.ServerEvents)
        {
            var lineKey = serverEvent.EventId.Contains("dialogue_started")
                ? "npcPrompt"
                : serverEvent.EventId.Contains("dialogue_choice_selected")
                    ? "npcResponse"
                    : string.Empty;
            if (string.IsNullOrEmpty(lineKey) ||
                !serverEvent.Data.TryGetValue(lineKey, out var line))
            {
                continue;
            }

            var eventKey = $"{serverEvent.Tick}:{serverEvent.EventId}:{lineKey}";
            if (eventKey == _lastSpokenEventKey)
            {
                continue;
            }

            serverEvent.Data.TryGetValue("npcName", out var npcName);
            SpeakNpcLine(npcName, line);
            _lastSpokenEventKey = eventKey;
        }
    }

    public void DebugSpeakNow(string npcName, string line)
    {
        SpeakNpcLine(npcName, line);
    }

    private void SpeakNpcLine(string npcName, string line)
    {
        var utterance = BuildUtterance(npcName, line);
        if (string.IsNullOrWhiteSpace(utterance))
        {
            return;
        }

        if (UseVoiceboxApi)
        {
            StopAnySystemTts();
            _ = SpeakWithVoiceboxAsync(utterance);
            return;
        }
        PlayFallbackUtteranceTone(utterance);
    }

    private async Task SpeakWithVoiceboxAsync(string utterance)
    {
        try
        {
            var baseUrl = (VoiceboxBaseUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Voicebox base URL is empty.");
            }

            var resolvedProfile = await ResolveProfileAsync(baseUrl);

            var speakBody = JsonSerializer.Serialize(new
            {
                text = utterance,
                profile = resolvedProfile.Id
            });
            var speakResp = await Http.PostAsync(
                $"{baseUrl}/speak",
                new StringContent(speakBody, Encoding.UTF8, "application/json"));
            speakResp.EnsureSuccessStatusCode();
            var speakJson = await speakResp.Content.ReadAsStringAsync();
            var generationId = ParseGenerationId(speakJson);
            if (string.IsNullOrWhiteSpace(generationId))
            {
                throw new InvalidOperationException("Voicebox /speak did not return a generation id.");
            }

            var completed = false;
            for (var i = 0; i < 80; i++)
            {
                await Task.Delay(250);
                var statusResp = await Http.GetAsync($"{baseUrl}/generate/{generationId}/status");
                statusResp.EnsureSuccessStatusCode();
                var statusRaw = await statusResp.Content.ReadAsStringAsync();
                var status = ParseGenerationStatus(statusRaw);
                if (status == "completed")
                {
                    completed = true;
                    break;
                }
                if (status == "failed")
                {
                    throw new InvalidOperationException("Voicebox generation failed.");
                }
            }
            if (!completed)
            {
                throw new TimeoutException("Voicebox generation timed out.");
            }

            var audioBytes = await Http.GetByteArrayAsync($"{baseUrl}/audio/{generationId}");
            if (audioBytes is null || audioBytes.Length == 0)
            {
                throw new InvalidOperationException("Voicebox audio download was empty.");
            }

            CallDeferred(nameof(BeginVoicePlayback), audioBytes);
        }
        catch (Exception ex)
        {
            WarnUnavailable($"Voicebox TTS failed: {ex.Message}");
            if (AlwaysPlayInEngineFallback)
            {
                CallDeferred(MethodName.PlayFallbackUtteranceTone, utterance);
            }
        }
    }

    private static string ParseGenerationId(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id)
            ? id.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string ParseGenerationStatus(string statusPayload)
    {
        // /generate/{id}/status can return plain JSON or SSE-ish "data: {...}".
        var trimmed = (statusPayload ?? string.Empty).Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var lines = trimmed
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (lines.Length > 0)
            {
                trimmed = lines[^1];
            }

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                trimmed = trimmed[start..(end + 1)];
            }
        }

        using var doc = JsonDocument.Parse(trimmed);
        return doc.RootElement.TryGetProperty("status", out var status)
            ? status.GetString() ?? string.Empty
            : string.Empty;
    }

    private void BeginVoicePlayback(byte[] wavFileBytes)
    {
        var frames = BuildVoiceFrames(wavFileBytes, VoiceboxLeadTrimMs / 1000f);
        if (frames is null || frames.Length == 0)
        {
            WarnUnavailable("Voicebox audio could not be decoded for generator playback.");
            return;
        }

        var generator = _fallbackPlayer.Stream as AudioStreamGenerator;
        if (generator is null || generator.MixRate != VoicePlaybackMixRate)
        {
            generator = new AudioStreamGenerator
            {
                MixRate = VoicePlaybackMixRate,
                BufferLength = VoicePlaybackBufferLengthSeconds
            };
            _fallbackPlayer.Stream = generator;
            _voicePlaybackCapacityFrames = Mathf.CeilToInt(generator.MixRate * generator.BufferLength);
        }
        else if (_voicePlaybackCapacityFrames <= 0)
        {
            _voicePlaybackCapacityFrames = Mathf.CeilToInt(generator.MixRate * generator.BufferLength);
        }

        _fallbackPlayer.Stop();
        _fallbackPlayer.VolumeDb = PercentToDb(Volume);
        _voiceFrames = frames;
        _voiceFrameIndex = 0;
        _fallbackPlayer.Play();
        _voicePlayback = _fallbackPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        PumpVoicePlayback();
    }

    private async Task<VoiceboxResolvedProfile> ResolveProfileAsync(string baseUrl)
    {
        var requestedProfile = VoiceboxProfile ?? string.Empty;
        if (_resolvedProfile is not null &&
            string.Equals(_resolvedProfileRequest, requestedProfile, StringComparison.Ordinal))
        {
            return _resolvedProfile;
        }

        _resolvedProfile = await VoiceboxProfileResolver.ResolveAsync(baseUrl, requestedProfile);
        _resolvedProfileRequest = requestedProfile;
        return _resolvedProfile;
    }

    private static Vector2[] BuildVoiceFrames(byte[] wavFileBytes, float leadTrimSeconds)
    {
        if (wavFileBytes is null || wavFileBytes.Length < 44)
        {
            return null;
        }

        using var ms = new MemoryStream(wavFileBytes);
        using var br = new BinaryReader(ms);
        if (new string(br.ReadChars(4)) != "RIFF")
        {
            return null;
        }
        br.ReadInt32(); // file size
        if (new string(br.ReadChars(4)) != "WAVE")
        {
            return null;
        }

        short channels = 0;
        int sampleRate = 0;
        short bitsPerSample = 0;
        byte[] pcmData = null;

        while (ms.Position + 8 <= ms.Length)
        {
            var chunkId = new string(br.ReadChars(4));
            var chunkSize = br.ReadInt32();
            if (chunkSize < 0 || ms.Position + chunkSize > ms.Length)
            {
                return null;
            }

            if (chunkId == "fmt ")
            {
                var format = br.ReadInt16();
                channels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32(); // byteRate
                br.ReadInt16(); // blockAlign
                bitsPerSample = br.ReadInt16();
                if (chunkSize > 16)
                {
                    br.ReadBytes(chunkSize - 16);
                }
                if (format != 1) // PCM
                {
                    return null;
                }
            }
            else if (chunkId == "data")
            {
                pcmData = br.ReadBytes(chunkSize);
            }
            else
            {
                br.ReadBytes(chunkSize);
            }

            if ((chunkSize & 1) == 1 && ms.Position < ms.Length)
            {
                br.ReadByte(); // pad byte
            }
        }

        if (pcmData is null || sampleRate <= 0 || channels is not (1 or 2))
        {
            return null;
        }

        pcmData = TrimLeadingPcmFrames(pcmData, bitsPerSample, channels, sampleRate, leadTrimSeconds);
        pcmData = SmoothPcmEdges(pcmData, bitsPerSample, channels, sampleRate);

        var playableData = ConvertToStereo16BitPcm(pcmData, bitsPerSample, channels, sampleRate, out var playableRate);
        if (playableData is null || playableData.Length == 0 || playableRate <= 0)
        {
            return null;
        }

        return ConvertStereo16BitPcmToFrames(playableData);
    }

    private static float PercentToDb(int volumePercent)
    {
        var clamped = Math.Clamp(volumePercent, 0, 100) / 100f;
        return clamped <= 0.0001f
            ? -80f
            : Mathf.LinearToDb(clamped);
    }

    private static byte[] TrimLeadingPcmFrames(byte[] pcmData, short bitsPerSample, short channels, int sampleRate, float trimSeconds)
    {
        if (pcmData is null || pcmData.Length == 0 || channels <= 0 || sampleRate <= 0 || trimSeconds <= 0f)
        {
            return pcmData;
        }

        var bytesPerSample = bitsPerSample switch
        {
            8 => 1,
            16 => 2,
            _ => 0
        };
        if (bytesPerSample == 0)
        {
            return pcmData;
        }

        var bytesPerFrame = bytesPerSample * channels;
        if (bytesPerFrame <= 0 || pcmData.Length <= bytesPerFrame)
        {
            return pcmData;
        }

        var trimFrames = Math.Max(0, (int)(sampleRate * trimSeconds));
        var trimBytes = Math.Min(pcmData.Length - bytesPerFrame, trimFrames * bytesPerFrame);
        if (trimBytes <= 0)
        {
            return pcmData;
        }

        var trimmed = new byte[pcmData.Length - trimBytes];
        Buffer.BlockCopy(pcmData, trimBytes, trimmed, 0, trimmed.Length);
        return trimmed;
    }

    private static byte[] SmoothPcmEdges(byte[] pcmData, short bitsPerSample, short channels, int sampleRate)
    {
        if (pcmData is null || pcmData.Length == 0 || channels <= 0 || sampleRate <= 0)
        {
            return pcmData;
        }

        return bitsPerSample switch
        {
            16 => Smooth16BitPcmEdges(pcmData, channels, sampleRate),
            8 => Smooth8BitPcmEdges(pcmData, channels, sampleRate),
            _ => pcmData
        };
    }

    private static byte[] Smooth16BitPcmEdges(byte[] pcmData, short channels, int sampleRate)
    {
        var bytesPerFrame = sizeof(short) * channels;
        if (bytesPerFrame <= 0 || pcmData.Length < bytesPerFrame)
        {
            return pcmData;
        }

        var frameCount = pcmData.Length / bytesPerFrame;
        var rampFrames = Math.Min(frameCount / 2, Math.Max(1, (int)(sampleRate * 0.008f)));
        if (rampFrames <= 1)
        {
            return pcmData;
        }

        var smoothed = new byte[pcmData.Length];
        Buffer.BlockCopy(pcmData, 0, smoothed, 0, pcmData.Length);

        for (var frame = 0; frame < frameCount; frame++)
        {
            float gain = 1f;
            if (frame < rampFrames)
            {
                gain = frame / (float)rampFrames;
            }
            else if (frame >= frameCount - rampFrames)
            {
                gain = (frameCount - 1 - frame) / (float)rampFrames;
            }

            if (gain >= 0.999f)
            {
                continue;
            }

            var clampedGain = Math.Clamp(gain, 0f, 1f);
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = frame * bytesPerFrame + (channel * sizeof(short));
                var sample = BitConverter.ToInt16(smoothed, offset);
                var scaled = (short)Math.Clamp((int)Math.Round(sample * clampedGain), short.MinValue, short.MaxValue);
                var bytes = BitConverter.GetBytes(scaled);
                smoothed[offset] = bytes[0];
                smoothed[offset + 1] = bytes[1];
            }
        }

        return smoothed;
    }

    private static byte[] Smooth8BitPcmEdges(byte[] pcmData, short channels, int sampleRate)
    {
        var bytesPerFrame = channels;
        if (bytesPerFrame <= 0 || pcmData.Length < bytesPerFrame)
        {
            return pcmData;
        }

        var frameCount = pcmData.Length / bytesPerFrame;
        var rampFrames = Math.Min(frameCount / 2, Math.Max(1, (int)(sampleRate * 0.008f)));
        if (rampFrames <= 1)
        {
            return pcmData;
        }

        var smoothed = new byte[pcmData.Length];
        Buffer.BlockCopy(pcmData, 0, smoothed, 0, pcmData.Length);

        for (var frame = 0; frame < frameCount; frame++)
        {
            float gain = 1f;
            if (frame < rampFrames)
            {
                gain = frame / (float)rampFrames;
            }
            else if (frame >= frameCount - rampFrames)
            {
                gain = (frameCount - 1 - frame) / (float)rampFrames;
            }

            if (gain >= 0.999f)
            {
                continue;
            }

            var clampedGain = Math.Clamp(gain, 0f, 1f);
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = frame * bytesPerFrame + channel;
                var centered = smoothed[offset] - 128;
                var scaled = (int)Math.Round(centered * clampedGain) + 128;
                smoothed[offset] = (byte)Math.Clamp(scaled, byte.MinValue, byte.MaxValue);
            }
        }

        return smoothed;
    }

    private static byte[] ConvertToStereo16BitPcm(byte[] pcmData, short bitsPerSample, short channels, int sampleRate, out int targetRate)
    {
        targetRate = VoicePlaybackMixRate;
        if (pcmData is null || pcmData.Length == 0 || channels is not (1 or 2) || sampleRate <= 0)
        {
            return null;
        }

        var frameCount = bitsPerSample switch
        {
            8 => pcmData.Length / channels,
            16 => pcmData.Length / (channels * sizeof(short)),
            _ => 0
        };
        if (frameCount <= 0)
        {
            return null;
        }

        var left = new float[frameCount];
        var right = new float[frameCount];
        if (bitsPerSample == 16)
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                var baseOffset = frame * channels * sizeof(short);
                left[frame] = BitConverter.ToInt16(pcmData, baseOffset) / 32768f;
                right[frame] = channels == 2
                    ? BitConverter.ToInt16(pcmData, baseOffset + sizeof(short)) / 32768f
                    : left[frame];
            }
        }
        else if (bitsPerSample == 8)
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                var baseOffset = frame * channels;
                left[frame] = (pcmData[baseOffset] - 128) / 128f;
                right[frame] = channels == 2
                    ? (pcmData[baseOffset + 1] - 128) / 128f
                    : left[frame];
            }
        }
        else
        {
            return null;
        }

        var resampledLeft = ResampleLinear(left, sampleRate, targetRate);
        var resampledRight = ResampleLinear(right, sampleRate, targetRate);
        var output = new byte[resampledLeft.Length * sizeof(short) * 2];
        for (var i = 0; i < resampledLeft.Length; i++)
        {
            var leftSample = (short)Math.Clamp((int)Math.Round(resampledLeft[i] * short.MaxValue), short.MinValue, short.MaxValue);
            var rightSample = (short)Math.Clamp((int)Math.Round(resampledRight[i] * short.MaxValue), short.MinValue, short.MaxValue);
            var leftBytes = BitConverter.GetBytes(leftSample);
            var rightBytes = BitConverter.GetBytes(rightSample);
            var offset = i * sizeof(short) * 2;
            output[offset] = leftBytes[0];
            output[offset + 1] = leftBytes[1];
            output[offset + 2] = rightBytes[0];
            output[offset + 3] = rightBytes[1];
        }

        return output;
    }

    private static Vector2[] ConvertStereo16BitPcmToFrames(byte[] pcmData)
    {
        if (pcmData is null || pcmData.Length < 4)
        {
            return Array.Empty<Vector2>();
        }

        var frameCount = pcmData.Length / 4;
        var frames = new Vector2[frameCount];
        for (var i = 0; i < frameCount; i++)
        {
            var offset = i * 4;
            var left = BitConverter.ToInt16(pcmData, offset) / 32768f;
            var right = BitConverter.ToInt16(pcmData, offset + 2) / 32768f;
            frames[i] = new Vector2(left, right);
        }

        return frames;
    }

    private static float[] ResampleLinear(float[] source, int sourceRate, int targetRate)
    {
        if (source is null || source.Length == 0)
        {
            return Array.Empty<float>();
        }

        if (sourceRate == targetRate)
        {
            var copy = new float[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        var targetLength = Math.Max(1, (int)Math.Round(source.Length * (targetRate / (double)sourceRate)));
        var result = new float[targetLength];
        var maxSourceIndex = source.Length - 1;
        for (var i = 0; i < targetLength; i++)
        {
            var sourceIndex = i * (sourceRate / (double)targetRate);
            var leftIndex = Math.Clamp((int)Math.Floor(sourceIndex), 0, maxSourceIndex);
            var rightIndex = Math.Min(leftIndex + 1, maxSourceIndex);
            var mix = (float)(sourceIndex - leftIndex);
            result[i] = Mathf.Lerp(source[leftIndex], source[rightIndex], mix);
        }

        return result;
    }

    private void StopAnySystemTts()
    {
        try
        {
            DisplayServer.TtsStop();
        }
        catch
        {
            // Best effort; not all platforms/backends support this consistently.
        }
    }

    private void PumpVoicePlayback()
    {
        if (_voicePlayback is null || _voiceFrames.Length == 0)
        {
            return;
        }

        var framesAvailable = _voicePlayback.GetFramesAvailable();
        while (framesAvailable > 0 && _voiceFrameIndex < _voiceFrames.Length)
        {
            _voicePlayback.PushFrame(_voiceFrames[_voiceFrameIndex]);
            _voiceFrameIndex++;
            framesAvailable--;
        }

        if (_voiceFrameIndex >= _voiceFrames.Length &&
            framesAvailable >= _voicePlaybackCapacityFrames - 1)
        {
            _fallbackPlayer.Stop();
            _voicePlayback = null;
            _voiceFrames = Array.Empty<Vector2>();
            _voiceFrameIndex = 0;
        }
    }

    private void PlayFallbackUtteranceTone(string utterance)
    {
        if (_fallbackPlayer is null)
        {
            return;
        }

        _voicePlayback = null;
        _voiceFrames = Array.Empty<Vector2>();
        _voiceFrameIndex = 0;

        var sampleRate = 44100;
        var perChar = 0.028f;
        var duration = Math.Clamp(utterance.Length * perChar, 0.35f, 2.2f);
        var frameCount = Math.Max(1, (int)(sampleRate * duration));
        var data = new byte[frameCount * sizeof(short) * 2];
        var rampFrames = Math.Max(1, (int)(sampleRate * 0.01f));
        var chars = utterance.ToCharArray();

        for (var i = 0; i < frameCount; i++)
        {
            var progress = i / (float)frameCount;
            var charIndex = Math.Min(chars.Length - 1, (int)(progress * chars.Length));
            var ch = chars[charIndex];
            var baseHz = 110f + ((ch % 24) * 6f);
            var overtoneHz = baseHz * 1.45f;
            var t = i / (float)sampleRate;

            var sampleFloat =
                MathF.Sin(2f * MathF.PI * baseHz * t) * 0.21f +
                MathF.Sin(2f * MathF.PI * overtoneHz * t) * 0.05f;

            if (i < rampFrames)
            {
                sampleFloat *= i / (float)rampFrames;
            }
            else if (i > frameCount - rampFrames)
            {
                sampleFloat *= (frameCount - i) / (float)rampFrames;
            }

            var pcm = (short)Math.Clamp((int)(sampleFloat * short.MaxValue), short.MinValue, short.MaxValue);
            var bytes = BitConverter.GetBytes(pcm);
            Buffer.BlockCopy(bytes, 0, data, i * 4, 2);
            Buffer.BlockCopy(bytes, 0, data, i * 4 + 2, 2);
        }

        _fallbackPlayer.Stream = new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = true,
            Data = data
        };
        _fallbackPlayer.Play();
    }

    private void WarnUnavailable(string message)
    {
        if (_warnedUnavailable)
        {
            return;
        }

        _warnedUnavailable = true;
        GD.PushWarning(message);
    }
}
