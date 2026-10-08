using System;
using NUnit.Framework;

public class CharacterCollectionServiceTests
{
    [Test]
    public void CreatingAndDeletingCharactersKeepsAnActiveSelection()
    {
        Type service = Type.GetType("CharacterCollectionService, Assembly-CSharp");
        Assert.NotNull(service);
        AppSaveData data = new AppSaveData();

        CharacterData first = (CharacterData)service.GetMethod("Create").Invoke(null, new object[] { data });
        CharacterData second = (CharacterData)service.GetMethod("Create").Invoke(null, new object[] { data });
        Assert.AreEqual(2, data.characters.Count);
        Assert.AreEqual(second.id, data.lastActiveCharacterId);

        Assert.AreEqual(true, service.GetMethod("Select").Invoke(null, new object[] { data, first.id }));
        Assert.AreEqual(true, service.GetMethod("Delete").Invoke(null, new object[] { data, first.id }));
        Assert.AreEqual(second.id, data.lastActiveCharacterId);
        Assert.AreEqual(true, service.GetMethod("Delete").Invoke(null, new object[] { data, second.id }));
        Assert.AreEqual(0, data.characters.Count);
        Assert.AreEqual("", data.lastActiveCharacterId);
    }

    [Test]
    public void MergingSameCharacterIdDoesNotChangeTheCurrentSave()
    {
        Type service = Type.GetType("CharacterCollectionImportService, Assembly-CSharp");
        Assert.NotNull(service);
        AppSaveData current = new AppSaveData();
        current.characters.Add(new CharacterData("same-id") { characterName = "Existing" });
        current.lastActiveCharacterId = "same-id";
        AppSaveData imported = new AppSaveData();
        imported.characters.Add(new CharacterData("same-id") { characterName = "Imported" });
        imported.lastActiveCharacterId = "same-id";

        AppSaveData candidate = (AppSaveData)service.GetMethod("Prepare").Invoke(null,
            new object[] { current, imported, true, "test" });
        Assert.AreEqual(1, current.characters.Count);
        Assert.AreEqual("same-id", current.characters[0].id);
        Assert.AreEqual(2, candidate.characters.Count);
        Assert.AreNotEqual(candidate.characters[0].id, candidate.characters[1].id);
        Assert.AreEqual("Imported", candidate.characters[1].characterName);
    }
}
