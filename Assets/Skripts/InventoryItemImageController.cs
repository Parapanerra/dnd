using System;
using UnityEngine;
using UnityEngine.UI;

// Owns one inventory cell's gallery image and the runtime texture/sprite lifecycle.
public class InventoryItemImageController : MonoBehaviour
{
    private Image image;
    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;
    private Sprite defaultSprite;
    private Color defaultColor = Color.white;
    private bool defaultPreserveAspect;

    public void Initialize(Image target)
    {
        if (image != null)
            return;

        image = target;
        if (image == null)
            return;

        defaultSprite = image.sprite;
        defaultColor = image.color;
        defaultPreserveAspect = image.preserveAspect;
    }

    public void SelectFromGallery(Action onChanged)
    {
        NativeGallery.GetImageFromGallery(
            path =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                try
                {
                    Texture2D source = NativeGallery.LoadImageAtPath(
                        path, AppConfig.Images.GalleryPreviewMaxSize, false, false);
                    if (source == null)
                        return;

                    Texture2D resized = ResizeToSquare(source);
                    DestroyRuntimeObject(source);
                    ApplyTexture(resized);
                    onChanged?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not load inventory item image: " + exception.Message);
                }
            },
            "Select item image",
            "image/*");
    }

    public void ApplyBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            RestoreDefault();
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(
                AppConfig.Images.TextureBootstrapSize,
                AppConfig.Images.TextureBootstrapSize,
                TextureFormat.RGBA32,
                false);
            if (texture.LoadImage(bytes))
                ApplyTexture(texture);
            else
                DestroyRuntimeObject(texture);
        }
        catch
        {
            RestoreDefault();
        }
    }

    public string GetBase64()
    {
        return runtimeTexture == null ? "" :
            Convert.ToBase64String(runtimeTexture.EncodeToJPG(AppConfig.Images.InventoryJpgQuality));
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (image == null || texture == null)
            return;

        ClearRuntimeImage();
        runtimeTexture = texture;
        runtimeSprite = Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        image.sprite = runtimeSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.SetAllDirty();
    }

    private void RestoreDefault()
    {
        if (image == null)
            return;

        ClearRuntimeImage();
        image.sprite = defaultSprite;
        image.color = defaultColor;
        image.preserveAspect = defaultPreserveAspect;
    }

    private void ClearRuntimeImage()
    {
        if (runtimeSprite != null)
        {
            DestroyRuntimeObject(runtimeSprite);
            runtimeSprite = null;
        }

        if (runtimeTexture != null)
        {
            DestroyRuntimeObject(runtimeTexture);
            runtimeTexture = null;
        }
    }

    private void OnDestroy()
    {
        ClearRuntimeImage();
    }

    private static void DestroyRuntimeObject(UnityEngine.Object value)
    {
        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }

    private static Texture2D ResizeToSquare(Texture2D source)
    {
        int size = AppConfig.Images.InventoryImageSize;
        Texture2D result = new Texture2D(size, size, TextureFormat.RGB24, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float sourceY = size == 1 ? 0f : (float)y / (size - 1);
            for (int x = 0; x < size; x++)
            {
                float sourceX = size == 1 ? 0f : (float)x / (size - 1);
                pixels[y * size + x] = source.GetPixelBilinear(sourceX, sourceY);
            }
        }

        result.SetPixels(pixels);
        result.Apply(false, false);
        return result;
    }
}
