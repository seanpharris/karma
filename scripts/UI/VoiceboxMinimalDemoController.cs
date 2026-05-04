using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Karma.Voice;

namespace Karma.UI;

public partial class VoiceboxMinimalDemoController : Control
{
    private const int PlaybackMixRate = 48000;
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    [Export]
    public string VoiceboxBaseUrl { get; set; } = "http://127.0.0.1:17493";

    [Export]
    public string VoiceboxProfile { get; set; } = "Deep Voice";

    [Export]
    public string DemoLine { get; set; } = "Well hello there, traveler.";

    [Export]
    public int VoiceboxLeadTrimMs { get; set; } = 120;

    [Export]
    public int VolumePercent { get; set; } = 38;

    private Label _statusLabel = null!;
    private LineEdit _lineEdit = null!;
    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback _playback;
    private Vector2[] _frames = Array.Empty<Vector2>();
    private int _frameIndex;
    private int _playbackCapacityFrames;
    private bool _requestInFlight;
    private string _resolvedProfileRequest = string.Empty;
    private VoiceboxResolvedProfile _resolvedProfile;

    public override void _Ready()
    {
        BuildUi();
        BuildAudio();
        _statusLabel.Text = "Click Play to request one Voicebox line.";
    }

    public override void _Process(double delta)
    {
        PumpPlayback();
    }

