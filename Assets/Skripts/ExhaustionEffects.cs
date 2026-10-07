using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// Base speed is saved separately so clamping to zero never loses movement.
public static class ExhaustionEffects
{
    private const string BaseSpeedKey = "Exhaustion.BaseSpeed";

    public static int Level
    {
        get
        {
            SceneRoleMarker exhaustionMarker = SceneRoleLookup.Find(SceneRole.Exhaustion);
            if (exhaustionMarker != null)
                return CountExhaustionToggles(exhaustionMarker.Panel);

            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (item.name == "vtoma")
                    return CountExhaustionToggles(item);

            CharacterData character = DndSaveManager.Instance?.GetActiveCharacter();
            if (character != null)
                foreach (CharacterSceneData scene in character.sceneStates)
                {
                    string panel = scene.GetString("RestResource_Exhaustion");
                    if (string.IsNullOrEmpty(panel)) continue;
                    int count = 0;
                    foreach (IntSaveEntry entry in scene.intData)
                        if (entry.key.StartsWith("Toggle_" + panel + "/", StringComparison.Ordinal) && entry.value != 0)
                            count++;
                    return Mathf.Clamp(count, 0, 6);
                }
            return 0;
        }
    }

    private static int CountExhaustionToggles(Transform panel)
    {
        int count = 0;
        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
            if (toggle.name.StartsWith("Toggle", StringComparison.Ordinal) && toggle.isOn)
                count++;
        return Mathf.Clamp(count, 0, 6);
    }

    public static bool IsD20Roll(string expression)
    {
        return Regex.IsMatch(expression ?? "", @"(?<![\w.])\d*[dD]20(?!\d)");
    }

    public static string PenaltyLabel(int penalty)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
        string label = language == AppLanguage.English ? "exhaustion" : language == AppLanguage.Russian ? "усталость" : "втома";
        return " −" + penalty + " " + label;
    }

    public static void Apply(int level)
    {
        if (DndSaveManager.Instance == null) return;
        CharacterSceneData data = DndSaveManager.Instance.GetActiveSceneData();
        foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name != "movsped") continue;
            InputField input = item.GetComponentInChildren<InputField>(true);
            if (input == null) continue;
            if (!data.HasInt(BaseSpeedKey) && int.TryParse(input.text, out int original))
                data.SetInt(BaseSpeedKey, Mathf.Max(0, original));
            if (data.HasInt(BaseSpeedKey))
                input.text = Mathf.Max(0, data.GetInt(BaseSpeedKey) - 5 * level).ToString();
            input.onEndEdit.RemoveListener(OnSpeedEdited);
            input.onEndEdit.AddListener(OnSpeedEdited);
        }

        if (level == 6)
        {
            foreach (HealthBar bar in UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
                if (bar.IsUsableForCalculator) bar.SetHealthToZero();
            foreach (HealthBar1 bar in UnityEngine.Object.FindObjectsByType<HealthBar1>(FindObjectsInactive.Include))
                if (bar.IsUsableForCalculator) bar.SetHealthToZero();
        }
        foreach (CalculatorManager calculator in UnityEngine.Object.FindObjectsByType<CalculatorManager>(FindObjectsInactive.Include))
            calculator.RefreshExhaustionDisplay();
        DndSaveManager.Instance.RequestSaveData();
    }

    private static void OnSpeedEdited(string value)
    {
        if (!int.TryParse(value, out int speed) || DndSaveManager.Instance == null) return;
        // The user edits the displayed (effective) speed.
        DndSaveManager.Instance.GetActiveSceneData().SetInt(BaseSpeedKey, Mathf.Max(0, speed) + 5 * Level);
        Apply(Level);
    }
}
