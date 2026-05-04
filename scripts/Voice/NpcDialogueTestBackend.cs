using System;
using System.Collections.Generic;
using Godot;
using Karma.Data;
using Karma.Net;

namespace Karma.Voice;

public sealed record NpcDialogueTestReply(
    string Reply,
    NpcLlmPromptPackage PromptPackage,
    string BackendLabel);

public static class NpcDialogueTestBackend
{
    public static bool TryGenerateReply(
        PrototypeServerSession session,
        string npcId,
        string playerMessage,
        out NpcDialogueTestReply reply,
        out string error)
    {
        reply = null;
        error = string.Empty;

        if (!NpcLlmPromptBuilder.TryBuild(session, npcId, playerMessage, out var promptPackage, out error))
        {
            return false;
        }

        var spokenReply = npcId switch
        {
            var id when id == StarterNpcs.Mara.Id => BuildMaraReply(promptPackage, playerMessage),
            _ => "What do you need?"
        };

        reply = new NpcDialogueTestReply(spokenReply, promptPackage, "Local Mara test backend");
        return true;
    }

    private static string BuildMaraReply(NpcLlmPromptPackage promptPackage, string playerMessage)
    {
        var message = Clean(playerMessage).ToLowerInvariant();
        var trustLevel = promptPackage.RuntimeFields.GetValueOrDefault("trust_level", "low-medium");
        var currentProblem = promptPackage.RuntimeFields.GetValueOrDefault("current_problem", "The clinic filters need work.");
        var followDisposition = promptPackage.RuntimeFields.GetValueOrDefault("follow_disposition", string.Empty);
        var urgency = promptPackage.RuntimeFields.GetValueOrDefault("mara_current_urgency", string.Empty).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Well hello there, traveler. Mind the sparks. What brings you by?";
        }

        if (ContainsAny(message, "hello", "hi", "hey", "greetings", "good morning", "good afternoon"))
        {
            return "Well hello there, traveler. Mind the sparks. What brings you by?";
        }

        if (ContainsAny(message, "help", "filters", "clinic", "repair"))
        {
            return "I would be grateful. The clinic filters have been clogging again, and the children need clean air before nightfall. Could you take a look when you have a moment?";
        }

        if (ContainsAny(message, "supplies", "bandages", "rations", "iron", "parts"))
        {
            return "Bandages first. Rations if you have them. Spare iron too, if any can be spared.";
        }

        if (ContainsAny(message, "who are you", "what do you do", "what is your work", "blacksmith", "forge"))
        {
            return "I keep the forge running and mend what this village cannot afford to lose.";
        }

        if (ContainsAny(message, "thank you", "thanks", "appreciate it"))
        {
            return "Aye. You are welcome. Every steady hand helps.";
        }

        if (ContainsAny(message, "baron", "forbidden", "secret", "stolen", "stores"))
        {
            return trustLevel is "high" or "medium"
                ? "I use what I can get, and I don't ask permission from people who've never missed a meal. Leave it there."
                : "You're digging where you don't need to. Ask me about the work in front of us.";
        }

        if (ContainsAny(message, "how are you", "how have you been"))
        {
            return "Busy. Tired too, if I am honest. Still, the work needs doing, so here I am.";
        }

        if (ContainsAny(message, "bye", "farewell", "goodbye", "later"))
        {
            return "All right. Mind yourself out there.";
        }

        if (ContainsAny(message, "what do you need", "what's wrong", "what is wrong", "what's needed"))
        {
            return Clean(currentProblem).TrimEnd('.') + ". If you've got the time, I could use the help.";
        }

        if (ContainsAny(message, "follow me", "come with me", "come along", "walk with me", "join me"))
        {
            if (followDisposition.Contains("does not trust", StringComparison.OrdinalIgnoreCase))
            {
                return "No. I've seen enough to know better than to trail after you.";
            }

            if (followDisposition.Contains("not ready", StringComparison.OrdinalIgnoreCase) ||
                urgency.Contains("high", StringComparison.OrdinalIgnoreCase))
            {
                return "Not yet. I've got too much riding on this work to leave it on a whim.";
            }

            return "All right, then. Lead on, and do not make me regret stepping away from the forge.";
        }

        if (ContainsAny(message, "wait here", "stay here", "stay put", "stop following", "hang back"))
        {
            return "Fine. I will stay here for the moment, but do not be long.";
        }

        return "Tell me plain, then. Are you here to trade, lend a hand, or ask after the clinic?";
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Clean(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace('\n', ' ').Trim();
    }
}
