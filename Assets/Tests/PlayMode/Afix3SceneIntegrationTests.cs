using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Afix3SceneIntegrationTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
    private static readonly (string Name, SceneRole[] Required)[] Scenes =
    {
        ("cartaPersonaj", new[] { SceneRole.MovementSpeed, SceneRole.DeathSaves, SceneRole.TemporaryHealth,
            SceneRole.Exhaustion, SceneRole.Overlay, SceneRole.ResetScene }),
        ("Spels", new[] { SceneRole.SpellSlots, SceneRole.Overlay, SceneRole.ResetScene }),
        ("spelBook", new[] { SceneRole.MainPage, SceneRole.Overlay, SceneRole.ResetScene }),
        ("inventory", new[] { SceneRole.MainPage, SceneRole.Overlay, SceneRole.ResetScene }),
        ("informForPerson", new[] { SceneRole.PortraitButton, SceneRole.PortraitImage, SceneRole.Overlay, SceneRole.ResetScene }),
        ("petsesn", new[] { SceneRole.DeathSaves, SceneRole.MovementSpeed,
            SceneRole.WildShapeOverlay, SceneRole.ResetScene })
    };

    [UnityTest]
    public IEnumerator AllCharacterScenesLoadWithComponentsRolesAndUniqueIds()
    {
        foreach (var entry in Scenes)
        {
            yield return SceneManager.LoadSceneAsync(entry.Name, LoadSceneMode.Single);
            yield return null;
            Scene scene = SceneManager.GetActiveScene();
            Assert.AreEqual(entry.Name, scene.name);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach (Transform transform in transforms)
                Assert.IsFalse(transform.GetComponents<Component>().Any(component => component == null),
                    "Missing script: " + entry.Name + "/" + transform.name);

            var roles = transforms.SelectMany(t => t.GetComponents<SceneRoleMarker>()).ToArray();
            foreach (SceneRole role in entry.Required)
                Assert.AreEqual(1, roles.Count(marker => marker.Role == role), entry.Name + ": " + role);

            var ids = transforms.SelectMany(t => t.GetComponents<PersistentFieldId>()).ToArray();
            Assert.Greater(ids.Length, 0, entry.Name + ": stable IDs missing");
            Assert.IsFalse(ids.Any(id => string.IsNullOrEmpty(id.Id) || id.Control == null),
                entry.Name + ": incomplete stable ID");
            Assert.AreEqual(ids.Length, ids.Select(id => id.Id).Distinct(StringComparer.Ordinal).Count(),
                entry.Name + ": duplicate stable ID");
        }
    }

    [UnityTest]
    public IEnumerator TwoCharactersRemainIsolatedAcrossScenesSaveReloadAndPageReset()
    {
        yield return SceneManager.LoadSceneAsync("cartaPersonaj", LoadSceneMode.Single);
        yield return null;
        Type managerType = Type.GetType("DndSaveManager, Assembly-CSharp", true);
        Component manager = (Component)UnityEngine.Object.FindObjectsByType(managerType, FindObjectsInactive.Include,
            FindObjectsSortMode.None).First(component => (bool)managerType.GetProperty("IsInitialized", Hidden).GetValue(component));
        var collection = new AppSaveData();
        managerType.GetField("saveData", Hidden).SetValue(manager, collection);
        managerType.GetField("characters", Hidden).SetValue(manager, null);
        Invoke(managerType, manager, "SaveData");

        CharacterData first = (CharacterData)Invoke(managerType, manager, "CreateNewCharacter");
        first.characterName = "First";
        first.maxHealth = 32;
        first.currentHealth = 17;
        first.SetSharedString("Portrait", "first-portrait");
        CharacterSceneData firstCard = first.GetSceneData("cartaPersonaj");
        firstCard.SetInt("HealthBar_test_maxHealth", 32);
        firstCard.SetInt("HealthBar_test_currentHealth", 17);
        firstCard.SetInt("HealthBar_test_maxTemporaryHealth", 5);
        firstCard.SetInt("HealthBar_test_currentTemporaryHealth", 5);
        firstCard.SetString("RestRole_death", "DeathSaves|1");
        firstCard.SetInt("StableField_v1_death", 1);
        firstCard.SetString("RestRole_exhaustion1", "Exhaustion|0");
        firstCard.SetString("RestRole_exhaustion2", "Exhaustion|1");
        firstCard.SetInt("StableField_v1_exhaustion1", 1);
        firstCard.SetInt("StableField_v1_exhaustion2", 1);
        first.GetSceneData("Spels").SetString("spell-name", "Magic Missile");
        first.GetSceneData("petsesn").SetString("wild-shape", "Wolf");

        managerType.GetField("lastCreateCharacterTime", Hidden).SetValue(manager, -1000f);
        CharacterData second = (CharacterData)Invoke(managerType, manager, "CreateNewCharacter");
        Assert.AreNotEqual(first.id, second.id);
        second.characterName = "Second";
        second.currentHealth = 9;
        second.GetSceneData("inventory").SetString("other-page-note", "Shield");
        Invoke(managerType, manager, "SaveData");
        Assert.IsTrue((bool)Invoke(managerType, manager, "SetActiveCharacter", first.id));
        yield return SceneManager.LoadSceneAsync("inventory", LoadSceneMode.Single);
        yield return null;
        Type cellType = Type.GetType("InventoryItemCell, Assembly-CSharp", true);
        Component cell = (Component)UnityEngine.Object.FindObjectsByType(cellType,
            FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        string cellKey = (string)cellType.GetProperty("CellKey", Hidden).GetValue(cell);
        var item = new InventoryItemExportData { itemName = "Potion", itemDescription = "Healing", customImageBase64 = Png };
        Invoke(cellType, cell, "ApplyData", item, true);
        Invoke(managerType, manager, "SaveData");
        Assert.AreEqual("Potion", first.GetSceneData("inventory").GetString(cellKey + "_Name"));
        yield return SceneManager.LoadSceneAsync("Spels", LoadSceneMode.Single);
        yield return null;
        Assert.IsTrue((bool)Invoke(managerType, manager, "SetActiveCharacter", second.id));
        yield return SceneManager.LoadSceneAsync("inventory", LoadSceneMode.Single);
        yield return null;
        cell = FindCell(cellType, cellKey);
        Assert.AreNotEqual("Potion", ((InventoryItemExportData)Invoke(cellType, cell, "ReadCurrentData")).itemName);
        yield return SceneManager.LoadSceneAsync("Spels", LoadSceneMode.Single);
        yield return null;
        Assert.IsTrue((bool)Invoke(managerType, manager, "SetActiveCharacter", first.id));
        yield return SceneManager.LoadSceneAsync("inventory", LoadSceneMode.Single);
        yield return null;
        cell = FindCell(cellType, cellKey);
        InventoryItemExportData displayed = (InventoryItemExportData)Invoke(cellType, cell, "ReadCurrentData");
        Assert.AreEqual("Potion", displayed.itemName);
        Assert.IsFalse(string.IsNullOrEmpty(displayed.customImageBase64));
        InventoryItemValidator.ValidateImage(Convert.FromBase64String(displayed.customImageBase64));
        Assert.IsTrue((bool)Invoke(managerType, manager, "SetActiveCharacter", second.id));
        yield return SceneManager.LoadSceneAsync("Spels", LoadSceneMode.Single);
        Assert.AreEqual(second.id, Invoke(managerType, manager, "GetActiveCharacterId"));

        string path = Path.Combine(Application.persistentDataPath, "DndCharactersData.taruck-data");
        Assert.IsTrue(File.Exists(path), "Save missing: " + path);
        AppSaveData restored = TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(path));
        Assert.AreEqual(2, restored.characters.Count);
        CharacterData restoredFirst = restored.characters.Single(c => c.id == first.id);
        CharacterData restoredSecond = restored.characters.Single(c => c.id == second.id);
        Assert.AreEqual(17, restoredFirst.currentHealth);
        Assert.AreEqual(9, restoredSecond.currentHealth);
        Assert.AreEqual(5, restoredFirst.GetSceneData("cartaPersonaj").GetInt("HealthBar_test_currentTemporaryHealth"));
        Assert.AreEqual(1, restoredFirst.GetSceneData("cartaPersonaj").GetInt("StableField_v1_death"));
        Assert.AreEqual(1, restoredFirst.GetSceneData("cartaPersonaj").GetInt("StableField_v1_exhaustion1"));
        Assert.AreEqual(1, restoredFirst.GetSceneData("cartaPersonaj").GetInt("StableField_v1_exhaustion2"));
        Assert.AreEqual("Magic Missile", restoredFirst.GetSceneData("Spels").GetString("spell-name"));
        Assert.AreEqual("Wolf", restoredFirst.GetSceneData("petsesn").GetString("wild-shape"));
        Assert.AreEqual("Potion", restoredFirst.GetSceneData("inventory").GetString(cellKey + "_Name"));
        InventoryItemValidator.ValidateImage(Convert.FromBase64String(
            restoredFirst.GetSceneData("inventory").GetString(cellKey + "_CustomImage")));

        Invoke(managerType, manager, "ClearSceneDataFamilyForCharacter", first.id, "cartaPersonaj");
        Invoke(managerType, manager, "SaveData");
        restored = TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(path));
        restoredFirst = restored.characters.Single(c => c.id == first.id);
        restoredSecond = restored.characters.Single(c => c.id == second.id);
        Assert.AreEqual(0, restoredFirst.GetSceneData("cartaPersonaj").GetInt("HealthBar_test_currentHealth"));
        Assert.AreEqual("first-portrait", restoredFirst.GetSharedString("Portrait"));
        Assert.AreEqual("Magic Missile", restoredFirst.GetSceneData("Spels").GetString("spell-name"));
        Assert.AreEqual("Shield", restoredSecond.GetSceneData("inventory").GetString("other-page-note"));

        Type singleImportType = Type.GetType("CharacterSingleImportService, Assembly-CSharp", true);
        object singleImport = Activator.CreateInstance(singleImportType, true);
        string importedId = (string)Invoke(singleImportType, singleImport, "Import", manager, restoredFirst, "test");
        Assert.AreNotEqual(first.id, importedId);
        AppSaveData afterImport = TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(path));
        Assert.AreEqual(3, afterImport.characters.Count);
        Assert.AreEqual("First (2)", afterImport.characters.Single(c => c.id == importedId).characterName);
        Assert.AreEqual("Shield", afterImport.characters.Single(c => c.id == second.id)
            .GetSceneData("inventory").GetString("other-page-note"));
    }

    [Test]
    public void LongRestAndCollectionImportsPreserveCurrentDataOnWriteFailure()
    {
        var current = new AppSaveData();
        var hero = new CharacterData("hero") { characterName = "Hero", maxHealth = 30, currentHealth = 12 };
        current.characters.Add(hero);
        current.lastActiveCharacterId = hero.id;
        CharacterSceneData card = hero.GetSceneData("cartaPersonaj");
        card.SetInt("HealthBar_test_maxHealth", 30);
        card.SetInt("HealthBar_test_currentHealth", 12);
        card.SetInt("HealthBar_test_currentTemporaryHealth", 4);
        card.SetString("RestRole_death", "DeathSaves|1");
        card.SetInt("StableField_v1_death", 1);
        card.SetString("RestRole_exhaustion1", "Exhaustion|0");
        card.SetString("RestRole_exhaustion2", "Exhaustion|1");
        card.SetInt("StableField_v1_exhaustion1", 1);
        card.SetInt("StableField_v1_exhaustion2", 1);
        CharacterSceneData spells = hero.GetSceneData("Spels");
        spells.SetString("RestRole_slot", "SpellSlots|1");
        spells.SetInt("StableField_v1_slot", 1);
        CharacterSceneData wild = hero.GetSceneData("petsesn");
        wild.SetString("RestRole_wild", "WildShape|1");
        wild.SetInt("StableField_v1_wild", 1);
        Type restType = Type.GetType("CharacterRestSavedStateService, Assembly-CSharp", true);
        object rest = Activator.CreateInstance(restType, Hidden, null, new object[] { null }, null);
        Invoke(restType, rest, "ApplyToCharacter", hero, true, null);
        Assert.AreEqual(30, card.GetInt("HealthBar_test_currentHealth"));
        Assert.AreEqual(0, card.GetInt("HealthBar_test_currentTemporaryHealth"));
        Assert.AreEqual(0, card.GetInt("StableField_v1_death"));
        Assert.AreEqual(0, card.GetInt("StableField_v1_exhaustion2"));
        Assert.AreEqual(0, spells.GetInt("StableField_v1_slot"));
        Assert.AreEqual(0, wild.GetInt("StableField_v1_wild"));

        string folder = Path.Combine(Path.GetTempPath(), "Taruck-Afix3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "collection.taruck-data");
            var repository = new TaruckLocalRepository();
            repository.Save(file, current, "test");
            var incoming = new AppSaveData();
            incoming.characters.Add(new CharacterData("guest") { characterName = "Guest" });
            incoming.lastActiveCharacterId = "guest";
            Type importerType = Type.GetType("CharacterCollectionImportService, Assembly-CSharp", true);
            object importer = Activator.CreateInstance(importerType, Hidden, null,
                new object[] { repository, file, "test" }, null);
            AppSaveData merged = (AppSaveData)Invoke(importerType, importer, "Import", current, incoming, true);
            Assert.AreEqual(2, merged.characters.Count);
            Assert.AreEqual(1, current.characters.Count);
            Assert.AreEqual(2, TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(file)).characters.Count);
            Assert.AreEqual(1, TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(file + ".preimport.bak")).characters.Count);
            AppSaveData replaced = (AppSaveData)Invoke(importerType, importer, "Import", merged, incoming, false);
            Assert.AreEqual(1, replaced.characters.Count);
            Assert.AreEqual("guest", replaced.characters[0].id);
            Assert.AreEqual(2, merged.characters.Count);

            object unwritable = Activator.CreateInstance(importerType, Hidden, null,
                new object[] { repository, folder, "test" }, null);
            Assert.Throws<TargetInvocationException>(() => Invoke(importerType, unwritable, "Import", current, incoming, true));
            Assert.AreEqual(1, current.characters.Count);
            Assert.AreEqual("hero", current.characters[0].id);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static object Invoke(Type type, object target, string method, params object[] args) =>
        type.GetMethod(method, Hidden).Invoke(target, args);

    private static Component FindCell(Type cellType, string key) =>
        (Component)UnityEngine.Object.FindObjectsByType(cellType, FindObjectsInactive.Include,
            FindObjectsSortMode.None).First(cell => (string)cellType.GetProperty("CellKey", Hidden).GetValue(cell) == key);
}
