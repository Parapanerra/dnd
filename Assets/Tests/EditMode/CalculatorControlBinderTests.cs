using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CalculatorControlBinderTests
{
    [Test]
    public void RoutesNumberAndHpButtonsWithoutBindingPotionButton()
    {
        Type binderType = Type.GetType("CalculatorControlBinder, Assembly-CSharp");
        Assert.NotNull(binderType);
        GameObject root = new GameObject("kalPanel", typeof(RectTransform));
        GameObject host = new GameObject("Calculator", typeof(RectTransform));
        try
        {
            host.transform.SetParent(root.transform);
            Button number = CreateButton(root.transform, "number", "1");
            Button damage = CreateButton(root.transform, "damage", "Damage");
            Button potion = CreateButton(root.transform, "potionplus", "+");
            List<Button> bound = new List<Button>();
            string mainLabel = null;
            string hpName = null;
            object binder = Activator.CreateInstance(binderType, host.transform,
                new Func<string, string>(label => label),
                new Func<string, bool>(label => label == "1"));

            binderType.GetMethod("BindButtons").Invoke(binder, new object[]
            {
                bound,
                new Action<string>(label => mainLabel = label),
                new Action<string, Button>((name, button) => hpName = name)
            });
            Assert.AreEqual(1, bound.Count);
            Assert.AreSame(number, bound[0]);

            number.onClick.Invoke();
            damage.onClick.Invoke();
            potion.onClick.Invoke();
            Assert.AreEqual("1", mainLabel);
            Assert.AreEqual("damage", hpName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(parent);
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform);
        textObject.GetComponent<Text>().text = label;
        return buttonObject.GetComponent<Button>();
    }
}
