using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StatConfig
{
    [SerializeField] internal InputField statField;
    [SerializeField] internal InputField masteryBonusField;
    [SerializeField] internal List<InputField> skillFields;
    [SerializeField] internal List<Toggle> skillToggles;

    [SerializeField, HideInInspector]
    internal List<bool> manuallyEditedSkills = new List<bool>();
}

public class StatManager : MonoBehaviour
{
    [SerializeField] private List<StatConfig> statConfigs;

    private bool listenersReady;
    private DndSaveManager saveManager;

    private void Start()
    {
        saveManager = DndSaveManager.EnsureExists();
        InitializeManualFlags();
        StartCoroutine(SubscribeAfterUiLoad());
    }

    private void InitializeManualFlags()
    {
        for (int configIndex = 0; configIndex < statConfigs.Count; configIndex++)
        {
            StatConfig config = statConfigs[configIndex];
            config.manuallyEditedSkills.Clear();

            for (int skillIndex = 0; skillIndex < config.skillFields.Count; skillIndex++)
                config.manuallyEditedSkills.Add(LoadManualFlag(configIndex, skillIndex));
        }
    }

    private IEnumerator SubscribeAfterUiLoad()
    {
        yield return null;

        for (int configIndex = 0; configIndex < statConfigs.Count; configIndex++)
        {
            StatConfig config = statConfigs[configIndex];
            int capturedConfigIndex = configIndex;

            if (config.statField != null)
                config.statField.onEndEdit.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            if (config.masteryBonusField != null)
                config.masteryBonusField.onEndEdit.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            foreach (Toggle toggle in config.skillToggles)
                if (toggle != null)
                    toggle.onValueChanged.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            for (int skillIndex = 0; skillIndex < config.skillFields.Count; skillIndex++)
            {
                int capturedSkillIndex = skillIndex;
                if (config.skillFields[capturedSkillIndex] != null)
                {
                    config.skillFields[capturedSkillIndex].onEndEdit.AddListener(
                        delegate { OnSkillFieldEdited(config, capturedConfigIndex, capturedSkillIndex); });
                }
            }
        }

        listenersReady = true;
    }

    private void OnStatOrBonusChanged(StatConfig config, int configIndex)
    {
        if (!listenersReady)
            return;

        for (int skillIndex = 0; skillIndex < config.manuallyEditedSkills.Count; skillIndex++)
        {
            config.manuallyEditedSkills[skillIndex] = false;
            SaveManualFlag(configIndex, skillIndex, false);
        }

        UpdateSkills(config);
        SaveDndData();
    }

    private void UpdateSkills(StatConfig config)
    {
        float masteryBonus = 0;
        if (config.masteryBonusField != null)
            float.TryParse(config.masteryBonusField.text, out masteryBonus);

        for (int skillIndex = 0; skillIndex < config.skillFields.Count; skillIndex++)
        {
            if (skillIndex < config.manuallyEditedSkills.Count && config.manuallyEditedSkills[skillIndex])
                continue;

            if (config.skillFields[skillIndex] == null)
                continue;

            float statValue = 0;
            if (config.statField != null)
                float.TryParse(config.statField.text, out statValue);

            float skillValue = statValue;
            if (skillIndex < config.skillToggles.Count &&
                config.skillToggles[skillIndex] != null &&
                config.skillToggles[skillIndex].isOn)
            {
                skillValue += masteryBonus;
            }

            config.skillFields[skillIndex].text = FormatValueWithSign(skillValue);
        }
    }

    private void OnSkillFieldEdited(StatConfig config, int configIndex, int skillIndex)
    {
        if (!listenersReady)
            return;

        if (skillIndex >= config.manuallyEditedSkills.Count)
            return;

        config.manuallyEditedSkills[skillIndex] = true;
        SaveManualFlag(configIndex, skillIndex, true);
        SaveDndData();
    }

    private bool LoadManualFlag(int configIndex, int skillIndex)
    {
        if (saveManager == null)
            return false;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData();
        return sceneData.GetInt(GetManualFlagKey(configIndex, skillIndex), 0) == 1;
    }

    private void SaveManualFlag(int configIndex, int skillIndex, bool value)
    {
        if (saveManager == null)
            return;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData();
        sceneData.SetInt(GetManualFlagKey(configIndex, skillIndex), value ? 1 : 0);
    }

    private string GetManualFlagKey(int configIndex, int skillIndex)
    {
        return "StatManager.ManualSkill." + configIndex + "." + skillIndex;
    }

    private void SaveDndData()
    {
        if (saveManager != null)
            saveManager.RequestSaveData();
    }

    private string FormatValueWithSign(float value)
    {
        if (value > 0)
            return "+" + value;

        if (value < 0)
            return value.ToString();

        return "0";
    }
}
