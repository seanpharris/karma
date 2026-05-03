using System;
using System.Collections.Generic;
using Godot;

namespace Karma.UI;

// Custom Control that draws a jagged lightning bolt across its rect.
// Call Strike() to regenerate the path and trigger a flash.
public partial class LightningBolt : Control
{
    private static readonly Color CoreColor = new(1f, 0.96f, 1f, 1f);
    private static readonly Color GlowColor = new(0.78f, 0.72f, 1f, 0.55f);

    private readonly List<Vector2> _trunk = new();
    private readonly List<List<Vector2>> _branches = new();
    private readonly Random _rng = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = new Color(1, 1, 1, 0f);
    }

    public void Strike()
    {
        RegeneratePath();
        QueueRedraw();

        var tween = CreateTween();
        // Sharp main strike.
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.025f);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.07f);
        // Brief gap, regenerate, then a dimmer after-strike.
        tween.TweenInterval(0.04);
        tween.TweenCallback(Callable.From(() => { RegeneratePath(); QueueRedraw(); }));
        tween.TweenProperty(this, "modulate:a", 0.55f, 0.03f);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.18f);
    }

    private void RegeneratePath()
    {
        _trunk.Clear();
        _branches.Clear();

        var size = Size;
        if (size.X <= 0f || size.Y <= 0f) return;

        // Trunk runs top → bottom with horizontal jitter on each segment.
        var startX = (float)_rng.NextDouble() * size.X * 0.6f + size.X * 0.2f;
        var endX = (float)_rng.NextDouble() * size.X * 0.7f + size.X * 0.15f;
        const int segments = 16;
        var jitter = size.X * 0.07f;

        _trunk.Add(new Vector2(startX, 0f));
        for (var i = 1; i < segments; i++)
        {
            var t = (float)i / segments;
            var baseX = Mathf.Lerp(startX, endX, t);
            var dx = ((float)_rng.NextDouble() - 0.5f) * 2f * jitter;
            _trunk.Add(new Vector2(baseX + dx, t * size.Y));
        }
        _trunk.Add(new Vector2(endX, size.Y));

        // 1–2 short branches that fork off the trunk and fade out.
        var branchCount = _rng.Next(1, 3);
        for (var b = 0; b < branchCount; b++)
        {
            var anchorIdx = _rng.Next(3, _trunk.Count - 3);
            var origin = _trunk[anchorIdx];
            var dir = _rng.NextDouble() < 0.5 ? -1f : 1f;
            var branch = new List<Vector2> { origin };
            var bx = origin.X;
            var by = origin.Y;
            var len = _rng.Next(4, 7);
            for (var s = 0; s < len; s++)
            {
                bx += dir * size.X * 0.035f + ((float)_rng.NextDouble() - 0.5f) * jitter * 0.7f;
                by += size.Y * 0.035f + (float)_rng.NextDouble() * size.Y * 0.015f;
                branch.Add(new Vector2(bx, by));
            }
            _branches.Add(branch);
        }
    }

    public override void _Draw()
    {
        if (_trunk.Count < 2) return;

        // Soft outer glow first, then bright core on top.
        DrawPolyline(_trunk, GlowColor, 12f, antialiased: true);
        foreach (var branch in _branches)
            DrawPolyline(branch, GlowColor, 7f, antialiased: true);

        DrawPolyline(_trunk, CoreColor, 3f, antialiased: true);
        foreach (var branch in _branches)
            DrawPolyline(branch, CoreColor, 1.8f, antialiased: true);
    }

    private void DrawPolyline(List<Vector2> points, Color color, float width, bool antialiased)
    {
        for (var i = 0; i < points.Count - 1; i++)
            DrawLine(points[i], points[i + 1], color, width, antialiased);
    }
}
