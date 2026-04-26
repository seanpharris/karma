using Godot;
using System.Linq;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.Player;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 120f;
    [Export] public float SprintMultiplier { get; set; } = 1.6f;
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
    private TilePosition? _lastSentTile;
    private Vector2I _lastFacing = Vector2I.Down;
    private float _stamina;
    private bool _isExhausted;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        _stamina = MaxStamina;
        UpdateStaminaHud();
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

        var wantsSprint = Input.IsActionPressed("sprint");
        _isExhausted = CalculateExhausted(_isExhausted, _stamina, SprintResumeStamina);
        var isSprinting = CanSprint(direction, wantsSprint, _stamina, _isExhausted);
        Velocity = CalculateVelocity(direction, Speed, SprintMultiplier, isSprinting);
        _stamina = CalculateNextStamina(
            _stamina,
            delta,
            isSprinting,
            MaxStamina,
            SprintStaminaCostPerSecond,
            StaminaRecoveryPerSecond);
        _isExhausted = CalculateExhausted(_isExhausted, _stamina, SprintResumeStamina);
        UpdateStaminaHud();
        MoveAndSlide();

        SendMoveIfTileChanged();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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

    private static TilePosition ToTilePosition(Vector2 position)
    {
        return new TilePosition(
            Mathf.RoundToInt(position.X / 32f),
            Mathf.RoundToInt(position.Y / 32f));
    }
}
