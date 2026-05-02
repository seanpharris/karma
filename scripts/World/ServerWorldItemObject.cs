using Godot;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.World;

public partial class ServerWorldItemObject : Area2D
{
    public string EntityId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string DropOwnerId { get; set; } = string.Empty;
    public string DropOwnerName { get; set; } = string.Empty;
    public long DropOwnerExpiresTick { get; set; }

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
        if (!_playerNearby || !@event.IsActionPressed("interact") || _serverSession is null)
        {
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = EntityId
            });
        if (result.WasAccepted)
        {
            QueueFree();
            return;
        }

        _hud?.ShowPrompt(result.RejectionReason);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not CharacterBody2D)
        {
            return;
        }

        _playerNearby = true;
        var item = StarterItems.GetById(ItemId);
        var prompt = string.IsNullOrWhiteSpace(DropOwnerName)
            ? ItemText.FormatPickupPrompt(item)
            : $"Press E to claim {DropOwnerName}'s Karma Break drop: {item.Name}";
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
