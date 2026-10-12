using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ScenePageButtonBinderTests
{
    private readonly List<GameObject> created = new List<GameObject>();
    private static readonly Type BinderType = Type.GetType("ScenePageButtonBinder, Assembly-CSharp");

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in created)
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        created.Clear();
    }

    [TestCase("page", 0)]
    [TestCase(" PAGE (2) ", 2)]
    [TestCase("page (12)", 12)]
    public void ParsesLegacyPageButtonNames(string name, int expectedIndex)
    {
        Assert.NotNull(BinderType);
        object binder = CreateBinder(_ => { });
        MethodInfo parse = BinderType.GetMethod("TryGetPageIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(parse);
        object[] arguments = { name, 0 };
        Assert.IsTrue((bool)parse.Invoke(binder, arguments));
        Assert.AreEqual(expectedIndex, arguments[1]);
    }

    [TestCase("page title")]
    [TestCase("page (x)")]
    [TestCase("notpage (2)")]
    public void IgnoresOtherButtonNames(string name)
    {
        Assert.NotNull(BinderType);
        object binder = CreateBinder(_ => { });
        MethodInfo parse = BinderType.GetMethod("TryGetPageIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(parse);
        Assert.IsFalse((bool)parse.Invoke(binder, new object[] { name, 0 }));
    }

    [Test]
    public void BindsOnlyPageButtonsToTheirOwnIndex()
    {
        Assert.NotNull(BinderType);
        Button first = CreateButton("page");
        Button third = CreateButton("page (2)");
        Button unrelated = CreateButton("other");
        List<int> selected = new List<int>();
        object binder = CreateBinder(selected.Add);

        BinderType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(binder, null);

        first.onClick.Invoke();
        third.onClick.Invoke();
        unrelated.onClick.Invoke();

        CollectionAssert.AreEqual(new[] { 0, 2 }, selected);
    }

    private Button CreateButton(string name)
    {
        GameObject gameObject = new GameObject(name);
        created.Add(gameObject);
        return gameObject.AddComponent<Button>();
    }

    private static object CreateBinder(Action<int> onPage)
    {
        return Activator.CreateInstance(BinderType,
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { onPage }, null);
    }
}
