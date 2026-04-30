using System;

namespace Karma.Net;

public enum WorldScale
{
    Small,
    Medium,
    Large
}

public sealed record ServerConfig(
    int MaxPlayers,
    int TargetPlayers,
    WorldScale Scale,
    int TickRate,
    int InterestRadiusTiles,
    int CombatRangeTiles,
    int ChunkSizeTiles,
    int MatchDurationSeconds,
    int ReputationDecayTickInterval = 10)
{
    public const int AbsoluteMaxPlayers = 100;
    public const int DefaultMatchDurationSeconds = 30 * 60;
    public const int DefaultChunkSizeTiles = 32;
    public const int DefaultMatchWinnerScripReward = 25;

    public int InterestRadiusChunks =>
        Math.Max(1, (int)Math.Ceiling(InterestRadiusTiles / (double)ChunkSizeTiles));

    public static ServerConfig Prototype4Player { get; } = new(
        MaxPlayers: 4,
        TargetPlayers: 4,
        Scale: WorldScale.Small,
        TickRate: 20,
        InterestRadiusTiles: 24,
        CombatRangeTiles: 2,
        ChunkSizeTiles: DefaultChunkSizeTiles,
        MatchDurationSeconds: DefaultMatchDurationSeconds);

    public static ServerConfig Large100Player { get; } = new(
        MaxPlayers: 100,
        TargetPlayers: 60,
        Scale: WorldScale.Large,
        TickRate: 20,
        InterestRadiusTiles: 16,
        CombatRangeTiles: 2,
        ChunkSizeTiles: DefaultChunkSizeTiles,
        MatchDurationSeconds: DefaultMatchDurationSeconds);

    public void Validate()
    {
        if (MaxPlayers is < 1 or > AbsoluteMaxPlayers)
        {
            throw new InvalidOperationException($"MaxPlayers must be between 1 and {AbsoluteMaxPlayers}.");
        }

        if (TargetPlayers < 1 || TargetPlayers > MaxPlayers)
        {
            throw new InvalidOperationException("TargetPlayers must be between 1 and MaxPlayers.");
        }

        if (TickRate < 1)
        {
            throw new InvalidOperationException("TickRate must be positive.");
        }

        if (InterestRadiusTiles < 1)
        {
            throw new InvalidOperationException("InterestRadiusTiles must be positive.");
        }

        if (CombatRangeTiles < 1)
        {
            throw new InvalidOperationException("CombatRangeTiles must be positive.");
        }

        if (ChunkSizeTiles < 1)
        {
            throw new InvalidOperationException("ChunkSizeTiles must be positive.");
        }

        if (MatchDurationSeconds < 1)
        {
            throw new InvalidOperationException("MatchDurationSeconds must be positive.");
        }
    }
}
