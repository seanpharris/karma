using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.UI;
using Karma.World;

namespace Karma.Player;

public partial class PeerStandInController : Area2D
{
    private bool _playerNearby;
    private bool _hasBeenHelped;
    private bool _hasBeenRobbed;
    private HudController _hud;
    private GameState _gameState;
    private PrototypeServerSession _serverSession;
    private WorldHealthBar _healthBar;
    private PrototypeCharacterSprite _characterSprite;
    private int _peerHealth = 100;
    private int _peerMaxHealth = 100;
    private IReadOnlyList<string> _peerStatusEffects = System.Array.Empty<string>();
    private string _peerDuelState = "Duel: none";
    private PlayerAppearanceSelection _lastAppliedAppearance = null;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        _characterSprite = GetNodeOrNull<PrototypeCharacterSprite>("PeerSprite");
        _gameState.SetPlayerPosition("peer_stand_in", ToTilePosition(GlobalPosition));
        TopDownDepth.Apply(this);
        AddHealthBar();
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            ApplySnapshot(_serverSession.LastLocalSnapshot);
        }
    }

    public override void _ExitTree()
    {
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged -= OnLocalSnapshotChanged;
        }
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
        else if (key.Keycode == Key.Key8)
        {
            LetPeerAttackLocal();
        }
        else if (key.Keycode == Key.Key9)
        {
            StealScrip();
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
                ["amount"] = "5",
                ["mode"] = "gift"
            });
    }

    private void StealScrip()
    {
        Send(
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "3",
                ["mode"] = "steal"
            });
    }

    private void LetPeerAttackLocal()
    {
        SendAsPeer(
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = GameState.LocalPlayerId
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
        _hud?.ShowPrompt(FormatPrompt(
            _hasBeenRobbed,
            _peerHealth,
            _peerMaxHealth,
            _peerStatusEffects,
            _peerDuelState));
    }

    public static string FormatPrompt(
        bool hasBeenRobbed,
        int health,
        int maxHealth,
        IReadOnlyList<string> statusEffects,
        string duelState)
    {
        var safeMaxHealth = Mathf.Max(1, maxHealth);
        var clampedHealth = Mathf.Clamp(health, 0, safeMaxHealth);
        var robbedState = hasBeenRobbed ? "Satchel: stolen" : "Satchel: nearby";
        var statusText = statusEffects is null || statusEffects.Count == 0
            ? "Status: none"
            : HudController.FormatStatusEffects(statusEffects);
        var duelText = string.IsNullOrWhiteSpace(duelState) ? "Duel: none" : duelState;
        var attackLabel = FormatAttackLabel(statusEffects, duelText);
        var requestDuelLabel = duelText.Contains("Requested") || duelText.Contains("Active")
            ? "5 - Duel already pending/active"
            : "5 - Request a friendly duel";
        var acceptDuelLabel = duelText.Contains("Requested")
            ? "6 - Let them accept the duel"
            : "6 - No duel request to accept";
        var peerAttackLabel = FormatPeerAttackLabel(statusEffects, duelText);
        return
            "Another player is stranded near the path.\n" +
            $"HP: {clampedHealth}/{safeMaxHealth}\n" +
            $"{statusText}\n" +
            $"{duelText}\n" +
            $"{robbedState}\n\n" +
            "1 - Help patch their gear\n" +
            $"{attackLabel}\n" +
            "3 - Rob their dropped satchel\n" +
            "4 - Return a lost item\n" +
            $"{requestDuelLabel}\n" +
            $"{acceptDuelLabel}\n" +
            "7 - Gift 5 scrip\n" +
            $"{peerAttackLabel}\n" +
            "9 - Swipe 3 scrip\n\n" +
            "Z - Equip Practice Stick\n" +
            "X - Equip Work Vest\n" +
            "C - Place first loose inventory item\n" +
            "R - Use Repair Kit on them\n" +
            "T - Use Repair Kit on yourself";
    }

    public static string FormatAttackLabel(IReadOnlyList<string> statusEffects, string duelState)
    {
        if (statusEffects?.Any(status => status.Contains("Karma Break Grace")) == true)
        {
            return "2 - Attack blocked by Karma Break grace";
        }

        if (!string.IsNullOrWhiteSpace(duelState) && duelState.Contains("Active"))
        {
            return "2 - Attack as duel strike";
        }

        return "2 - Attack them outside a duel";
    }

    public static string FormatPeerAttackLabel(IReadOnlyList<string> statusEffects, string duelState)
    {
        if (statusEffects?.Any(status => status.Contains("Attack Cooldown")) == true)
        {
            return "8 - Their attack is cooling down";
        }

        if (!string.IsNullOrWhiteSpace(duelState) && duelState.Contains("Active"))
        {
            return "8 - Let them duel strike you";
        }

        return "8 - Let them attack you";
    }

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }

    private void AddHealthBar()
    {
        _healthBar = new WorldHealthBar
        {
            Name = "HealthBar",
            DisplayName = "Stranded Player",
            Position = new Vector2(0f, -48f),
            ZIndex = 10
        };
        AddChild(_healthBar);
        if (_gameState.Players.TryGetValue("peer_stand_in", out var peer))
        {
            _peerHealth = peer.Health;
            _peerMaxHealth = peer.MaxHealth;
            _healthBar.SetHealth(peer.Health, peer.MaxHealth);
        }
    }

    private void OnLocalSnapshotChanged(string snapshotSummary)
    {
        ApplySnapshot(_serverSession?.LastLocalSnapshot);
    }

    private void ApplySnapshot(ClientInterestSnapshot snapshot)
    {
        var peer = snapshot?.Players.FirstOrDefault(player => player.Id == "peer_stand_in");
        if (peer is not null)
        {
            ApplyAppearance(peer.Appearance);
            GlobalPosition = PlayerController.CalculateWorldPosition(peer.TileX, peer.TileY);
            TopDownDepth.Apply(this);
            _peerHealth = peer.Health;
            _peerMaxHealth = peer.MaxHealth;
            _peerStatusEffects = peer.StatusEffects;
            _peerDuelState = FormatDuelState(snapshot);
            _healthBar?.SetHealth(peer.Health, peer.MaxHealth);
            _healthBar?.SetStatusEffects(peer.StatusEffects);
            if (_playerNearby)
            {
                ShowPrompt();
            }
        }
    }

    private void ApplyAppearance(PlayerAppearanceSelection appearance)
    {
        if (_characterSprite is null || _lastAppliedAppearance == appearance)
        {
            return;
        }

        _characterSprite.ApplyPlayerAppearanceSelection(appearance);
        _lastAppliedAppearance = appearance;
    }

    private static string FormatDuelState(ClientInterestSnapshot snapshot)
    {
        var duel = snapshot.Duels.FirstOrDefault(candidate =>
            (candidate.ChallengerId == GameState.LocalPlayerId && candidate.TargetId == "peer_stand_in") ||
            (candidate.ChallengerId == "peer_stand_in" && candidate.TargetId == GameState.LocalPlayerId));
        return duel is null
            ? "Duel: none"
            : $"Duel: {duel.Status}";
    }
}
