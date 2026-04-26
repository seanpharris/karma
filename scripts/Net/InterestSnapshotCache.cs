using System.Collections.Generic;
using System.Linq;

namespace Karma.Net;

public sealed record InterestSnapshotApplyResult(
    long AppliedTick,
    bool WasDelta,
    int AddedChunks,
    int UpdatedChunks,
    int UnchangedChunks,
    int RemovedChunks);

public sealed class InterestSnapshotCache
{
    private readonly Dictionary<string, int> _chunkRevisions = new();

    public ClientInterestSnapshot LastSnapshot { get; private set; }
    public long LastAppliedTick { get; private set; }
    public int LastVisibleMapRevision { get; private set; }
    public InterestSnapshotApplyResult LastApplyResult { get; private set; } = new(0, false, 0, 0, 0, 0);
    public int KnownChunkCount => _chunkRevisions.Count;
    public IReadOnlyDictionary<string, int> ChunkRevisions => _chunkRevisions;

    public InterestSnapshotApplyResult Apply(ClientInterestSnapshot snapshot)
    {
        var visibleChunkKeys = snapshot.MapChunks
            .Select(chunk => chunk.ChunkKey)
            .ToHashSet();
        var removedChunks = 0;
        foreach (var removedKey in _chunkRevisions.Keys.Where(key => !visibleChunkKeys.Contains(key)).ToArray())
        {
            _chunkRevisions.Remove(removedKey);
            removedChunks++;
        }

        var addedChunks = 0;
        var updatedChunks = 0;
        var unchangedChunks = 0;
        foreach (var chunk in snapshot.MapChunks)
        {
            if (!_chunkRevisions.TryGetValue(chunk.ChunkKey, out var existingRevision))
            {
                _chunkRevisions[chunk.ChunkKey] = chunk.Revision;
                addedChunks++;
                continue;
            }

            if (existingRevision == chunk.Revision)
            {
                unchangedChunks++;
                continue;
            }

            _chunkRevisions[chunk.ChunkKey] = chunk.Revision;
            updatedChunks++;
        }

        LastSnapshot = snapshot;
        LastAppliedTick = snapshot.Tick;
        LastVisibleMapRevision = snapshot.SyncHint.VisibleMapRevision;
        LastApplyResult = new InterestSnapshotApplyResult(
            snapshot.Tick,
            snapshot.SyncHint.IsDelta,
            addedChunks,
            updatedChunks,
            unchangedChunks,
            removedChunks);

        return LastApplyResult;
    }
}
