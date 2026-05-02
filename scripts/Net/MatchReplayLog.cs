using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Karma.Net;

public sealed record MatchReplayRow(
    long Tick,
    ServerIntent Intent,
    bool WasAccepted,
    string RejectionReason,
    ClientInterestSnapshot Snapshot);

public static class MatchReplayLog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    public static void Append(string path, MatchReplayRow row)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.AppendAllText(path, JsonSerializer.Serialize(row, Options) + "\n");
    }

    public static IReadOnlyList<MatchReplayRow> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return System.Array.Empty<MatchReplayRow>();

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<MatchReplayRow>(line, Options))
            .Where(row => row is not null)
            .ToArray();
    }

    public static IReadOnlyList<ClientInterestSnapshot> ReconstructSnapshots(string path)
    {
        return Load(path)
            .OrderBy(row => row.Tick)
            .Select(row => row.Snapshot)
            .ToArray();
    }
}
