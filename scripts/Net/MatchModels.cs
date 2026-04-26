using Karma.Data;

namespace Karma.Net;

public enum MatchStatus
{
    Running,
    Finished
}

public sealed record MatchSnapshot(
    MatchStatus Status,
    int DurationSeconds,
    int ElapsedSeconds,
    string SaintWinnerId,
    string SaintWinnerName,
    int SaintWinnerScore,
    string ScourgeWinnerId,
    string ScourgeWinnerName,
    int ScourgeWinnerScore)
{
    public int RemainingSeconds => System.Math.Max(0, DurationSeconds - ElapsedSeconds);

    public string Summary => Status == MatchStatus.Running
        ? $"Match: {FormatTime(RemainingSeconds)} remaining"
        : $"Match complete: Saint {SaintWinnerName} ({SaintWinnerScore:+#;-#;0}) | Scourge {ScourgeWinnerName} ({ScourgeWinnerScore:+#;-#;0})";

    private static string FormatTime(int seconds)
    {
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
}

public sealed class MatchState
{
    private readonly int _durationSeconds;
    private int _elapsedSeconds;
    private LeaderboardSnapshot _winners = EmptyWinners;

    public MatchState(int durationSeconds)
    {
        _durationSeconds = durationSeconds;
    }

    public MatchStatus Status { get; private set; } = MatchStatus.Running;
    public int DurationSeconds => _durationSeconds;
    public int ElapsedSeconds => _elapsedSeconds;

    public void Advance(int seconds, LeaderboardStanding standing)
    {
        if (Status == MatchStatus.Finished || seconds <= 0)
        {
            return;
        }

        _elapsedSeconds = System.Math.Min(_durationSeconds, _elapsedSeconds + seconds);
        if (_elapsedSeconds < _durationSeconds)
        {
            return;
        }

        Status = MatchStatus.Finished;
        _winners = SnapshotBuilder.LeaderboardFrom(standing);
    }

    public MatchSnapshot Snapshot(LeaderboardStanding currentStanding)
    {
        var winners = Status == MatchStatus.Finished
            ? _winners
            : SnapshotBuilder.LeaderboardFrom(currentStanding);
        return new MatchSnapshot(
            Status,
            _durationSeconds,
            _elapsedSeconds,
            winners.SaintPlayerId,
            winners.SaintName,
            winners.SaintScore,
            winners.ScourgePlayerId,
            winners.ScourgeName,
            winners.ScourgeScore);
    }

    private static LeaderboardSnapshot EmptyWinners { get; } =
        new(string.Empty, "--", 0, string.Empty, "--", 0);
}
