using System;
using Godot;

namespace Karma.UI;

// Custom Control that draws a shooting star — a bright head with a
// trailing tail — arcing across its rect. Call Shoot() to launch a
// fresh trajectory; the star fades in, crosses the rect, then fades
// out. Peaceful counterpart to LightningBolt's sharp strike.
public partial class ShootingStar : Control
{
    private const float Duration = 0.9f;
    private const float FadeIn = 0.10f;
    private const float FadeOut = 0.22f;

    private readonly Random _rng = new();
    private Vector2 _start;
    private Vector2 _end;
    private float _elapsed = -1f; // -1 ⇒ inactive

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = new Color(1, 1, 1, 0f);
    }

    public void Shoot()
    {
        var size = Size;
        if (size.X <= 0f || size.Y <= 0f) return;

        // Start just above the top edge near the left; arc down/right
        // into the middle of the rect. Both endpoints randomized so each
        // shot looks fresh.
        var startX = (float)_rng.NextDouble() * size.X * 0.35f - size.X * 0.05f;
        var startY = -size.Y * 0.05f;
        var endX = size.X * 0.75f + (float)_rng.NextDouble() * size.X * 0.30f;
        var endY = size.Y * 0.40f + (float)_rng.NextDouble() * size.Y * 0.25f;
        _start = new Vector2(startX, startY);
        _end = new Vector2(endX, endY);
        _elapsed = 0f;
    }

    public override void _Process(double delta)
    {
        if (_elapsed < 0f) return;
        _elapsed += (float)delta;

        float alpha;
        if (_elapsed < FadeIn)
            alpha = _elapsed / FadeIn;
        else if (_elapsed > Duration - FadeOut)
            alpha = MathF.Max(0f, (Duration - _elapsed) / FadeOut);
        else
            alpha = 1f;

        if (_elapsed >= Duration)
        {
            _elapsed = -1f;
            alpha = 0f;
        }

        Modulate = new Color(1, 1, 1, alpha);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_elapsed < 0f) return;

        var t = Math.Clamp(_elapsed / Duration, 0f, 1f);
        var head = _start.Lerp(_end, t);
        var span = _end - _start;
        var dir = span.Normalized();
        var trailLength = span.Length() * 0.30f;
        var tail = head - dir * trailLength;

        // Tail: short segments fading from invisible at the tail to
        // bright at the head. Quadratic falloff keeps the bright zone
        // tight to the head and lets the trail dissolve.
        const int segments = 16;
        for (var i = 0; i < segments; i++)
        {
            var s0 = (float)i / segments;
            var s1 = (float)(i + 1) / segments;
            var p0 = tail.Lerp(head, s0);
            var p1 = tail.Lerp(head, s1);
            var a = s1 * s1;
            DrawLine(p0, p1, new Color(1f, 0.96f, 0.85f, a * 0.55f), 7f, antialiased: true);
            DrawLine(p0, p1, new Color(1f, 1f, 1f, a), 2f, antialiased: true);
        }

        // Bright head dot.
        DrawCircle(head, 4.5f, new Color(1f, 0.98f, 0.85f, 1f));
        DrawCircle(head, 9f, new Color(1f, 0.95f, 0.7f, 0.30f));
    }
}
