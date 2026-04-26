using Godot;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.World;

public partial class PickupObject : Area2D
{
    [Export] public string ItemId { get; set; } = StarterItems.WhoopieCushionId;
    [Export] public string EntityId { get; set; } = string.Empty;

    private bool _playerNearby;
    private HudController _hud;
    private PrototypeServerSession _serverSession;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        EntityId = string.IsNullOrWhiteSpace(EntityId) ? Name.ToString() : EntityId;
        TopDownDepth.Apply(this, TopDownDepth.ItemOffsetZ);

        if (_serverSession is not null && StarterItems.TryGetById(ItemId, out var item))
        {
            _serverSession.RegisterWorldItem(EntityId, item, ToTilePosition(GlobalPosition));
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby || !@event.IsActionPressed("interact"))
        {
            return;
        }

        if (_serverSession is null)
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
        _hud?.ShowPrompt(ItemText.FormatPickupPrompt(item));
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

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }
}
