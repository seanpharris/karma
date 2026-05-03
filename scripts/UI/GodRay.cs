using System;
using Godot;

namespace Karma.UI;

// Custom Control that draws a soft golden ray of light descending
// across its rect. Call Shine() to randomize the angle and trigger a
// slow fade-in/out — the peaceful counterpart to LightningBolt.
public partial class GodRay : Control
{
    private static readonly Color OuterGlow = new(1f, 0.85f, 0.35f, 0.18f);
    private static readonly Color MidGlow = new(1f, 0.90f, 0.45f, 0.32f);
    private static readonly Color Core = new(1f, 0.95f, 0.65f, 0.65f);
    private static readonly Color Highlight = new(1f, 0.98f, 0.85f, 0.85f);

    private Vector2 _start;
    private Vector2 _end;
    private readonly Random _rng = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = new Color(1, 1, 1, 0f);
    }

    public void Shine()
    {
        Regenerate();
        QueueRedraw();

        var tween = CreateTween();
        // Slow ease in, brief hold, slow ease out — peaceful counterpart
        // to the lightning's sharp strike.
        tween.TweenProperty(this, "modulate:a", 1.0f, 1.2f).SetEase(Tween.EaseType.InOut);
        tween.TweenInterval(0.5);
        tween.TweenProperty(this, "modulate:a", 0.0f, 1.6f).SetEase(Tween.EaseType.InOut);
    }

    private void Regenerate()
    {
        var size = Size;
        if (size.X <= 0f || size.Y <= 0f) return;

        // Beam falls diagonally top-left → bottom-right. Randomize entry
        // point along the top and slope so each shine looks fresh.
        var topX = (float)_rng.NextDouble() * size.X * 0.6f + size.X * 0.10f;
        var slope = (float)_rng.NextDouble() * 0.5f + 0.35f; // 0.35..0.85
        var bottomX = topX + slope * size.Y;

        // Extend past the rect on both ends so the beam reads as
        // continuing off-image rather than terminating mid-frame.
        _start = new Vector2(topX - slope * size.Y * 0.2f, -size.Y * 0.2f);
        _end = new Vector2(bottomX + slope * size.Y * 0.2f, size.Y * 1.2f);
    }

    public override void _Draw()
    {
        if (_start == _end) return;
        // Layered passes: wide low-alpha halo → mid → bright thin core.
        DrawLine(_start, _end, OuterGlow, 96f, antialiased: true);
        DrawLine(_start, _end, MidGlow, 56f, antialiased: true);
        DrawLine(_start, _end, Core, 24f, antialiased: true);
        DrawLine(_start, _end, Highlight, 6f, antialiased: true);
    }
}
