using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class A7SpellSlotSceneTests
{
    private const string ScenePath = "Assets/Scenes/personag/Spels.unity";

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void SpellSceneUsesItsExistingSheetControllerWithoutAddingAnother()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        Type contextType = Type.GetType("CharacterSceneContext, Assembly-CSharp");
        Type sheetType = Type.GetType("CharacterSheetManagerScene1, Assembly-CSharp");
        Assert.NotNull(contextType);
        Assert.NotNull(sheetType);
        Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType(sheetType, FindObjectsInactive.Include, FindObjectsSortMode.None).Length);

        object context = Activator.CreateInstance(contextType);
        contextType.GetMethod("OnSceneLoaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, new object[] { scene });

        Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType(sheetType, FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
    }

    [Test]
    public void RestUsesActiveSpellSceneSaveController()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        Type contextType = Type.GetType("CharacterSceneContext, Assembly-CSharp");
        Type sheetType = Type.GetType("CharacterSheetManagerScene1, Assembly-CSharp");
        Type restType = Type.GetType("CharacterRestService, Assembly-CSharp");
        Assert.NotNull(contextType);
        Assert.NotNull(restType);

        object context = Activator.CreateInstance(contextType);
        contextType.GetMethod("OnSceneLoaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, new object[] { scene });
        Component controller = UnityEngine.Object.FindAnyObjectByType(sheetType) as Component;
        Assert.NotNull(controller);

        object rest = Activator.CreateInstance(restType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
        MethodInfo resolver = restType.GetMethod("FindActiveSceneSaveController",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(resolver);
        Assert.AreSame(controller, resolver.Invoke(rest, null));
    }

    [Test]
    public void SpellSlotGroupsAdvanceInVisualOrder()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Type sequenceType = Type.GetType("DeathSaveToggleSequence, Assembly-CSharp");
        Type installerType = Type.GetType("ToggleSequenceSceneInstaller, Assembly-CSharp");
        Assert.NotNull(sequenceType);
        Assert.NotNull(installerType);
        object installer = Activator.CreateInstance(installerType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
        installerType.GetMethod("Install", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(installer, null);

        int groups = 0;
        foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (!transform.name.StartsWith("uspih", StringComparison.Ordinal))
                continue;
            Component sequence = transform.GetComponent(sequenceType);
            Assert.NotNull(sequence, transform.name + " lacks toggle sequence");
            Toggle[] ordered = (Toggle[])sequenceType.GetField("orderedToggles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sequence);
            Assert.AreEqual(8, ordered.Length, transform.name);
            Assert.AreEqual("Toggle (2)", ordered[0].name, transform.name);
            foreach (Toggle toggle in ordered)
                toggle.SetIsOnWithoutNotify(false);
            sequenceType.GetMethod("Configure", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sequence, new object[] { ordered, false, null });
            ordered[2].isOn = true;
            Assert.IsTrue(ordered[0].isOn, transform.name);
            Assert.IsFalse(ordered[2].isOn, transform.name);
            groups++;
        }
        Assert.AreEqual(9, groups);
    }
}
