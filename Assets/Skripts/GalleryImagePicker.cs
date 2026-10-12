using System;
using UnityEngine;

// Loads and resizes an image selected on the device. The receiver owns the returned texture.
internal sealed class GalleryImagePicker
{
    private readonly TextureResizer resizer = new TextureResizer();

    internal void Pick(string title, int width, int height,
        Action<Texture2D> onReady, Action<Exception> onError)
    {
        NativeGallery.GetImageFromGallery(path =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            Texture2D source = null;
            Texture2D resized = null;
            try
            {
                source = NativeGallery.LoadImageAtPath(
                    path, AppConfig.Images.GalleryPreviewMaxSize, false, false);
                if (source != null)
                    resized = resizer.Resize(source, width, height);
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception);
            }
            finally
            {
                DestroyRuntimeObject(source);
            }

            if (resized != null)
            {
                if (onReady != null)
                    onReady(resized);
                else
                    DestroyRuntimeObject(resized);
            }
        }, title, "image/*");
    }

    private void DestroyRuntimeObject(UnityEngine.Object value)
    {
        if (value == null)
            return;
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(value);
        else
            UnityEngine.Object.DestroyImmediate(value);
    }
}
