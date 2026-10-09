using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct CharacterRestResult
{
    public bool HasHealthBar { get; }
    public bool HasHitDice { get; }
    public int Healed { get; }
    public int DiceRolled { get; }
    public int DiceSides { get; }
    public int Roll { get; }

    public CharacterRestResult(bool hasHealthBar, bool hasHitDice = false, int healed = 0,
        int diceRolled = 0, int diceSides = 0, int roll = 0)
    {
        HasHealthBar = hasHealthBar;
        HasHitDice = hasHitDice;
        Healed = healed;
        DiceRolled = diceRolled;
        DiceSides = diceSides;
        Roll = roll;
    }
}
public static class CharacterRestService
{
    private const string RestResourceKeyPrefix = "RestResource_";

    public static CharacterRestResult ApplyLongRest(HealthBar healthBar)
    {
        int healed = healthBar != null ? healthBar.RestoreToMaxHealth() : 0;
        Apply(true);
        return new CharacterRestResult(healthBar != null, healed: healed);
    }

    public static CharacterRestResult ApplyShortRest(HealthBar healthBar)
    {
        if (healthBar != null)
            healthBar.ClearTemporaryHealth();

        Apply(false);
        if (healthBar == null || !TryGetHitDice(out int diceCount, out int diceSides))
            return new CharacterRestResult(healthBar != null);

        int diceRolled = Mathf.CeilToInt(diceCount / AppConfig.Calculator.ShortRestDiceDivisor);
        int roll = 0;
        for (int i = 0; i < diceRolled; i++)
            roll += UnityEngine.Random.Range(1, diceSides + 1);

        int healed = healthBar.ApplyHeal(roll);
        return new CharacterRestResult(true, true, healed, diceRolled, diceSides, roll);
    }

    private static bool TryGetHitDice(out int diceCount, out int diceSides)
    {
        diceCount = 0;
        diceSides = 0;

        InputField allDiceField = FindInputFieldByName("alldise", "alldaise");
        InputField diceValueField = FindInputFieldByName("daicevalueperson");
        if (allDiceField == null || diceValueField == null ||
            !int.TryParse(ExtractFirstNumber(allDiceField.text), out diceCount) ||
            !int.TryParse(ExtractFirstNumber(diceValueField.text), out diceSides))
            return false;

        diceCount = Mathf.Clamp(diceCount, 0, AppConfig.Calculator.MaximumDiceCount);
        diceSides = Mathf.Clamp(diceSides, AppConfig.Calculator.MinimumDiceSides,
            AppConfig.Calculator.MaximumDiceSides);
        return diceCount > 0;
    }

    private static InputField FindInputFieldByName(params string[] objectNames)
    {
        InputField[] fields = UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include);
        foreach (InputField field in fields)
        {
            if (field == null || !field.gameObject.activeInHierarchy)
                continue;
            foreach (string objectName in objectNames)
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;
            foreach (string objectName in objectNames)
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;
            foreach (string objectName in objectNames)
                if (field.gameObject.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return field;
        }

        return null;
    }

