using Godot;
using System.Collections.Generic;

namespace Karma.Art;

public static class AtlasTextureLoader
{
    public static Texture2D Load(string atlasPath, bool removeDarkBackground = false)
    {
        if (string.IsNullOrWhiteSpace(atlasPath))
        {
            return null;
        }

        if (removeDarkBackground)
        {
            var keyedImage = LoadImage(atlasPath);
            if (keyedImage is not null && !keyedImage.IsEmpty())
            {
                RemoveEdgeConnectedDarkBackground(keyedImage);
                return ImageTexture.CreateFromImage(keyedImage);
            }
        }

        if (ResourceLoader.Exists(atlasPath))
        {
            var importedTexture = ResourceLoader.Load<Texture2D>(atlasPath);
            if (importedTexture is not null)
            {
                return importedTexture;
            }
        }

        var image = LoadImage(atlasPath);
        return image is null || image.IsEmpty()
            ? null
            : ImageTexture.CreateFromImage(image);
    }

    private static Image LoadImage(string atlasPath)
    {
        if (!FileAccess.FileExists(atlasPath))
        {
            return null;
        }

        return Image.LoadFromFile(atlasPath);
    }

    private static void RemoveEdgeConnectedDarkBackground(Image image)
    {
        const float threshold = 14f / 255f;
        var width = image.GetWidth();
        var height = image.GetHeight();
        var visited = new bool[width * height];
        var queue = new Queue<int>();

        void TryEnqueue(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            var index = y * width + x;
            if (visited[index])
            {
                return;
            }

            visited[index] = true;
            if (IsDarkBackground(image.GetPixel(x, y), threshold))
            {
                queue.Enqueue(index);
            }
        }

        for (var x = 0; x < width; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, height - 1);
        }

        for (var y = 0; y < height; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(width - 1, y);
        }

        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            var x = index % width;
            var y = index / width;
            image.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            TryEnqueue(x - 1, y);
            TryEnqueue(x + 1, y);
            TryEnqueue(x, y - 1);
            TryEnqueue(x, y + 1);
        }
    }

    private static bool IsDarkBackground(Color color, float threshold)
    {
        return color.A > 0f && color.R <= threshold && color.G <= threshold && color.B <= threshold;
    }
}
