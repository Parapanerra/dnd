using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Afix1SceneBindingTests
{
    [Test]
    public void ValidatorRejectsMissingInventoryControl()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/personag/inventory.unity");
        Type markerType = Type.GetType("UiControlMarker, Assembly-CSharp", true);
        Component marker = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(markerType, true))
            .First(m => markerType.GetProperty("Role").GetValue(m).ToString() == "ExportItem");
        UnityEngine.Object.DestroyImmediate(marker);
        Type validator = Type.GetType("UiDependencyMigration, Assembly-CSharp-Editor", true);
        TargetInvocationException error = Assert.Throws<TargetInvocationException>(() =>
            validator.GetMethod("Validate").Invoke(null, new object[] { scene }));
        StringAssert.Contains("ExportItem", error.InnerException.Message);
    }

    [Test]
    public void ValidatorRejectsBrokenSequenceReference()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/personag/Spels.unity");
        Type config = Type.GetType("ToggleSequenceConfig, Assembly-CSharp", true);
        Component group = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(config, true)).First();
        var toggles = (UnityEngine.UI.Toggle[])config.GetProperty("Toggles").GetValue(group);
        toggles[0] = null;
        Type validator = Type.GetType("UiDependencyMigration, Assembly-CSharp-Editor", true);
        TargetInvocationException error = Assert.Throws<TargetInvocationException>(() =>
            validator.GetMethod("Validate").Invoke(null, new object[] { scene }));
        StringAssert.Contains("Invalid toggle sequence", error.InnerException.Message);
    }

    [TearDown]
    public void TearDown() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [TestCase("cartaPersonaj")]
    [TestCase("Spels")]
    [TestCase("spelBook")]
    [TestCase("inventory")]
    [TestCase("informForPerson")]
    [TestCase("petsesn")]
    public void MigratedSceneBindingsSurviveRenamedObjects(string sceneName)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/personag/" + sceneName + ".unity");
        Type validator = Type.GetType("SceneRoleMigration, Assembly-CSharp-Editor", true);
        MethodInfo validate = validator.GetMethod("ValidateScene", BindingFlags.Static | BindingFlags.NonPublic);
        validate.Invoke(null, new object[] { scene });
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.name = "Renamed " + t.GetInstanceID();
        validate.Invoke(null, new object[] { scene });

        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        Type installer = Type.GetType("ToggleSequenceSceneInstaller, Assembly-CSharp", true);
        object instance = Activator.CreateInstance(installer, hidden, null, new object[] { null }, null);
        installer.GetMethod("Install", hidden).Invoke(instance, null);
        Type config = Type.GetType("ToggleSequenceConfig, Assembly-CSharp", true);
        Type sequence = Type.GetType("DeathSaveToggleSequence, Assembly-CSharp", true);
        foreach (Component group in UnityEngine.Object.FindObjectsByType(config, FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Component installed = group.GetComponent(sequence);
            Assert.NotNull(installed, group.name);
            var expected = (UnityEngine.UI.Toggle[])config.GetProperty("Toggles").GetValue(group);
            var actual = (UnityEngine.UI.Toggle[])sequence.GetField("orderedToggles", hidden).GetValue(installed);
            CollectionAssert.AreEqual(expected, actual);
        }
        if (sceneName == "inventory")
        {
            Type markerType = Type.GetType("UiControlMarker, Assembly-CSharp", true);
            Type cellType = Type.GetType("InventoryItemCell, Assembly-CSharp", true);
            foreach (Component marker in UnityEngine.Object.FindObjectsByType(markerType, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (markerType.GetProperty("Role").GetValue(marker).ToString() != "InventoryCell") continue;
                Component cell = marker.GetComponent(cellType) ?? marker.gameObject.AddComponent(cellType);
                cellType.GetMethod("FindControls", hidden).Invoke(cell, null);
                Assert.NotNull(cellType.GetField("itemNameInput", hidden).GetValue(cell));
                Assert.NotNull(cellType.GetField("categoryDropdown", hidden).GetValue(cell));
                Assert.NotNull(cellType.GetField("exportButton", hidden).GetValue(cell));
            }
        }
    }
}
