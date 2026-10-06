using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class TaruckBinarySpecificationTests
{
    [Test]
    public void FrozenV1ControlVectors()
    {
        byte[] encoded = TaruckBinaryCodec.EncodeFullSave(new AppSaveData(), "test");
        CollectionAssert.AreEqual(new byte[] { 0x54, 0x41, 0x52, 0x55, 0x43, 0x4B, 0x50, 0x4B,
            1, 0, 2, 0, 0 }, new ArraySegment<byte>(encoded, 0, 13));
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 2, 4, 0, 0, 0, 1, 0, 0, 0 },
            new ArraySegment<byte>(encoded, 29, 13));
        Assert.That(TaruckBinaryCodec.Crc32(System.Text.Encoding.ASCII.GetBytes("123456789")),
            Is.EqualTo(0xCBF43926u));
    }

    [Test]
    public void IndependentBinaryFixtures_KeepExactV1Bytes()
    {
        string root = Path.Combine(Application.dataPath, "Tests", "Fixtures", "Binary");
        byte[] full = File.ReadAllBytes(Path.Combine(root, "v1_empty_full.taruck-save"));
        CollectionAssert.AreEqual(full,
            TaruckBinaryCodec.EncodeFullSave(TaruckBinaryCodec.DecodeFullSave(full), "fixture-1"));
        byte[] character = File.ReadAllBytes(Path.Combine(root, "v1_character.taruck-character"));
        CollectionAssert.AreEqual(character,
            TaruckBinaryCodec.EncodeCharacterExport(TaruckBinaryCodec.DecodeCharacterExport(character), "fixture-1"));
        byte[] item = File.ReadAllBytes(Path.Combine(root, "v1_item_png.taruck-item"));
        CollectionAssert.AreEqual(item,
            TaruckBinaryCodec.EncodeItemExport(TaruckBinaryCodec.DecodeItemExport(item), "fixture-1"));
    }

    [Test]
    public void FullSaveCharacterAndItem_RoundTripAndRejectWrongTypeOrDamage()
    {
        var character = new CharacterData("one") { characterName = "Герой", currentHealth = 17, maxHealth = 25 };
        character.inputData.Add("Strength");
        character.sharedStringData.Add(new StringSaveEntry { key = "portrait", value = "image-data" });
        var scene = new CharacterSceneData("cartaPersonaj");
        scene.inputData.Add("Клерик");
        scene.toggleData.Add(true);
        scene.sliderData.Add(0.375f);
        scene.dropdownData.Add(2);
        scene.stringData.Add(new StringSaveEntry { key = "a", value = "б" });
        scene.intData.Add(new IntSaveEntry { key = "b", value = 42 });
        scene.floatData.Add(new FloatSaveEntry { key = "c", value = 1.25f });
        scene.stableFieldVersion = 1;
        character.sceneStates.Add(scene);
        var save = new AppSaveData { lastActiveCharacterId = "one" };
        save.characters.Add(character);
        byte[] full = TaruckBinaryCodec.EncodeFullSave(save, "test");
        AppSaveData decoded = TaruckBinaryCodec.DecodeFullSave(full);
        Assert.That(decoded.characters[0].sceneStates[0].inputData[0], Is.EqualTo("Клерик"));
        Assert.That(decoded.characters[0].sceneStates[0].sliderData[0], Is.EqualTo(0.375f));
        Assert.That(decoded.characters[0].sharedStringData[0].value, Is.EqualTo("image-data"));
        Assert.Throws<InvalidDataException>(() => TaruckBinaryCodec.DecodeCharacterExport(full));
        byte[] futureVersion = (byte[])full.Clone();
        futureVersion[8] = 2;
        Assert.Throws<UnsupportedTaruckVersionException>(() => TaruckBinaryCodec.DecodeFullSave(futureVersion));
        byte[] truncated = new byte[full.Length - 1];
        Array.Copy(full, truncated, truncated.Length);
        Assert.Throws<InvalidDataException>(() => TaruckBinaryCodec.DecodeFullSave(truncated));
        full[40] ^= 1;
        Assert.Throws<InvalidDataException>(() => TaruckBinaryCodec.DecodeFullSave(full));

        byte[] single = TaruckBinaryCodec.EncodeCharacterExport(character, "test");
        Assert.That(TaruckBinaryCodec.DecodeCharacterExport(single).sceneStates[0].intData[0].value, Is.EqualTo(42));
        var item = new InventoryItemExportData { itemName = "Зілля", category = 3 };
        byte[] itemBytes = TaruckBinaryCodec.EncodeItemExport(item, "test");
        Assert.That(TaruckBinaryCodec.DecodeItemExport(itemBytes).itemName, Is.EqualTo("Зілля"));
        item.customImageBase64 = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47, 13, 10, 26, 10 });
        itemBytes = TaruckBinaryCodec.EncodeItemExport(item, "test");
        Assert.That(TaruckBinaryCodec.DecodeItemExport(itemBytes).customImageBase64, Is.EqualTo(item.customImageBase64));
        itemBytes[itemBytes.Length - 1] ^= 1;
        Assert.Throws<InvalidDataException>(() => TaruckBinaryCodec.DecodeItemExport(itemBytes));
    }

    [Test]
    public void LegacyImporters_ReadFrozenFilesAndRepairMissingActiveId()
    {
        string fixtureRoot = Path.Combine(Application.dataPath, "Tests", "Fixtures", "Legacy");
        AppSaveData full = LegacyJsonImporter.FullSave(File.ReadAllText(Path.Combine(fixtureRoot, "legacy_full_v1.json")));
        Assert.That(full.characters[0].characterName, Is.EqualTo("Тестовий герой"));
        full.lastActiveCharacterId = "missing";
        Assert.That(SaveMigrationService.FromLegacy(full).lastActiveCharacterId, Is.EqualTo(full.characters[0].id));
        CharacterData single = LegacyJsonImporter.Character(File.ReadAllText(Path.Combine(fixtureRoot, "legacy_character_v1.json")));
        Assert.That(single.id, Is.EqualTo("fixture-character"));
        InventoryItemExportData item = LegacyJsonImporter.Item(File.ReadAllText(Path.Combine(fixtureRoot, "legacy_item_v1.json")));
        Assert.That(item.itemName, Is.EqualTo("Тестовий артефакт"));
        Assert.Throws<InvalidDataException>(() => LegacyJsonImporter.FullSave("{}"));
    }

    [Test]
    public void LocalRepository_MigratesLegacyAndKeepsItAfterBinarySave()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TaruckA4", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string legacy = Path.Combine(directory, "DndCharactersData.json");
            string binary = Path.Combine(directory, "DndCharactersData.taruck-data");
            File.WriteAllText(legacy, File.ReadAllText(Path.Combine(Application.dataPath, "Tests", "Fixtures", "Legacy", "legacy_full_v1.json")));
            TaruckLocalLoadResult result = TaruckLocalRepository.Load(binary, legacy, "test");
            Assert.That(result.Source, Is.EqualTo(TaruckLocalSource.LegacyPrimary));
            Assert.That(result.Data.characters[0].characterName, Is.EqualTo("Тестовий герой"));
            Assert.That(File.Exists(legacy), Is.True);
            result.Data.characters[0].characterName = "Нове ім'я";
            TaruckLocalRepository.Save(binary, result.Data, "test");
            Assert.That(TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binary)).characters[0].characterName, Is.EqualTo("Нове ім'я"));
            Assert.That(File.Exists(binary + ".bak"), Is.True);
            Assert.That(TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binary + ".bak")).characters[0].characterName, Is.EqualTo("Тестовий герой"));
            Assert.That(File.Exists(legacy), Is.True);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void UnreadableBinary_BlocksLegacyFallbackAndWrite()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TaruckA4", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string legacy = Path.Combine(directory, "DndCharactersData.json");
            string binary = Path.Combine(directory, "DndCharactersData.taruck-data");
            File.WriteAllText(legacy, File.ReadAllText(Path.Combine(Application.dataPath, "Tests", "Fixtures", "Legacy", "legacy_full_v1.json")));
            File.WriteAllBytes(binary, new byte[] { 1, 2, 3 });
            TaruckLocalLoadResult result = TaruckLocalRepository.Load(binary, legacy, "test");
            Assert.That(result.Source, Is.EqualTo(TaruckLocalSource.Unreadable));
            Assert.That(result.Data.characters[0].id, Is.EqualTo("fixture-character"));
            Assert.That(result.CanWrite, Is.False);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(binary));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void DamagedPrimary_RecoversValidBinaryBackupWithoutLosingDamagedBytes()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TaruckA4", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string binary = Path.Combine(directory, "DndCharactersData.taruck-data");
            var save = new AppSaveData();
            save.characters.Add(new CharacterData("one"));
            save.lastActiveCharacterId = "one";
            TaruckLocalRepository.Save(binary, save, "test");
            byte[] damaged = new byte[] { 1, 2, 3 };
            File.Copy(binary, binary + ".bak");
            File.WriteAllBytes(binary, damaged);
            TaruckLocalLoadResult result = TaruckLocalRepository.Load(binary, Path.Combine(directory, "legacy.json"), "test");
            Assert.That(result.Source, Is.EqualTo(TaruckLocalSource.Backup));
            Assert.That(result.Data.characters[0].id, Is.EqualTo("one"));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(binary + ".corrupt"));
            Assert.That(TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binary)).characters[0].id, Is.EqualTo("one"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void FutureVersionPrimary_IsNeverReplacedByOlderBackup()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TaruckA4", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string binary = Path.Combine(directory, "DndCharactersData.taruck-data");
            var save = new AppSaveData();
            byte[] future = TaruckBinaryCodec.EncodeLocal(save, "test");
            File.WriteAllBytes(binary + ".bak", future);
            future[8] = 2;
            File.WriteAllBytes(binary, future);
            TaruckLocalLoadResult result = TaruckLocalRepository.Load(binary, Path.Combine(directory, "legacy.json"), "test");
            Assert.That(result.Source, Is.EqualTo(TaruckLocalSource.Unreadable));
            Assert.That(result.CanWrite, Is.False);
            CollectionAssert.AreEqual(future, File.ReadAllBytes(binary));
        }
        finally { Directory.Delete(directory, true); }
    }
}
