using System;
using UnityEngine;

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

public sealed class SceneRoleLookup
{
    public SceneRoleMarker Find(SceneRole role)
    {
        foreach (SceneRoleMarker marker in UnityEngine.Object.FindObjectsByType<SceneRoleMarker>(FindObjectsInactive.Include))
            if (marker != null && marker.gameObject.scene.IsValid() && marker.Role == role)
                return marker;

        return null;
    }

    public Transform FindPanel(SceneRole role, string legacyName, bool legacyUseParent = false)
    {
        SceneRoleMarker marker = Find(role);
        if (marker != null)
            return marker.Panel;

        foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid() || !MatchesLegacyName(candidate.name, legacyName))
                continue;

            return legacyUseParent && candidate.parent != null ? candidate.parent : candidate;
        }

        return null;
    }

    internal static bool MatchesLegacyName(string actual, string expected)
    {
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            return true;

        return actual.StartsWith(expected + " (", StringComparison.OrdinalIgnoreCase);
    }
}
