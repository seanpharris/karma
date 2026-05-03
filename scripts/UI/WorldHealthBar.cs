using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Karma.UI;

public partial class WorldHealthBar : Node2D
{
    [Export] public float Width { get; set; } = 38f;
    [Export] public float Height { get; set; } = 5f;
    [Export] public string DisplayName { get; set; } = string.Empty;

    private int _health = 100;
    private int _maxHealth = 100;
    private string _statusText = string.Empty;

    public void SetHealth(int health, int maxHealth)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        _health = Mathf.Clamp(health, 0, _maxHealth);
        QueueRedraw();
    }

    public void SetStatusEffects(IReadOnlyList<string> statusEffects)
    {
        _statusText = statusEffects is null || statusEffects.Count == 0
            ? string.Empty
            : string.Join(", ", statusEffects.Take(2));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            DrawString(
                ThemeDB.FallbackFont,
                new Vector2(-Width * 0.5f, -8f),
                DisplayName,
                HorizontalAlignment.Center,
                Width,
                10,
                Colors.White);
        }

        var frame = new Rect2(-Width * 0.5f, 0f, Width, Height);
        DrawRect(frame.Grow(1f), new Color(0f, 0f, 0f, 0.72f));
        DrawRect(frame, new Color(0.16f, 0.04f, 0.04f, 0.95f));

        var fillWidth = Width * CalculateHealthPercent(_health, _maxHealth) / 100f;
        var fillColor = _health <= _maxHealth * 0.3f
            ? new Color(0.95f, 0.2f, 0.16f)
            : new Color(0.18f, 0.85f, 0.28f);
        DrawRect(new Rect2(frame.Position, new Vector2(fillWidth, Height)), fillColor);

        if (!string.IsNullOrWhiteSpace(_statusText))
        {
            DrawString(
                ThemeDB.FallbackFont,
                new Vector2(-Width * 0.5f, Height + 11f),
                _statusText,
                HorizontalAlignment.Center,
                Width,
                9,
                new Color(0.45f, 0.85f, 1f));
        }
    }

    public static float CalculateHealthPercent(int health, int maxHealth)
    {
        var safeMax = Mathf.Max(1, maxHealth);
        var clampedHealth = Mathf.Clamp(health, 0, safeMax);
        return clampedHealth * 100f / safeMax;
    }
}
