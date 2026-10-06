using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class SceneSaveControllerTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    private T Create<T>(string id, string collection, int index) where T : Component
    {
        var go = new GameObject(id, typeof(RectTransform));
        objects.Add(go);
        var control = go.AddComponent<T>();
        go.AddComponent<PersistentFieldId>().Configure(id, control, collection, index, "");
        return control;
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
    }

    [Test]
    public void SaveLoadResetAndEvents_PreserveAllSixControlTypes()
    {
        var controller = new SceneSaveController();
        var input = Create<InputField>("input", "inputData", 0);
        var tmp = Create<TMP_InputField>("tmp", "inputData", 1);
        var toggle = Create<Toggle>("toggle", "toggleData", 0);
        var slider = Create<Slider>("slider", "sliderData", 0);
        var dropdown = Create<Dropdown>("dropdown", "dropdownData", 0);
        var tmpDropdown = Create<TMP_Dropdown>("tmpDropdown", "dropdownData", 1);
        dropdown.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("A"), new Dropdown.OptionData("B") };
        tmpDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("A"), new TMP_Dropdown.OptionData("B") };
        controller.InputFields.Add(input);
        controller.TmpInputFields.Add(tmp);
        controller.Toggles.Add(toggle);
        controller.Sliders.Add(slider);
        controller.Dropdowns.Add(dropdown);
        controller.TmpDropdowns.Add(tmpDropdown);

        input.text = "alpha";
        tmp.text = "beta";
        toggle.isOn = true;
        slider.value = 0.375f;
        dropdown.value = 1;
        tmpDropdown.value = 1;
        var data = new CharacterSceneData("page");
        int hookCalls = 0;
        controller.Save(data, () =>
        {
            Assert.That(data.inputData, Is.EqualTo(new[] { "alpha", "beta" }));
            hookCalls++;
        });
        Assert.That(hookCalls, Is.EqualTo(1));

        int notifications = 0;
        int flushes = 0;
        controller.Subscribe(() => notifications++, () => flushes++);
        controller.Reset();
        Assert.That(input.text, Is.Empty);
        Assert.That(tmp.text, Is.Empty);
        Assert.That(toggle.isOn, Is.False);
        Assert.That(slider.value, Is.Zero);
        Assert.That(dropdown.value, Is.Zero);
        Assert.That(tmpDropdown.value, Is.Zero);
        Assert.That(notifications, Is.Zero);
        controller.Load(JsonUtility.FromJson<CharacterSceneData>(JsonUtility.ToJson(data)));
        Assert.That(input.text, Is.EqualTo("alpha"));
        Assert.That(tmp.text, Is.EqualTo("beta"));
        Assert.That(toggle.isOn, Is.True);
        Assert.That(slider.value, Is.EqualTo(0.375f));
        Assert.That(dropdown.value, Is.EqualTo(1));
        Assert.That(tmpDropdown.value, Is.EqualTo(1));
        Assert.That(notifications, Is.Zero, "Loading must not trigger autosave");
        input.onValueChanged.Invoke("edit");
        input.onEndEdit.Invoke("edit");
        tmp.onValueChanged.Invoke("edit");
        tmp.onEndEdit.Invoke("edit");
        Assert.That(notifications, Is.EqualTo(2), "Ending text edit must not queue a second save");
        Assert.That(flushes, Is.EqualTo(2), "Ending text edit must flush pending changes");
        toggle.onValueChanged.Invoke(false);
        slider.onValueChanged.Invoke(0.5f);
        dropdown.onValueChanged.Invoke(0);
        tmpDropdown.onValueChanged.Invoke(0);
        Assert.That(notifications, Is.EqualTo(6));
    }

    [Test]
    public void ReusedController_KeepsPageDataSeparateAfterReorder()
    {
        var controller = new SceneSaveController();
        var a = Create<InputField>("a", "inputData", 0);
        var b = Create<InputField>("b", "inputData", 1);
        controller.InputFields.AddRange(new[] { b, a });
        var first = new CharacterSceneData("first");
        first.inputData.AddRange(new[] { "first A", "first B" });
        var second = new CharacterSceneData("second");
        second.inputData.AddRange(new[] { "second A", "second B" });
        controller.Load(first);
        Assert.That(a.text, Is.EqualTo("first A"));
        a.text = "edited A";
        controller.Save(first, () => { });
        controller.Load(second);
        Assert.That(a.text, Is.EqualTo("second A"));
        Assert.That(b.text, Is.EqualTo("second B"));
        controller.Load(first);
        Assert.That(a.text, Is.EqualTo("edited A"));
        Assert.That(b.text, Is.EqualTo("first B"));
        Assert.That(first.inputData, Is.EqualTo(new[] { "edited A", "first B" }));
    }

    [Test]
    public void CollectionInAllSixScenes_MatchesFrozenMapAndLegacyFiltering()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var group in StableFieldIdTool.ReadMap().GroupBy(r => r["scenePath"]))
            {
                EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single);
                var controller = new SceneSaveController();
                bool excludeTemplates = group.First()["controller"] != "CharacterSheetManagerScene1";
                controller.Collect(t => t != null && t.GetComponentsInParent<MonoBehaviour>(true)
                    .Any(c => c != null && (c.GetType().Name == "HealthBar" || c.GetType().Name == "HealthBar1")),
                    excludeTemplates);
                var actual = new List<Component>();
                actual.AddRange(controller.InputFields);
                actual.AddRange(controller.TmpInputFields);
                actual.AddRange(controller.Toggles);
                actual.AddRange(controller.Sliders);
                actual.AddRange(controller.Dropdowns);
                actual.AddRange(controller.TmpDropdowns);
                Assert.That(actual.Count, Is.EqualTo(group.Count()), group.Key);
                CollectionAssert.AreEquivalent(group.Select(r => r["proposedStableId"]),
                    actual.Select(c => PersistentFieldId.For(c)?.Id), group.Key);
                foreach (var collection in group.GroupBy(r => r["legacyCollection"]))
                {
                    var fields = actual.Select(PersistentFieldId.For)
                        .Where(f => f.LegacyCollection == collection.Key).ToList();
                    CollectionAssert.AreEqual(Enumerable.Range(0, fields.Count),
                        fields.Select(f => f.LegacyIndex), group.Key + "/" + collection.Key);
                }
            }
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
