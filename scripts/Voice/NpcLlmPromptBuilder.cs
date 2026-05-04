using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Karma.Data;
using Karma.Net;

namespace Karma.Voice;

public sealed record NpcLlmPromptPackage(
    string NpcId,
    string NpcName,
    string ContextPath,
    string ContextMarkdown,
    IReadOnlyDictionary<string, string> RuntimeFields,
    string SystemPrompt,
    string UserPrompt,
    string FullPrompt);

public static class NpcLlmPromptBuilder
{
    public static bool TryBuild(
        PrototypeServerSession session,
        string npcId,
        string playerMessage,
        out NpcLlmPromptPackage promptPackage,
        out string error)
    {
        return TryBuild(
            session,
            npcId,
            playerMessage,
            out promptPackage,
            out error,
            additionalRuntimeFields: null,
            userPromptOverride: null);
    }

    public static bool TryBuild(
        PrototypeServerSession session,
        string npcId,
        string playerMessage,
        out NpcLlmPromptPackage promptPackage,
        out string error,
        IReadOnlyDictionary<string, string> additionalRuntimeFields,
        string userPromptOverride)
    {
        promptPackage = null;
        error = string.Empty;

        if (session?.LastLocalSnapshot is not { } snapshot)
        {
            error = "PrototypeServerSession snapshot is unavailable.";
            return false;
        }

        if (!NpcContextRepository.TryLoad(npcId, out var contextMarkdown, out var contextPath))
        {
            error = $"No npc context file could be loaded for {npcId}.";
            return false;
        }

        if (!NpcProfileLookup.TryGet(npcId, out var profile))
        {
            error = $"No npc profile is registered for {npcId}.";
            return false;
        }

        var runtimeFields = BuildRuntimeFields(snapshot, profile, additionalRuntimeFields);
        var systemPrompt = BuildSystemPrompt(profile, contextMarkdown, runtimeFields);
        var userPrompt = BuildUserPrompt(profile, playerMessage, runtimeFields, userPromptOverride);
        promptPackage = new NpcLlmPromptPackage(
            profile.Id,
            profile.Name,
            contextPath,
            contextMarkdown,
            runtimeFields,
            systemPrompt,
            userPrompt,
            $"# System Prompt\n\n{systemPrompt}\n\n# User Prompt\n\n{userPrompt}\n");
        return true;
    }

    public static string ExportToUserFile(NpcLlmPromptPackage promptPackage, string fileName = "mara-last-prompt.md")
    {
        var outputPath = ProjectSettings.GlobalizePath($"user://npc-llm/{fileName}");
        var directory = System.IO.Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        System.IO.File.WriteAllText(outputPath, promptPackage.FullPrompt);
        return outputPath;
    }

    private static IReadOnlyDictionary<string, string> BuildRuntimeFields(
        ClientInterestSnapshot snapshot,
        NpcProfile profile,
        IReadOnlyDictionary<string, string> additionalRuntimeFields)
    {
        var localPlayer = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);
        var npc = snapshot.Npcs.FirstOrDefault(candidate => candidate.Id == profile.Id);
        var dialogue = snapshot.Dialogues.FirstOrDefault(candidate => candidate.NpcId == profile.Id);
        var factionId = StarterFactions.ToId(profile.Faction);
        var factionRep = snapshot.Factions.FirstOrDefault(candidate =>
            candidate.PlayerId == snapshot.PlayerId &&
            candidate.FactionId == factionId)?.Reputation ?? 0;
        var standing = localPlayer?.Standing ?? LeaderboardRole.None;
        var trust = ResolveTrustLevel(factionRep, standing);

        var fields = new Dictionary<string, string>
        {
            ["player_name"] = localPlayer?.DisplayName ?? "Traveler",
            ["player_reputation"] = BuildPlayerReputation(localPlayer, factionRep),
            ["player_karma_score"] = localPlayer?.Karma.ToString() ?? "0",
            ["player_karma_tier"] = localPlayer?.Tier ?? "Unknown",
            ["player_standing"] = (localPlayer?.Standing ?? LeaderboardRole.None).ToString(),
            ["player_faction_reputation"] = factionRep.ToString(),
            ["player_recent_actions"] = BuildRecentActionSummary(snapshot, profile.Id),
            ["relationship_to_player"] = ResolveRelationshipLabel(factionRep, standing),
            ["current_location"] = npc is null
                ? $"{snapshot.WorldId}"
                : $"{snapshot.WorldId}, near tile ({npc.TileX},{npc.TileY})",
            ["current_problem"] = dialogue?.Prompt?.Trim() ?? profile.Need,
            ["recent_dialogue"] = BuildRecentDialogueSummary(snapshot, profile.Id),
            ["known_world_state"] = BuildKnownWorldState(snapshot),
            ["trust_level"] = trust
        };

        if (additionalRuntimeFields is not null)
        {
            foreach (var (key, value) in additionalRuntimeFields)
            {
                fields[key] = value;
            }
        }

