using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Karma.Voice;

public sealed record WindowsMicrophoneRecordingResult(
    string FilePath,
    double DurationSeconds,
    long FileSizeBytes,
    string DeviceName);

public sealed class WindowsMicrophoneRecorder : IDisposable
{
    private WaveInEvent _waveIn;
    private WaveFileWriter _writer;
    private string _activeOutputPath = string.Empty;
    private DateTime _recordingStartedUtc;
    private string _deviceName = string.Empty;
    private TaskCompletionSource<WindowsMicrophoneRecordingResult> _stopTcs;

    public bool IsRecording => _waveIn is not null;

    public static bool HasInputDevice => WaveInEvent.DeviceCount > 0;

    public static string GetDefaultDeviceName()
    {
        if (WaveInEvent.DeviceCount <= 0)
        {
            return "No microphone device detected";
        }

        return WaveInEvent.GetCapabilities(0).ProductName;
    }

    public void StartRecording(string outputPath)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Microphone recording is already active.");
        }

        if (WaveInEvent.DeviceCount <= 0)
        {
            throw new InvalidOperationException("No microphone input device is available.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output path directory is invalid."));
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        _deviceName = GetDefaultDeviceName();
        _activeOutputPath = outputPath;
        _recordingStartedUtc = DateTime.UtcNow;
        _stopTcs = new TaskCompletionSource<WindowsMicrophoneRecordingResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer = new WaveFileWriter(outputPath, new WaveFormat(16000, 1));
        _waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(16000, 1),
            BufferMilliseconds = 100
        };
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
        _waveIn.StartRecording();
    }

    public async Task<WindowsMicrophoneRecordingResult> StopRecordingAsync()
    {
        if (!IsRecording || _stopTcs is null)
        {
            throw new InvalidOperationException("Microphone recording is not active.");
        }

        _waveIn.StopRecording();
        return await _stopTcs.Task.ConfigureAwait(false);
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        _writer?.Flush();
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        var stopTcs = _stopTcs;
        var outputPath = _activeOutputPath;
        var deviceName = _deviceName;
        var durationSeconds = Math.Max(0d, (DateTime.UtcNow - _recordingStartedUtc).TotalSeconds);

        CleanupRecorder();

        if (e.Exception is not null)
        {
            stopTcs?.TrySetException(e.Exception);
            return;
        }

        var fileInfo = new FileInfo(outputPath);
        stopTcs?.TrySetResult(new WindowsMicrophoneRecordingResult(
            outputPath,
            durationSeconds,
            fileInfo.Exists ? fileInfo.Length : 0,
            deviceName));
    }

    private void CleanupRecorder()
    {
        if (_waveIn is not null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _writer?.Dispose();
        _writer = null;
        _activeOutputPath = string.Empty;
    }

    public void Dispose()
    {
        CleanupRecorder();
        _stopTcs = null;
    }
}
