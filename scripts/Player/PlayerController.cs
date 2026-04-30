using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.UI;
using Karma.Util;
using Karma.World;

namespace Karma.Player;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 120f;
    [Export] public float SprintMultiplier { get; set; } = 1.6f;
    [Export] public float Acceleration { get; set; } = 1200f;
    [Export] public float Friction { get; set; } = 1800f;
    [Export] public float MaxStamina { get; set; } = 100f;
    [Export] public float SprintStaminaCostPerSecond { get; set; } = 24f;
    [Export] public float StaminaRecoveryPerSecond { get; set; } = 18f;
    [Export] public float SprintResumeStamina { get; set; } = 25f;
    [Export] public float MinCameraZoom { get; set; } = 1.25f;
    [Export] public float MaxCameraZoom { get; set; } = 5f;
    [Export] public float CameraZoomStep { get; set; } = 0.25f;

    private GameState _gameState = null!;
    private PrototypeServerSession _serverSession;
    private HudController _hud;
    private Camera2D _camera;
    private PrototypeCharacterSprite _characterSprite;
    private TilePosition? _lastSentTile;
    private Vector2 _predictedPosition;
    private Vector2I _lastFacing = Vector2I.Down;
    private float _stamina;
    private bool _isExhausted;
    private PlayerAppearanceSelection _lastAppliedAppearance = null;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        _characterSprite = GetNodeOrNull<PrototypeCharacterSprite>("PlayerSprite");
        _stamina = MaxStamina;
        UpdateStaminaHud();
        _predictedPosition = GlobalPosition;
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            ApplyAuthoritativePosition(_serverSession.LastLocalSnapshot);
        }

        SendMoveIfTileChanged();
    }

    public override void _ExitTree()
    {
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged -= OnLocalSnapshotChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (direction.LengthSquared() > 0)
        {
            _lastFacing = DirectionHelper.ToCardinalVector(direction);
        }

        var wantsSprint = Input.IsActionPressed("sprint");
        _isExhausted = CalculateExhausted(_isExhausted, _stamina, SprintResumeStamina);
        var isSprinting = CanSprint(direction, wantsSprint, _stamina, _isExhausted);
        var perks = _gameState.LocalPerks;
        Velocity = CalculateSmoothedVelocity(
            Velocity,
            direction,
            Speed,
            SprintMultiplier,
            isSprinting,
            Acceleration,
            Friction,
            (float)delta);
        _stamina = CalculateNextStamina(
            _stamina,
            delta,
            isSprinting,
            MaxStamina,
            CalculateEffectiveSprintCost(SprintStaminaCostPerSecond, perks),
            CalculateEffectiveStaminaRecovery(StaminaRecoveryPerSecond, perks));
        _isExhausted = CalculateExhausted(_isExhausted, _stamina, SprintResumeStamina);
        UpdateStaminaHud();
        MoveAndSlide();
        _predictedPosition = GlobalPosition;
        TopDownDepth.Apply(this);

        SendMoveIfTileChanged();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("attack"))
        {
            AttackNearestThroughServer();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("inventory_toggle"))
        {
            _hud?.ToggleInventoryOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        for (var i = 1; i <= 9; i++)
        {
            if (@event.IsActionPressed($"hotbar_{i}"))
            {
                EquipHotbarSlotThroughServer(i - 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseButton { Pressed: true } mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                AdjustCameraZoom(CameraZoomStep);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                AdjustCameraZoom(-CameraZoomStep);
            }

            return;
        }

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
        else if (key.Keycode == Key.R)
        {
            UseRepairKitOnPeerThroughServer();
        }
        else if (key.Keycode == Key.T)
        {
            UseRepairKitOnSelfThroughServer();
        }
        else if (key.Keycode == Key.V)
        {
            CycleAppearanceThroughServer(AppearanceCycleSlot.Skin);
        }
        else if (key.Keycode == Key.B)
        {
            CycleAppearanceThroughServer(AppearanceCycleSlot.Hair);
        }
        else if (key.Keycode == Key.N)
        {
            CycleAppearanceThroughServer(AppearanceCycleSlot.Outfit);
        }
        else if (key.Keycode == Key.O)
        {
            ToggleShopOverlayForNearestVendor(sellMode: false);
        }
        else if (key.Keycode == Key.K)
        {
            ToggleShopOverlayForNearestVendor(sellMode: true);
        }
    }

    private void ToggleShopOverlayForNearestVendor(bool sellMode)
    {
        if (_hud is null || _serverSession is null) return;
        if (_hud.IsShopOpen)
        {
            _hud.CloseShop();
            return;
        }

        var snapshot = _serverSession.LastLocalSnapshot;
        if (snapshot is null) return;
        var local = snapshot.Players.FirstOrDefault(p => p.Id == GameState.LocalPlayerId);
        if (local is null) return;

        var vendorIds = snapshot.ShopOffers.Select(o => o.VendorNpcId).Distinct().ToHashSet();
        var nearestVendor = snapshot.Npcs
            .Where(npc => vendorIds.Contains(npc.Id))
            .OrderBy(npc => (npc.TileX - local.TileX) * (npc.TileX - local.TileX) +
                            (npc.TileY - local.TileY) * (npc.TileY - local.TileY))
            .FirstOrDefault();
        if (nearestVendor is null)
        {
            _hud.ShowPrompt("No vendor in range.");
            return;
        }

        _hud.OpenShopForVendor(nearestVendor.Id, sellMode);
    }

    public void AdjustCameraZoom(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        var nextZoom = CalculateCameraZoom(_camera.Zoom.X, delta, MinCameraZoom, MaxCameraZoom);
        _camera.Zoom = new Vector2(nextZoom, nextZoom);
    }

    public static float CalculateCameraZoom(float currentZoom, float delta, float minZoom, float maxZoom)
    {
        var lower = Mathf.Min(minZoom, maxZoom);
        var upper = Mathf.Max(minZoom, maxZoom);
        return Mathf.Clamp(currentZoom + delta, lower, upper);
    }

    public static Vector2 CalculateVelocity(Vector2 direction, float speed, float sprintMultiplier, bool isSprinting)
    {
        var multiplier = isSprinting ? Mathf.Max(1f, sprintMultiplier) : 1f;
        return direction * speed * multiplier;
    }

    public static Vector2 CalculateSmoothedVelocity(
        Vector2 currentVelocity,
        Vector2 direction,
        float speed,
        float sprintMultiplier,
        bool isSprinting,
        float acceleration,
        float friction,
        float delta)
    {
        var targetVelocity = CalculateVelocity(direction, speed, sprintMultiplier, isSprinting);
        var rate = direction.LengthSquared() > 0f
            ? Mathf.Max(0f, acceleration)
            : Mathf.Max(0f, friction);
        return currentVelocity.MoveToward(targetVelocity, rate * Mathf.Max(0f, delta));
    }

    public static bool CanSprint(Vector2 direction, bool wantsSprint, float stamina, bool isExhausted)
    {
        return wantsSprint && !isExhausted && direction.LengthSquared() > 0f && stamina > 0f;
    }

    public static float CalculateNextStamina(
        float currentStamina,
        double delta,
        bool isSprinting,
        float maxStamina,
        float sprintCostPerSecond,
        float recoveryPerSecond)
    {
        var rate = isSprinting
            ? -Mathf.Max(0f, sprintCostPerSecond)
            : Mathf.Max(0f, recoveryPerSecond);
        return Mathf.Clamp(
            currentStamina + ((float)delta * rate),
            0f,
            Mathf.Max(0f, maxStamina));
    }

    public static bool CalculateExhausted(bool wasExhausted, float stamina, float resumeStamina)
    {
        if (stamina <= 0f)
        {
            return true;
        }

        return wasExhausted && stamina < Mathf.Max(0f, resumeStamina);
    }

    public static string FormatStamina(float stamina, float maxStamina, bool isExhausted)
    {
        var roundedStamina = Mathf.RoundToInt(stamina);
        var roundedMax = Mathf.RoundToInt(maxStamina);
        if (isExhausted)
        {
            return $"Stamina: {roundedStamina}/{roundedMax} (winded)";
        }

        return maxStamina > 0f && stamina / maxStamina <= 0.25f
            ? $"Stamina: {roundedStamina}/{roundedMax} (low)"
            : $"Stamina: {roundedStamina}/{roundedMax}";
    }

    public static float CalculateEffectiveSprintCost(float baseCost, IReadOnlyList<KarmaPerk> perks)
    {
        var cost = Mathf.Max(0f, baseCost);
        return HasPerk(perks, PerkCatalog.RenegadeNerveId)
            ? cost * 0.85f
            : cost;
    }

    public static float CalculateEffectiveStaminaRecovery(float baseRecovery, IReadOnlyList<KarmaPerk> perks)
    {
        var recovery = Mathf.Max(0f, baseRecovery);
        return HasPerk(perks, PerkCatalog.BeaconAuraId)
            ? recovery * 1.25f
            : recovery;
    }

    private static bool HasPerk(IReadOnlyList<KarmaPerk> perks, string perkId)
    {
        return perks.Any(perk => perk.Id == perkId);
    }

    private void UpdateStaminaHud()
    {
        _hud?.ShowStamina(FormatStamina(_stamina, MaxStamina, _isExhausted));
    }

    private void EquipThroughServer(string itemId)
    {
        SendLocalWithPrompt(
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = itemId
            });
    }

    private void EquipHotbarSlotThroughServer(int slotIndex)
    {
        if (_serverSession is null) return;
        var inventory = _gameState.Inventory;
        if (slotIndex < 0 || slotIndex >= inventory.Count) return;
        EquipThroughServer(inventory[slotIndex].Id);
    }

    private void AttackNearestThroughServer()
    {
        if (_serverSession is null) return;
        var snapshot = _serverSession.LastLocalSnapshot;
        if (snapshot is null) return;
        var combatRange = _serverSession.Server.Config.CombatRangeTiles;
        var target = HudController.FindAttackTarget(snapshot, GameState.LocalPlayerId, combatRange);
        if (target is null)
        {
            _hud?.ShowPrompt("No target in range.");
            return;
        }

        SendLocalWithPrompt(
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = target.Id
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

    private void UseRepairKitOnPeerThroughServer()
    {
        SendLocalWithPrompt(
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RepairKitId,
                ["targetId"] = "peer_stand_in"
            });
    }

    private void UseRepairKitOnSelfThroughServer()
    {
        SendLocalWithPrompt(
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RepairKitId,
                ["targetId"] = GameState.LocalPlayerId
            });
    }

    private enum AppearanceCycleSlot
    {
        Skin,
        Hair,
        Outfit
    }

    private void CycleAppearanceThroughServer(AppearanceCycleSlot slot)
    {
        var current = _serverSession?.LastLocalSnapshot?.Players
            .FirstOrDefault(player => player.Id == GameState.LocalPlayerId)?
            .Appearance ?? PlayerAppearanceSelection.Default;
        var payload = new System.Collections.Generic.Dictionary<string, string>();
        switch (slot)
        {
            case AppearanceCycleSlot.Hair:
                payload["hairLayerId"] = CycleHairLayerId(current.HairLayerId);
                break;
            case AppearanceCycleSlot.Outfit:
                payload["outfitLayerId"] = CycleOutfitLayerId(current.OutfitLayerId);
                break;
            default:
                payload["skinLayerId"] = CycleSkinLayerId(current.SkinLayerId);
                break;
        }

        SendLocalWithPrompt(IntentType.SetAppearance, payload);
    }

    public static string CycleSkinLayerId(string currentSkinLayerId)
    {
        return currentSkinLayerId switch
        {
            "skin_light_32x64" => "skin_medium_32x64",
            "skin_medium_32x64" => "skin_deep_32x64",
            "skin_deep_32x64" => "skin_light_32x64",
            "skin_light" => "skin_medium_32x64",
            "skin_medium" => "skin_deep_32x64",
            "skin_deep" => "skin_light_32x64",
            _ => "skin_medium_32x64"
        };
    }

    public static string CycleHairLayerId(string currentHairLayerId)
    {
        return currentHairLayerId switch
        {
            "hair_short_dark_32x64" => "hair_short_blond_32x64",
            "hair_short_blond_32x64" => "hair_short_copper_32x64",
            "hair_short_copper_32x64" => "hair_short_white_32x64",
            "hair_short_white_32x64" => "hair_short_dark_32x64",
            "hair_short_dark" => "hair_short_blond_32x64",
            "hair_short_blond" => "hair_short_copper_32x64",
            _ => "hair_short_dark_32x64"
        };
    }

    public static string CycleOutfitLayerId(string currentOutfitLayerId)
    {
        return currentOutfitLayerId switch
        {
            "outfit_engineer_32x64" => "outfit_settler_32x64",
            "outfit_settler_32x64" => "outfit_medic_32x64",
            "outfit_medic_32x64" => "outfit_ranger_32x64",
            "outfit_ranger_32x64" => "outfit_engineer_32x64",
            "outfit_engineer" => "outfit_settler_32x64",
            "outfit_settler" => "outfit_medic_32x64",
            _ => "outfit_engineer_32x64"
        };
    }

    private void SendLocalWithPrompt(
        IntentType intentType,
        System.Collections.Generic.IReadOnlyDictionary<string, string> payload)
    {
        if (_serverSession is null)
        {
            return;
        }

        var result = _serverSession.SendLocal(intentType, payload);
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
        }
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

    public static Vector2 CalculateWorldPosition(int tileX, int tileY)
    {
        return new Vector2(tileX * 32f, tileY * 32f);
    }

    public static bool ShouldSnapToAuthoritativePosition(Vector2 currentPosition, Vector2 authoritativePosition)
    {
        const float snapThresholdPixels = 48f;
        return currentPosition.DistanceSquaredTo(authoritativePosition) >= snapThresholdPixels * snapThresholdPixels;
    }

    public static bool ShouldSnapToAuthoritativePosition(
        Vector2 currentPosition,
        Vector2 authoritativePosition,
        Vector2 predictedPosition)
    {
        if (!ShouldSnapToAuthoritativePosition(currentPosition, authoritativePosition))
        {
            return false;
        }

        const float predictionGracePixels = 24f;
        return predictedPosition.DistanceSquaredTo(authoritativePosition) >= predictionGracePixels * predictionGracePixels;
    }

    private void OnLocalSnapshotChanged(string snapshotSummary)
    {
        ApplyAuthoritativePosition(_serverSession?.LastLocalSnapshot);
        ApplyInteriorCameraClamp(_serverSession?.LastLocalSnapshot);
    }

    private void ApplyInteriorCameraClamp(ClientInterestSnapshot snapshot)
    {
        if (_camera is null || snapshot is null) return;
        var local = snapshot.Players.FirstOrDefault(p => p.Id == GameState.LocalPlayerId);
        if (local is null) return;
        if (string.IsNullOrEmpty(local.InsideStructureId))
        {
            // Outside: remove camera limits.
            _camera.LimitLeft = int.MinValue;
            _camera.LimitTop = int.MinValue;
            _camera.LimitRight = int.MaxValue;
            _camera.LimitBottom = int.MaxValue;
            return;
        }
        var structure = snapshot.Structures.FirstOrDefault(s => s.EntityId == local.InsideStructureId);
        if (structure is null || structure.InteriorWidth <= 0 || structure.InteriorHeight <= 0) return;
        var tileSizePx = (int)WorldRoot.TilePixelSize;
        _camera.LimitLeft = structure.InteriorMinX * tileSizePx;
        _camera.LimitTop = structure.InteriorMinY * tileSizePx;
        _camera.LimitRight = (structure.InteriorMinX + structure.InteriorWidth) * tileSizePx;
        _camera.LimitBottom = (structure.InteriorMinY + structure.InteriorHeight) * tileSizePx;
    }

    private void ApplyAuthoritativePosition(ClientInterestSnapshot snapshot)
    {
        var player = snapshot?.Players.FirstOrDefault(player => player.Id == GameState.LocalPlayerId);
        if (player is null)
        {
            return;
        }

        ApplyAppearance(player.Appearance);

        var authoritativePosition = CalculateWorldPosition(player.TileX, player.TileY);
        if (!ShouldSnapToAuthoritativePosition(GlobalPosition, authoritativePosition, _predictedPosition))
        {
            return;
        }

        GlobalPosition = authoritativePosition;
        _predictedPosition = authoritativePosition;
        Velocity = Vector2.Zero;
        _lastSentTile = new TilePosition(player.TileX, player.TileY);
        TopDownDepth.Apply(this);
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

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }
}