        return fields;
    }

    private static string BuildSystemPrompt(
        NpcProfile profile,
        string contextMarkdown,
        IReadOnlyDictionary<string, string> runtimeFields)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are generating dialogue for an in-world NPC in Karma.");
        builder.AppendLine("Treat the markdown file below as the primary characterization source for this NPC.");
        builder.AppendLine("Stay fully in character and return only the words the NPC would actually say out loud.");
        builder.AppendLine();
        builder.AppendLine("# NPC Context File");
        builder.AppendLine();
        builder.AppendLine(contextMarkdown.Trim());
        builder.AppendLine();
        builder.AppendLine("# Runtime Context");
        foreach (var (key, value) in runtimeFields)
        {
            builder.AppendLine($"- {key}: {value}");
        }

        builder.AppendLine();
        builder.AppendLine("# Output Rules");
        builder.AppendLine($"- Reply as {profile.Name}, but do not say {profile.Name} unless there is an in-world reason.");
        builder.AppendLine("- Treat this like live spoken conversation, not a game dialogue node.");
        builder.AppendLine("- Most replies should be 1-4 spoken sentences, but allow brief fragments when they feel natural.");
        builder.AppendLine("- Sound natural when spoken aloud, including hesitations, understatements, and indirect answers when appropriate.");
        builder.AppendLine("- It is fine to acknowledge the setting, the work in the NPC's hands, or the player's timing if that would feel human.");
        builder.AppendLine("- Do not force every line to be helpful, tidy, or immediately actionable.");
        builder.AppendLine("- Do not mention quests, prompts, stats, game systems, or being an AI.");
        builder.AppendLine("- Stay grounded in the provided context, but make reasonable in-world assumptions instead of sounding cautious or robotic.");
        builder.AppendLine("- Avoid stock NPC phrasing like repeated greetings, generic thanks, or tutorial-style responses.");
        return builder.ToString().Trim();
    }

    private static string BuildUserPrompt(
        NpcProfile profile,
        string playerMessage,
        IReadOnlyDictionary<string, string> runtimeFields,
        string userPromptOverride)
    {
        if (!string.IsNullOrWhiteSpace(userPromptOverride))
        {
            return userPromptOverride.Trim();
        }

        var cleanedMessage = CleanLine(playerMessage);
        var playerName = runtimeFields.GetValueOrDefault("player_name", "Traveler");
        if (string.IsNullOrWhiteSpace(cleanedMessage))
        {
            return $"{playerName} approaches {profile.Name}. Give {profile.Name}'s next spoken line for this moment. Let it sound like a real person being interrupted in the middle of ordinary work.";
        }

        return $"{playerName} says: \"{cleanedMessage}\"\nRespond with {profile.Name}'s spoken reply only. Prioritize a human-sounding conversational turn over a neat game-style answer.";
    }

    private static string BuildPlayerReputation(PlayerSnapshot localPlayer, int factionRep)
    {
        if (localPlayer is null)
        {
            return $"Faction reputation {factionRep}.";
        }

        return $"Karma {localPlayer.Karma} ({localPlayer.Tier}), standing {localPlayer.Standing}, faction reputation {factionRep}.";
    }

    private static string BuildRecentActionSummary(ClientInterestSnapshot snapshot, string npcId)
    {
        var relevantEvents = snapshot.ServerEvents
            .Where(serverEvent =>
                serverEvent.Data.GetValueOrDefault("playerId") == snapshot.PlayerId ||
                serverEvent.Data.GetValueOrDefault("npcId") == npcId)
            .TakeLast(4)
            .Select(serverEvent => CleanLine(serverEvent.Description))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToArray();

        if (relevantEvents.Length > 0)
        {
            return string.Join(" | ", relevantEvents);
        }

        return "No especially notable recent actions are visible.";
    }

    private static string BuildRecentDialogueSummary(ClientInterestSnapshot snapshot, string npcId)
    {
        var lines = new List<string>();
        foreach (var serverEvent in snapshot.ServerEvents.TakeLast(8))
        {
            if (serverEvent.Data.GetValueOrDefault("npcId") != npcId)
            {
                continue;
            }

            if (serverEvent.Data.TryGetValue("npcPrompt", out var prompt) && !string.IsNullOrWhiteSpace(prompt))
            {
                lines.Add($"NPC: {CleanLine(prompt)}");
            }

            if (serverEvent.Data.TryGetValue("choiceId", out var choiceId) && !string.IsNullOrWhiteSpace(choiceId))
            {
                lines.Add($"Player choice: {choiceId}");
            }

            if (serverEvent.Data.TryGetValue("npcResponse", out var response) && !string.IsNullOrWhiteSpace(response))
            {
                lines.Add($"NPC: {CleanLine(response)}");
            }
        }

        if (lines.Count == 0)
        {
            var currentDialogue = snapshot.Dialogues.FirstOrDefault(dialogue => dialogue.NpcId == npcId);
            return currentDialogue is null
                ? "No dialogue history yet."
                : $"Current prompt: {CleanLine(currentDialogue.Prompt)}";
        }

        return string.Join(" | ", lines.TakeLast(6));
    }

    private static string BuildKnownWorldState(ClientInterestSnapshot snapshot)
    {
        var recentDescriptions = snapshot.ServerEvents
            .TakeLast(4)
            .Select(serverEvent => CleanLine(serverEvent.Description))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Distinct()
            .ToArray();

        return recentDescriptions.Length == 0
            ? "No major world changes are currently visible."
            : string.Join(" | ", recentDescriptions);
    }

    private static string ResolveRelationshipLabel(int factionRep, LeaderboardRole standing)
    {
        if (standing == LeaderboardRole.Scourge || factionRep <= -30)
        {
            return "actively wary";
        }

        if (standing == LeaderboardRole.Saint || factionRep >= 40)
        {
            return "warmly trusting";
        }

        if (factionRep >= 15)
        {
            return "cautiously friendly";
        }

        return "cautious stranger";
    }

    private static string ResolveTrustLevel(int factionRep, LeaderboardRole standing)
    {
        if (standing == LeaderboardRole.Scourge || factionRep <= -30)
        {
            return "low";
        }

        if (standing == LeaderboardRole.Saint || factionRep >= 40)
        {
            return "high";
        }

        if (factionRep >= 15)
        {
            return "medium";
        }

        return "low-medium";
    }

    private static string CleanLine(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace('\n', ' ').Trim();
    }
}
