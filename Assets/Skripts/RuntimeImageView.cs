using UnityEngine;
using UnityEngine.UI;

// Owns one Image's temporary sprite and texture, including EditMode cleanup.
internal sealed class RuntimeImageView
{
    private Image image;
    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;
    private Sprite defaultSprite;
    private Color defaultColor;
    private bool defaultPreserveAspect;

    internal bool IsBound => image != null;
    internal Texture2D Texture => runtimeTexture;

    internal void Bind(Image target)
    {
        if (IsBound || target == null)
            return;

        image = target;
        defaultSprite = image.sprite;
        defaultColor = image.color;
        defaultPreserveAspect = image.preserveAspect;
    }

    internal void Show(Texture2D texture)
    {
        if (image == null || texture == null)
            return;

        ClearRuntime();
        runtimeTexture = texture;
        runtimeSprite = Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        image.sprite = runtimeSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.SetAllDirty();
    }

    internal void Restore()
    {
        if (image == null)
            return;

        ClearRuntime();
        image.sprite = defaultSprite;
        image.color = defaultColor;
        image.preserveAspect = defaultPreserveAspect;
    }

    internal void Dispose()
    {
        ClearRuntime();
    }

    private void ClearRuntime()
    {
        DestroyRuntimeObject(runtimeSprite);
        DestroyRuntimeObject(runtimeTexture);
        runtimeSprite = null;
        runtimeTexture = null;
    }

    private void DestroyRuntimeObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(value);
        else
            Object.DestroyImmediate(value);
    }
}
