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
    ResetScene,
    HitDiceCount,
    HitDiceSides,
    MovementSpeed,
    PortraitImage,
    PortraitButton,
    MainPage,
    Overlay,
    WildShapeOverlay
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
    private readonly UnityEngine.SceneManagement.Scene? scope;

    public SceneRoleLookup() { }
    public SceneRoleLookup(UnityEngine.SceneManagement.Scene scene) { scope = scene; }

    private bool InScope(GameObject candidate)
    {
        var scene = scope ?? UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return candidate.scene.IsValid() && candidate.scene.isLoaded && candidate.scene == scene;
    }

    public SceneRoleMarker Find(SceneRole role)
    {
        SceneRoleMarker result = null;
        foreach (SceneRoleMarker marker in UnityEngine.Object.FindObjectsByType<SceneRoleMarker>(FindObjectsInactive.Include))
            if (marker != null && InScope(marker.gameObject) && marker.Role == role)
            {
                if (result != null)
                    throw new InvalidOperationException("Duplicate scene role: " + role);
                result = marker;
            }

        return result;
    }

    public Transform FindPanel(SceneRole role, string legacyName, bool legacyUseParent = false)
    {
        SceneRoleMarker marker = Find(role);
        if (marker != null)
            return marker.Panel;

        foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate == null || !InScope(candidate.gameObject) || !MatchesLegacyName(candidate.name, legacyName))
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
