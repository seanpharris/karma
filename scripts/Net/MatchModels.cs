using Karma.Data;

namespace Karma.Net;

public enum MatchStatus
{
    Lobby,
    Running,
    Finished
}

public sealed record MatchSnapshot(
    MatchStatus Status,
    int DurationSeconds,
    int ElapsedSeconds,
    string CurrentSaintId,
    string CurrentSaintName,
    int CurrentSaintScore,
    string CurrentScourgeId,
    string CurrentScourgeName,
    int CurrentScourgeScore,
    string SaintWinnerId,
    string SaintWinnerName,
    int SaintWinnerScore,
    string ScourgeWinnerId,
    string ScourgeWinnerName,
    int ScourgeWinnerScore)
{
    public int RemainingSeconds => System.Math.Max(0, DurationSeconds - ElapsedSeconds);

    public string Summary => Status switch
    {
        MatchStatus.Lobby => "Lobby: waiting for players to ready up",
        MatchStatus.Finished => $"Match complete: Saint {SaintWinnerName} ({SaintWinnerScore:+#;-#;0}) | Scourge {ScourgeWinnerName} ({ScourgeWinnerScore:+#;-#;0})",
        _ => $"Match: {FormatTime(RemainingSeconds)} | Saint {CurrentSaintName} ({CurrentSaintScore:+#;-#;0}) | Scourge {CurrentScourgeName} ({CurrentScourgeScore:+#;-#;0})"
    };

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

    public MatchStatus Status { get; private set; } = MatchStatus.Lobby;
    public int DurationSeconds => _durationSeconds;
    public int ElapsedSeconds => _elapsedSeconds;

    public void StartMatch()
    {
        if (Status == MatchStatus.Lobby)
            Status = MatchStatus.Running;
    }

    public void Advance(int seconds, LeaderboardStanding standing)
    {
        if (Status != MatchStatus.Running || seconds <= 0)
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
        var leaders = SnapshotBuilder.LeaderboardFrom(currentStanding);
        var winners = Status == MatchStatus.Finished
            ? _winners
            : leaders;
        return new MatchSnapshot(
            Status,
            _durationSeconds,
            _elapsedSeconds,
            leaders.SaintPlayerId,
            leaders.SaintName,
            leaders.SaintScore,
            leaders.ScourgePlayerId,
            leaders.ScourgeName,
            leaders.ScourgeScore,
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
