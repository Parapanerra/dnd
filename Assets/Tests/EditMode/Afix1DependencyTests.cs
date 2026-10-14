using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class Afix1DependencyTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;

    [SetUp] public void SetUp() => root = new GameObject("Dependency test");
    [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

    private GameObject Child(string name, params Type[] types)
    {
        GameObject child = new GameObject(name, types);
        child.transform.SetParent(root.transform);
        return child;
    }
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Service(string name, params object[] args) =>
        Activator.CreateInstance(RuntimeType(name), Hidden | BindingFlags.Public, null, args, null);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Hidden).Invoke(target, args);

    [Test]
    public void HitDiceUseRolesAfterRenameInsteadOfNamedDecoys()
    {
        InputField count = Child("renamed count", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>();
        count.text = "4";
        count.gameObject.AddComponent<SceneRoleMarker>().Configure(SceneRole.HitDiceCount, false);
        InputField sides = Child("renamed sides", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>();
        sides.text = "d8";
        sides.gameObject.AddComponent<SceneRoleMarker>().Configure(SceneRole.HitDiceSides, false);
        Child("alldaise", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>().text = "99";
        object service = Service("CharacterRestService", new object[] { null });
        object[] values = { 0, 0 };
        Assert.IsTrue((bool)Call(service, "TryGetHitDice", values));
        Assert.AreEqual(4, values[0]);
        Assert.AreEqual(8, values[1]);
    }

    [Test]
    public void HitDiceStillReadUnmigratedLegacyFields()
    {
        Child("alldaise", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>().text = "3";
        Child("daicevalueperson", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>().text = "d10";
        object service = Service("CharacterRestService", new object[] { null });
        object[] values = { 0, 0 };
        Assert.IsTrue((bool)Call(service, "TryGetHitDice", values));
        Assert.AreEqual(3, values[0]);
        Assert.AreEqual(10, values[1]);
    }

    [Test]
    public void HealthFillKeepsExplicitReferenceAcrossRepeatedRenders()
    {
        Slider slider = Child("health", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        Image decoy = Child("fild", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        decoy.transform.SetParent(slider.transform);
        Image fill = Child("renamed fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(slider.transform);
        slider.fillRect = fill.rectTransform;
        object view = Service("HealthBarView", null, slider, null, null, null);
        Call(view, "Render", 7, 10, 0, 0);
        Call(view, "Render", 3, 10, 0, 0);
        Assert.AreEqual(0.3f, fill.fillAmount, 0.0001f);
        Assert.AreEqual(1f, decoy.fillAmount, 0.0001f);
    }

    [Test]
    public void RenamedResourceToggleRetainsItsRestNumber()
    {
        Toggle toggle = Child("renamed resource", typeof(RectTransform), typeof(Toggle)).GetComponent<Toggle>();
        Component marker = toggle.gameObject.AddComponent(RuntimeType("ResourceToggleMarker"));
        marker.GetType().GetMethod("Configure").Invoke(marker, new object[] { 3 });
        object finder = Service("RestSceneToggleFinder");
        var included = (System.Collections.IList)Call(finder, "Find", root.transform, 3, 3);
        var excluded = (System.Collections.IList)Call(finder, "Find", root.transform, 0, 2);
        CollectionAssert.Contains(included, toggle);
        Assert.AreEqual(0, excluded.Count);
    }

    [Test]
    public void RenamedPageButtonUsesSerializedPageIndex()
    {
        Button button = Child("renamed page", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        Component marker = button.gameObject.AddComponent(RuntimeType("UiControlMarker"));
        marker.GetType().GetMethod("Configure").Invoke(marker, new[] {
            Enum.Parse(RuntimeType("UiControlRole"), "PageButton"), (object)4 });
        int selected = -1;
        object binder = Service("ScenePageButtonBinder", new Action<int>(value => selected = value));
        Call(binder, "Bind");
        button.onClick.Invoke();
        Assert.AreEqual(4, selected);
    }

    [Test]
    public void InventoryControlRoleWinsOverLegacyNamedDecoy()
    {
        Component cell = root.AddComponent(RuntimeType("InventoryItemCell"));
        Transform actual = Child("renamed item name", typeof(RectTransform), typeof(InputField)).transform;
        Child("itemNameInput", typeof(RectTransform), typeof(InputField));
        Component marker = actual.gameObject.AddComponent(RuntimeType("UiControlMarker"));
        object role = Enum.Parse(RuntimeType("UiControlRole"), "ItemName");
        marker.GetType().GetMethod("Configure").Invoke(marker, new[] { role, (object)0 });
        cell.GetType().GetField("controls", Hidden).SetValue(cell, root.GetComponentsInChildren<Transform>(true));
        Assert.AreSame(actual, Call(cell, "FindChild", role, "itemNameInput"));
    }

    [Test]
    public void DuplicateSceneRoleFailsExplicitly()
    {
        Child("first").AddComponent<SceneRoleMarker>().Configure(SceneRole.HitDiceSides, false);
        Child("second").AddComponent<SceneRoleMarker>().Configure(SceneRole.HitDiceSides, false);
        Assert.Throws<InvalidOperationException>(() => new SceneRoleLookup().Find(SceneRole.HitDiceSides));
    }

    [Test]
    public void RoleLookupIgnoresAnotherLoadedScene()
    {
        UnityEngine.Object.DestroyImmediate(root);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/personag/informForPerson.unity");
        root = new GameObject("Dependency test");
        var other = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
        GameObject foreign = new GameObject("foreign");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(foreign, other);
        foreign.AddComponent<SceneRoleMarker>().Configure(SceneRole.HitDiceSides, false);
        try
        {
            Assert.IsNull(new SceneRoleLookup(root.scene).Find(SceneRole.HitDiceSides));
            Assert.AreSame(foreign.GetComponent<SceneRoleMarker>(), new SceneRoleLookup(other).Find(SceneRole.HitDiceSides));
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(other, true);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        }
    }
}
