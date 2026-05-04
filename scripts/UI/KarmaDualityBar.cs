using System;
using Godot;

namespace Karma.UI;

// Horizontal karma-spectrum bar for the HUD top-center. Renders a
// blue→vortex→red gradient, a gold frame, tier tick marks, and a
// triangle marker showing where the player's score sits along the
// spectrum. Drawn programmatically — no asset dependency.
public partial class KarmaDualityBar : Control
{
    public const float BarWidth = 600f;
    public const float BarHeight = 14f;

    // Score range that the bar covers end-to-end. Beyond this the
    // marker pins to the appropriate edge (Paragon / Renegade are
    // open-ended past 100).
    public const int MinScore = -150;
    public const int MaxScore = 150;

    private static readonly Color FrameColor = new(0.85f, 0.68f, 0.32f);
    private static readonly Color FrameDarkColor = new(0.45f, 0.35f, 0.16f);
    private static readonly Color CenterLine = new(1f, 0.96f, 0.78f, 0.85f);
    private static readonly Color TickColor = new(1f, 0.88f, 0.52f, 0.55f);
    private static readonly Color MarkerFill = new(1f, 0.96f, 0.78f);
    private static readonly Color MarkerBorder = new(0.10f, 0.10f, 0.14f);

    // Major tier thresholds — drawn as tick marks behind the marker.
    private static readonly int[] TierTicks = { -100, -75, -50, -35, -20, -10, 10, 20, 35, 50, 75, 100 };

    private int _score;
    private Texture2D _gradientTexture;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(BarWidth, BarHeight);
        _gradientTexture = MakeGradientTexture((int)BarWidth, 1);
    }

    public void SetScore(int score)
    {
        _score = score;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(0, 0, BarWidth, BarHeight);

        // Gradient fill (blue → vortex → red), drawn before frame so
        // the gold border sits on top.
        if (_gradientTexture is not null)
            DrawTextureRect(_gradientTexture, rect, tile: false);

        // Tier tick marks — short vertical pips along the bottom edge.
        foreach (var tick in TierTicks)
        {
            var tickX = ScoreToX(tick);
            DrawLine(
                new Vector2(tickX, BarHeight * 0.55f),
                new Vector2(tickX, BarHeight - 1f),
                TickColor, 1f, antialiased: true);
        }

        // Single gold border, thinner so it doesn't dominate the slim bar.
        DrawRect(rect, FrameColor, filled: false, width: 1.5f);
        DrawRect(rect.Grow(1f), FrameDarkColor, filled: false, width: 1f);

        // Marker: downward triangle pip pointing at the player's score.
        var markerX = ScoreToX(_score);
        DrawTriangle(
            tip: new Vector2(markerX, -1),
            baseHalfWidth: 4f,
            baseY: -8f,
            fill: MarkerFill,
            border: MarkerBorder);
    }

    private void DrawTriangle(Vector2 tip, float baseHalfWidth, float baseY, Color fill, Color border)
    {
        var pts = new[]
        {
            tip,
            new Vector2(tip.X - baseHalfWidth, baseY),
            new Vector2(tip.X + baseHalfWidth, baseY)
        };
        var colors = new[] { fill, fill, fill };
        DrawPolygon(pts, colors);
        // Border outline.
        DrawLine(pts[0], pts[1], border, 1.5f, antialiased: true);
        DrawLine(pts[1], pts[2], border, 1.5f, antialiased: true);
        DrawLine(pts[2], pts[0], border, 1.5f, antialiased: true);
    }

    private static float ScoreToX(int score)
    {
        var clamped = Mathf.Clamp(score, MinScore, MaxScore);
        var t = (clamped - MinScore) / (float)(MaxScore - MinScore);
        return t * BarWidth;
    }

    private static Texture2D MakeGradientTexture(int width, int height)
    {
        var img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (var x = 0; x < width; x++)
        {
            var t = x / (float)(width - 1);
            var color = SampleDualityGradient(t);
            for (var y = 0; y < height; y++)
                img.SetPixel(x, y, color);
        }
        return ImageTexture.CreateFromImage(img);
    }

    // 5-stop gradient: cool blue (Paragon edge) → mid blue → vortex
    // dark purple at center → mid red → deep red (Renegade edge).
    private static Color SampleDualityGradient(float t)
    {
        var stops = new (float t, Color color)[]
        {
            (0.00f, new Color(0.10f, 0.28f, 0.62f)),
            (0.35f, new Color(0.20f, 0.42f, 0.78f)),
            (0.50f, new Color(0.10f, 0.06f, 0.18f)),
            (0.65f, new Color(0.62f, 0.18f, 0.18f)),
            (1.00f, new Color(0.46f, 0.10f, 0.10f))
        };

        for (var i = 0; i < stops.Length - 1; i++)
        {
            var a = stops[i];
            var b = stops[i + 1];
            if (t <= b.t)
            {
                var local = (t - a.t) / MathF.Max(0.0001f, b.t - a.t);
                return a.color.Lerp(b.color, local);
            }
        }
        return stops[^1].color;
    }
}
