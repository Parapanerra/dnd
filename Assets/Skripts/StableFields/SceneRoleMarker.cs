using System;
using UnityEngine;
using UnityEngine.UI;

public enum SceneRole
{
    Rage,
    WildShape,
    ChannelDivinity,
    KiPoints,
    SorceryPoints,
    BloodCurse,
    DragonBreath,
    Flight,
    SpellSlots,
    Exhaustion,
    DeathSaves,
    ClassResources,
    TemporaryHealth,
    ResetScene
}

// The role survives visual renames. Legacy scene names remain a fallback while scenes migrate.
public sealed class SceneRoleMarker : MonoBehaviour
{
    [SerializeField] private SceneRole role;
    [SerializeField] private bool useParentAsPanel;

    public SceneRole Role => role;
    public bool UsesParentAsPanel => useParentAsPanel;

    public void Configure(SceneRole newRole, bool parentAsPanel)
    {
        role = newRole;
        useParentAsPanel = parentAsPanel;
    }

    public Transform Panel => useParentAsPanel && transform.parent != null ? transform.parent : transform;
}

public static class SceneRoleLookup
{
    private const string RestResourceKeyPrefix = "RestResource_";

    public static SceneRoleMarker Find(SceneRole role)
    {
        foreach (SceneRoleMarker marker in UnityEngine.Object.FindObjectsByType<SceneRoleMarker>(FindObjectsInactive.Include))
            if (marker != null && marker.gameObject.scene.IsValid() && marker.Role == role)
                return marker;

        return null;
    }

    public static Transform FindPanel(SceneRole role, string legacyName, bool legacyUseParent = false)
    {
        SceneRoleMarker marker = Find(role);
        if (marker != null)
            return marker.Panel;

        foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid() || !NameMatches(candidate.name, legacyName))
                continue;

            return legacyUseParent && candidate.parent != null ? candidate.parent : candidate;
        }

        return null;
    }

    public static void SaveRestResourcePaths(CharacterSceneData sceneData)
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
        Transform deathSaves = FindPanel(SceneRole.DeathSaves, "deadChekBox") ??
                               FindPanel(SceneRole.DeathSaves, "deadCheckBox");
        if (deathSaves != null)
        {
            sceneData.SetString(RestResourceKeyPrefix + SceneRole.DeathSaves, GetControlPath(deathSaves));
            SaveStableToggleRoles(sceneData, SceneRole.DeathSaves, deathSaves);
        }
    }

    private static void SavePanel(CharacterSceneData sceneData, SceneRole role, string legacyName, bool legacyUseParent = false)
    {
        Transform panel = FindPanel(role, legacyName, legacyUseParent);
        if (panel != null)
        {
            sceneData.SetString(RestResourceKeyPrefix + role, GetControlPath(panel));
            SaveStableToggleRoles(sceneData, role, panel);
        }
    }

    private static void SaveStableToggleRoles(CharacterSceneData sceneData, SceneRole role, Transform panel)
    {
        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle == null || !NameMatches(toggle.name, "Toggle") || IsDropdownChild(toggle.transform, panel))
                continue;

            PersistentFieldId field = PersistentFieldId.For(toggle);
            if (field == null)
                continue;

            sceneData.SetString("RestRole_" + field.Id, role + "|" + GetToggleNumber(toggle.name));
        }
    }

    private static bool IsDropdownChild(Transform transform, Transform panel)
    {
        for (Transform current = transform; current != null && current != panel; current = current.parent)
            if (current.GetComponent<Dropdown>() != null)
                return true;
        return false;
    }

    private static int GetToggleNumber(string name)
    {
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        return open >= 0 && close > open && int.TryParse(name.Substring(open + 1, close - open - 1), out int number)
            ? number : 0;
    }

    private static string GetControlPath(Transform transform)
    {
        string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name + "/" + path;
        }
        return path;
    }

    private static bool NameMatches(string actual, string expected)
    {
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            return true;

        return actual.StartsWith(expected + " (", StringComparison.OrdinalIgnoreCase);
    }
}
