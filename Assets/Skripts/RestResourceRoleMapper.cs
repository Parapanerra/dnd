using System;
using UnityEngine;

// Saves scene role paths and stable toggle roles; scene lookup stays in SceneRoleLookup.
internal sealed class RestResourceRoleMapper
{
    private const string RestResourceKeyPrefix = "RestResource_";
    private readonly SceneRoleLookup roleLookup = new SceneRoleLookup();
    private readonly RestSceneToggleFinder toggles = new RestSceneToggleFinder();

    internal void Save(CharacterSceneData sceneData)
    {
        if (sceneData == null)
            return;

        if (sceneData.stringData != null)
            sceneData.stringData.RemoveAll(entry => entry != null && entry.key != null && entry.key.StartsWith("RestRole_", StringComparison.Ordinal));

        SavePanel(sceneData, SceneRole.Rage, "Rage", true);
        SavePanel(sceneData, SceneRole.WildShape, "WildShape", true);
        SavePanel(sceneData, SceneRole.ChannelDivinity, "ChannelDivinity", true);
        SavePanel(sceneData, SceneRole.KiPoints, "KiPoints", true);
        SavePanel(sceneData, SceneRole.SorceryPoints, "SorceryPoints", true);
        SavePanel(sceneData, SceneRole.BloodCurse, "BloodCurse", true);
        SavePanel(sceneData, SceneRole.DragonBreath, "DragonBreath", true);
        SavePanel(sceneData, SceneRole.Flight, "Flight", true);
        SavePanel(sceneData, SceneRole.SpellSlots, "spelChek");
        SavePanel(sceneData, SceneRole.Exhaustion, "vtoma");
        Transform deathSaves = roleLookup.FindPanel(SceneRole.DeathSaves, "deadChekBox") ??
                               roleLookup.FindPanel(SceneRole.DeathSaves, "deadCheckBox");
        if (deathSaves != null)
        {
            sceneData.SetString(RestResourceKeyPrefix + SceneRole.DeathSaves, StableFieldPath.For(deathSaves));
            SaveStableToggleRoles(sceneData, SceneRole.DeathSaves, deathSaves);
        }
    }

    private void SavePanel(CharacterSceneData sceneData, SceneRole role, string legacyName, bool legacyUseParent = false)
    {
        Transform panel = roleLookup.FindPanel(role, legacyName, legacyUseParent);
        if (panel != null)
        {
            sceneData.SetString(RestResourceKeyPrefix + role, StableFieldPath.For(panel));
            SaveStableToggleRoles(sceneData, role, panel);
        }
    }

    private void SaveStableToggleRoles(CharacterSceneData sceneData, SceneRole role, Transform panel)
    {
        foreach (UnityEngine.UI.Toggle toggle in toggles.Find(panel))
        {
            PersistentFieldId field = PersistentFieldId.For(toggle);
            if (field == null)
                continue;

            sceneData.SetString("RestRole_" + field.Id, role + "|" + SceneObjectName.ToggleNumber(toggle.name));
        }
    }

}
