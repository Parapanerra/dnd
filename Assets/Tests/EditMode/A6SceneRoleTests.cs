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

        RestSavedToggleUpdater.Clear(data, "DeathSaves", "0001_karta/0007_deadChekBox");

        Assert.AreEqual(0, data.GetInt(alias));
        Assert.AreEqual(0, data.GetInt(stable));
    }

    [Test]
    public void RestClearsStableIdAfterContainerRename()
    {
        var data = new CharacterSceneData("cartaPersonaj");
        data.SetString("RestRole_test-id", "Rage|2");
        data.SetInt("StableField_v1_test-id", 1);

        RestSavedToggleUpdater.Clear(data, "Rage", "");

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

        RestSavedToggleUpdater.ReduceExhaustionByOne(data, "", 6);

        Assert.AreEqual(1, data.GetInt("StableField_v1_first"));
        Assert.AreEqual(0, data.GetInt("StableField_v1_second"));
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

            Assert.AreSame(panel.transform, SceneRoleLookup.FindPanel(SceneRole.Rage, "Rage", true));

            panel.name = "Renamed frame";
            child.name = "Renamed icon";
            CharacterSceneData data = new CharacterSceneData("test");
            SceneRoleLookup.SaveRestResourcePaths(data);

            Assert.AreSame(panel.transform, SceneRoleLookup.FindPanel(SceneRole.Rage, "Rage", true));
            StringAssert.Contains("Renamed frame", data.GetString("RestResource_Rage"));
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }
}
