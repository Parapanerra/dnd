using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class LegacySaveFormatTests
{
    private string tempDirectory;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "TaruckShipTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, true);
    }

    [Test]
    public void CurrentJsonFixture_LoadsCharacterAndSceneValues()
    {
        AppSaveData data = LegacyJsonSaveRepository.Deserialize(
            File.ReadAllText(FixturePath("legacy_full_v1.json")));

        Assert.That(data.lastActiveCharacterId, Is.EqualTo("fixture-character"));
        Assert.That(data.characters, Has.Count.EqualTo(1));

        CharacterData character = data.characters[0];
        Assert.That(character.characterName, Is.EqualTo("Тестовий герой"));
        Assert.That(character.currentHealth, Is.EqualTo(17));
        Assert.That(character.sceneStates, Has.Count.EqualTo(2));
        Assert.That(character.sceneStates[0].inputData, Is.EqualTo(new[] { "14", "Клерик" }));
        Assert.That(character.sceneStates[1].GetString("Inventory_Page_0_Cell_0_Name"), Is.EqualTo("Зілля"));
    }

    [Test]
    public void CorruptedPrimary_LoadsValidBackup()
    {
        string primaryPath = Path.Combine(tempDirectory, "DndCharactersData.json");
        string backupPath = primaryPath + ".bak";
        File.Copy(FixturePath("legacy_corrupted.json"), primaryPath);
        File.Copy(FixturePath("legacy_full_v1.json"), backupPath);

        LegacyJsonLoadResult result = LegacyJsonSaveRepository.Load(primaryPath, backupPath);

        Assert.That(result.Source, Is.EqualTo(LegacyJsonSaveSource.Backup));
        Assert.That(result.PrimaryError, Is.Not.Null);
        Assert.That(result.BackupError, Is.Null);
        Assert.That(result.Data.lastActiveCharacterId, Is.EqualTo("fixture-character"));
    }

    [Test]
    public void CorruptedPrimaryAndBackup_ReturnErrorsWithoutData()
    {
        string primaryPath = Path.Combine(tempDirectory, "DndCharactersData.json");
        string backupPath = primaryPath + ".bak";
        File.Copy(FixturePath("legacy_corrupted.json"), primaryPath);
        File.Copy(FixturePath("legacy_corrupted.json"), backupPath);

        LegacyJsonLoadResult result = LegacyJsonSaveRepository.Load(primaryPath, backupPath);

        Assert.That(result.Source, Is.EqualTo(LegacyJsonSaveSource.None));
        Assert.That(result.Data, Is.Null);
        Assert.That(result.PrimaryError, Is.Not.Null);
        Assert.That(result.BackupError, Is.Not.Null);
    }

    [Test]
    public void SaveTwice_WritesNewPrimaryAndKeepsPreviousPrimaryAsBackup()
    {
        string primaryPath = Path.Combine(tempDirectory, "DndCharactersData.json");
        string backupPath = primaryPath + ".bak";
        AppSaveData first = BuildSave("first", "Перший");
        AppSaveData second = BuildSave("second", "Другий");

        LegacyJsonSaveRepository.Save(primaryPath, backupPath, first);
        LegacyJsonSaveRepository.Save(primaryPath, backupPath, second);

        LegacyJsonLoadResult current = LegacyJsonSaveRepository.Load(primaryPath, backupPath);
        AppSaveData backup = LegacyJsonSaveRepository.Deserialize(File.ReadAllText(backupPath));
        Assert.That(current.Source, Is.EqualTo(LegacyJsonSaveSource.Primary));
        Assert.That(current.Data.lastActiveCharacterId, Is.EqualTo("second"));
        Assert.That(backup.lastActiveCharacterId, Is.EqualTo("first"));
    }

    [Test]
    public void NullLegacyCollections_AreNormalizedWithoutLosingCharacter()
    {
        AppSaveData data = LegacyJsonSaveRepository.Deserialize(
            File.ReadAllText(FixturePath("legacy_null_collections.json")));

        data = SaveDataNormalizer.Normalize(data);

        Assert.That(data.characters, Has.Count.EqualTo(1));
        CharacterData character = data.characters[0];
        Assert.That(character.id, Is.Not.Empty);
        Assert.That(character.characterName, Is.EqualTo("Новий персонаж"));
        Assert.That(character.inputData, Is.Not.Null);
        Assert.That(character.toggleData, Is.Not.Null);
        Assert.That(character.sliderData, Is.Not.Null);
        Assert.That(character.dropdownData, Is.Not.Null);
        Assert.That(character.sharedStringData, Is.Not.Null);
        Assert.That(character.sceneStates, Has.Count.EqualTo(1));
        Assert.That(character.sceneStates[0].stringData, Is.Not.Null);
        Assert.That(character.sceneStates[0].intData, Is.Not.Null);
    }

    [Test]
    public void CharacterExportV1_LoadsLegacyEnvelope()
    {
        CharacterExportData export = JsonUtility.FromJson<CharacterExportData>(
            File.ReadAllText(FixturePath("legacy_character_v1.json")));

        Assert.That(export.version, Is.EqualTo(1));
        Assert.That(export.character.id, Is.EqualTo("fixture-character"));
        Assert.That(export.character.characterName, Is.EqualTo("Тестовий герой"));
    }

    [Test]
    public void ItemExport_LoadsAllLegacyFieldsAndEmbeddedImage()
    {
        InventoryItemExportData item = JsonUtility.FromJson<InventoryItemExportData>(
            File.ReadAllText(FixturePath("legacy_item_v1.json")));

        Assert.That(item.itemName, Is.EqualTo("Тестовий артефакт"));
        Assert.That(item.itemDescription, Is.EqualTo("Санітизований golden-файл"));
        Assert.That(item.category, Is.EqualTo(3));
        Assert.That(item.magicIndex, Is.EqualTo(4));
        byte[] image = Convert.FromBase64String(item.customImageBase64);
        Assert.That(image.Length, Is.GreaterThan(8));
        Assert.That(image[0], Is.EqualTo(0x89));
        Assert.That(image[1], Is.EqualTo(0x50));
        Assert.That(image[2], Is.EqualTo(0x4e));
        Assert.That(image[3], Is.EqualTo(0x47));
    }

    private static AppSaveData BuildSave(string id, string name)
    {
        CharacterData character = new CharacterData(id) { characterName = name };
        return new AppSaveData
        {
            lastActiveCharacterId = id,
            characters = new List<CharacterData> { character }
        };
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(Application.dataPath, "Tests", "Fixtures", "Legacy", fileName);
    }
}
