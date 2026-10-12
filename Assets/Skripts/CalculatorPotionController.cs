using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns healing-potion selection, counts, and their scene persistence.
public class CalculatorPotionController : MonoBehaviour
{
    private const string PotionSaveKeyPrefix = "PotionCount_";
    private readonly HealingPotionCatalog catalog = new HealingPotionCatalog();
    private int[] potionCounts;
    private CalculatorManager owner;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private Dropdown potionDropdown;
    private Button potionPlusButton;
    private Button potionMinusButton;
    private Button potionUseButton;

    private void Awake()
    {
        potionCounts = new int[catalog.Count];
    }

    internal void Initialize(CalculatorManager calculator, DndSaveManager dataManager, RuntimeLocalization textCatalog)
    {
        owner = calculator;
        saveManager = dataManager;
        localization = textCatalog;
    }

    internal void Wire()
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

        string rolledExpression = DiceExpressionEvaluator.RollDice(catalog.GetFormula(index));
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
        owner.ShowHpResult(owner.GetHealedText(healed) + " (" + catalog.GetFormula(index) + "=" + roll + ")");
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

    internal void RefreshPotionDropdownOptions()
    {
        if (potionDropdown == null)
            return;

        int selectedDropdownValue = Mathf.Clamp(potionDropdown.value, 0, potionCounts.Length);
        potionDropdown.options.Clear();
        potionDropdown.options.Add(new Dropdown.OptionData(owner.GetCalculatorText("choosePotion")));
        for (int i = 0; i < potionCounts.Length; i++)
            potionDropdown.options.Add(new Dropdown.OptionData(catalog.GetName(i, localization.CurrentLanguage) + " x" + potionCounts[i]));

        potionDropdown.SetValueWithoutNotify(selectedDropdownValue);
        potionDropdown.RefreshShownValue();
    }


}
