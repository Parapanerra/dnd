using System;
using NUnit.Framework;

public class CharacterSceneStateServiceTests
{
    [Test]
    public void ClearingOnePageClearsItsFamilyButKeepsOtherScenes()
    {
        Type service = Type.GetType("CharacterSceneStateService, Assembly-CSharp");
        Assert.NotNull(service);

        CharacterData character = new CharacterData("test");
        CharacterSceneData first = character.GetSceneData("cartaPersonaj 1");
        CharacterSceneData second = character.GetSceneData("cartaPersonaj 2");
        CharacterSceneData inventory = character.GetSceneData("inventory 1");
        first.SetString("note", "first");
        second.SetString("note", "second");
        inventory.SetString("note", "keep");

        service.GetMethod("ClearFamily").Invoke(null, new object[] { character, "cartaPersonaj 1" });

        Assert.AreEqual("", first.GetString("note"));
        Assert.AreEqual("", second.GetString("note"));
        Assert.AreEqual("keep", inventory.GetString("note"));
    }
}
