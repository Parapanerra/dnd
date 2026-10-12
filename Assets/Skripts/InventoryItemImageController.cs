using System;
using UnityEngine;
using UnityEngine.UI;

// Owns one inventory cell's gallery image and the runtime texture/sprite lifecycle.
public class InventoryItemImageController : MonoBehaviour
{
    private readonly GalleryImagePicker galleryPicker = new GalleryImagePicker();
    private readonly RuntimeImageView imageView = new RuntimeImageView();

    internal void Initialize(Image target)
    {
        imageView.Bind(target);
    }

    internal void SelectFromGallery(Action onChanged)
    {
        if (!imageView.IsBound)
            return;

        galleryPicker.Pick("Select item image",
            AppConfig.Images.InventoryImageSize, AppConfig.Images.InventoryImageSize,
            texture =>
            {
                try
                {
                    imageView.Show(texture);
                    onChanged?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not load inventory item image: " + exception.Message);
                }
            },
            exception => Debug.LogError("Could not load inventory item image: " + exception.Message));
    }

    internal void ApplyBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            imageView.Restore();
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
            {
                imageView.Show(texture);
            }
            else
            {
                DestroyRuntimeObject(texture);
                imageView.Restore();
            }
        }
        catch
        {
            imageView.Restore();
        }
    }

    internal string GetBase64()
    {
        return imageView.Texture == null ? "" :
            Convert.ToBase64String(imageView.Texture.EncodeToJPG(AppConfig.Images.InventoryJpgQuality));
    }

    private void OnDestroy()
    {
        imageView.Dispose();
    }

    private static void DestroyRuntimeObject(UnityEngine.Object value)
    {
        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }

}
