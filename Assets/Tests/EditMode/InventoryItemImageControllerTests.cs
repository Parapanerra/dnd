using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemImageControllerTests
{
    [Test]
    public void ImportedImageCanBeExportedAndCleared()
    {
        Type controllerType = Type.GetType("InventoryItemImageController, Assembly-CSharp");
        Assert.NotNull(controllerType);
        GameObject cell = new GameObject("Cell");
        Texture2D source = new Texture2D(2, 2);
        try
        {
            Image image = cell.AddComponent<Image>();
            Component controller = cell.AddComponent(controllerType);
            controllerType.GetMethod("Initialize").Invoke(controller, new object[] { image });
            source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            source.Apply();
            string encoded = Convert.ToBase64String(source.EncodeToJPG());

            controllerType.GetMethod("ApplyBase64").Invoke(controller, new object[] { encoded });
            Assert.NotNull(image.sprite);
            Assert.IsNotEmpty((string)controllerType.GetMethod("GetBase64").Invoke(controller, null));

            controllerType.GetMethod("ApplyBase64").Invoke(controller, new object[] { "" });
            Assert.IsTrue(image.sprite == null);
            Assert.AreEqual("", controllerType.GetMethod("GetBase64").Invoke(controller, null));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(cell);
        }
    }
}
