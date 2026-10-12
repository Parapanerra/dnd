using NUnit.Framework;
using UnityEngine;

public class A6SceneRoleTests
{
    [Test]
    public void RestClearsLegacyAliasAndStableIdFromOldSave()
    {
        var data = new CharacterSceneData("cartaPersonaj");
        const string alias = "Toggle_0001_karta/0007_deadChekBox/0002_proval/0000_Toggle";
        const string stable = "StableField_v1_dnd.cartapersonaj.toggle.toggle.edb2c58c73cf";
        data.SetInt(alias, 1);
        data.SetInt(stable, 1);

        InvokeToggleUpdater("Clear", data, "DeathSaves", "0001_karta/0007_deadChekBox", int.MinValue, int.MaxValue);

        Assert.AreEqual(0, data.GetInt(alias));
        Assert.AreEqual(0, data.GetInt(stable));
    }

    [Test]
    public void RestClearsStableIdAfterContainerRename()
    {
        var data = new CharacterSceneData("cartaPersonaj");
        data.SetString("RestRole_test-id", "Rage|2");
        data.SetInt("StableField_v1_test-id", 1);

        InvokeToggleUpdater("Clear", data, "Rage", "", int.MinValue, int.MaxValue);

        Assert.AreEqual(0, data.GetInt("StableField_v1_test-id"));
    }

    [Test]
    public void LongRestReducesMappedExhaustionOnce()
    {
        var data = new CharacterSceneData("cartaPersonaj");
        data.SetString("RestRole_first", "Exhaustion|0");
        data.SetString("RestRole_second", "Exhaustion|1");
        data.SetString("RestRole_third", "Exhaustion|2");
        data.SetInt("StableField_v1_first", 1);
        data.SetInt("StableField_v1_second", 1);
        data.SetInt("StableField_v1_third", 0);

        InvokeToggleUpdater("ReduceExhaustionByOne", data, "", 6);

        Assert.AreEqual(1, data.GetInt("StableField_v1_first"));
        Assert.AreEqual(0, data.GetInt("StableField_v1_second"));
    }

    [Test]
    public void ExhaustionPenaltyUsesSelectedLanguageAndOnlyD20Rolls()
    {
        System.Type evaluatorType = System.Type.GetType("CalculatorResultEvaluator, Assembly-CSharp");
        System.Type textCatalogType = System.Type.GetType("CalculatorTextCatalog, Assembly-CSharp");
        System.Type languageType = System.Type.GetType("AppLanguage, Assembly-CSharp");
        Assert.NotNull(evaluatorType);
        Assert.NotNull(textCatalogType);
        Assert.NotNull(languageType);
        object evaluator = System.Activator.CreateInstance(evaluatorType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, new object[] { new System.Func<int>(() => 2) }, null);
        object textCatalog = System.Activator.CreateInstance(textCatalogType, true);
        var isD20Roll = evaluatorType.GetMethod("IsD20Roll",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var penaltyLabel = textCatalogType.GetMethod("PenaltyLabel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsTrue((bool)isD20Roll.Invoke(evaluator, new object[] { "1d20+4" }));
        Assert.IsFalse((bool)isD20Roll.Invoke(evaluator, new object[] { "1d12+4" }));
        Assert.AreEqual(" −4 втома", penaltyLabel.Invoke(textCatalog, new object[] { 4, System.Enum.Parse(languageType, "Ukrainian") }));
        Assert.AreEqual(" −4 exhaustion", penaltyLabel.Invoke(textCatalog, new object[] { 4, System.Enum.Parse(languageType, "English") }));
    }

    [Test]
    public void RestPanelMarkerSurvivesVisualRename()
    {
        GameObject panel = new GameObject("Unrelated panel");
        GameObject child = new GameObject("Former resource label");
        try
        {
            child.transform.SetParent(panel.transform);
            SceneRoleMarker marker = child.AddComponent<SceneRoleMarker>();
            marker.Configure(SceneRole.Rage, true);

            Assert.AreSame(panel.transform, new SceneRoleLookup().FindPanel(SceneRole.Rage, "Rage", true));

            panel.name = "Renamed frame";
            child.name = "Renamed icon";
            CharacterSceneData data = new CharacterSceneData("test");
            System.Type mapperType = System.Type.GetType("RestResourceRoleMapper, Assembly-CSharp");
            Assert.NotNull(mapperType);
            object mapper = System.Activator.CreateInstance(mapperType, true);
            mapperType.GetMethod("Save", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(mapper, new object[] { data });

            Assert.AreSame(panel.transform, new SceneRoleLookup().FindPanel(SceneRole.Rage, "Rage", true));
            StringAssert.Contains("Renamed frame", data.GetString("RestResource_Rage"));
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }
    private static void InvokeToggleUpdater(string methodName, params object[] arguments)
    {
        System.Type type = System.Type.GetType("RestSavedToggleUpdater, Assembly-CSharp");
        Assert.NotNull(type);
        object updater = System.Activator.CreateInstance(type, true);
        System.Reflection.MethodInfo method = type.GetMethod(
            methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(updater, arguments);
    }
}
