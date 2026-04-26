using Godot;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.Player;

public partial class PeerStandInController : Area2D
{
    private bool _playerNearby;
    private bool _hasBeenHelped;
    private bool _hasBeenRobbed;
    private HudController _hud;
    private GameState _gameState;
    private PrototypeServerSession _serverSession;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        _gameState.SetPlayerPosition("peer_stand_in", ToTilePosition(GlobalPosition));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby || !(@event is InputEventKey { Pressed: true, Echo: false } key))
        {
            return;
        }

        if (key.Keycode == Key.Key1)
        {
            HelpPeer();
        }
        else if (key.Keycode == Key.Key2)
        {
            AttackPeer();
        }
        else if (key.Keycode == Key.Key3)
        {
            RobPeer();
        }
        else if (key.Keycode == Key.Key4)
        {
            ReturnItem();
        }
        else if (key.Keycode == Key.Key5)
        {
            RequestDuel();
        }
        else if (key.Keycode == Key.Key6)
        {
            AcceptDuelAsPeer();
        }
        else if (key.Keycode == Key.Key7)
        {
            GiftScrip();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not CharacterBody2D)
        {
            return;
        }

        _playerNearby = true;
        ShowPrompt();
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

    private void HelpPeer()
    {
        if (_hasBeenHelped)
        {
            _hud?.ShowPrompt("They are already patched up. The helpful hovering is noted emotionally, not numerically.");
            return;
        }

        if (SendKarmaAction(PrototypeActions.HelpPeerId))
        {
            _hasBeenHelped = true;
            ShowPrompt();
        }
    }

    private void AttackPeer()
    {
        Send(
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in"
            });
    }

    private void RobPeer()
    {
        if (_hasBeenRobbed)
        {
            _hud?.ShowPrompt("The satchel has already been robbed. There is only lint and consequences.");
            return;
        }

        if (Send(
                IntentType.TransferItem,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["targetId"] = "peer_stand_in",
                    ["itemId"] = StarterItems.RepairKitId,
                    ["mode"] = "steal"
                }))
        {
            _hasBeenRobbed = true;
            ShowPrompt();
        }
    }

    private void ReturnItem()
    {
        if (!_hasBeenRobbed)
        {
            _hud?.ShowPrompt("There is nothing stolen to return yet.");
            return;
        }

        if (Send(
                IntentType.TransferItem,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["targetId"] = "peer_stand_in",
                    ["itemId"] = StarterItems.RepairKitId,
                    ["mode"] = "gift"
                }))
        {
            _hasBeenRobbed = false;
            ShowPrompt();
        }
    }

    private void RequestDuel()
    {
        Send(
            IntentType.RequestDuel,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in"
            });
    }

    private void AcceptDuelAsPeer()
    {
        SendAsPeer(
            IntentType.AcceptDuel,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["challengerId"] = GameState.LocalPlayerId
            });
    }

    private void GiftScrip()
    {
        Send(
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "5"
            });
    }

    private bool SendKarmaAction(string actionId)
    {
        return Send(
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = actionId
            });
    }

    private bool Send(IntentType intentType, System.Collections.Generic.IReadOnlyDictionary<string, string> payload)
    {
        if (_serverSession is null)
        {
            return false;
        }

        var result = _serverSession.SendLocal(intentType, payload);
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
        }

        return result.WasAccepted;
    }

    private bool SendAsPeer(IntentType intentType, System.Collections.Generic.IReadOnlyDictionary<string, string> payload)
    {
        if (_serverSession is null)
        {
            return false;
        }

        var result = _serverSession.Send("peer_stand_in", intentType, payload);
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
        }

        return result.WasAccepted;
    }

    private void ShowPrompt()
    {
        var robbedState = _hasBeenRobbed ? "Satchel: stolen" : "Satchel: nearby";
        _hud?.ShowPrompt(
            "Another player is stranded near the path.\n" +
            $"{robbedState}\n\n" +
            "1 - Help patch their gear\n" +
            "2 - Attack them outside a duel\n" +
            "3 - Rob their dropped satchel\n" +
            "4 - Return a lost item\n" +
            "5 - Request a friendly duel\n" +
            "6 - Let them accept the duel\n" +
            "7 - Gift 5 scrip\n\n" +
            "Z - Equip Practice Stick\n" +
            "X - Equip Work Vest\n" +
            "C - Place first loose inventory item\n" +
            "R - Use Repair Kit on them");
    }

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }
}
