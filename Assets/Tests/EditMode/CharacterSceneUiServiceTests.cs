using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSceneUiServiceTests
{
    [Test]
    public void ResetButtonUsesRoleMarkerBeforeLegacyName()
    {
        Type service = Type.GetType("CharacterSceneUiService, Assembly-CSharp");
        Type markerType = Type.GetType("SceneRoleMarker, TaruckShip.StableFields");
        Assert.NotNull(service);
        Assert.NotNull(markerType);
        MethodInfo isResetButton = service.GetMethod("IsResetButton", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(isResetButton);

        GameObject objectWithButton = new GameObject("resetseve", typeof(RectTransform), typeof(Button));
        try
        {
            Button button = objectWithButton.GetComponent<Button>();
            Assert.IsTrue((bool)isResetButton.Invoke(null, new object[] { button }));

            Component marker = objectWithButton.AddComponent(markerType);
            MethodInfo configure = markerType.GetMethod("Configure");
            Assert.NotNull(configure);
            Type roleType = markerType.GetProperty("Role").PropertyType;
            configure.Invoke(marker, new object[] { Enum.Parse(roleType, "Rage"), false });
            Assert.IsFalse((bool)isResetButton.Invoke(null, new object[] { button }),
                "A role marker must override a legacy-looking object name.");

            configure.Invoke(marker, new object[] { Enum.Parse(roleType, "ResetScene"), false });
            Assert.IsTrue((bool)isResetButton.Invoke(null, new object[] { button }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objectWithButton);
        }
    }
}
