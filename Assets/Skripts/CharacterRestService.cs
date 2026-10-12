using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

internal readonly struct CharacterRestResult
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
internal sealed class CharacterRestService
{
    // Short rest spends half the available hit dice, rounded up.
    private const float HitDiceDivisorForShortRest = 2f;

    private readonly CharacterRestSavedStateService savedState;
    private readonly SceneRoleLookup roleLookup = new SceneRoleLookup();
    private readonly RestSceneToggleFinder toggleFinder = new RestSceneToggleFinder();

    internal CharacterRestService(DndSaveManager saveManager)
    {
        savedState = new CharacterRestSavedStateService(saveManager);
    }

    internal CharacterRestResult ApplyLongRest(HealthBar healthBar)
    {
        int healed = healthBar != null ? healthBar.RestoreToMaxHealth() : 0;
        Apply(true);
        return new CharacterRestResult(healthBar != null, healed: healed);
    }

    internal CharacterRestResult ApplyShortRest(HealthBar healthBar)
    {
        if (healthBar != null)
            healthBar.ClearTemporaryHealth();

        Apply(false);
        if (healthBar == null || !TryGetHitDice(out int diceCount, out int diceSides))
            return new CharacterRestResult(healthBar != null);

        int diceRolled = Mathf.CeilToInt(diceCount / HitDiceDivisorForShortRest);
        int roll = 0;
        for (int i = 0; i < diceRolled; i++)
            roll += UnityEngine.Random.Range(1, diceSides + 1);

        int healed = healthBar.ApplyHeal(roll);
        return new CharacterRestResult(true, true, healed, diceRolled, diceSides, roll);
    }

    private bool TryGetHitDice(out int diceCount, out int diceSides)
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

    private InputField FindInputFieldByName(params string[] objectNames)
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

    private string ExtractFirstNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        Match match = Regex.Match(value, @"\d+");
        return match.Success ? match.Value : "";
    }

    internal void Apply(bool isLongRest)
    {
        ApplyRestResources(isLongRest);
        if (isLongRest)
        {
            ClearSpellSlots();
            ReduceExhaustionByOne();
            ClearDeathSaves();
        }
        SaveSceneAfterRest();
        savedState.Apply(isLongRest);
    }

    private void ApplyRestResources(bool isLongRest)
    {
        Transform resourceRoot = roleLookup.FindPanel(SceneRole.ClassResources, "resursClas");
        if (resourceRoot == null)
            return;

        ClearPanelsByMarkers(resourceRoot, SceneRole.WildShape, SceneRole.ChannelDivinity,
            SceneRole.KiPoints, SceneRole.DragonBreath);

        Transform bloodPanel = FindPanelByMarker(resourceRoot, SceneRole.BloodCurse);
        if (bloodPanel == null)
            bloodPanel = FindDirectChild(resourceRoot, RestToggleGroups.LegacyBloodCursePanelName);

        if (bloodPanel != null)
        {
            ClearPanelToggles(bloodPanel, RestToggleGroups.BloodCurseShortRest.First,
                RestToggleGroups.BloodCurseShortRest.Last);
            if (isLongRest)
                ClearPanelToggles(
                    bloodPanel,
                    RestToggleGroups.BloodCurseLongRestExtra.First,
                    RestToggleGroups.BloodCurseLongRestExtra.Last);
        }

        if (isLongRest)
            ClearPanelsByMarkers(resourceRoot, SceneRole.Rage, SceneRole.SorceryPoints, SceneRole.Flight);
    }

    private void ClearPanelsByMarkers(Transform resourceRoot, params SceneRole[] roles)
    {
        HashSet<Transform> clearedPanels = new HashSet<Transform>();
        foreach (SceneRole role in roles)
        {
            Transform panel = FindPanelByMarker(resourceRoot, role);
            if (panel != null && clearedPanels.Add(panel))
                ClearPanelToggles(panel);
        }
    }

    private Transform FindPanelByMarker(Transform resourceRoot, SceneRole role)
    {
        SceneRoleMarker roleMarker = roleLookup.Find(role);
        if (roleMarker != null)
            return roleMarker.Panel;

        if (resourceRoot == null)
            return null;

        foreach (Transform child in resourceRoot.GetComponentsInChildren<Transform>(true))
            if (child != resourceRoot && SceneObjectName.Matches(child.name, role.ToString()))
                return child.parent;

        return null;
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
            if (child != null && child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;

        return null;
    }

    private void ReduceExhaustionByOne()
    {
        Transform exhaustionRoot = roleLookup.FindPanel(SceneRole.Exhaustion, "vtoma");
        if (exhaustionRoot == null)
            return;

        List<Toggle> toggles = toggleFinder.Find(
            exhaustionRoot,
            RestToggleGroups.Exhaustion.First,
            RestToggleGroups.Exhaustion.Last);
        if (toggles.Count == 0)
            return;

        toggles.Sort((left, right) => SceneObjectName.ToggleNumber(left.name).CompareTo(SceneObjectName.ToggleNumber(right.name)));

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

    private void ClearDeathSaves()
    {
        Transform deathRoot = roleLookup.FindPanel(SceneRole.DeathSaves, "deadChekBox");
        if (deathRoot == null)
            deathRoot = roleLookup.FindPanel(SceneRole.DeathSaves, "deadCheckBox");

        if (deathRoot == null)
            return;

        ClearPanelToggles(deathRoot);
    }

    private void ClearSpellSlots()
    {
        Transform spellSlotsRoot = roleLookup.FindPanel(SceneRole.SpellSlots, "spelChek");
        if (spellSlotsRoot == null)
            return;

        ClearPanelToggles(spellSlotsRoot);
    }

    private void ClearPanelToggles(Transform panel, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        foreach (Toggle toggle in toggleFinder.Find(panel, minToggleNumber, maxToggleNumber))
            if (toggle != null)
                toggle.isOn = false;
    }

    private void SaveSceneAfterRest()
    {
        FindActiveSceneSaveController()?.SaveCharacterData();
    }

    private CharacterSheetManagerScene1 FindActiveSceneSaveController()
    {
        return UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
    }

}
