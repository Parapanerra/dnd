using System.Collections.Generic;
using UnityEngine;

// Moves only the distant background along X. The character sheet keeps its normal camera motion.
internal sealed class HorizontalBackgroundParallax
{
    private readonly Dictionary<Transform, float> startingX = new Dictionary<Transform, float>();
    private float startingCameraX;

    internal void Collect(Camera camera, IEnumerable<SpriteRenderer> backgrounds)
    {
        startingX.Clear();
        startingCameraX = camera.transform.position.x;

        foreach (SpriteRenderer background in backgrounds)
        {
            if (background != null)
                startingX[background.transform] = background.transform.position.x;
        }
    }

    internal void Update(Camera camera, float movementFraction)
    {
        float cameraDisplacement = camera.transform.position.x - startingCameraX;
        float cameraFollowFraction = 1f - Mathf.Clamp01(movementFraction);
        foreach (KeyValuePair<Transform, float> background in startingX)
        {
            Transform layer = background.Key;
            if (layer == null)
                continue;

            Vector3 position = layer.position;
            position.x = background.Value + cameraDisplacement * cameraFollowFraction;
            layer.position = position;
        }
    }
}
