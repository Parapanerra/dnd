using System;
using System.Reflection;
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
        Sprite placeholder = Sprite.Create(source, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        try
        {
            Image image = cell.AddComponent<Image>();
            image.sprite = placeholder;
            Component controller = cell.AddComponent(controllerType);
            controllerType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { image });
            Assert.IsTrue(image.enabled, "The dark placeholder must remain visible before a photo is selected.");
            Assert.AreSame(placeholder, image.sprite);
            source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            source.Apply();
            string encoded = Convert.ToBase64String(source.EncodeToJPG());

            controllerType.GetMethod("ApplyBase64", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { encoded });
            Assert.NotNull(image.sprite);
            Assert.AreNotSame(placeholder, image.sprite);
            Assert.IsNotEmpty((string)controllerType.GetMethod("GetBase64", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null));

            controllerType.GetMethod("ApplyBase64", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { "" });
            Assert.AreSame(placeholder, image.sprite);
            Assert.IsTrue(image.enabled);
            Assert.AreEqual("", controllerType.GetMethod("GetBase64", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(placeholder);
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(cell);
        }
    }
}
