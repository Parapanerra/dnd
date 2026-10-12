using System.Collections.Generic;
using UnityEngine;

// Keeps tiled backgrounds wide enough for the current orthographic camera.
internal sealed class AdaptiveBackgroundFitter
{
    private const int BackgroundSortingOrder = -2;
    private const float BackgroundOverscan = 1.05f;
    private readonly Dictionary<SpriteRenderer, Vector2> originalSizes =
        new Dictionary<SpriteRenderer, Vector2>();

    internal void Collect()
    {
        originalSizes.Clear();
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (renderer.sortingOrder != BackgroundSortingOrder)
                continue;

            originalSizes.Add(renderer, renderer.size);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
        }
    }

    internal void Fit()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
            return;

        foreach (KeyValuePair<SpriteRenderer, Vector2> background in originalSizes)
        {
            SpriteRenderer renderer = background.Key;
            if (renderer == null)
                continue;

            float scaleX = Mathf.Abs(renderer.transform.lossyScale.x);
            if (scaleX <= Mathf.Epsilon)
                continue;

            float halfWidth = camera.orthographicSize * camera.aspect * BackgroundOverscan;
            float distance = Mathf.Abs(camera.transform.position.x - renderer.bounds.center.x);
            float requiredWorldWidth = (halfWidth + distance) * 2f;

            Vector2 size = renderer.size;
            size.x = Mathf.Max(background.Value.x, requiredWorldWidth / scaleX);
            size.y = background.Value.y;
            renderer.size = size;
        }
    }
}
