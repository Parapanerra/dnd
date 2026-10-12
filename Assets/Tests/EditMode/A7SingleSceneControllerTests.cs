using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class A7SingleSceneControllerTests
{
    private static readonly string[] CharacterScenes =
    {
        "cartaPersonaj", "informForPerson", "inventory", "petsesn", "spelBook", "Spels"
    };

    [Test]
    public void UnrelatedSceneNameDoesNotGetCharacterController()
    {
        Type contextType = Type.GetType("CharacterSceneContext, Assembly-CSharp");
        Assert.NotNull(contextType);
        var isCharacterScene = contextType.GetMethod("IsCharacterSheetScene",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(isCharacterScene);
        Assert.IsFalse((bool)isCharacterScene.Invoke(null, new object[] { "myinventorytest" }));
        Assert.IsFalse((bool)isCharacterScene.Invoke(null, new object[] { "menu" }));
    }
    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void EveryCharacterSceneHasExactlyOneSaveControllerAfterBootstrap()
    {
        Type contextType = Type.GetType("CharacterSceneContext, Assembly-CSharp");
        Type controllerType = Type.GetType("CharacterSheetManagerScene1, Assembly-CSharp");
        Assert.NotNull(contextType);
        Assert.NotNull(controllerType);

        foreach (string name in CharacterScenes)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/personag/" + name + ".unity");
            object context = Activator.CreateInstance(contextType);
            contextType.GetMethod("OnSceneLoaded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(context, new object[] { scene });
            int count = UnityEngine.Object.FindObjectsByType(
                controllerType, FindObjectsInactive.Include).Length;
            Assert.AreEqual(1, count, name);
        }
    }
}