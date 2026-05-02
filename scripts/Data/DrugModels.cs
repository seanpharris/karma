using System.Collections.Generic;

namespace Karma.Data;

// Declarative drug catalog. Each drug describes the immediate effect a player
// gets on use, the duration that effect lasts, and the addiction load it adds
// to the player's exposure history. Past `AddictionThreshold` cumulative load,
// missing a fresh dose triggers withdrawal — modelled by emitting a status
// effect the server can read off `GetStatusEffectsFor`.
//
// Mirrors the LootTableCatalog / DialogueRegistry shape (built-ins +
// runtime overrides + reset for tests). Item-level wiring: a `GameItem`
// whose id matches a registered drug id is treated as a usable dose.

public sealed record DrugEffect(
    string StatusKind,      // e.g. "Energised", "Stunned", "Numb" — surfaced as a status string.
    int DurationTicks,      // how long the effect lasts after use.
    int HealAmount = 0,     // optional immediate HP delta.
    int StaminaDelta = 0,   // optional immediate stamina delta (negative for cost).
    int HungerDelta = 0,    // optional immediate hunger delta.
    int KarmaDelta = 0);    // optional immediate karma delta.

public sealed record DrugDefinition(
    string Id,
    string DisplayName,
    DrugEffect OnUse,
    int AddictionLoadPerUse,
    int AddictionThreshold,           // cumulative load before withdrawal kicks in.
    int WithdrawalGraceTicks,         // ticks after last dose before withdrawal starts.
    DrugEffect Withdrawal);           // status emitted when grace expires while addicted.

public static class DrugCatalog
{
    public const string StimSpikeId = "stim_spike";
    public const string DownerHazeId = "downer_haze";
    public const string TremorTabId = "tremor_tab";

    private static readonly Dictionary<string, DrugDefinition> BuiltIns = new()
    {
        [StimSpikeId] = new DrugDefinition(
            Id: StimSpikeId,
            DisplayName: "Stim Spike",
            OnUse: new DrugEffect(
                StatusKind: "Energised",
                DurationTicks: 600,
                StaminaDelta: 60,
                KarmaDelta: -1),
            AddictionLoadPerUse: 25,
            AddictionThreshold: 75,
            WithdrawalGraceTicks: 1800,
            Withdrawal: new DrugEffect(
                StatusKind: "Crashing",
                DurationTicks: 900,
                StaminaDelta: -40)),

        [DownerHazeId] = new DrugDefinition(
            Id: DownerHazeId,
            DisplayName: "Downer Haze",
            OnUse: new DrugEffect(
                StatusKind: "Numb",
                DurationTicks: 800,
                HealAmount: 20,
                StaminaDelta: -10,
                KarmaDelta: -1),
            AddictionLoadPerUse: 35,
            AddictionThreshold: 70,
            WithdrawalGraceTicks: 1500,
            Withdrawal: new DrugEffect(
                StatusKind: "Twitchy",
                DurationTicks: 1200,
                HealAmount: -5)),

        [TremorTabId] = new DrugDefinition(
            Id: TremorTabId,
            DisplayName: "Tremor Tab",
            OnUse: new DrugEffect(
                StatusKind: "Wired",
                DurationTicks: 400,
                StaminaDelta: 30,
                HungerDelta: -20),
            AddictionLoadPerUse: 15,
            AddictionThreshold: 90,
            WithdrawalGraceTicks: 2400,
            Withdrawal: new DrugEffect(
                StatusKind: "Sluggish",
                DurationTicks: 1800,
                StaminaDelta: -20)),
    };

    private static readonly Dictionary<string, DrugDefinition> _runtimeOverrides = new();

    public static IReadOnlyDictionary<string, DrugDefinition> All
    {
        get
        {
            var merged = new Dictionary<string, DrugDefinition>(BuiltIns);
            foreach (var (key, value) in _runtimeOverrides)
                merged[key] = value;
            return merged;
        }
    }

    public static void Register(DrugDefinition drug)
    {
        if (drug is null || string.IsNullOrEmpty(drug.Id)) return;
        _runtimeOverrides[drug.Id] = drug;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }

    public static bool TryGet(string id, out DrugDefinition drug)
    {
        if (_runtimeOverrides.TryGetValue(id, out drug)) return true;
        return BuiltIns.TryGetValue(id, out drug);
    }
}

// Per-player exposure snapshot. The server tracks (cumulative load, last dose
// tick) per (playerId, drugId) so it can compute addiction state and surface
// withdrawal as a status effect when grace ticks elapse with no fresh dose.
public sealed class DrugExposure
{
    public int CumulativeLoad { get; private set; }
    public long LastDoseTick { get; private set; }

    public void RecordDose(int load, long tick)
    {
        if (load <= 0) return;
        CumulativeLoad += load;
        LastDoseTick = tick;
    }

    public bool IsAddicted(int threshold) => CumulativeLoad >= threshold;

    public bool InWithdrawal(int threshold, int graceTicks, long currentTick)
    {
        if (!IsAddicted(threshold)) return false;
        return currentTick - LastDoseTick > graceTicks;
    }
}
