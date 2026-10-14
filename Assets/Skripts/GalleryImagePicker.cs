using System;
using UnityEngine;

// Loads and resizes an image selected on the device. The receiver owns the returned texture.
internal sealed class GalleryImagePicker
{
    private readonly TextureResizer resizer = new TextureResizer();

    internal void Pick(string title, int width, int height,
        Action<Texture2D> onReady, Action<Exception> onError)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            string path = WindowsNativeImagePicker.Pick(title);
            LoadSelectedImage(path, width, height, onReady, onError);
        }
        catch (Exception exception)
        {
            onError?.Invoke(exception);
        }
#else
        NativeGallery.GetImageFromGallery(path =>
            LoadSelectedImage(path, width, height, onReady, onError), title, "image/*");
#endif
    }

    private void LoadSelectedImage(string path, int width, int height,
        Action<Texture2D> onReady, Action<Exception> onError)
    {
        if (string.IsNullOrEmpty(path))
            return;

        Texture2D source = null;
        Texture2D resized = null;
        try
        {
            source = NativeGallery.LoadImageAtPath(
                path, AppConfig.Images.GalleryPreviewMaxSize, false, false);
            if (source == null)
                throw new System.IO.InvalidDataException("Selected image could not be decoded");
            resized = resizer.Resize(source, width, height);
            if (resized == null)
                throw new System.IO.InvalidDataException("Selected image could not be resized");
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
