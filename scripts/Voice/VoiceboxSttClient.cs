using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Karma.Voice;

public sealed record VoiceboxTranscriptionResult(
    string Text,
    double DurationSeconds,
    string Language);

public static class VoiceboxSttClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public static async Task<VoiceboxTranscriptionResult> TranscribeFileAsync(
        string baseUrl,
        string audioFilePath,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            throw new FileNotFoundException("Audio file for Voicebox transcription was not found.", audioFilePath);
        }

        var trimmedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(trimmedBaseUrl))
        {
            throw new InvalidOperationException("Voicebox STT base URL is not configured.");
        }

        await using var fileStream = File.OpenRead(audioFilePath);
        using var form = new MultipartFormDataContent();
        using var audioContent = new StreamContent(fileStream);
        audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        form.Add(audioContent, "file", Path.GetFileName(audioFilePath));
        if (!string.IsNullOrWhiteSpace(language))
        {
            form.Add(new StringContent(language), "language");
        }

        using var response = await Http.PostAsync($"{trimmedBaseUrl}/transcribe", form).ConfigureAwait(false);
        var rawJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var text = root.TryGetProperty("text", out var textValue)
            ? (textValue.GetString() ?? string.Empty).Trim()
            : string.Empty;
        var duration = root.TryGetProperty("duration", out var durationValue)
            ? durationValue.GetDouble()
            : 0d;
        return new VoiceboxTranscriptionResult(text, duration, language ?? string.Empty);
    }
}
