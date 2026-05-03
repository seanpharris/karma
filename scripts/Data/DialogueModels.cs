using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

// Declarative dialogue trees. Each node has text plus zero or more choices;
// each choice can advance to a next node id, fire an action id (mirroring
// the existing NpcDialogueChoice ActionId), or terminate the conversation.
//
// Existing flat `NpcDialogueChoice[]` arrays per NPC remain valid for now;
// this is the seam authors can grow into for branching dialogue without
// editing server code. `BuildChoiceArray` adapts a node into the legacy
// shape so today's HUD/server consumers can keep working unchanged.
public sealed record DialogueChoice(
    string Id,
    string Label,
    string NextNodeId = "",
    string ActionId = "",
    string RequiredItemId = "",
    bool Terminates = false);

public sealed record DialogueNode(
    string Id,
    string Text,
    IReadOnlyList<DialogueChoice> Choices);

public sealed record DialogueTree(
    string Id,
    string RootNodeId,
    IReadOnlyDictionary<string, DialogueNode> Nodes)
{
    public DialogueNode Root =>
        Nodes.TryGetValue(RootNodeId, out var root) ? root : null;

    public DialogueNode Get(string nodeId) =>
        Nodes.TryGetValue(nodeId ?? string.Empty, out var node) ? node : null;
}

public static class DialogueRegistry
{
    public const string MaraClinicTreeId = "mara_clinic_default";
    public const string DallenShopkeeperTreeId = "dallen_shopkeeper_default";

    private static readonly Dictionary<string, DialogueTree> BuiltIns = new()
    {
        [MaraClinicTreeId] = new DialogueTree(
            Id: MaraClinicTreeId,
            RootNodeId: "root",
            Nodes: new Dictionary<string, DialogueNode>
            {
                ["root"] = new DialogueNode(
                    "root",
                    "Mara: \"The forge keeps the village mended. Got a few minutes?\"",
                    new[]
                    {
                        new DialogueChoice("offer_help", "I can scrounge the iron you need.",
                            NextNodeId: "accepted"),
                        new DialogueChoice("ask_about_supplies", "Anything else short?",
                            NextNodeId: "supplies"),
                        new DialogueChoice("decline", "Maybe later.", Terminates: true),
                    }),
                ["accepted"] = new DialogueNode(
                    "accepted",
                    "Mara: \"You're a saint. Bring it back when you've got it.\"",
                    new[]
                    {
                        new DialogueChoice("close", "On my way.",
                            ActionId: "help_mara",
                            Terminates: true),
                    }),
                ["supplies"] = new DialogueNode(
                    "supplies",
                    "Mara: \"Bandages mostly. Rations would help. Any spare iron, really.\"",
                    new[]
                    {
                        new DialogueChoice("back_to_root", "Tell me about the forge work again.",
                            NextNodeId: "root"),
                        new DialogueChoice("close", "I'll keep it in mind.", Terminates: true),
                    }),
            }),

        [DallenShopkeeperTreeId] = new DialogueTree(
            Id: DallenShopkeeperTreeId,
            RootNodeId: "root",
            Nodes: new Dictionary<string, DialogueNode>
            {
                ["root"] = new DialogueNode(
                    "root",
                    "Dallen: \"Keep your voice low and your coin counted. What do you need?\"",
                    new[]
                    {
                        new DialogueChoice("browse_wares", "Browse wares.",
                            NextNodeId: "wares"),
                        new DialogueChoice("sell_items", "Sell items.",
                            ActionId: "open_shop_sell"),
                        new DialogueChoice("ask_about_mara", "Ask about Mara.",
                            NextNodeId: "mara"),
                        new DialogueChoice("leave", "Leave.", Terminates: true),
                    }),
                ["wares"] = new DialogueNode(
                    "wares",
                    "Dallen: \"Tools, rations, oddments. Nothing fancy, but it all works.\"",
                    new[]
                    {
                        new DialogueChoice("back_to_root", "Something else.",
                            NextNodeId: "root"),
                        new DialogueChoice("close", "I'll look around.", Terminates: true),
                    }),
                ["mara"] = new DialogueNode(
                    "mara",
                    "Dallen: \"Mara fixes what others abandon. She keeps more of this village alive than she admits.\"",
                    new[]
                    {
                        new DialogueChoice("back_to_root", "Back to business.",
                            NextNodeId: "root"),
                        new DialogueChoice("close", "Good to know.", Terminates: true),
                    }),
            }),
    };

    private static readonly Dictionary<string, DialogueTree> _runtimeOverrides = new();

    public static IReadOnlyDictionary<string, DialogueTree> All
    {
        get
        {
            var merged = new Dictionary<string, DialogueTree>(BuiltIns);
            foreach (var (key, value) in _runtimeOverrides)
                merged[key] = value;
            return merged;
        }
    }

    public static void Register(DialogueTree tree)
    {
        if (tree is null || string.IsNullOrEmpty(tree.Id)) return;
        _runtimeOverrides[tree.Id] = tree;
    }

    public static void Reset()
    {
        _runtimeOverrides.Clear();
    }

    public static bool TryGet(string id, out DialogueTree tree)
    {
        if (_runtimeOverrides.TryGetValue(id, out tree)) return true;
        return BuiltIns.TryGetValue(id, out tree);
    }

    // Adapter so existing `NpcDialogueChoice[]` consumers can render a node's
    // choices without knowing about the tree shape. Skips terminating-only
    // choices that have no ActionId since the legacy shape always implies an
    // action; non-terminating choices project their NextNodeId into the
    // ActionId slot prefixed so dispatchers can route accordingly.
    public static IReadOnlyList<Karma.Net.NpcDialogueChoice> BuildChoiceArray(DialogueNode node)
    {
        if (node is null) return System.Array.Empty<Karma.Net.NpcDialogueChoice>();
        return node.Choices
            .Select(c => new Karma.Net.NpcDialogueChoice(
                Id: c.Id,
                Label: c.Label,
                ActionId: !string.IsNullOrEmpty(c.ActionId)
                    ? c.ActionId
                    : !string.IsNullOrEmpty(c.NextNodeId)
                        ? $"dialogue_advance:{c.NextNodeId}"
                        : "dialogue_close",
                RequiredItemId: c.RequiredItemId))
            .ToArray();
    }
}