    private static string ExtractFirstNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        Match match = Regex.Match(value, @"\d+");
        return match.Success ? match.Value : "";
    }

    public static void Apply(bool isLongRest)
    {
        ApplyRestResources(isLongRest);
        if (isLongRest)
        {
            ClearSpellSlots();
            ReduceExhaustionByOne();
            ClearDeathSaves();
        }
        SaveSceneAfterRest();
        ApplyGlobalRestToSaveData(isLongRest);
    }

    private static void ApplyRestResources(bool isLongRest)
    {
        Transform resourceRoot = SceneRoleLookup.FindPanel(SceneRole.ClassResources, "resursClas");
        if (resourceRoot == null)
            return;

        ClearPanelsByMarkers(resourceRoot, "WildShape", "ChannelDivinity", "KiPoints", "DragonBreath");

        Transform bloodPanel = FindPanelByMarker(resourceRoot, "BloodCurse");
        if (bloodPanel == null)
            bloodPanel = FindDirectChild(resourceRoot, AppConfig.Calculator.BloodHunterPanelName);

        if (bloodPanel != null)
        {
            ClearPanelToggles(bloodPanel, 0, AppConfig.Calculator.BloodHunterPrimaryToggleLastIndex);
            if (isLongRest)
                ClearPanelToggles(
                    bloodPanel,
                    AppConfig.Calculator.BloodHunterSecondaryToggleFirstIndex,
                    AppConfig.Calculator.BloodHunterSecondaryToggleLastIndex);
        }

        if (isLongRest)
            ClearPanelsByMarkers(resourceRoot, "Rage", "SorceryPoints", "Flight");
    }

    private static void ClearPanelsByMarkers(Transform resourceRoot, params string[] markerNames)
    {
        HashSet<Transform> clearedPanels = new HashSet<Transform>();
        foreach (string markerName in markerNames)
        {
            Transform panel = FindPanelByMarker(resourceRoot, markerName);
            if (panel != null && clearedPanels.Add(panel))
                ClearPanelToggles(panel);
        }
    }

    private static Transform FindPanelByMarker(Transform resourceRoot, string markerName)
    {
        if (Enum.TryParse(markerName, out SceneRole role))
        {
            SceneRoleMarker roleMarker = SceneRoleLookup.Find(role);
            if (roleMarker != null)
                return roleMarker.Panel;
        }

        if (resourceRoot == null)
            return null;

        foreach (Transform child in resourceRoot.GetComponentsInChildren<Transform>(true))
            if (child != resourceRoot && SceneObjectName.Matches(child.name, markerName))
                return child.parent;

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
            if (child != null && child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;

        return null;
    }

    private static void ReduceExhaustionByOne()
    {
        Transform exhaustionRoot = SceneRoleLookup.FindPanel(SceneRole.Exhaustion, "vtoma");
        if (exhaustionRoot == null)
            return;

        List<Toggle> toggles = GetPanelToggles(
            exhaustionRoot,
            0,
            AppConfig.Calculator.ExhaustionToggleLastIndex);
        if (toggles.Count == 0)
            return;

        toggles.Sort((left, right) => GetToggleNumber(left.name).CompareTo(GetToggleNumber(right.name)));

        int checkedCount = 0;
        foreach (Toggle toggle in toggles)
            if (toggle != null && toggle.isOn)
                checkedCount++;

        if (checkedCount <= 0)
            return;

        Toggle lastCheckedToggle = toggles[Mathf.Clamp(checkedCount - 1, 0, toggles.Count - 1)];
        if (lastCheckedToggle != null)
            lastCheckedToggle.isOn = false;
    }

    private static void ClearDeathSaves()
    {
        Transform deathRoot = SceneRoleLookup.FindPanel(SceneRole.DeathSaves, "deadChekBox");
        if (deathRoot == null)
            deathRoot = SceneRoleLookup.FindPanel(SceneRole.DeathSaves, "deadCheckBox");

        if (deathRoot == null)
            return;

        ClearPanelToggles(deathRoot);
    }

    private static void ClearSpellSlots()
    {
        Transform spellSlotsRoot = SceneRoleLookup.FindPanel(SceneRole.SpellSlots, "spelChek");
        if (spellSlotsRoot == null)
            return;

        ClearPanelToggles(spellSlotsRoot);
    }

    private static void ClearPanelToggles(Transform panel, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        foreach (Toggle toggle in GetPanelToggles(panel, minToggleNumber, maxToggleNumber))
            if (toggle != null)
                toggle.isOn = false;
    }

    private static List<Toggle> GetPanelToggles(Transform panel, int minToggleNumber, int maxToggleNumber)
    {
        List<Toggle> toggles = new List<Toggle>();
        if (panel == null)
            return toggles;

        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle == null || !SceneObjectName.Matches(toggle.name, "Toggle"))
                continue;

            int toggleNumber = GetToggleNumber(toggle.name);
            if (toggleNumber < minToggleNumber || toggleNumber > maxToggleNumber)
                continue;

            if (IsInsideDropdown(toggle.transform, panel))
                continue;

            toggles.Add(toggle);
        }

        return toggles;
    }

    private static bool IsInsideDropdown(Transform transform, Transform stopAt)
    {
        Transform current = transform;
        while (current != null && current != stopAt)
        {
            if (current.GetComponent<Dropdown>() != null || current.GetComponent<TMP_Dropdown>() != null)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static int GetToggleNumber(string name)
    {
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open >= 0 && close > open && int.TryParse(name.Substring(open + 1, close - open - 1), out int number))
            return number;

        return 0;
    }

    private static void SaveSceneAfterRest()
    {
        CharacterSheetManagerScene1 sheetManager = UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
        if (sheetManager != null)
        {
            sheetManager.SaveCharacterData();
            return;
        }

        CharacterSceneAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<CharacterSceneAutoSave>();
        if (autoSave != null)
            autoSave.SaveSceneData();
    }

    private static void ApplyGlobalRestToSaveData(bool isLongRest)
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
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
                0,
                AppConfig.Calculator.BloodHunterPrimaryToggleLastIndex);
            if (isLongRest)
                ClearSavedPanelToggles(
                    sceneData,
                    bloodPanelPath,
                    "BloodCurse",
                    AppConfig.Calculator.BloodHunterSecondaryToggleFirstIndex,
                    AppConfig.Calculator.BloodHunterSecondaryToggleLastIndex);

            if (!isLongRest)
                continue;

            RestoreSavedHealthBars(sceneData);
            ClearSavedPanelsByMarkers(sceneData, "Rage", "SorceryPoints", "Flight");
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "SpellSlots"), "SpellSlots");
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "DeathSaves"), "DeathSaves");
            // The current scene's toggles were already reduced and saved above.
            if (sceneData != saveManager.GetActiveSceneData())
                ReduceSavedExhaustionByOne(sceneData, GetRestPanelPath(sceneData, "Exhaustion"));
        }

        saveManager.SaveData();
    }

    private static void RestoreSavedHealthBars(CharacterSceneData sceneData)
    {
        if (sceneData == null || sceneData.intData == null)
            return;

        foreach (IntSaveEntry entry in sceneData.intData)
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

    private static void ClearSavedPanelsByMarkers(CharacterSceneData sceneData, params string[] markerNames)
    {
        foreach (string markerName in markerNames)
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, markerName), markerName);
    }

    private static string GetRestPanelPath(CharacterSceneData sceneData, string markerName)
    {
        return sceneData != null ? sceneData.GetString(RestResourceKeyPrefix + markerName, "") : "";
    }

    private static void ClearSavedPanelToggles(CharacterSceneData sceneData, string panelPath, string role, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        RestSavedToggleUpdater.Clear(sceneData, role, panelPath, minToggleNumber, maxToggleNumber);
    }

    private static void ReduceSavedExhaustionByOne(CharacterSceneData sceneData, string panelPath)
    {
        RestSavedToggleUpdater.ReduceExhaustionByOne(
            sceneData, panelPath, AppConfig.Calculator.ExhaustionToggleLastIndex);
    }

}
