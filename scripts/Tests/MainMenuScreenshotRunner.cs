using Godot;
using System;
using System.Threading.Tasks;

namespace Karma.Tests;

// One-shot runner that loads the main menu, lets it lay out, then
// snapshots the viewport so we can inspect button-rect placement
// against the painted splash without launching the editor.
//
// Run via tools/screenshot_main_menu.ps1 (or directly):
//   godot --path . res://scenes/MainMenuScreenshot.tscn
public partial class MainMenuScreenshotRunner : Node
{
    private const string OutputPath = "res://debug/main_menu_screenshot.png";

    public override async void _Ready()
    {
        try
        {
            await CaptureAsync();
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"Main menu screenshot failed: {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task CaptureAsync()
    {
        GetWindow().Size = new Vector2I(1920, 1080);

        var packed = GD.Load<PackedScene>("res://scenes/MainMenu.tscn");
        var menu = packed.Instantiate();
        AddChild(menu);

        // Let the menu run a few frames so anchors layout, splash
        // loads, glow textures attach, and any startup tweens settle.
        await WaitFrames(20);

        var image = GetViewport().GetTexture().GetImage();
        var absolutePath = ProjectSettings.GlobalizePath(OutputPath);
        DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(absolutePath));
        var saveErr = image.SavePng(absolutePath);
        if (saveErr != Error.Ok)
            throw new InvalidOperationException($"Could not save screenshot: {saveErr}");
        GD.Print($"Main menu screenshot written: {absolutePath}");
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
