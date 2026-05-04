using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Karma.Data;

namespace Karma.Voice;

public static class NpcContextRepository
{
    private static readonly IReadOnlyDictionary<string, string> KnownContextPaths = new Dictionary<string, string>
    {
        [StarterNpcs.Mara.Id] = "res://docs/worldbuilding/npc-context/mara.md"
    };

    public static bool TryLoad(string npcId, out string contextMarkdown, out string contextPath)
    {
        contextMarkdown = string.Empty;
        contextPath = ResolvePath(npcId);
        if (string.IsNullOrWhiteSpace(contextPath))
        {
            return false;
        }

        var absolutePath = ProjectSettings.GlobalizePath(contextPath);
        if (!File.Exists(absolutePath))
        {
            GD.PushWarning($"NPC context file was not found for {npcId}: {absolutePath}");
            return false;
        }

        contextMarkdown = File.ReadAllText(absolutePath).Trim();
        return !string.IsNullOrWhiteSpace(contextMarkdown);
    }

    public static string ResolvePath(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return string.Empty;
        }

        if (KnownContextPaths.TryGetValue(npcId, out var knownPath))
        {
            return knownPath;
        }

        var slug = npcId.Trim().ToLowerInvariant();
        return $"res://docs/worldbuilding/npc-context/{slug}.md";
    }
}
