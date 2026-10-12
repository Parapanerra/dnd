using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SceneNavigationButtonBinderTests
{
    private GameObject buttonObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
            UnityEngine.Object.DestroyImmediate(buttonObject);
    }

    [Test]
    public void BindAllSkipsButtonsOutsideRequestedScene()
    {
        buttonObject = new GameObject("spellbook", typeof(RectTransform), typeof(Button));
        Button allowed = buttonObject.GetComponent<Button>();
        GameObject skippedObject = new GameObject("spellbook", typeof(RectTransform), typeof(Button));
        try
        {
            Button skipped = skippedObject.GetComponent<Button>();
            int calls = 0;
            Type binderType = Type.GetType("SceneNavigationButtonBinder, Assembly-CSharp");
            object binder = Activator.CreateInstance(binderType,
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { new Action<string>(_ => calls++), new[] { allowed, skipped } }, null);
            MethodInfo bindAll = binderType.GetMethod("BindAll", BindingFlags.Instance | BindingFlags.NonPublic);
            int count = (int)bindAll.Invoke(binder,
                new object[] { "spelBook", new Func<Button, bool>(button => button == allowed),
                    new[] { "spelbook", "spellbook" } });

            Assert.AreEqual(1, count);
            allowed.onClick.Invoke();
            skipped.onClick.Invoke();
            Assert.AreEqual(1, calls);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(skippedObject);
        }
    }
    [Test]
    public void NamedLegacyButtonNavigatesOnceToRequestedScene()
    {
        buttonObject = new GameObject("SpElBoOk (1)", typeof(RectTransform), typeof(Button));
        Button button = buttonObject.GetComponent<Button>();
        string destination = null;
        int calls = 0;
        Type binderType = Type.GetType("SceneNavigationButtonBinder, Assembly-CSharp");
        Assert.NotNull(binderType);
        object binder = Activator.CreateInstance(binderType,
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { new Action<string>(name => { destination = name; calls++; }), new[] { button } }, null);
        MethodInfo bind = binderType.GetMethod("BindFirst", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsTrue((bool)bind.Invoke(binder, new object[] { "spelBook", new[] { "spelbook", "spellbook" } }));

        button.onClick.Invoke();
        Assert.AreEqual("spelBook", destination);
        Assert.AreEqual(1, calls);
    }
}