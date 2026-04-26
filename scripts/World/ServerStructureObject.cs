using System.Collections.Generic;
using Godot;
using Karma.Net;
using Karma.UI;

namespace Karma.World;

public partial class ServerStructureObject : Area2D
{
    public string EntityId { get; set; } = string.Empty;
    public string StructureName { get; set; } = string.Empty;
    public string InteractionPrompt { get; set; } = string.Empty;
    public bool IsInteractable { get; set; }

    private bool _playerNearby;
    private HudController _hud;
    private PrototypeServerSession _serverSession;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby || !IsInteractable || _serverSession is null)
        {
            return;
        }

        var action = ResolveAction(@event);
        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.Interact,
            new Dictionary<string, string>
            {
                ["entityId"] = EntityId,
                ["action"] = action
            });
        _hud?.ShowPrompt(result.WasAccepted ? result.Event.Data["result"] : result.RejectionReason);
    }

    private static string ResolveAction(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            return "inspect";
        }

        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.J)
            {
                return "repair";
            }

            if (key.Keycode == Key.K)
            {
                return "sabotage";
            }
        }

        return string.Empty;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not CharacterBody2D)
        {
            return;
        }

        _playerNearby = true;
        var prompt = string.IsNullOrWhiteSpace(InteractionPrompt)
            ? $"Press E to inspect {StructureName}."
            : InteractionPrompt;
        _hud?.ShowPrompt(prompt);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is not CharacterBody2D)
        {
            return;
        }

        _playerNearby = false;
        _hud?.HidePrompt();
    }
}
