using System;
using Godot;
using Karma.Art;
using Karma.Data;

namespace Karma.UI;

// Circular karma tier crest for the HUD's top-right corner.
// - Outer dark medallion + gold border ring
// - Progress arc (gold for Ascend, crimson for Descend) sweeping the
//   share of the way to the next tier
// - Score number ("+45" / "-32") in the center
// - Tier name in gold caps below ("PARAGON" / "RENEGADE" / etc.)
// - Progress line underneath ("Progress: 35 / 100  →  Luminary")
//
// Drawn programmatically — no asset dependency. Call SetKarma() each
// time the score / tier changes.
public partial class KarmaTierBadge : Control
{
    private const string MedallionTexturePath = "res://assets/art/third_party/Fantasy Minimal Pixel Art GUI by eta-commercial-free/UI/BlackBigCircleBoxWithBorder_27x27.png";

    private const float MedallionDiameter = 140f;
    // The pack's circle frame is 27×27. 5× nearest-neighbour scale →
    // 135×135 keeps the pixel art crisp and fits the 140-wide badge.
    private const float TextureScale = 5f;
    private const float TextureSize = 27f * TextureScale;
    private const float ProgressRingRadius = 56f;  // sits just inside the texture's gold ring
    private const float MedallionRadius = 44f;     // sits just inside the progress ring

    private static readonly Color BackingColor = new(0.06f, 0.09f, 0.14f, 0.96f);
    private static readonly Color FrameGold = new(0.85f, 0.68f, 0.32f, 1f);
    private static readonly Color FrameGoldDim = new(0.55f, 0.45f, 0.22f, 0.55f);
    private static readonly Color AscendGlow = new(1.00f, 0.86f, 0.45f);
    private static readonly Color DescendGlow = new(0.86f, 0.22f, 0.22f);
    private static readonly Color NeutralGlow = new(0.55f, 0.50f, 0.40f);
    private static readonly Color ScoreCream = new(1.00f, 0.96f, 0.78f);
    private static readonly Color TierGold = new(0.92f, 0.78f, 0.42f);
    private static readonly Color SubtleCream = new(0.78f, 0.72f, 0.58f);

    private int _score;
    private string _tierName = "Unmarked";
    private string _nextTierName = "Trusted";
    private int _progress;
    private int _rankSize = 10;
    private KarmaDirection _direction = KarmaDirection.Neutral;

    private Texture2D _medallionTexture;
    private Label _scoreLabel;
    private Label _tierLabel;
    private Label _progressLabel;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(MedallionDiameter, MedallionDiameter + 56);
        // Routed through AtlasTextureLoader so we work even when the
        // pack PNGs don't yet have .import sidecars (raw load fallback).
        _medallionTexture = AtlasTextureLoader.Load(MedallionTexturePath, forceImageLoad: true);

        _scoreLabel = MakeLabel(ScoreCream, fontSize: 28, top: MedallionDiameter * 0.5f - 22, height: 44);
        _scoreLabel.Text = "0";
        AddChild(_scoreLabel);

        _tierLabel = MakeLabel(TierGold, fontSize: 14, top: MedallionDiameter + 4, height: 22);
        _tierLabel.Text = "UNMARKED";
        AddChild(_tierLabel);

        _progressLabel = MakeLabel(SubtleCream, fontSize: 11, top: MedallionDiameter + 28, height: 22);
        _progressLabel.Text = string.Empty;
        AddChild(_progressLabel);
    }

    public void SetKarma(int score, KarmaRankProgress rankProgress, KarmaDirection direction)
    {
        _score = score;
        _tierName = rankProgress.CurrentRankName;
        _nextTierName = rankProgress.NextRankName;
        _progress = rankProgress.Progress;
        _rankSize = rankProgress.RankSize;
        _direction = direction;

        _scoreLabel.Text = score > 0 ? $"+{score}" : score.ToString();
        _tierLabel.Text = (_tierName ?? string.Empty).ToUpperInvariant();
        _progressLabel.Text = _rankSize <= 0
            ? "—"
            : $"{_progress} / {_rankSize}  →  {_nextTierName}";

        QueueRedraw();
    }

    public override void _Draw()
    {
        var center = new Vector2(MedallionDiameter * 0.5f, MedallionDiameter * 0.5f);
        var pathColor = _direction switch
        {
            KarmaDirection.Ascend => AscendGlow,
            KarmaDirection.Descend => DescendGlow,
            _ => NeutralGlow
        };

        // Pack-supplied gold-ring-on-dark medallion frame, scaled 5×.
        if (_medallionTexture is not null)
        {
            var dest = new Rect2(
                center.X - TextureSize * 0.5f,
                center.Y - TextureSize * 0.5f,
                TextureSize, TextureSize);
            DrawTextureRect(_medallionTexture, dest, tile: false);
        }
        else
        {
            // Procedural fallback if the texture failed to load.
            DrawCircle(center, 64f, BackingColor);
            DrawArc(center, 64f, 0f, MathF.Tau, 96, FrameGold, 3.0f, antialiased: true);
        }

        // Dim full progress ring track underneath the lit arc, so the
        // empty share is visible (not just blank space). Sits inside
        // the texture's gold frame.
        DrawArc(center, ProgressRingRadius, 0f, MathF.Tau, 96, FrameGoldDim, 4.0f, antialiased: true);

        // Lit progress arc — sweeps clockwise from 12 o'clock.
        if (_rankSize > 0)
        {
            var fraction = Mathf.Clamp(_progress / (float)_rankSize, 0f, 1f);
            if (fraction > 0f)
            {
                var startAngle = -MathF.PI * 0.5f;
                var endAngle = startAngle + (fraction * MathF.Tau);
                DrawArc(center, ProgressRingRadius, startAngle, endAngle, 96, pathColor, 5.0f, antialiased: true);
            }
        }

        // Inner path-tint wash so the score number reads against a
        // path-aware backdrop. The texture supplies the dark base.
        DrawCircle(center, MedallionRadius, new Color(pathColor.R * 0.30f, pathColor.G * 0.30f, pathColor.B * 0.30f, 0.45f));
        DrawArc(center, MedallionRadius, 0f, MathF.Tau, 64, pathColor, 2.0f, antialiased: true);
    }

    private static Label MakeLabel(Color color, int fontSize, float top, float height)
    {
        var label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorLeft = 0f,
            AnchorRight = 1f,
            OffsetTop = top,
            OffsetBottom = top + height,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
