using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class CharacterCollectionServiceTests
{
    [Test]
    public void CreatingAndDeletingCharactersKeepsAnActiveSelection()
    {
        Type service = Type.GetType("CharacterCollectionService, Assembly-CSharp");
        Assert.NotNull(service);
        AppSaveData data = new AppSaveData();
        object collection = Activator.CreateInstance(service, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { data }, null);

        CharacterData first = (CharacterData)service.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, null);
        CharacterData second = (CharacterData)service.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, null);
        Assert.AreEqual(2, data.characters.Count);
        Assert.AreEqual(second.id, data.lastActiveCharacterId);

        Assert.AreEqual(true, service.GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, new object[] { first.id }));
        Assert.AreEqual(true, service.GetMethod("Delete", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, new object[] { first.id }));
        Assert.AreEqual(second.id, data.lastActiveCharacterId);
        Assert.AreEqual(true, service.GetMethod("Delete", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, new object[] { second.id }));
        Assert.AreEqual(0, data.characters.Count);
        Assert.AreEqual("", data.lastActiveCharacterId);
    }

    [Test]
    public void CollectionInstanceOnlyMutatesItsBoundSave()
    {
        Type service = Type.GetType("CharacterCollectionService, Assembly-CSharp");
        Assert.NotNull(service);
        AppSaveData firstSave = new AppSaveData();
        AppSaveData secondSave = new AppSaveData();
        object first = Activator.CreateInstance(service, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { firstSave }, null);
        object second = Activator.CreateInstance(service, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { secondSave }, null);
        MethodInfo create = service.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);

        create.Invoke(first, null);
        Assert.AreEqual(1, firstSave.characters.Count);
        Assert.AreEqual(0, secondSave.characters.Count);
        create.Invoke(second, null);
        Assert.AreEqual(1, firstSave.characters.Count);
        Assert.AreEqual(1, secondSave.characters.Count);
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

        object importer = Activator.CreateInstance(service, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new TaruckLocalRepository(), Path.Combine(Path.GetTempPath(), "unused-import-test.taruck-data"), "test" }, null);
        AppSaveData candidate = (AppSaveData)service.GetMethod("Prepare", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(importer,
            new object[] { current, imported, true });
        Assert.AreEqual(1, current.characters.Count);
        Assert.AreEqual("same-id", current.characters[0].id);
        Assert.AreEqual(2, candidate.characters.Count);
        Assert.AreNotEqual(candidate.characters[0].id, candidate.characters[1].id);
        Assert.AreEqual("Imported", candidate.characters[1].characterName);
    }

    [Test]
    public void ImportWritesMergedCollectionAndPreservesPreimportCopy()
    {
        Type service = Type.GetType("CharacterCollectionImportService, Assembly-CSharp");
        Assert.NotNull(service);
        string folder = Path.Combine(Path.GetTempPath(), "Taruck-A7-Import-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "characters.taruck-data");
        Directory.CreateDirectory(folder);
        try
        {
            var current = new AppSaveData();
            current.characters.Add(new CharacterData("existing") { characterName = "Existing" });
            current.lastActiveCharacterId = "existing";
            var incoming = new AppSaveData();
            incoming.characters.Add(new CharacterData("incoming") { characterName = "Incoming" });
            incoming.lastActiveCharacterId = "incoming";
            var repository = new TaruckLocalRepository();
            repository.Save(path, current, "test");

            object importer = Activator.CreateInstance(service, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { repository, path, "test" }, null);
            AppSaveData candidate = (AppSaveData)service.GetMethod("Import", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(importer, new object[] { current, incoming, true });

            Assert.AreEqual(1, current.characters.Count);
            Assert.AreEqual(2, candidate.characters.Count);
            Assert.AreEqual(2, TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(path)).characters.Count);
            Assert.AreEqual(1, TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(path + ".preimport.bak")).characters.Count);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
