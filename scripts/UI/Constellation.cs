using System;
using System.Collections.Generic;
using Godot;

namespace Karma.UI;

// Custom Control that draws a handful of softly twinkling stars over
// its rect. Always-on ambient — no timer trigger needed. Each star has
// its own pulse phase + period so they twinkle out of sync.
public partial class Constellation : Control
{
    private const int StarCount = 14;
    private static readonly Color CoreColor = new(1f, 0.99f, 0.92f, 1f);
    private static readonly Color HaloColor = new(1f, 0.96f, 0.78f, 1f);

    private readonly List<Star> _stars = new();
    private readonly Random _rng = new();
    private float _time;

    private struct Star
    {
        public Vector2 Pos;
        public float BaseSize;   // core radius in px
        public float Phase;      // 0..2π pulse offset
        public float Period;     // seconds for one full pulse
        public float MinAlpha;
        public float MaxAlpha;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += LayoutStars;
        LayoutStars();
    }

    private void LayoutStars()
    {
        _stars.Clear();
        var size = Size;
        if (size.X <= 0f || size.Y <= 0f) return;

        for (var i = 0; i < StarCount; i++)
        {
            var x = (float)_rng.NextDouble() * size.X * 0.95f + size.X * 0.025f;
            var y = (float)_rng.NextDouble() * size.Y * 0.90f + size.Y * 0.05f;
            _stars.Add(new Star
            {
                Pos = new Vector2(x, y),
                BaseSize = 1.4f + (float)_rng.NextDouble() * 1.8f,    // 1.4..3.2 px
                Phase = (float)_rng.NextDouble() * Mathf.Tau,
                Period = 1.8f + (float)_rng.NextDouble() * 2.4f,       // 1.8..4.2s
                MinAlpha = 0.20f,
                MaxAlpha = 0.95f
            });
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_stars.Count == 0) return;

        foreach (var star in _stars)
        {
            var phaseT = (_time / star.Period) * Mathf.Tau + star.Phase;
            var pulse = (MathF.Sin(phaseT) + 1f) * 0.5f;
            var a = Mathf.Lerp(star.MinAlpha, star.MaxAlpha, pulse);

            // Outer halo, mid glow, bright core — gives a soft twinkle
            // without a texture.
            DrawCircle(star.Pos, star.BaseSize * 3.6f, new Color(HaloColor.R, HaloColor.G, HaloColor.B, a * 0.18f));
            DrawCircle(star.Pos, star.BaseSize * 1.9f, new Color(HaloColor.R, HaloColor.G, HaloColor.B, a * 0.42f));
            DrawCircle(star.Pos, star.BaseSize, new Color(CoreColor.R, CoreColor.G, CoreColor.B, a));
        }
    }
}
