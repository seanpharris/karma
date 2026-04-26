using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Karma.Tests;

public partial class PrototypeScreenshotRunner : Node
{
    private const string OutputDirectory = "res://debug/prototype-screenshots";

    public override async void _Ready()
    {
        try
        {
            await CaptureAsync();
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"Prototype screenshot failed: {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task CaptureAsync()
    {
        GetWindow().Size = new Vector2I(1280, 720);

        var packedScene = GD.Load<PackedScene>("res://scenes/Main.tscn");
        var main = packedScene.Instantiate();
        AddChild(main);

        await WaitFrames(8);

        var captures = new[]
        {
            new DirectionCapture("front", new[] { "move_down" }),
            new DirectionCapture("front-right", new[] { "move_down", "move_right" }),
            new DirectionCapture("right", new[] { "move_right" }),
            new DirectionCapture("back-right", new[] { "move_up", "move_right" }),
            new DirectionCapture("back", new[] { "move_up" }),
            new DirectionCapture("back-left", new[] { "move_up", "move_left" }),
            new DirectionCapture("left", new[] { "move_left" }),
            new DirectionCapture("front-left", new[] { "move_down", "move_left" })
        };

        foreach (var capture in captures)
        {
            foreach (var action in capture.Actions)
            {
                Input.ActionPress(action);
            }

            await WaitPhysicsFrames(10);
            await WaitFrames(3);
            SaveScreenshot(capture.Name);

            foreach (var action in capture.Actions)
            {
                Input.ActionRelease(action);
            }

            await WaitPhysicsFrames(4);
        }
    }

    private void SaveScreenshot(string name)
    {
        var image = GetViewport().GetTexture().GetImage();
        var outputPath = $"{OutputDirectory}/{name}.png";
        var absolutePath = ProjectSettings.GlobalizePath(outputPath);
        DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(absolutePath));
        var result = image.SavePng(absolutePath);
        if (result != Error.Ok)
        {
            throw new InvalidOperationException($"Could not save screenshot to {absolutePath}: {result}");
        }

        GD.Print($"Prototype screenshot written: {absolutePath}");
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private sealed record DirectionCapture(string Name, IReadOnlyList<string> Actions);
}
