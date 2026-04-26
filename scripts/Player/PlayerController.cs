using Godot;
using System.Linq;
using Karma.Core;
using Karma.Data;
using Karma.Net;

namespace Karma.Player;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 120f;
    private GameState _gameState = null!;
    private PrototypeServerSession _serverSession;
    private TilePosition? _lastSentTile;
    private Vector2I _lastFacing = Vector2I.Down;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        SendMoveIfTileChanged();
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (direction.LengthSquared() > 0)
        {
            _lastFacing = Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
                ? new Vector2I(direction.X > 0 ? 1 : -1, 0)
                : new Vector2I(0, direction.Y > 0 ? 1 : -1);
        }

        Velocity = direction * Speed;
        MoveAndSlide();

        SendMoveIfTileChanged();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode == Key.Z)
        {
            EquipThroughServer(StarterItems.PracticeStickId);
        }
        else if (key.Keycode == Key.X)
        {
            EquipThroughServer(StarterItems.WorkVestId);
        }
        else if (key.Keycode == Key.C)
        {
            PlaceFirstLooseItemThroughServer();
        }
    }

    private void EquipThroughServer(string itemId)
    {
        if (_serverSession is null)
        {
            return;
        }

        _serverSession.SendLocal(
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = itemId
            });
    }

    private void PlaceFirstLooseItemThroughServer()
    {
        if (_serverSession is null)
        {
            return;
        }

        var item = _gameState.Inventory.FirstOrDefault(candidate => candidate.Slot == EquipmentSlot.None);
        if (item is null)
        {
            return;
        }

        var playerTile = ToTilePosition(GlobalPosition);
        var placeTile = new TilePosition(
            playerTile.X + _lastFacing.X,
            playerTile.Y + _lastFacing.Y);

        _serverSession.SendLocal(
            IntentType.PlaceObject,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = item.Id,
                ["x"] = placeTile.X.ToString(),
                ["y"] = placeTile.Y.ToString()
            });
    }

    private void SendMoveIfTileChanged()
    {
        var tile = ToTilePosition(GlobalPosition);
        if (_lastSentTile == tile)
        {
            return;
        }

        _lastSentTile = tile;

        if (_serverSession is null)
        {
            _gameState.SetPlayerPosition(GameState.LocalPlayerId, tile);
            return;
        }

        _serverSession.SendLocal(
            IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["x"] = tile.X.ToString(),
                ["y"] = tile.Y.ToString()
            });
    }

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }
}
