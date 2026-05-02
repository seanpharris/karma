using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

// Data-driven loot drops. Each weighted entry can roll 0+ copies of an item
// based on its quantity range. Tables are referenced by id from containers,
// kills, supply drops, and event rewards so per-call-site arrays don't have
// to keep enumerating ids and weights inline.
public sealed record LootTableEntry(string ItemId, int Weight, int MinQuantity = 1, int MaxQuantity = 1);

public sealed record LootTable(string Id, IReadOnlyList<LootTableEntry> Entries, int Rolls = 1);

public sealed record LootRollResult(string ItemId, int Quantity);

public static class LootTableCatalog
{
    public const string SupplyDropCommonId = "supply_drop_common";
    public const string SupplyDropAmmoId = "supply_drop_ammo";
    public const string SupplyDropMedicalId = "supply_drop_medical";
    public const string ContainerScavengeId = "container_scavenge";
    public const string DownedPlayerDropsId = "downed_player_drops";

    private static readonly Dictionary<string, LootTable> BuiltIns = new()
    {
        [SupplyDropCommonId] = new LootTable(SupplyDropCommonId, new[]
        {
            new LootTableEntry(StarterItems.RationPackId, Weight: 30),
            new LootTableEntry(StarterItems.RepairKitId, Weight: 20),
            new LootTableEntry(StarterItems.MediPatchId, Weight: 15),
            new LootTableEntry(StarterItems.BallisticRoundId, Weight: 15, MinQuantity: 2, MaxQuantity: 4),
            new LootTableEntry(StarterItems.EnergyCellId, Weight: 10, MinQuantity: 1, MaxQuantity: 3),
            new LootTableEntry(StarterItems.PowerCellId, Weight: 5),
            new LootTableEntry(StarterItems.DataChipId, Weight: 5),
        }, Rolls: 3),

        [SupplyDropAmmoId] = new LootTable(SupplyDropAmmoId, new[]
        {
            new LootTableEntry(StarterItems.BallisticRoundId, Weight: 60, MinQuantity: 4, MaxQuantity: 8),
            new LootTableEntry(StarterItems.EnergyCellId, Weight: 40, MinQuantity: 2, MaxQuantity: 5),
        }, Rolls: 2),

        [SupplyDropMedicalId] = new LootTable(SupplyDropMedicalId, new[]
        {
            new LootTableEntry(StarterItems.MediPatchId, Weight: 50, MinQuantity: 1, MaxQuantity: 2),
            new LootTableEntry(StarterItems.RationPackId, Weight: 30),
            new LootTableEntry(StarterItems.RepairKitId, Weight: 20),
        }, Rolls: 2),

        [ContainerScavengeId] = new LootTable(ContainerScavengeId, new[]
        {
            new LootTableEntry(StarterItems.RepairKitId, Weight: 30),
            new LootTableEntry(StarterItems.RationPackId, Weight: 25),
            new LootTableEntry(StarterItems.FilterCoreId, Weight: 15),
            new LootTableEntry(StarterItems.PortableTerminalId, Weight: 10),
            new LootTableEntry(StarterItems.BoltCuttersId, Weight: 10),
            new LootTableEntry(StarterItems.ChemInjectorId, Weight: 10),
        }, Rolls: 1),

        [DownedPlayerDropsId] = new LootTable(DownedPlayerDropsId, new[]
        {
            new LootTableEntry(StarterItems.BallisticRoundId, Weight: 35, MinQuantity: 1, MaxQuantity: 3),
            new LootTableEntry(StarterItems.EnergyCellId, Weight: 25, MinQuantity: 1, MaxQuantity: 2),
            new LootTableEntry(StarterItems.RationPackId, Weight: 20),
            new LootTableEntry(StarterItems.MediPatchId, Weight: 15),
            new LootTableEntry(StarterItems.RepairKitId, Weight: 5),
        }, Rolls: 1),
    };

    private static readonly Dictionary<string, LootTable> _runtimeOverrides = new();

    public static IReadOnlyDictionary<string, LootTable> All
    {
        get
        {
            var merged = new Dictionary<string, LootTable>(BuiltIns);
            foreach (var (key, value) in _runtimeOverrides)
                merged[key] = value;
            return merged;
        }
    }

    public static void Register(LootTable table)
    {
        if (table is null || string.IsNullOrEmpty(table.Id)) return;
        _runtimeOverrides[table.Id] = table;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }

    public static bool TryGet(string id, out LootTable table)
    {
        if (_runtimeOverrides.TryGetValue(id, out table)) return true;
        return BuiltIns.TryGetValue(id, out table);
    }

    public static IReadOnlyList<LootRollResult> Roll(string tableId, Random rng)
    {
        if (!TryGet(tableId, out var table) || table.Entries.Count == 0)
            return Array.Empty<LootRollResult>();

        var totalWeight = table.Entries.Sum(entry => Math.Max(0, entry.Weight));
        if (totalWeight <= 0) return Array.Empty<LootRollResult>();

        var results = new List<LootRollResult>(table.Rolls);
        for (var i = 0; i < table.Rolls; i++)
        {
            var pick = rng.Next(totalWeight);
            var cursor = 0;
            foreach (var entry in table.Entries)
            {
                cursor += Math.Max(0, entry.Weight);
                if (pick >= cursor) continue;
                var qty = entry.MinQuantity == entry.MaxQuantity
                    ? entry.MinQuantity
                    : rng.Next(entry.MinQuantity, entry.MaxQuantity + 1);
                if (qty > 0) results.Add(new LootRollResult(entry.ItemId, qty));
                break;
            }
        }
        return results;
    }
}
