using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Karma.Voice;

public sealed record VoiceboxResolvedProfile(
    string Id,
    string Name,
    bool UsedFallback);

public static class VoiceboxProfileResolver
{
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task<VoiceboxResolvedProfile> ResolveAsync(string baseUrl, string requestedProfile)
    {
        var normalizedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
        {
            throw new InvalidOperationException("Voicebox base URL is empty.");
        }

        var response = await Http.GetAsync($"{normalizedBaseUrl}/profiles");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var profiles = ParseProfiles(json);
        if (profiles.Count == 0)
        {
            throw new InvalidOperationException(
                "Voicebox has no profiles loaded. Open Voicebox and create or import a voice profile, then try again.");
        }

        if (!string.IsNullOrWhiteSpace(requestedProfile))
        {
            foreach (var profile in profiles)
            {
                if (string.Equals(profile.Id, requestedProfile, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.Name, requestedProfile, StringComparison.OrdinalIgnoreCase))
                {
                    return new VoiceboxResolvedProfile(profile.Id, profile.Name, UsedFallback: false);
                }
            }
        }

        var first = profiles[0];
        return new VoiceboxResolvedProfile(first.Id, first.Name, UsedFallback: !string.IsNullOrWhiteSpace(requestedProfile));
    }

    private static List<VoiceboxResolvedProfile> ParseProfiles(string json)
    {
        var profiles = new List<VoiceboxResolvedProfile>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return profiles;
        }

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var id = element.TryGetProperty("id", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;
            var name = element.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            profiles.Add(new VoiceboxResolvedProfile(id, name, UsedFallback: false));
        }

        return profiles;
    }
}
