using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Karma.Core;
using Karma.Data;

namespace Karma.Tests;

public partial class SnapshotExportRunner : Node
{
    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        BuildSampleState(state);

        var snapshot = state.CreateSnapshot();
        var outputPath = ProjectSettings.GlobalizePath("res://debug/latest-snapshot.json");
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(outputPath, SnapshotJson.Write(snapshot));
        GD.Print($"Snapshot written: {outputPath}");
        GD.Print(snapshot.Summary);
        GetTree().Quit(0);
    }

    private static void BuildSampleState(GameState state)
    {
        state.TriggerKarmaBreak();
        state.AddItem(StarterItems.RepairKit);
        state.StartQuest(StarterQuests.MaraClinicFiltersId);
        state.CompleteQuest(GameState.LocalPlayerId, StarterQuests.MaraClinicFiltersId);
        state.StartEntanglement(
            GameState.LocalPlayerId,
            StarterNpcs.Mara.Id,
            StarterNpcs.Dallen.Id,
            EntanglementType.Romantic,
            PrototypeActions.StartMaraEntanglement());

        var entanglement = state.Entanglements.All.First();
        state.ExposeEntanglement(
            GameState.LocalPlayerId,
            entanglement.Id,
            PrototypeActions.ExposeMaraEntanglement());
        state.Duels.Request(GameState.LocalPlayerId, "peer_stand_in");
        state.Duels.Accept("peer_stand_in", GameState.LocalPlayerId, out _);
        state.NotifyDuelsChanged();
    }
}

public static class SnapshotJson
{
    public static string Write(GameSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"summary\": \"{Escape(snapshot.Summary)}\",");
        builder.Append("  \"leaderboard\": ");
        builder.Append($"{{ \"saintPlayerId\": \"{Escape(snapshot.Leaderboard.SaintPlayerId)}\", ");
        builder.Append($"\"saintName\": \"{Escape(snapshot.Leaderboard.SaintName)}\", ");
        builder.Append($"\"saintScore\": {snapshot.Leaderboard.SaintScore}, ");
        builder.Append($"\"scourgePlayerId\": \"{Escape(snapshot.Leaderboard.ScourgePlayerId)}\", ");
        builder.Append($"\"scourgeName\": \"{Escape(snapshot.Leaderboard.ScourgeName)}\", ");
        builder.AppendLine($"\"scourgeScore\": {snapshot.Leaderboard.ScourgeScore} }},");
        builder.AppendLine("  \"players\": [");
        for (var i = 0; i < snapshot.Players.Count; i++)
        {
            var player = snapshot.Players[i];
            builder.Append("    ");
            builder.Append($"{{ \"id\": \"{Escape(player.Id)}\", \"name\": \"{Escape(player.DisplayName)}\", ");
            builder.Append($"\"karma\": {player.Karma}, \"tier\": \"{Escape(player.Tier)}\", ");
            builder.Append($"\"karmaRank\": {player.KarmaRank}, ");
            builder.Append($"\"karmaProgress\": \"{Escape(player.KarmaProgress)}\", ");
            builder.Append($"\"standing\": \"{player.Standing}\", ");
            builder.Append($"\"tileX\": {player.TileX}, \"tileY\": {player.TileY}, ");
            builder.Append($"\"inventory\": [{string.Join(", ", player.InventoryItemIds.Select(itemId => $"\"{Escape(itemId)}\""))}], ");
            builder.Append($"\"health\": {player.Health}, \"maxHealth\": {player.MaxHealth} }}");
            builder.AppendLine(i == snapshot.Players.Count - 1 ? string.Empty : ",");
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"quests\": [");
        for (var i = 0; i < snapshot.Quests.Count; i++)
        {
            var quest = snapshot.Quests[i];
            builder.AppendLine($"    {{ \"id\": \"{Escape(quest.Id)}\", \"status\": \"{quest.Status}\" }}{(i == snapshot.Quests.Count - 1 ? string.Empty : ",")}");
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"factions\": [");
        for (var i = 0; i < snapshot.Factions.Count; i++)
        {
            var faction = snapshot.Factions[i];
            builder.Append("    ");
            builder.Append($"{{ \"id\": \"{Escape(faction.FactionId)}\", \"playerId\": \"{Escape(faction.PlayerId)}\", ");
            builder.Append($"\"reputation\": {faction.Reputation} }}");
            builder.AppendLine(i == snapshot.Factions.Count - 1 ? string.Empty : ",");
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"duels\": [");
        for (var i = 0; i < snapshot.Duels.Count; i++)
        {
            var duel = snapshot.Duels[i];
            builder.Append("    ");
            builder.Append($"{{ \"id\": \"{Escape(duel.Id)}\", \"challengerId\": \"{Escape(duel.ChallengerId)}\", ");
            builder.Append($"\"targetId\": \"{Escape(duel.TargetId)}\", \"status\": \"{duel.Status}\" }}");
            builder.AppendLine(i == snapshot.Duels.Count - 1 ? string.Empty : ",");
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"worldEvents\": [");
        for (var i = 0; i < snapshot.WorldEvents.Count; i++)
        {
            var worldEvent = snapshot.WorldEvents[i];
            builder.Append("    ");
            builder.Append($"{{ \"id\": \"{Escape(worldEvent.Id)}\", \"type\": \"{worldEvent.Type}\", ");
            builder.Append($"\"summary\": \"{Escape(worldEvent.Summary)}\" }}");
            builder.AppendLine(i == snapshot.WorldEvents.Count - 1 ? string.Empty : ",");
        }
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
