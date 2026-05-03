using Godot;
using System.Linq;
using Karma.Art;

namespace Karma.World;

public partial class PrototypeWanderingNpc : CharacterBody2D
{
    private Vector2 _origin;
    private double _elapsed;

    [Export] public float WalkRadius { get; set; } = 72f;
    [Export] public float WalkSpeed { get; set; } = 42f;
    [Export] public float VerticalPatrolSlowdown { get; set; } = 5f;
    [Export] public bool HorizontalOnly { get; set; }
    [Export] public PrototypeSpriteKind SpriteKind { get; set; } = PrototypeSpriteKind.PixellabTrialNpc;

    public override void _Ready()
    {
        _origin = Position;
        ZIndex = TopDownDepth.CalculateZIndex(Position.Y);
        if (GetNodeOrNull<PrototypeCharacterSprite>("PrototypeCharacterSprite") is null)
        {
            AddChild(new PrototypeCharacterSprite
            {
                Name = "PrototypeCharacterSprite",
                Kind = SpriteKind
            });
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += delta;
        var target = CalculatePatrolTarget(_elapsed, _origin, WalkRadius, HorizontalOnly, VerticalPatrolSlowdown);
        var toTarget = target - Position;
        Velocity = toTarget.Length() <= 2f
            ? Vector2.Zero
            : toTarget.Normalized() * WalkSpeed;
        MoveAndSlide();
        TopDownDepth.Apply(this);
    }

    public static Vector2 CalculatePatrolTarget(
        double elapsedSeconds,
        Vector2 origin,
        float radius,
        bool horizontalOnly = true,
        float verticalPatrolSlowdown = 5f)
    {
        if (horizontalOnly)
        {
            var t = Mathf.Sin((float)(elapsedSeconds * 0.65));
            return origin + new Vector2(t * radius, 0f);
        }

        var verticalWeight = Mathf.Max(1f, verticalPatrolSlowdown);
        var segmentWeights = new[] { 1f, verticalWeight, 1f, verticalWeight };
        var totalWeight = segmentWeights.Sum();
        var phase = (float)((elapsedSeconds * 0.22) % totalWeight);
        var corner = 0;
        while (corner < segmentWeights.Length - 1 && phase >= segmentWeights[corner])
        {
            phase -= segmentWeights[corner];
            corner++;
        }

        var t2 = phase / segmentWeights[corner];
        var points = new[]
        {
            origin + new Vector2(-radius, -radius * 0.35f),
            origin + new Vector2(radius, -radius * 0.35f),
            origin + new Vector2(radius, radius * 0.35f),
            origin + new Vector2(-radius, radius * 0.35f)
        };
        return points[corner].Lerp(points[(corner + 1) % points.Length], t2);
    }
}
