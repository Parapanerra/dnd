using System;
using UnityEngine;
using UnityEngine.UI;

// Base speed is saved separately so clamping to zero never loses movement.
internal sealed class ExhaustionEffects
{
    internal const int DicePenaltyPerLevel = 2;
    private const string BaseSpeedKey = "Exhaustion.BaseSpeed";
    private const int MaximumExhaustionLevel = 6;
    private const int SpeedPenaltyPerLevel = 5;
    private readonly DndSaveManager saveManager;
    private readonly SceneRoleLookup roleLookup = new SceneRoleLookup();
    private readonly RestSceneToggleFinder toggleFinder = new RestSceneToggleFinder();

    public ExhaustionEffects(DndSaveManager dataManager)
    {
        saveManager = dataManager;
    }

    public int Level
    {
        get
        {
            SceneRoleMarker exhaustionMarker = roleLookup.Find(SceneRole.Exhaustion);
            if (exhaustionMarker != null)
                return CountExhaustionToggles(exhaustionMarker.Panel);

            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (item.name == "vtoma")
                    return CountExhaustionToggles(item);

            CharacterData character = saveManager != null ? saveManager.GetActiveCharacter() : null;
            if (character != null)
                foreach (CharacterSceneData scene in character.sceneStates)
                {
                    string panel = scene.GetString("RestResource_Exhaustion");
                    if (string.IsNullOrEmpty(panel)) continue;
                    int count = 0;
                    foreach (IntSaveEntry entry in scene.intData)
                        if (entry.key.StartsWith("Toggle_" + panel + "/", StringComparison.Ordinal) && entry.value != 0)
                            count++;
                    return Mathf.Clamp(count, 0, MaximumExhaustionLevel);
                }
            return 0;
        }
    }

    private int CountExhaustionToggles(Transform panel)
    {
        int count = 0;
        foreach (Toggle toggle in toggleFinder.Find(panel))
            if (toggle.isOn)
                count++;
        return Mathf.Clamp(count, 0, MaximumExhaustionLevel);
    }

    public void Apply(int level)
    {
        if (saveManager == null) return;
        CharacterSceneData data = saveManager.GetActiveSceneData();
        foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name != "movsped") continue;
            InputField input = item.GetComponentInChildren<InputField>(true);
            if (input == null) continue;
            if (!data.HasInt(BaseSpeedKey) && int.TryParse(input.text, out int original))
                data.SetInt(BaseSpeedKey, Mathf.Max(0, original));
            if (data.HasInt(BaseSpeedKey))
                input.text = Mathf.Max(0, data.GetInt(BaseSpeedKey) - SpeedPenaltyPerLevel * level).ToString();
            input.onEndEdit.RemoveListener(OnSpeedEdited);
            input.onEndEdit.AddListener(OnSpeedEdited);
        }

        if (level == MaximumExhaustionLevel)
        {
            foreach (HealthBar bar in UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
                if (bar.IsUsableForCalculator) bar.SetHealthToZero();
        }
        foreach (CalculatorManager calculator in UnityEngine.Object.FindObjectsByType<CalculatorManager>(FindObjectsInactive.Include))
            calculator.RefreshExhaustionDisplay();
        saveManager.RequestSaveData();
    }

    private void OnSpeedEdited(string value)
    {
        if (!int.TryParse(value, out int speed) || saveManager == null) return;
        // The user edits the displayed (effective) speed.
        saveManager.GetActiveSceneData().SetInt(BaseSpeedKey, Mathf.Max(0, speed) + SpeedPenaltyPerLevel * Level);
        Apply(Level);
    }
}
