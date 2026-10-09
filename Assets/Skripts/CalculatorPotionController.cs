using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns healing-potion selection, counts, and their scene persistence.
public class CalculatorPotionController : MonoBehaviour
{
    private const string PotionSaveKeyPrefix = "PotionCount_";
    private readonly string[] potionFormulas = (string[])AppConfig.Calculator.PotionFormulas.Clone();
    private readonly int[] potionCounts = new int[AppConfig.Calculator.PotionTypeCount];
    private CalculatorManager owner;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private Dropdown potionDropdown;
    private Button potionPlusButton;
    private Button potionMinusButton;
    private Button potionUseButton;

    public void Initialize(CalculatorManager calculator, DndSaveManager dataManager, RuntimeLocalization textCatalog)
    {
        owner = calculator;
        saveManager = dataManager;
        localization = textCatalog;
    }

    public void Wire()
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
        {
            if (item == null)
                continue;

            string objectName = item.gameObject.name;
            if (string.Equals(objectName, "potionDropdown", StringComparison.OrdinalIgnoreCase))
                potionDropdown = item.GetComponent<Dropdown>();
            else if (string.Equals(objectName, "potionPlus", StringComparison.OrdinalIgnoreCase))
                potionPlusButton = item.GetComponent<Button>();
            else if (string.Equals(objectName, "potionMinus", StringComparison.OrdinalIgnoreCase))
                potionMinusButton = item.GetComponent<Button>();
            else if (string.Equals(objectName, "potionUse", StringComparison.OrdinalIgnoreCase))
                potionUseButton = item.GetComponent<Button>();
        }

        if (potionDropdown == null)
            return;

        LoadPotionCounts();
        RefreshPotionDropdownOptions();

        if (potionPlusButton != null)
        {
            potionPlusButton.onClick.RemoveAllListeners();
            potionPlusButton.onClick.AddListener(() => ChangeSelectedPotionCount(1));
        }

        if (potionMinusButton != null)
        {
            potionMinusButton.onClick.RemoveAllListeners();
            potionMinusButton.onClick.AddListener(() => ChangeSelectedPotionCount(-1));
        }

        if (potionUseButton != null)
        {
            potionUseButton.onClick.RemoveAllListeners();
            potionUseButton.onClick.AddListener(UseSelectedPotion);
        }
    }

    private void LoadPotionCounts()
    {
        CharacterSceneData sceneData = GetPotionSceneData(false);
        for (int i = 0; i < potionCounts.Length; i++)
            potionCounts[i] = Mathf.Max(0, sceneData != null ? sceneData.GetInt(PotionSaveKeyPrefix + i, 0) : 0);
    }

    private void SavePotionCounts()
    {
        CharacterSceneData sceneData = GetPotionSceneData(true);
        if (sceneData == null || saveManager == null)
            return;

        for (int i = 0; i < potionCounts.Length; i++)
            sceneData.SetInt(PotionSaveKeyPrefix + i, Mathf.Max(0, potionCounts[i]));

        saveManager.SaveData();
    }

    private CharacterSceneData GetPotionSceneData(bool createIfMissing)
    {
        if (saveManager == null)
            return null;

        CharacterData character = saveManager.EnsureActiveCharacter();
        return character != null ? character.GetSceneData(saveManager.GetActiveSceneDataName(), createIfMissing) : null;
    }

    private void ChangeSelectedPotionCount(int delta)
    {
        int index = GetSelectedPotionIndex();
        if (index < 0)
        {
            owner.ShowHpResult(owner.GetCalculatorText("choosePotion"));
            return;
        }

        potionCounts[index] = Mathf.Max(0, potionCounts[index] + delta);
        SavePotionCounts();
        RefreshPotionDropdownOptions();
    }

    private void UseSelectedPotion()
    {
        owner.CaptureHpTextColorFromButton(potionUseButton);

        int index = GetSelectedPotionIndex();
        if (index < 0)
        {
            owner.ShowHpResult(owner.GetCalculatorText("choosePotion"));
            return;
        }

        if (potionCounts[index] <= 0)
        {
            owner.ShowHpResult(owner.GetCalculatorText("noPotion"));
            return;
        }

        HealthBar healthBar = owner.FindActiveHealthBar();
        if (healthBar == null)
        {
            owner.ShowHpResult(owner.GetCalculatorText("hpBarNotFound"));
            return;
        }

        string rolledExpression = DiceExpressionEvaluator.RollDice(potionFormulas[index]);
        if (!DiceExpressionEvaluator.TryEvaluate(rolledExpression, out double rollResult))
        {
            owner.ShowHpResult(owner.GetCalculatorText("potionError"));
            return;
        }

        int roll = Mathf.Max(0, Mathf.RoundToInt((float)rollResult));
        int healed = healthBar.ApplyHeal(roll);
        potionCounts[index]--;
        SavePotionCounts();
        RefreshPotionDropdownOptions();
        owner.ShowHpResult(owner.GetHealedText(healed) + " (" + potionFormulas[index] + "=" + roll + ")");
        owner.ResetHpInputState();
    }

    private int GetSelectedPotionIndex()
    {
        if (potionDropdown == null)
            return -1;

        if (potionDropdown.value <= 0)
            return -1;

        return Mathf.Clamp(potionDropdown.value - 1, 0, potionCounts.Length - 1);
    }

    public void RefreshPotionDropdownOptions()
    {
        if (potionDropdown == null)
            return;

        int selectedDropdownValue = Mathf.Clamp(potionDropdown.value, 0, potionCounts.Length);
        potionDropdown.options.Clear();
        potionDropdown.options.Add(new Dropdown.OptionData(owner.GetCalculatorText("choosePotion")));
        for (int i = 0; i < potionCounts.Length; i++)
            potionDropdown.options.Add(new Dropdown.OptionData(GetPotionName(i) + " x" + potionCounts[i]));

        potionDropdown.SetValueWithoutNotify(selectedDropdownValue);
        potionDropdown.RefreshShownValue();
    }

    private string GetPotionName(int index)
    {
        AppLanguage language = localization.CurrentLanguage;
        if (language == AppLanguage.English)
        {
            switch (index)
            {
                case 0:
                    return "Potion of Healing";
                case 1:
                    return "Potion of Greater Healing";
                case 2:
                    return "Potion of Superior Healing";
                case 3:
                    return "Potion of Supreme Healing";
            }
        }

        if (language == AppLanguage.Russian)
        {
            switch (index)
            {
                case 0:
                    return "Зелье лечения";
                case 1:
                    return "Большое зелье лечения";
                case 2:
                    return "Улучшенное зелье лечения";
                case 3:
                    return "Высшее зелье лечения";
            }
        }

        switch (index)
        {
            case 0:
                return "Зілля лікування";
            case 1:
                return "Велике зілля лікування";
            case 2:
                return "Покращене зілля лікування";
            case 3:
                return "Найвище зілля лікування";
            default:
                return "Зілля";
        }
    }

}
