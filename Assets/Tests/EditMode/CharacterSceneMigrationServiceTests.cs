using System;
using System.Reflection;
using NUnit.Framework;

public class CharacterSceneMigrationServiceTests
{
    [Test]
    public void LegacyFieldsFillEmptySceneOnceWithoutOverwritingLaterEdits()
    {
        Type service = Type.GetType("CharacterSceneMigrationService, Assembly-CSharp");
        Assert.NotNull(service);
        object instance = Activator.CreateInstance(service, true);
        CharacterData character = new CharacterData("old");
        character.inputData.Add("legacy name");
        character.toggleData.Add(true);
        CharacterSceneData scene = new CharacterSceneData("cartaPersonaj 1");

        service.GetMethod("CopyLegacyFieldsIfEmpty", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, new object[] { character, scene });
        Assert.AreEqual("legacy name", scene.inputData[0]);
        Assert.IsTrue(scene.toggleData[0]);

        scene.inputData[0] = "new name";
        service.GetMethod("CopyLegacyFieldsIfEmpty", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, new object[] { character, scene });
        Assert.AreEqual("new name", scene.inputData[0]);
    }
}
