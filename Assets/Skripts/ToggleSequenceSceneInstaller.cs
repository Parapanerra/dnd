using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Locates legacy scene groups and attaches sequence components; each component handles only its own toggles.
internal sealed class ToggleSequenceSceneInstaller
{
    // Legacy UI names encode toggle positions; retain this order for existing scenes and saves.
    private const int ArtifactSkillMaximumToggle = 4;
    private const int ArtifactInfusionMaximumToggle = 7;
    private const int BloodHunterMaximumToggle = 7;
    private readonly int[] spellCheckToggleOrder = { 2, 1, 0, 3, 4, 5, 6, 7 };

    internal int[] CreateSpellCheckToggleOrder() => (int[])spellCheckToggleOrder.Clone();
    private readonly ExhaustionEffects exhaustionEffects;
    private bool captureConfiguration;

    internal ToggleSequenceSceneInstaller(DndSaveManager saveManager)
    {
        exhaustionEffects = new ExhaustionEffects(saveManager);
    }

    internal void Install()
    {
        ToggleSequenceConfig[] configs = UnityEngine.Object.FindObjectsByType<ToggleSequenceConfig>(FindObjectsInactive.Include);
        foreach (ToggleSequenceConfig config in configs)
            if (!captureConfiguration)
                config.Install(exhaustionEffects);

        // Legacy fallback only in scenes without migrated group configuration.
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
        {
            if (System.Array.Exists(configs, c => c.gameObject.scene == transform.gameObject.scene))
                continue;
            if (IsDeathCheckContainer(transform.name))
            {
                ConfigureGroup(transform, "uspih", false);
                ConfigureGroup(transform, "proval", false);
                continue;
            }

            SceneRoleMarker roleMarker = transform.GetComponent<SceneRoleMarker>();
            if (roleMarker != null ? roleMarker.Role == SceneRole.Exhaustion : SceneObjectName.Matches(transform.name, "vtoma"))
                ConfigureGroup(transform, true, false, exhaustion: exhaustionEffects);

            if (roleMarker != null ? roleMarker.Role == SceneRole.SpellSlots : SceneObjectName.Matches(transform.name, "spelChek"))
                ConfigureSpellCheckGroups(transform);

            if (SceneObjectName.Matches(transform.name, "artefactsNavuk"))
                ConfigureArtifactSkillGroups(
                    transform,
                    ArtifactSkillMaximumToggle);

            if (SceneObjectName.Matches(transform.name, "artefactInfuz"))
                ConfigureArtifactSkillGroups(
                    transform,
                    ArtifactInfusionMaximumToggle);

            if (SceneObjectName.Matches(transform.name, "resursClas"))
            {
                ConfigureExactOrBaseChildGroup(transform, "Panel", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (1)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (2)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (3)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (4)", true, false);
                ConfigureExactOrBaseChildGroup(
                    transform,
                    RestToggleGroups.LegacyBloodCursePanelName,
                    true,
                    false,
                    BloodHunterMaximumToggle);
                ConfigureExactOrBaseChildGroup(transform, "Panel (8)", true, false);
            }
        }
    }

    private void ConfigureSpellCheckGroups(Transform container)
    {
        foreach (Transform child in container)
            if (SceneObjectName.Matches(child.name, "uspih"))
                ConfigureGroup(
                    child,
                    true,
                    false,
                    int.MaxValue,
                    CreateSpellCheckToggleOrder());
    }

    private void ConfigureArtifactSkillGroups(Transform container, int maxToggleNumber)
    {
        foreach (Transform child in container.GetComponentsInChildren<Transform>(true))
            if (child != container && (SceneObjectName.Matches(child.name, "tongle") || SceneObjectName.Matches(child.name, "tonggle")))
                ConfigureGroup(child, true, false, maxToggleNumber, null, "Togglespaspaw");
    }

    private void ConfigureGroup(Transform container, string groupName, bool ascending)
    {
        foreach (Transform child in container)
        {
            if (!SceneObjectName.Matches(child.name, groupName))
                continue;

            ConfigureGroup(child, ascending, false);
        }
    }

    private void ConfigureExactOrBaseChildGroup(Transform container, string exactName, bool ascending, bool clampToActive, int maxToggleNumber = int.MaxValue)
    {
        foreach (Transform child in container)
        {
            if (!child.name.Equals(exactName, StringComparison.OrdinalIgnoreCase))
                continue;

            ConfigureGroup(child, ascending, clampToActive, maxToggleNumber);
            return;
        }
    }

    private void ConfigureGroup(Transform groupRoot, bool ascending, bool clampToActive, int maxToggleNumber = int.MaxValue, int[] customOrder = null, string toggleBaseName = "Toggle", ExhaustionEffects exhaustion = null)
    {
        List<Toggle> groupToggles = new List<Toggle>();
        foreach (Toggle toggle in groupRoot.GetComponentsInChildren<Toggle>(true))
            if (IsSequenceToggle(toggle, groupRoot, maxToggleNumber, toggleBaseName))
                groupToggles.Add(toggle);

        if (groupToggles.Count == 0)
            return;

        groupToggles.Sort((left, right) => CompareTogglesBySequenceOrder(left, right, ascending, customOrder));

        if (captureConfiguration)
        {
            ToggleSequenceConfig config = groupRoot.GetComponent<ToggleSequenceConfig>();
            if (config == null) config = groupRoot.gameObject.AddComponent<ToggleSequenceConfig>();
            config.Configure(groupToggles.ToArray(), clampToActive, exhaustion != null);
            return;
        }

        DeathSaveToggleSequence sequence = groupRoot.GetComponent<DeathSaveToggleSequence>();
        if (sequence == null)
            sequence = groupRoot.gameObject.AddComponent<DeathSaveToggleSequence>();

        sequence.Configure(groupToggles.ToArray(), clampToActive, exhaustion);
    }

    private int CompareTogglesBySequenceOrder(Toggle left, Toggle right, bool ascending, int[] customOrder = null)
    {
        int leftIndex = SceneObjectName.ToggleNumber(left != null ? left.name : "");
        int rightIndex = SceneObjectName.ToggleNumber(right != null ? right.name : "");
        if (customOrder != null && customOrder.Length > 0)
        {
            int leftOrder = GetCustomOrderIndex(leftIndex, customOrder);
            int rightOrder = GetCustomOrderIndex(rightIndex, customOrder);
            return leftOrder.CompareTo(rightOrder);
        }

        return ascending ? leftIndex.CompareTo(rightIndex) : rightIndex.CompareTo(leftIndex);
    }

    private int GetCustomOrderIndex(int toggleNumber, int[] customOrder)
    {
        for (int i = 0; i < customOrder.Length; i++)
            if (customOrder[i] == toggleNumber)
                return i;

        return customOrder.Length + toggleNumber;
    }

    private bool IsSequenceToggle(Toggle toggle, Transform groupRoot, int maxToggleNumber, string toggleBaseName = "Toggle")
    {
        if (toggle == null || !SceneObjectName.Matches(toggle.name, toggleBaseName))
            return false;

        if (SceneObjectName.ToggleNumber(toggle.name) > maxToggleNumber)
            return false;

        Transform current = toggle.transform;
        while (current != null && current != groupRoot)
        {
            if (current.GetComponent<Dropdown>() != null || current.GetComponent<TMP_Dropdown>() != null)
                return false;

            current = current.parent;
        }

        return true;
    }

    private bool IsDeathCheckContainer(string name)
    {
        return SceneObjectName.Matches(name, "deadChekBox") || SceneObjectName.Matches(name, "deadCheckBox");
    }

    // Used only by the Editor migration. It captures bindings without attaching listeners.
    internal void CaptureConfiguration()
    {
        captureConfiguration = true;
        try { Install(); }
        finally { captureConfiguration = false; }
    }

}
