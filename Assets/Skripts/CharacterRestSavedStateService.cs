using System;
using UnityEngine;

// Applies rest to persisted pages; the live scene is handled by CharacterRestService.
internal sealed class CharacterRestSavedStateService
{
    private const string RestResourceKeyPrefix = "RestResource_";
    private readonly DndSaveManager saveManager;
    private readonly RestSavedToggleUpdater toggleUpdater = new RestSavedToggleUpdater();

    internal CharacterRestSavedStateService(DndSaveManager saveManager)
    {
        this.saveManager = saveManager;
    }

    internal void Apply(bool isLongRest)
    {
        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character == null || character.sceneStates == null)
            return;

        CharacterSceneData activeScene = null;
        if (isLongRest && character.sceneStates.Exists(state => state != null))
            activeScene = saveManager.GetActiveSceneData();

        ApplyToCharacter(character, isLongRest, activeScene);
        saveManager.SaveData();
    }

    internal void ApplyToCharacter(CharacterData character, bool isLongRest, CharacterSceneData activeScene)
    {
        if (character == null || character.sceneStates == null)
            return;

        foreach (CharacterSceneData sceneData in character.sceneStates)
        {
            if (sceneData == null)
                continue;

            ClearSavedPanelsByMarkers(sceneData, "WildShape", "ChannelDivinity", "KiPoints", "DragonBreath");

            string bloodPanelPath = GetRestPanelPath(sceneData, "BloodCurse");
            ClearSavedPanelToggles(
                sceneData,
                bloodPanelPath,
                "BloodCurse",
                RestToggleGroups.BloodCurseShortRest.First,
                RestToggleGroups.BloodCurseShortRest.Last);
            if (isLongRest)
                ClearSavedPanelToggles(
                    sceneData,
                    bloodPanelPath,
                    "BloodCurse",
                    RestToggleGroups.BloodCurseLongRestExtra.First,
                    RestToggleGroups.BloodCurseLongRestExtra.Last);

            if (!isLongRest)
                continue;

            RestoreSavedHealthBars(sceneData);
            ClearSavedPanelsByMarkers(sceneData, "Rage", "SorceryPoints", "Flight");
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "SpellSlots"), "SpellSlots");
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "DeathSaves"), "DeathSaves");
            // The current scene's toggles were already reduced and saved above.
            if (sceneData != activeScene)
                ReduceSavedExhaustionByOne(sceneData, GetRestPanelPath(sceneData, "Exhaustion"));
        }
    }

    private void RestoreSavedHealthBars(CharacterSceneData sceneData)
    {
        if (sceneData == null || sceneData.intData == null)
            return;

        foreach (IntSaveEntry entry in sceneData.intData.ToArray())
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || !entry.key.StartsWith("HealthBar_", StringComparison.Ordinal))
                continue;

            if (!entry.key.EndsWith("_maxHealth", StringComparison.Ordinal))
                continue;

            string prefix = entry.key.Substring(0, entry.key.Length - "maxHealth".Length);
            sceneData.SetInt(prefix + "currentHealth", Mathf.Max(0, entry.value));
            sceneData.SetInt(prefix + "maxTemporaryHealth", 0);
            sceneData.SetInt(prefix + "currentTemporaryHealth", 0);
        }
    }

    private void ClearSavedPanelsByMarkers(CharacterSceneData sceneData, params string[] markerNames)
    {
        foreach (string markerName in markerNames)
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, markerName), markerName);
    }

    private string GetRestPanelPath(CharacterSceneData sceneData, string markerName)
    {
        return sceneData != null ? sceneData.GetString(RestResourceKeyPrefix + markerName, "") : "";
    }

    private void ClearSavedPanelToggles(CharacterSceneData sceneData, string panelPath, string role, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        toggleUpdater.Clear(sceneData, role, panelPath, minToggleNumber, maxToggleNumber);
    }

    private void ReduceSavedExhaustionByOne(CharacterSceneData sceneData, string panelPath)
    {
        toggleUpdater.ReduceExhaustionByOne(
            sceneData, panelPath, RestToggleGroups.Exhaustion.Last);
    }
}
