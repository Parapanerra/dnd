using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneRoleMigration
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/personag/cartaPersonaj.unity",
        "Assets/Scenes/personag/Spels.unity",
        "Assets/Scenes/personag/spelBook.unity",
        "Assets/Scenes/personag/inventory.unity",
        "Assets/Scenes/personag/informForPerson.unity",
        "Assets/Scenes/personag/petsesn.unity"
    };

    private static readonly Dictionary<string, SceneRole> LegacyRoles = new Dictionary<string, SceneRole>(StringComparer.OrdinalIgnoreCase)
    {
        { "Rage", SceneRole.Rage },
        { "WildShape", SceneRole.WildShape },
        { "ChannelDivinity", SceneRole.ChannelDivinity },
        { "KiPoints", SceneRole.KiPoints },
        { "SorceryPoints", SceneRole.SorceryPoints },
        { "BloodCurse", SceneRole.BloodCurse },
        { "DragonBreath", SceneRole.DragonBreath },
        { "Flight", SceneRole.Flight },
        { "spelChek", SceneRole.SpellSlots },
        { "vtoma", SceneRole.Exhaustion },
        { "deadChekBox", SceneRole.DeathSaves },
        { "deadCheckBox", SceneRole.DeathSaves },
        { "resursClas", SceneRole.ClassResources },
        { "folslive", SceneRole.TemporaryHealth }
    };

    [MenuItem("Tools/Taruck/Migrate Scene Roles")]
    public static void MigrateAndValidate()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        int changes = 0;
        try
        {
            foreach (string path in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        SceneRoleMarker marker = transform.GetComponent<SceneRoleMarker>();
                        if (marker != null && marker.Role == SceneRole.TemporaryHealth &&
                            transform.GetComponentInChildren<Slider>(true) == null)
                        {
                            UnityEngine.Object.DestroyImmediate(marker);
                            marker = null;
                            changed = true;
                        }

                        if (!TryGetRole(transform, out SceneRole role, out bool useParent))
                            continue;

                        if (marker == null)
                        {
                            marker = transform.gameObject.AddComponent<SceneRoleMarker>();
                            changes++;
                            changed = true;
                        }

                        if (marker.Role != role || marker.UsesParentAsPanel != useParent)
                        {
                            marker.Configure(role, useParent);
                            EditorUtility.SetDirty(marker);
                            changed = true;
                        }
                    }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                ValidateScene(scene);
            }
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        Debug.Log($"Taruck scene roles: added {changes} markers; validated {ScenePaths.Length} scenes.");
    }

    [MenuItem("Tools/Taruck/Validate Scene Roles")]
    public static void ValidateOnly()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in ScenePaths)
                ValidateScene(EditorSceneManager.OpenScene(path, OpenSceneMode.Single));
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static bool TryGetRole(Transform transform, out SceneRole role, out bool useParent)
    {
        string name = transform.name;
        int suffix = name.LastIndexOf(" (", StringComparison.Ordinal);
        string baseName = suffix >= 0 ? name.Substring(0, suffix) : name;
        if (LegacyRoles.TryGetValue(baseName, out role))
        {
            if (role == SceneRole.TemporaryHealth && transform.GetComponentInChildren<Slider>(true) == null)
            {
                useParent = false;
                return false;
            }

            useParent = role == SceneRole.Rage || role == SceneRole.WildShape ||
                        role == SceneRole.ChannelDivinity || role == SceneRole.KiPoints ||
                        role == SceneRole.SorceryPoints || role == SceneRole.BloodCurse ||
                        role == SceneRole.DragonBreath || role == SceneRole.Flight;
            return true;
        }

        if (transform.GetComponent<Button>() != null &&
            (name.IndexOf("resetseve", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("resetsave", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("reset save", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            role = SceneRole.ResetScene;
            useParent = false;
            return true;
        }

        role = default;
        useParent = false;
        return false;
    }

    private static void ValidateScene(Scene scene)
    {
        var counts = new Dictionary<SceneRole, int>();
        var errors = new List<string>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SceneRoleMarker marker in root.GetComponentsInChildren<SceneRoleMarker>(true))
            {
                counts.TryGetValue(marker.Role, out int count);
                counts[marker.Role] = count + 1;
                if (marker.UsesParentAsPanel && marker.transform.parent == null)
                    errors.Add($"Scene role {marker.Role} has no parent");
                if (marker.Role == SceneRole.ResetScene && marker.GetComponent<Button>() == null)
                    errors.Add("Reset scene role has no Button");
                if (marker.Role == SceneRole.TemporaryHealth && marker.GetComponentInChildren<Slider>(true) == null)
                    errors.Add("Temporary health role has no Slider");
            }

        foreach (KeyValuePair<SceneRole, int> entry in counts)
            if (entry.Key != SceneRole.ResetScene && entry.Value > 1)
                errors.Add($"Duplicate scene role {entry.Key}: {entry.Value}");

        RequireRole(SceneRole.ResetScene);
        if (scene.name == "cartaPersonaj")
            foreach (SceneRole role in new[]
            {
                SceneRole.Rage, SceneRole.WildShape, SceneRole.ChannelDivinity,
                SceneRole.KiPoints, SceneRole.SorceryPoints, SceneRole.BloodCurse,
                SceneRole.DragonBreath, SceneRole.Flight, SceneRole.Exhaustion,
                SceneRole.DeathSaves, SceneRole.ClassResources
            })
                RequireRole(role);
        if (scene.name == "Spels")
            RequireRole(SceneRole.SpellSlots);
        if (scene.name == "petsesn")
            RequireRole(SceneRole.DeathSaves);

        if (errors.Count > 0)
            throw new InvalidOperationException(scene.path + ": " + string.Join("; ", errors));

        void RequireRole(SceneRole role)
        {
            if (!counts.ContainsKey(role))
                errors.Add("Missing scene role " + role);
        }
    }
}
