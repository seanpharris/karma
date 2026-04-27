using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Data;

namespace Karma.World;

/// <summary>
/// Deterministic best-candidate placement for points that should feel naturally spaced.
///
/// The pattern is inspired by common blue-noise / Poisson-disc sampling techniques
/// used in procedural generation demos, but kept tiny and project-native so server
/// generation stays deterministic and easy to test.
/// </summary>
public static class ProceduralPlacementSampler
{
    public static IReadOnlyList<TilePosition> GenerateSeparatedPoints(
        Random random,
        int width,
        int height,
        int count,
        int edgePadding,
        int candidateAttemptsPerPoint,
        IReadOnlyCollection<TilePosition>? reservedPoints = null)
    {
        if (count <= 0)
        {
            return Array.Empty<TilePosition>();
        }

        var minX = Math.Clamp(edgePadding, 0, Math.Max(0, width - 1));
        var minY = Math.Clamp(edgePadding, 0, Math.Max(0, height - 1));
        var maxX = Math.Max(minX, width - edgePadding - 1);
        var maxY = Math.Max(minY, height - edgePadding - 1);
        var attempts = Math.Max(1, candidateAttemptsPerPoint);
        var points = new List<TilePosition>(count);
        var blockers = reservedPoints?.ToArray() ?? Array.Empty<TilePosition>();

        for (var i = 0; i < count; i++)
        {
            var best = new TilePosition(random.Next(minX, maxX + 1), random.Next(minY, maxY + 1));
            var bestDistance = GetNearestDistanceSquared(best, points, blockers);

            for (var attempt = 1; attempt < attempts; attempt++)
            {
                var candidate = new TilePosition(random.Next(minX, maxX + 1), random.Next(minY, maxY + 1));
                var distance = GetNearestDistanceSquared(candidate, points, blockers);
                if (distance > bestDistance || (distance == bestDistance && random.Next(2) == 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            points.Add(best);
        }

        return points;
    }

    public static int GetNearestDistanceSquared(
        TilePosition candidate,
        IReadOnlyCollection<TilePosition> points,
        IReadOnlyCollection<TilePosition>? reservedPoints = null)
    {
        var nearest = int.MaxValue;
        foreach (var point in points)
        {
            nearest = Math.Min(nearest, candidate.DistanceSquaredTo(point));
        }

        if (reservedPoints is not null)
        {
            foreach (var point in reservedPoints)
            {
                nearest = Math.Min(nearest, candidate.DistanceSquaredTo(point));
            }
        }

        return nearest;
    }
}
