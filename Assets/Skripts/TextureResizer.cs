using UnityEngine;

// Resamples imported character and item images to their saved dimensions.
internal sealed class TextureResizer
{
    internal Texture2D Resize(Texture2D source, int width, int height)
    {
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float sourceY = height == 1 ? 0f : (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    float sourceX = width == 1 ? 0f : (float)x / (width - 1);
                    pixels[y * width + x] = source.GetPixelBilinear(sourceX, sourceY);
                }
            }

            result.SetPixels(pixels);
            result.Apply(false, false);
            return result;
        }
        catch
        {
            if (Application.isPlaying)
                Object.Destroy(result);
            else
                Object.DestroyImmediate(result);
            throw;
        }
    }
}
