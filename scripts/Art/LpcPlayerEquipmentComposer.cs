using Godot;
using Karma.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Art;

public static class LpcPlayerEquipmentComposer
{
    private const string LpcRoot = "res://assets/art/sprites/spritesheets/";
    private const int LpcFrame = 64;
    private const int LpcColumns = 9;
    private const int LpcRows = 4;
    private const int LpcWidth = LpcFrame * LpcColumns;
    private const int LpcHeight = LpcFrame * LpcRows;
    private const int TargetCellWidth = 32;
    private const int TargetCellHeight = 64;
    private const int KarmaWidth = TargetCellWidth * 8;
    private const int KarmaHeight = TargetCellHeight * 4;
    private static readonly int[] ColumnLpcRow = { 2, 2, 3, 0, 0, 0, 1, 1 };
    private static readonly int[] RowLpcFrame = { 0, 1, 4, 7 };

    public static string ComposeEquippedAtlas(
        string bundleId,
        IReadOnlyDictionary<EquipmentSlot, string> equipmentItemIds,
        string cacheRoot = "user://lpc_player_equipment")
    {
        if (string.IsNullOrWhiteSpace(bundleId))
            return string.Empty;

        var basePath = LpcPlayerAppearanceRegistry.BuildAtlasPath(bundleId);
        if (string.IsNullOrWhiteSpace(basePath) || !FileAccess.FileExists(basePath))
            return string.Empty;

        var layers = ResolveEquipmentLayers(bundleId, equipmentItemIds).ToArray();
        if (layers.Length == 0)
            return basePath;

        var cacheKey = $"{Sanitize(bundleId)}__{Sanitize(EquipmentSignature(equipmentItemIds))}";
        var outputPath = $"{cacheRoot.TrimEnd('/')}/{cacheKey}.png";
        if (FileAccess.FileExists(outputPath))
            return outputPath;

        var composed = Image.LoadFromFile(basePath);
        if (composed is null || composed.IsEmpty())
            return basePath;
        if (composed.GetFormat() != Image.Format.Rgba8)
            composed.Convert(Image.Format.Rgba8);

        foreach (var layerPath in layers)
        {
            var overlay = BuildKarmaOverlay(layerPath);
            if (overlay is null)
                continue;
            composed.BlendRect(overlay, new Rect2I(Vector2I.Zero, overlay.GetSize()), Vector2I.Zero);
        }

        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(cacheRoot));
        return composed.SavePng(outputPath) == Error.Ok ? outputPath : basePath;
    }

    public static string EquipmentSignature(IReadOnlyDictionary<EquipmentSlot, string> equipmentItemIds)
    {
        if (equipmentItemIds is null || equipmentItemIds.Count == 0)
            return "none";

        return string.Join("__",
            equipmentItemIds
                .Where(pair => pair.Key != EquipmentSlot.None && !string.IsNullOrWhiteSpace(pair.Value))
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}:{pair.Value.Trim()}"));
    }

    private static IEnumerable<string> ResolveEquipmentLayers(
        string bundleId,
        IReadOnlyDictionary<EquipmentSlot, string> equipmentItemIds)
    {
        if (equipmentItemIds is null || equipmentItemIds.Count == 0)
            yield break;

        if (equipmentItemIds.TryGetValue(EquipmentSlot.Body, out var bodyItemId))
        {
            foreach (var layer in ResolveBodyLayers(bundleId, bodyItemId))
                yield return layer;
        }

        if (equipmentItemIds.TryGetValue(EquipmentSlot.Backpack, out var backpackItemId))
        {
            foreach (var layer in ResolveBackpackLayers(bundleId, backpackItemId))
                yield return layer;
        }

        if (equipmentItemIds.TryGetValue(EquipmentSlot.MainHand, out var mainHandItemId))
        {
            foreach (var layer in ResolveMainHandLayers(mainHandItemId))
                yield return layer;
        }
    }

    private static IEnumerable<string> ResolveBodyLayers(string bundleId, string itemId)
    {
        var bodyKind = bundleId.Contains("_female", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
        switch (itemId)
        {
            case StarterItems.WorkVestId:
                yield return $"torso/armour/leather/{bodyKind}/walk.png";
                break;
            case StarterItems.PortableShieldId:
                yield return "shield/spartan/fg/walk/spartan.png";
                break;
        }
    }

    private static IEnumerable<string> ResolveBackpackLayers(string bundleId, string itemId)
    {
        if (itemId != StarterItems.BackpackBrownId)
            yield break;

        var bodyKind = bundleId.Contains("_female", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
        yield return $"backpack/backpack/{bodyKind}/walk/walnut.png";
    }

    private static IEnumerable<string> ResolveMainHandLayers(string itemId)
    {
        switch (itemId)
        {
            case StarterItems.PracticeStickId:
            case StarterItems.StunBatonId:
            case StarterItems.FlameThrowerId:
            case StarterItems.EmpGrenadeId:
                yield return "weapon/blunt/club/club.png";
                break;
            case StarterItems.ElectroPistolId:
            case StarterItems.Smg11Id:
            case StarterItems.LockpickSetId:
                yield return "weapon/sword/dagger/walk/dagger.png";
                break;
            case StarterItems.ShotgunMk1Id:
            case StarterItems.GrenadeLauncherId:
            case StarterItems.ImpactMineId:
                yield return "weapon/blunt/waraxe/walk/waraxe.png";
                break;
            case StarterItems.Rifle27Id:
            case StarterItems.RailgunId:
                yield return "weapon/ranged/crossbow/walk/crossbow.png";
                break;
            case StarterItems.SniperX9Id:
                yield return "weapon/ranged/bow/normal/walk/foreground.png";
                break;
            case StarterItems.PlasmaCutterId:
                yield return "weapon/sword/longsword/walk/longsword.png";
                break;
            case StarterItems.WeldingTorchId:
            case StarterItems.FlashlightId:
            case StarterItems.ChemInjectorId:
                yield return "weapon/magic/wand/male/slash/wand.png";
                break;
            case StarterItems.MultiToolId:
            case StarterItems.RepairKitId:
                yield return "tools/smash/foreground/hammer.png";
                break;
            case StarterItems.BoltCuttersId:
                yield return "tools/smash/foreground/axe.png";
                break;
            case StarterItems.GrapplingHookId:
            case StarterItems.MagneticGrabberId:
                yield return "tools/rod/foreground/rod.png";
                break;
        }
    }

    private static Image BuildKarmaOverlay(string lpcLayerPath)
    {
        var path = LpcRoot + lpcLayerPath;
        if (!FileAccess.FileExists(path))
            return null;

        var lpcSheet = Image.LoadFromFile(path);
        if (lpcSheet is null || lpcSheet.GetWidth() != LpcWidth || lpcSheet.GetHeight() != LpcHeight)
            return null;
        if (lpcSheet.GetFormat() != Image.Format.Rgba8)
            lpcSheet.Convert(Image.Format.Rgba8);

        var output = Image.CreateEmpty(KarmaWidth, KarmaHeight, false, Image.Format.Rgba8);
        output.Fill(new Color(0f, 0f, 0f, 0f));

        for (var column = 0; column < 8; column++)
        {
            var lpcRow = ColumnLpcRow[column];
            for (var row = 0; row < 4; row++)
            {
                var frameIndex = RowLpcFrame[row];
                var src = new Rect2I(frameIndex * LpcFrame, lpcRow * LpcFrame, LpcFrame, LpcFrame);
                var fitted = FitLpcCell(lpcSheet, src);
                output.BlendRect(
                    fitted,
                    new Rect2I(0, 0, TargetCellWidth, TargetCellHeight),
                    new Vector2I(column * TargetCellWidth, row * TargetCellHeight));
            }
        }

        return output;
    }

    private static Image FitLpcCell(Image lpcSheet, Rect2I src)
    {
        var sub = Image.CreateEmpty(LpcFrame, LpcFrame, false, Image.Format.Rgba8);
        sub.Fill(new Color(0f, 0f, 0f, 0f));
        sub.BlitRect(lpcSheet, src, Vector2I.Zero);

        var minX = LpcFrame;
        var minY = LpcFrame;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < LpcFrame; y++)
        {
            for (var x = 0; x < LpcFrame; x++)
            {
                if (sub.GetPixel(x, y).A <= 0.04f)
                    continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        var output = Image.CreateEmpty(TargetCellWidth, TargetCellHeight, false, Image.Format.Rgba8);
        output.Fill(new Color(0f, 0f, 0f, 0f));
        if (maxX < 0)
            return output;

        var bodyWidth = maxX - minX + 1;
        var bodyHeight = maxY - minY + 1;
        var scale = Math.Min(
            Math.Min((float)(TargetCellWidth - 2) / bodyWidth, (float)(TargetCellHeight - 2) / bodyHeight),
            1f);
        var targetWidth = Math.Max(1, (int)Math.Round(bodyWidth * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(bodyHeight * scale));
        var cropped = Image.CreateEmpty(bodyWidth, bodyHeight, false, Image.Format.Rgba8);
        cropped.Fill(new Color(0f, 0f, 0f, 0f));
        cropped.BlitRect(sub, new Rect2I(minX, minY, bodyWidth, bodyHeight), Vector2I.Zero);
        cropped.Resize(targetWidth, targetHeight, Image.Interpolation.Nearest);

        output.BlendRect(
            cropped,
            new Rect2I(0, 0, targetWidth, targetHeight),
            new Vector2I((TargetCellWidth - targetWidth) / 2, TargetCellHeight - targetHeight - 2));
        return output;
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "none";

        var chars = value.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_').ToArray();
        return new string(chars);
    }
}
