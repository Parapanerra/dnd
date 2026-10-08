using System;
using NUnit.Framework;

public class CharacterSceneMigrationServiceTests
{
    [Test]
    public void LegacyFieldsFillEmptySceneOnceWithoutOverwritingLaterEdits()
    {
        Type service = Type.GetType("CharacterSceneMigrationService, Assembly-CSharp");
        Assert.NotNull(service);
        CharacterData character = new CharacterData("old");
        character.inputData.Add("legacy name");
        character.toggleData.Add(true);
        CharacterSceneData scene = new CharacterSceneData("cartaPersonaj 1");

        service.GetMethod("CopyLegacyFieldsIfEmpty").Invoke(null, new object[] { character, scene });
        Assert.AreEqual("legacy name", scene.inputData[0]);
        Assert.IsTrue(scene.toggleData[0]);

        scene.inputData[0] = "new name";
        service.GetMethod("CopyLegacyFieldsIfEmpty").Invoke(null, new object[] { character, scene });
        Assert.AreEqual("new name", scene.inputData[0]);
    }
}
