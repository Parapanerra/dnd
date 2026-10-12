using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class A7HealthBarViewTests
{
    [Test]
    public void RenderUpdatesCurrentAndTemporaryHealthWithoutChangingSavedState()
    {
        Type viewType = Type.GetType("HealthBarView, Assembly-CSharp");
        Assert.NotNull(viewType);

        GameObject healthObject = new GameObject("Health", typeof(RectTransform), typeof(Slider));
        GameObject temporaryObject = new GameObject("TemporaryHealth", typeof(RectTransform), typeof(Slider));
        GameObject healthTextObject = new GameObject("HealthText", typeof(RectTransform), typeof(Text));
        GameObject temporaryTextObject = new GameObject("TemporaryText", typeof(RectTransform), typeof(Text));
        try
        {
            Slider health = healthObject.GetComponent<Slider>();
            Slider temporary = temporaryObject.GetComponent<Slider>();
            Text healthText = healthTextObject.GetComponent<Text>();
            Text temporaryText = temporaryTextObject.GetComponent<Text>();
            object view = Activator.CreateInstance(viewType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { null, health, temporary, healthText, temporaryText }, null);
            MethodInfo render = viewType.GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(render);

            render.Invoke(view, new object[] { 7, 10, 2, 5 });
            Assert.AreEqual("7 / 10", healthText.text);
            Assert.AreEqual("2 / 5", temporaryText.text);
            Assert.AreEqual(0.7f, health.value, 0.0001f);
            Assert.AreEqual(0.4f, temporary.value, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temporaryTextObject);
            UnityEngine.Object.DestroyImmediate(healthTextObject);
            UnityEngine.Object.DestroyImmediate(temporaryObject);
            UnityEngine.Object.DestroyImmediate(healthObject);
        }
    }
}