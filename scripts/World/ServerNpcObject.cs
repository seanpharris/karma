using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.World;

public partial class ServerNpcObject : Area2D
{
    public string NpcId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Faction { get; set; } = string.Empty;
    public PrototypeSpriteKind SpriteKind { get; set; } = PrototypeSpriteKind.Peer;

    private bool _playerNearby;
    private int _selectedOfferIndex;
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
        if (!_playerNearby || NpcId != StarterNpcs.Dallen.Id || @event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode == Key.Minus)
        {
            CycleShopOffer(-1);
        }
        else if (key.Keycode == Key.Equal)
        {
            CycleShopOffer(1);
        }
        else if (key.Keycode == Key.Key9)
        {
            PurchaseSelectedOffer();
        }
    }

    public static string FormatPrompt(string displayName, string role, string faction)
    {
        var safeName = string.IsNullOrWhiteSpace(displayName) ? "Unknown NPC" : displayName;
        var safeRole = string.IsNullOrWhiteSpace(role) ? "Wanderer" : role;
        var safeFaction = string.IsNullOrWhiteSpace(faction) ? "Unaffiliated" : faction;
        return $"{safeName}\n{safeRole}\nFaction: {safeFaction}";
    }

    public static string FormatVendorPrompt(
        string displayName,
        string role,
        string faction,
        IReadOnlyList<ShopOfferSnapshot> offers,
        int selectedOfferIndex)
    {
        var lines = new List<string>
        {
            FormatPrompt(displayName, role, faction),
            string.Empty
        };

        if (offers is null || offers.Count == 0)
        {
            lines.Add("No shop offers nearby.");
            return string.Join("\n", lines);
        }

        var index = WrapIndex(selectedOfferIndex, offers.Count);
        var offer = offers[index];
        lines.Add($"9 - Buy {offer.ItemName} ({offer.Price} {offer.Currency})");
        lines.Add($"-/= - Browse shop ({index + 1}/{offers.Count})");
        return string.Join("\n", lines);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _playerNearby = true;
            ShowPrompt();
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _playerNearby = false;
            _hud?.HidePrompt();
        }
    }

    private void CycleShopOffer(int direction)
    {
        var offers = GetVisibleOffers();
        if (offers.Count == 0)
        {
            _hud?.ShowPrompt(FormatVendorPrompt(DisplayName, Role, Faction, offers, _selectedOfferIndex));
            return;
        }

        _selectedOfferIndex = WrapIndex(_selectedOfferIndex + direction, offers.Count);
        ShowPrompt();
    }

    private void PurchaseSelectedOffer()
    {
        if (_serverSession is null)
        {
            return;
        }

        var offers = GetVisibleOffers();
        if (offers.Count == 0)
        {
            _hud?.ShowPrompt(FormatVendorPrompt(DisplayName, Role, Faction, offers, _selectedOfferIndex));
            return;
        }

        _selectedOfferIndex = WrapIndex(_selectedOfferIndex, offers.Count);
        var result = _serverSession.PurchaseOffer(offers[_selectedOfferIndex].OfferId);
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
            return;
        }

        ShowPrompt();
    }

    private void ShowPrompt()
    {
        if (NpcId == StarterNpcs.Dallen.Id)
        {
            _hud?.ShowPrompt(FormatVendorPrompt(DisplayName, Role, Faction, GetVisibleOffers(), _selectedOfferIndex));
            return;
        }

        _hud?.ShowPrompt(FormatPrompt(DisplayName, Role, Faction));
    }

    private IReadOnlyList<ShopOfferSnapshot> GetVisibleOffers()
    {
        return _serverSession?.LastLocalSnapshot?.ShopOffers
            .Where(offer => offer.VendorNpcId == NpcId)
            .ToArray() ?? System.Array.Empty<ShopOfferSnapshot>();
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return ((index % count) + count) % count;
    }
}
