using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StatConfig
{
    [SerializeField] private InputField statField;
    [SerializeField] private InputField masteryBonusField;
    [SerializeField] private List<InputField> skillFields;
    [SerializeField] private List<Toggle> skillToggles;
    [SerializeField, HideInInspector] private List<bool> manuallyEditedSkills = new List<bool>();

    internal InputField StatField => statField;
    internal InputField MasteryBonusField => masteryBonusField;
    internal int SkillCount => skillFields != null ? skillFields.Count : 0;
    internal IEnumerable<Toggle> SkillToggles => skillToggles ?? (IEnumerable<Toggle>)Array.Empty<Toggle>();

    internal InputField GetSkillField(int index)
    {
        return index >= 0 && index < SkillCount ? skillFields[index] : null;
    }

    internal Toggle GetSkillToggle(int index)
    {
        return skillToggles != null && index >= 0 && index < skillToggles.Count ? skillToggles[index] : null;
    }

    internal void InitializeManualFlags(Func<int, bool> load)
    {
        if (manuallyEditedSkills == null)
            manuallyEditedSkills = new List<bool>();
        manuallyEditedSkills.Clear();
        for (int index = 0; index < SkillCount; index++)
            manuallyEditedSkills.Add(load(index));
    }

    internal void ClearManualFlags(Action<int> persist)
    {
        if (manuallyEditedSkills == null)
            return;
        for (int index = 0; index < manuallyEditedSkills.Count; index++)
        {
            manuallyEditedSkills[index] = false;
            persist(index);
        }
    }

    internal bool IsManuallyEdited(int index)
    {
        return manuallyEditedSkills != null && index >= 0 && index < manuallyEditedSkills.Count && manuallyEditedSkills[index];
    }

    internal bool MarkManuallyEdited(int index)
    {
        if (manuallyEditedSkills == null || index < 0 || index >= manuallyEditedSkills.Count)
            return false;
        manuallyEditedSkills[index] = true;
        return true;
    }
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
            int capturedConfigIndex = configIndex;
            statConfigs[configIndex].InitializeManualFlags(
                skillIndex => LoadManualFlag(capturedConfigIndex, skillIndex));
        }
    }

    private IEnumerator SubscribeAfterUiLoad()
    {
        yield return null;

        for (int configIndex = 0; configIndex < statConfigs.Count; configIndex++)
        {
            StatConfig config = statConfigs[configIndex];
            int capturedConfigIndex = configIndex;

            if (config.StatField != null)
                config.StatField.onEndEdit.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            if (config.MasteryBonusField != null)
                config.MasteryBonusField.onEndEdit.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            foreach (Toggle toggle in config.SkillToggles)
                if (toggle != null)
                    toggle.onValueChanged.AddListener(delegate { OnStatOrBonusChanged(config, capturedConfigIndex); });

            for (int skillIndex = 0; skillIndex < config.SkillCount; skillIndex++)
            {
                int capturedSkillIndex = skillIndex;
                InputField skillField = config.GetSkillField(skillIndex);
                if (skillField != null)
                    skillField.onEndEdit.AddListener(
                        delegate { OnSkillFieldEdited(config, capturedConfigIndex, capturedSkillIndex); });
            }
        }

        listenersReady = true;
    }

    private void OnStatOrBonusChanged(StatConfig config, int configIndex)
    {
        if (!listenersReady)
            return;

        config.ClearManualFlags(skillIndex => SaveManualFlag(configIndex, skillIndex, false));
        UpdateSkills(config);
        SaveDndData();
    }

    private void UpdateSkills(StatConfig config)
    {
        float masteryBonus = 0;
        if (config.MasteryBonusField != null)
            float.TryParse(config.MasteryBonusField.text, out masteryBonus);

        for (int skillIndex = 0; skillIndex < config.SkillCount; skillIndex++)
        {
            if (config.IsManuallyEdited(skillIndex))
                continue;

            InputField skillField = config.GetSkillField(skillIndex);
            if (skillField == null)
                continue;

            float statValue = 0;
            if (config.StatField != null)
                float.TryParse(config.StatField.text, out statValue);

            float skillValue = statValue;
            Toggle skillToggle = config.GetSkillToggle(skillIndex);
            if (skillToggle != null && skillToggle.isOn)
                skillValue += masteryBonus;

            skillField.text = FormatValueWithSign(skillValue);
        }
    }

    private void OnSkillFieldEdited(StatConfig config, int configIndex, int skillIndex)
    {
        if (!listenersReady || !config.MarkManuallyEdited(skillIndex))
            return;

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