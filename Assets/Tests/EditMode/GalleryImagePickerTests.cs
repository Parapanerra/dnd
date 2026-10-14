using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GalleryImagePickerTests
{
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Test]
    public void SelectedImageLoadsResizesAndReportsBadPath()
    {
        string file = Path.Combine(Path.GetTempPath(), "Taruck-gallery-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(file, Convert.FromBase64String(Png));
        Texture2D received = null;
        Exception failure = null;
        try
        {
            Type pickerType = Type.GetType("GalleryImagePicker, Assembly-CSharp", true);
            object picker = Activator.CreateInstance(pickerType, true);
            MethodInfo load = pickerType.GetMethod("LoadSelectedImage", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<Texture2D> ready = texture => received = texture;
            Action<Exception> error = exception => failure = exception;
            load.Invoke(picker, new object[] { file, 64, 64, ready, error });
            Assert.IsNull(failure);
            Assert.NotNull(received);
            Assert.AreEqual(64, received.width);
            Assert.AreEqual(64, received.height);

            load.Invoke(picker, new object[] { file + ".missing", 64, 64, ready, error });
            Assert.IsInstanceOf<FileNotFoundException>(failure);
        }
        finally
        {
            if (received != null) UnityEngine.Object.DestroyImmediate(received);
            File.Delete(file);
        }
    }
}