    private void BuildUi()
    {
        var backdrop = new ColorRect
        {
            Color = new Color("12171d"),
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        AddChild(backdrop);

        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -280f,
            OffsetTop = -120f,
            OffsetRight = 280f,
            OffsetBottom = 120f
        };
        AddChild(panel);

        var root = new VBoxContainer();
        panel.AddChild(root);

        var title = new Label { Text = "Voicebox Minimal Demo" };
        title.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(title);

        root.AddChild(new Label { Text = "One line, one button, one playback path." });

        _lineEdit = new LineEdit { Text = DemoLine, PlaceholderText = "Line to speak" };
        root.AddChild(_lineEdit);

        var playButton = new Button { Text = "Play Mara Line" };
        playButton.Pressed += OnPlayPressed;
        root.AddChild(playButton);

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_statusLabel);
    }

    private void BuildAudio()
    {
        _player = new AudioStreamPlayer
        {
            Name = "VoiceboxMinimalPlayer",
            Bus = "Master",
            VolumeDb = PercentToDb(VolumePercent)
        };
        _player.Stream = new AudioStreamGenerator
        {
            MixRate = PlaybackMixRate,
            BufferLength = 2.0f
        };
        AddChild(_player);
        _playbackCapacityFrames = Mathf.CeilToInt(PlaybackMixRate * 2.0f);
    }

    private void OnPlayPressed()
    {
        if (_requestInFlight)
        {
            _statusLabel.Text = "Voice request already running...";
            return;
        }

        _ = RequestAndPlayAsync(_lineEdit.Text);
    }

    private async Task RequestAndPlayAsync(string line)
    {
        var cleaned = CleanLine(line);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            _statusLabel.Text = "Enter a line first.";
            return;
        }

        _requestInFlight = true;
        _statusLabel.Text = $"Requesting Voicebox for: \"{cleaned}\"";

        try
        {
            var baseUrl = (VoiceboxBaseUrl ?? string.Empty).TrimEnd('/');
            var resolvedProfile = await ResolveProfileAsync(baseUrl);
            if (resolvedProfile.UsedFallback)
            {
                _statusLabel.Text =
                    $"Configured profile \"{VoiceboxProfile}\" was unavailable. Using \"{resolvedProfile.Name}\".";
            }

            var requestBody = JsonSerializer.Serialize(new
            {
                text = cleaned,
                profile = resolvedProfile.Id
            });

            var speakResponse = await Http.PostAsync(
                $"{baseUrl}/speak",
                new System.Net.Http.StringContent(requestBody, Encoding.UTF8, "application/json"));
            speakResponse.EnsureSuccessStatusCode();
            var generationId = ParseGenerationId(await speakResponse.Content.ReadAsStringAsync());
            if (string.IsNullOrWhiteSpace(generationId))
            {
                throw new InvalidOperationException("Voicebox did not return a generation id.");
            }

            for (var i = 0; i < 80; i++)
            {
                await Task.Delay(250);
                var statusResp = await Http.GetAsync($"{baseUrl}/generate/{generationId}/status");
                statusResp.EnsureSuccessStatusCode();
                var status = ParseGenerationStatus(await statusResp.Content.ReadAsStringAsync());
                if (status == "completed")
                {
                    break;
                }

                if (status == "failed")
                {
                    throw new InvalidOperationException("Voicebox generation failed.");
                }
            }

            var audioBytes = await Http.GetByteArrayAsync($"{baseUrl}/audio/{generationId}");
            CallDeferred(nameof(BeginPlayback), audioBytes, cleaned);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Voicebox failed: {ex.Message}";
        }
        finally
        {
            _requestInFlight = false;
        }
    }

    private void BeginPlayback(byte[] wavBytes, string cleanedLine)
    {
        var frames = DecodeVoiceFrames(wavBytes, VoiceboxLeadTrimMs / 1000f);
        if (frames is null || frames.Length == 0)
        {
            _statusLabel.Text = "Decoded audio was empty.";
            return;
        }

        _frames = frames;
        _frameIndex = 0;
        _player.Stop();
        _player.VolumeDb = PercentToDb(VolumePercent);
        _player.Play();
        _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        PumpPlayback();
        _statusLabel.Text = $"Playing: \"{cleanedLine}\"";
    }

    private void PumpPlayback()
    {
        if (_playback is null || _frames.Length == 0)
        {
            return;
        }

        var framesAvailable = _playback.GetFramesAvailable();
        while (framesAvailable > 0 && _frameIndex < _frames.Length)
        {
            _playback.PushFrame(_frames[_frameIndex]);
            _frameIndex++;
            framesAvailable--;
        }

        if (_frameIndex >= _frames.Length && framesAvailable >= _playbackCapacityFrames - 1)
        {
            _player.Stop();
            _playback = null;
            _frames = Array.Empty<Vector2>();
            _frameIndex = 0;
            _statusLabel.Text = "Playback finished. Press Play again to retest.";
        }
    }

    private static string CleanLine(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace('\n', ' ').Trim();
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

    private static string ParseGenerationId(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id)
            ? id.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string ParseGenerationStatus(string statusPayload)
    {
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

    private static Vector2[] DecodeVoiceFrames(byte[] wavFileBytes, float leadTrimSeconds)
    {
        if (wavFileBytes is null || wavFileBytes.Length < 44)
        {
            return Array.Empty<Vector2>();
        }

        using var ms = new MemoryStream(wavFileBytes);
        using var br = new BinaryReader(ms);
        if (new string(br.ReadChars(4)) != "RIFF")
        {
            return Array.Empty<Vector2>();
        }

        br.ReadInt32();
        if (new string(br.ReadChars(4)) != "WAVE")
        {
            return Array.Empty<Vector2>();
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
                return Array.Empty<Vector2>();
            }

            if (chunkId == "fmt ")
            {
                var format = br.ReadInt16();
                channels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt16();
                bitsPerSample = br.ReadInt16();
                if (chunkSize > 16)
                {
                    br.ReadBytes(chunkSize - 16);
                }

                if (format != 1)
                {
                    return Array.Empty<Vector2>();
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
                br.ReadByte();
            }
        }

        if (pcmData is null || sampleRate <= 0 || channels is not (1 or 2))
        {
            return Array.Empty<Vector2>();
        }

        pcmData = TrimLeadingPcmFrames(pcmData, bitsPerSample, channels, sampleRate, leadTrimSeconds);
        pcmData = SmoothPcmEdges(pcmData, bitsPerSample, channels, sampleRate);
        pcmData = ConvertToStereo16BitPcm(pcmData, bitsPerSample, channels, sampleRate, out _);
        return ConvertStereo16BitPcmToFrames(pcmData);
    }

    private static float PercentToDb(int volumePercent)
    {
        var clamped = Math.Clamp(volumePercent, 0, 100) / 100f;
        return clamped <= 0.0001f ? -80f : Mathf.LinearToDb(clamped);
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
        targetRate = PlaybackMixRate;
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
}
