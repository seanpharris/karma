using Godot;
using Karma.Art;
using Karma.UI;

namespace Karma.World;

public partial class ServerNpcObject : Area2D
{
    public string NpcId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Faction { get; set; } = string.Empty;
    public PrototypeSpriteKind SpriteKind { get; set; } = PrototypeSpriteKind.Peer;

    private HudController _hud;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
    }

    public static string FormatPrompt(string displayName, string role, string faction)
    {
        var safeName = string.IsNullOrWhiteSpace(displayName) ? "Unknown NPC" : displayName;
        var safeRole = string.IsNullOrWhiteSpace(role) ? "Wanderer" : role;
        var safeFaction = string.IsNullOrWhiteSpace(faction) ? "Unaffiliated" : faction;
        return $"{safeName}\n{safeRole}\nFaction: {safeFaction}";
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _hud?.ShowPrompt(FormatPrompt(DisplayName, Role, Faction));
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _hud?.HidePrompt();
        }
    }
}
