using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StableFieldStorageTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void Cleanup()
    {
        foreach (var item in objects) if (item != null) UnityEngine.Object.DestroyImmediate(item);
        objects.Clear();
    }

    private T Field<T>(string id, string collection, int index) where T : Component
    {
        var go = new GameObject(id, typeof(RectTransform));
        objects.Add(go);
        var control = go.AddComponent<T>();
        go.AddComponent<PersistentFieldId>().Configure(id, control, collection, index, "legacy-" + id);
        return control;
    }

    [Test]
    public void ReorderedRenamedInputs_ReadFrozenIndicesThenStableValues()
    {
        var first = Field<InputField>("first", "inputData", 0);
        var second = Field<TMP_InputField>("second", "inputData", 1);
        var data = new CharacterSceneData("test");
        data.inputData.AddRange(new[] { "alpha", "beta" });
        first.transform.SetAsLastSibling();
        first.name = "renamed";
        Assert.That(StableFieldStorage.ReadText(data, second, "wrong-current-index"), Is.EqualTo("beta"));
        Assert.That(StableFieldStorage.ReadText(data, first, "wrong-current-index"), Is.EqualTo("alpha"));
        data.inputData.Clear();
        Assert.That(StableFieldStorage.ReadText(data, first, ""), Is.EqualTo("alpha"));
        Assert.That(StableFieldStorage.ReadText(data, second, ""), Is.EqualTo("beta"));
        Assert.That(data.stableFieldVersion, Is.EqualTo(1));
    }

    [Test]
    public void SaveAfterReorder_KeepsFrozenLegacySlotsAndRoundTripsStableValues()
    {
        var first = Field<InputField>("first", "inputData", 0);
        var second = Field<TMP_InputField>("second", "inputData", 1);
        first.text = "updated alpha";
        second.text = "updated beta";
        var data = new CharacterSceneData("test");
        StableFieldStorage.Save(data, new Component[] { second, first });
        Assert.That(data.inputData, Is.EqualTo(new[] { "updated alpha", "updated beta" }));
        var restored = JsonUtility.FromJson<CharacterSceneData>(JsonUtility.ToJson(data));
        restored.inputData.Clear();
        Assert.That(StableFieldStorage.ReadText(restored, first, ""), Is.EqualTo("updated alpha"));
        Assert.That(StableFieldStorage.ReadText(restored, second, ""), Is.EqualTo("updated beta"));
    }

    [Test]
    public void MissingLegacySlot_DoesNotMarkMigratedOrCopyNeighbour()
    {
        var field = Field<InputField>("missing", "inputData", 3);
        var data = new CharacterSceneData("test");
        data.inputData.Add("neighbour");
        Assert.That(StableFieldStorage.ReadText(data, field, "neighbour"), Is.Empty);
        Assert.That(data.stableFieldVersion, Is.Zero);
        Assert.That(data.stringData, Is.Empty);
        data.inputData.AddRange(new[] { "one", "two", "arrived later" });
        Assert.That(StableFieldStorage.ReadText(data, field, ""), Is.EqualTo("arrived later"));
    }

    [Test]
    public void FloatToggleAndBothDropdownTypes_MigrateAndRoundTrip()
    {
        var slider = Field<Slider>("float", "sliderData", 0);
        var toggle = Field<Toggle>("toggle", "toggleData", 0);
        var dropdown = Field<Dropdown>("dropdown", "dropdownData", 0);
        var tmpDropdown = Field<TMP_Dropdown>("tmpDropdown", "dropdownData", 1);
        var data = new CharacterSceneData("test");
        data.sliderData.Add(0.375f);
        data.toggleData.Add(true);
        data.dropdownData.AddRange(new[] { 2, 3 });
        data.SetInt("legacy-toggle", 0); // Frozen position wins over a stale keyed alias.
        Assert.That(StableFieldStorage.ReadFloat(data, slider, 0), Is.EqualTo(0.375f));
        Assert.That(StableFieldStorage.ReadInt(data, toggle, 0), Is.EqualTo(1));
        Assert.That(StableFieldStorage.ReadInt(data, dropdown, 0), Is.EqualTo(2));
        Assert.That(StableFieldStorage.ReadInt(data, tmpDropdown, 0), Is.EqualTo(3));
        var restored = JsonUtility.FromJson<CharacterSceneData>(JsonUtility.ToJson(data));
        restored.sliderData.Clear();
        restored.toggleData.Clear();
        restored.dropdownData.Clear();
        Assert.That(StableFieldStorage.ReadFloat(restored, slider, 0), Is.EqualTo(0.375f));
        Assert.That(StableFieldStorage.ReadInt(restored, tmpDropdown, 0), Is.EqualTo(3));
        slider.value = 0.625f;
        toggle.isOn = false;
        dropdown.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("A"), new Dropdown.OptionData("B") };
        tmpDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("A"), new TMP_Dropdown.OptionData("B") };
        dropdown.value = 1;
        tmpDropdown.value = 1;
        StableFieldStorage.Save(restored, new Component[] { slider, toggle, dropdown, tmpDropdown });
        Assert.That(StableFieldStorage.ReadFloat(restored, slider, 0), Is.EqualTo(0.625f));
        Assert.That(StableFieldStorage.ReadInt(restored, toggle, 1), Is.Zero);
        Assert.That(StableFieldStorage.ReadInt(restored, dropdown, 0), Is.EqualTo(1));
        Assert.That(StableFieldStorage.ReadInt(restored, tmpDropdown, 0), Is.EqualTo(1));
        restored.ClearValues();
        Assert.That(StableFieldStorage.ReadFloat(restored, slider, 1), Is.Zero);
        Assert.That(StableFieldStorage.ReadInt(restored, dropdown, 1), Is.Zero);
    }

    [Test]
    public void CharactersPagesAndReset_RemainIsolated()
    {
        var field = Field<InputField>("field", "inputData", 0);
        var first = new CharacterData("one");
        var second = new CharacterData("two");
        var a = first.GetSceneData("page");
        var b = second.GetSceneData("page");
        var c = first.GetSceneData("other");
        a.inputData.Add("A");
        b.inputData.Add("B");
        c.inputData.Add("C");
        Assert.That(StableFieldStorage.ReadText(a, field, ""), Is.EqualTo("A"));
        Assert.That(StableFieldStorage.ReadText(b, field, ""), Is.EqualTo("B"));
        Assert.That(StableFieldStorage.ReadText(c, field, ""), Is.EqualTo("C"));
        a.ClearValues();
        Assert.That(StableFieldStorage.ReadText(a, field, ""), Is.Empty);
        Assert.That(StableFieldStorage.ReadText(b, field, ""), Is.EqualTo("B"));
        Assert.That(a.stableFieldVersion, Is.Zero);
    }

    [Test]
    public void PilotScene_HasEveryFrozenBinding()
    {
        CheckScenes("informForPerson");
    }

    [Test]
    public void AllSixScenes_MigrateEveryGoldenValueAfterReorder()
    {
        CheckScenes();
    }

    [Test]
    public void ValidatorRejectsEmptyDuplicateAndMissingIds()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var rows = StableFieldIdTool.ReadMap().Where(r => r["sceneName"] == "informForPerson").ToList();
            var scene = EditorSceneManager.OpenScene(rows[0]["scenePath"], OpenSceneMode.Single);
            var fields = StableFieldIdTool.Fields(scene);
            var field = fields[0];
            string original = field.Id;
            field.Configure("", field.Control, field.LegacyCollection, field.LegacyIndex, field.LegacyKey);
            Assert.Throws<InvalidDataException>(() => StableFieldIdTool.ValidateScene(scene, rows));
            field.Configure(fields[1].Id, field.Control, field.LegacyCollection, field.LegacyIndex, field.LegacyKey);
            Assert.Throws<InvalidDataException>(() => StableFieldIdTool.ValidateScene(scene, rows));
            field.Configure(original, field.Control, field.LegacyCollection, field.LegacyIndex, field.LegacyKey);
            var unbound = new GameObject("new unbound input", typeof(RectTransform), typeof(InputField));
            Assert.Throws<InvalidDataException>(() => StableFieldIdTool.ValidateScene(scene, rows));
            var newField = unbound.AddComponent<PersistentFieldId>();
            newField.Configure("new-field", unbound.GetComponent<InputField>(), "inputData", -1, "");
            Assert.DoesNotThrow(() => StableFieldIdTool.ValidateScene(scene, rows));
            newField.Configure("new-field", unbound.GetComponent<InputField>(), "inputData", 0, "");
            Assert.Throws<InvalidDataException>(() => StableFieldIdTool.ValidateScene(scene, rows));
            UnityEngine.Object.DestroyImmediate(unbound);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    public static void CheckScenes(string onlyScene = null)
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var golden = JsonUtility.FromJson<AppSaveData>(File.ReadAllText(
                Path.Combine(Application.dataPath, "Tests/Fixtures/Legacy/legacy_all_fields_v1.json")));
            foreach (var group in StableFieldIdTool.ReadMap().GroupBy(r => r["scenePath"]))
            {
                if (onlyScene != null && group.First()["sceneName"] != onlyScene) continue;
                var scene = EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single);
                StableFieldIdTool.ValidateScene(scene, group.ToList());
                var data = golden.characters[0].GetSceneData(scene.name, false);
                var fields = StableFieldIdTool.Fields(scene);
                // Reverse siblings and rename in memory, then read without using current paths/indices.
                foreach (var field in fields)
                {
                    field.transform.SetAsFirstSibling();
                    field.gameObject.name = "reordered";
                    int index = field.LegacyIndex;
                    switch (field.LegacyCollection)
                    {
                        case "inputData":
                            Assert.That(StableFieldStorage.ReadText(data, field.Control, "wrong"),
                                Is.EqualTo(scene.name + ":input:" + index), field.Id);
                            break;
                        case "toggleData":
                            Assert.That(StableFieldStorage.ReadInt(data, field.Control, -1),
                                Is.EqualTo(index % 2), field.Id);
                            break;
                        case "dropdownData":
                            Assert.That(StableFieldStorage.ReadInt(data, field.Control, -1),
                                Is.EqualTo(index + 1), field.Id);
                            break;
                    }
                }
                var restored = JsonUtility.FromJson<CharacterSceneData>(JsonUtility.ToJson(data));
                restored.inputData.Clear();
                restored.toggleData.Clear();
                restored.dropdownData.Clear();
                foreach (var field in fields)
                    if (field.LegacyCollection == "inputData")
                        Assert.That(StableFieldStorage.ReadText(restored, field.Control, "wrong"),
                            Is.EqualTo(scene.name + ":input:" + field.LegacyIndex), field.Id);
                    else if (field.LegacyCollection == "toggleData" || field.LegacyCollection == "dropdownData")
                        Assert.That(StableFieldStorage.ReadInt(restored, field.Control, -1),
                            Is.EqualTo(field.LegacyCollection == "toggleData" ? field.LegacyIndex % 2 : field.LegacyIndex + 1), field.Id);
            }
        }
        finally
        {
            // Discard only these temporary scene mutations; never save them.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (setup.Any(s => s.isLoaded && s.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
