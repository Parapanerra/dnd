using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalculatorManager : MonoBehaviour
{
    private const string PotionSaveKeyPrefix = "PotionCount_";
    private const string EasterEgg67CountKey = "Calculator.EasterEgg67.Count";

    public List<Button> buttons;
    public Text equationText;
    public Text resultText;

    private readonly string[] potionFormulas = (string[])AppConfig.Calculator.PotionFormulas.Clone();
    private readonly int[] potionCounts = new int[AppConfig.Calculator.PotionTypeCount];
    private string currentEquation = "";
    private bool showingEasterEgg;
    private string easterEggTopSource = "";
    private string easterEggMessageSource = "";
    private bool resultBestFitBeforeEasterEgg;
    private int resultMinSizeBeforeEasterEgg;
    private int resultMaxSizeBeforeEasterEgg;
    private string hpModeLabel = "";
    private Color hpTextColor = Color.white;
    private bool hasHpTextColor;
    private bool isOperatorClicked;
    private bool isLastInputDice;
    private Dropdown potionDropdown;
    private Button potionPlusButton;
    private Button potionMinusButton;
    private Button potionUseButton;
    private HpCalculatorMode hpMode = HpCalculatorMode.None;

    private enum HpCalculatorMode
    {
        None,
        MaxHp,
        TemporaryHp,
        Damage,
        Heal
    }

    private void Start()
    {
        RuntimeLocalization.EnsureExists();
        EnsureDisplayTexts();
        AssignButtonFunctions();
        AssignHpButtonFunctions();
        AssignPotionControls();
    }

    public void RefreshLocalization()
    {
        if (hpMode != HpCalculatorMode.None)
        {
            hpModeLabel = GetHpModeLabel(hpMode);
            RefreshEquationText();
        }

        RefreshPotionDropdownOptions();
        RefreshEquationText();
        if (showingEasterEgg)
            RefreshEasterEggText();
    }

    public void RefreshExhaustionDisplay()
    {
        RefreshEquationText();
    }

    private void EnsureDisplayTexts()
    {
        if (equationText != null && resultText != null)
            return;

        Transform searchRoot = FindCalculatorRoot();
        List<Text> displayTexts = new List<Text>();
        foreach (Text text in searchRoot.GetComponentsInChildren<Text>(true))
        {
            if (text == null || IsInsideInteractiveControl(text.transform) || IsStaticCalculatorLabel(text.text))
                continue;

            displayTexts.Add(text);
        }

        displayTexts.Sort(CompareDisplayTextCandidates);

        if (equationText == null && displayTexts.Count > 0)
            equationText = displayTexts[0];

        if (resultText == null)
            resultText = displayTexts.Count > 1 ? displayTexts[1] : equationText;
    }

    private int CompareDisplayTextCandidates(Text left, Text right)
    {
        bool leftEmpty = left == null || string.IsNullOrWhiteSpace(left.text);
        bool rightEmpty = right == null || string.IsNullOrWhiteSpace(right.text);
        if (leftEmpty != rightEmpty)
            return leftEmpty ? -1 : 1;

        return right.rectTransform.position.y.CompareTo(left.rectTransform.position.y);
    }

    private void AssignButtonFunctions()
    {
        EnsureCalculatorButtons();

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string label = GetButtonLabel(button);
            if (string.IsNullOrEmpty(label))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClick(label));
        }
    }

    private void EnsureCalculatorButtons()
    {
        if (buttons == null)
            buttons = new List<Button>();

        buttons.Clear();
        Transform searchRoot = FindCalculatorRoot();
        foreach (Button button in searchRoot.GetComponentsInChildren<Button>(true))
        {
            if (button == null || IsSpecialCalculatorButton(button))
                continue;

            string label = NormalizeLabel(GetButtonLabel(button));
            if (IsNumberLabel(label) || IsOperator(label) || IsDiceLabel(label) || label == "C" || label == "CE" || label == "=")
                buttons.Add(button);
        }
    }

    private Transform FindCalculatorRoot()
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, "kalPanel", StringComparison.OrdinalIgnoreCase))
                return current;

            current = current.parent;
        }

        return transform.parent != null ? transform.parent : transform;
    }

    private string GetButtonLabel(Button button)
    {
        if (button == null)
            return "";

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
            return labelText.text.Trim();

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        return tmpText != null ? tmpText.text.Trim() : "";
    }

    private bool IsInsideInteractiveControl(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.GetComponent<Button>() != null ||
                current.GetComponent<Dropdown>() != null ||
                current.GetComponent<TMP_Dropdown>() != null ||
                current.GetComponent<InputField>() != null ||
                current.GetComponent<TMP_InputField>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsStaticCalculatorLabel(string text)
    {
        string label = NormalizeLabel(text).ToLowerInvariant();
        return string.IsNullOrEmpty(label) == false &&
               (label.Contains("калькулятор") ||
                label.Contains("calculator") ||
                label.Contains("меню") ||
                label.Contains("menu") ||
                label.Contains("шкода") ||
                label.Contains("урон") ||
                label.Contains("damage") ||
                label.Contains("зцілення") ||
                label.Contains("heal") ||
                label.Contains("маххп") ||
                label.Contains("maxhp") ||
                label.Contains("відпочинок") ||
                label.Contains("rest") ||
                label.Contains("випити") ||
                label.Contains("зілля") ||
                label.Contains("псевдожиття"));
    }

    private bool IsSpecialCalculatorButton(Button button)
    {
        string buttonName = NormalizeLabel(button.gameObject.name).ToLowerInvariant();
        return IsHpButtonName(buttonName) ||
               buttonName == "potionplus" ||
               buttonName == "potionminus" ||
               buttonName == "potionuse";
    }

    private void AssignHpButtonFunctions()
    {
        Transform searchRoot = FindCalculatorRoot();
        Button[] calculatorButtons = searchRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in calculatorButtons)
        {
            if (button == null)
                continue;

            string buttonName = NormalizeLabel(button.gameObject.name);
            if (!IsHpButtonName(buttonName))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnHpButtonClick(buttonName, button));
        }
    }

    private void AssignPotionControls()
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
        if (sceneData == null || DndSaveManager.Instance == null)
            return;

        for (int i = 0; i < potionCounts.Length; i++)
            sceneData.SetInt(PotionSaveKeyPrefix + i, Mathf.Max(0, potionCounts[i]));

        DndSaveManager.Instance.SaveData();
    }

    private CharacterSceneData GetPotionSceneData(bool createIfMissing)
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
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
            ShowHpResult(GetCalculatorText("choosePotion"));
            return;
        }

        potionCounts[index] = Mathf.Max(0, potionCounts[index] + delta);
        SavePotionCounts();
        RefreshPotionDropdownOptions();
    }

    private void UseSelectedPotion()
    {
        CaptureHpTextColorFromButton(potionUseButton);

        int index = GetSelectedPotionIndex();
        if (index < 0)
        {
            ShowHpResult(GetCalculatorText("choosePotion"));
            return;
        }

        if (potionCounts[index] <= 0)
        {
            ShowHpResult(GetCalculatorText("noPotion"));
            return;
        }

        HealthBar healthBar = FindActiveHealthBar();
        if (healthBar == null)
        {
            ShowHpResult(GetCalculatorText("hpBarNotFound"));
            return;
        }

        string rolledExpression = DiceExpressionEvaluator.RollDice(potionFormulas[index]);
        if (!DiceExpressionEvaluator.TryEvaluate(rolledExpression, out double rollResult))
        {
            ShowHpResult(GetCalculatorText("potionError"));
            return;
        }

        int roll = Mathf.Max(0, Mathf.RoundToInt((float)rollResult));
        int healed = healthBar.ApplyHeal(roll);
        potionCounts[index]--;
        SavePotionCounts();
        RefreshPotionDropdownOptions();
        ShowHpResult(GetHealedText(healed) + " (" + potionFormulas[index] + "=" + roll + ")");
        ResetHpInputState();
    }

    private int GetSelectedPotionIndex()
    {
        if (potionDropdown == null)
            return -1;

        if (potionDropdown.value <= 0)
            return -1;

        return Mathf.Clamp(potionDropdown.value - 1, 0, potionCounts.Length - 1);
    }

    private void RefreshPotionDropdownOptions()
    {
        if (potionDropdown == null)
            return;

        int selectedDropdownValue = Mathf.Clamp(potionDropdown.value, 0, potionCounts.Length);
        potionDropdown.options.Clear();
        potionDropdown.options.Add(new Dropdown.OptionData(GetCalculatorText("choosePotion")));
        for (int i = 0; i < potionCounts.Length; i++)
            potionDropdown.options.Add(new Dropdown.OptionData(GetPotionName(i) + " x" + potionCounts[i]));

        potionDropdown.SetValueWithoutNotify(selectedDropdownValue);
        potionDropdown.RefreshShownValue();
    }

    private string GetPotionName(int index)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
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

    private string GetCalculatorText(string key)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
        bool english = language == AppLanguage.English;
        bool russian = language == AppLanguage.Russian;

        switch (key)
        {
            case "choosePotion":
                return english ? "Choose potion" : russian ? "Выберите зелье" : "Оберіть зілля";
            case "noPotion":
                return english ? "No potion" : russian ? "Нет зелья" : "Немає зілля";
            case "potionError":
                return english ? "Potion error" : russian ? "Ошибка зелья" : "Помилка зілля";
            case "hpBarNotFound":
                return english ? "HP bar not found" : russian ? "HP бар не найден" : "HP бар не знайдено";
            case "tempHp":
                return english ? "Temp HP" : russian ? "Врем. HP" : "Тимч. HP";
            case "damageDone":
                return english ? "Damage taken" : russian ? "Получено урона" : "Отримано урону";
            case "healed":
                return english ? "Healed" : russian ? "Исцелено" : "Зцілено";
            case "longRest":
                return english ? "Long rest" : russian ? "Долгий отдых" : "Довгий відпочинок";
            case "shortRest":
                return english ? "Short rest" : russian ? "Короткий отдых" : "Короткий відпочинок";
            case "hitDiceNotFound":
                return english ? "Hit dice not found" : russian ? "Кости хитов не найдены" : "Кістки хітів не знайдено";
        }

        return key;
    }

    private string GetHpModeLabel(HpCalculatorMode mode)
    {
        switch (mode)
        {
            case HpCalculatorMode.MaxHp:
                return "Max HP:";
            case HpCalculatorMode.TemporaryHp:
                return GetCalculatorText("tempHp") + ":";
            case HpCalculatorMode.Damage:
                return RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English ? "Damage:" :
                    RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.Russian ? "Урон:" : "Урон:";
            case HpCalculatorMode.Heal:
                return RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English ? "Healing:" :
                    RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.Russian ? "Лечение:" : "Зцілення:";
            default:
                return "";
        }
    }

    private string GetHealedText(int value)
    {
        return GetCalculatorText("healed") + "  " + value + " HP";
    }

    private string GetDamageText(int value)
    {
        return GetCalculatorText("damageDone") + "  " + value;
    }

    private void OnHpButtonClick(string buttonName, Button button)
    {
        CaptureHpTextColorFromButton(button);

        OnButtonClick(buttonName);
    }

    private void CaptureHpTextColorFromButton(Button button)
    {
        Text buttonText = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (buttonText != null)
        {
            hpTextColor = buttonText.color;
            hasHpTextColor = true;
            ApplyHpTextColor();
        }
    }

    private void OnButtonClick(string rawLabel)
    {
        string label = NormalizeLabel(rawLabel);
        if (string.IsNullOrEmpty(label))
            return;

        if (HandleHpModeButton(label))
            return;

        if (label == "C")
        {
            ResetCalculator();
            return;
        }

        if (label == "CE")
        {
            ClearEntry();
            return;
        }

        if (label == "=")
        {
            CalculateResult();
            return;
        }

        if (IsOperator(label))
        {
            AddOperator(label[0]);
            return;
        }

        if (IsDiceLabel(label))
        {
            AddDice(label);
            return;
        }

        if (IsNumberLabel(label))
        {
            AddNumber(label);
        }
    }

    private string NormalizeLabel(string label)
    {
        if (label == "×" || label == "x" || label == "X")
            return "*";

        if (label == "÷")
            return "/";

        return label.Replace(" ", "");
    }

    private bool HandleHpModeButton(string label)
    {
        string normalized = label.ToLowerInvariant();
        if (normalized == "maxhp")
        {
            SetHpMode(HpCalculatorMode.MaxHp);
            return true;
        }

        if (normalized == "folslive")
        {
            SetHpMode(HpCalculatorMode.TemporaryHp);
            return true;
        }

        if (normalized == "damage")
        {
            SetHpMode(HpCalculatorMode.Damage);
            return true;
        }

        if (normalized == "heal")
        {
            SetHpMode(HpCalculatorMode.Heal);
            return true;
        }

        if (normalized == "shortrest")
        {
            ApplyShortRest();
            return true;
        }

        if (normalized == "longrest")
        {
            ApplyLongRest();
            return true;
        }

        return false;
    }

    private bool IsHpButtonName(string label)
    {
        string normalized = label.ToLowerInvariant();
        return normalized == "maxhp" ||
               normalized == "damage" ||
               normalized == "heal" ||
               normalized == "folslive" ||
               normalized == "shortrest" ||
               normalized == "longrest";
    }

    private void SetHpMode(HpCalculatorMode mode)
    {
        if (showingEasterEgg)
            ResetCalculator();
        hpMode = mode;
        hpModeLabel = GetHpModeLabel(mode);
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        if (equationText != null)
            equationText.text = hpModeLabel;
        if (resultText != null)
            resultText.text = "";
        ApplyHpTextColor();
    }

    private void AddNumber(string label)
    {
        if (HasDisplayedResult())
            ResetCalculator();

        currentEquation += label;
        isOperatorClicked = false;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void AddOperator(char operatorChar)
    {
        if (currentEquation.Length == 0)
        {
            if (operatorChar == '-')
            {
                currentEquation = "-";
                RefreshEquationText();
            }

            return;
        }

        if (IsLastCharOperator())
        {
            currentEquation = currentEquation.Substring(0, currentEquation.Length - 1) + operatorChar;
        }
        else
        {
            currentEquation += operatorChar;
        }

        isOperatorClicked = true;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void AddDice(string label)
    {
        if (HasDisplayedResult())
            ResetCalculator();

        if (isLastInputDice)
            return;

        currentEquation += label.ToLowerInvariant();
        isOperatorClicked = false;
        isLastInputDice = true;
        RefreshEquationText();
    }

    private void CalculateResult()
    {
        if (string.IsNullOrWhiteSpace(currentEquation))
            return;

        if (TryShowEasterEgg())
            return;

        string expression = TrimTrailingOperators(currentEquation);
        if (string.IsNullOrWhiteSpace(expression))
            return;

        int exhaustionPenalty = hpMode == HpCalculatorMode.None && ExhaustionEffects.IsD20Roll(expression)
            ? 2 * ExhaustionEffects.Level : 0;
        expression = DiceExpressionEvaluator.RollDice(expression);
        if (!DiceExpressionEvaluator.TryEvaluate(expression, out double result))
        {
            if (resultText != null)
                resultText.text = "=0";
            return;
        }

        if (hpMode != HpCalculatorMode.None)
        {
            ApplyHpMode(Mathf.RoundToInt((float)result));
            return;
        }

        result -= exhaustionPenalty;
        string formattedResult = FormatNumber(result);
        if (resultText != null)
            resultText.text = "=" + formattedResult + (exhaustionPenalty > 0 ? " (" + ExhaustionEffects.PenaltyLabel(exhaustionPenalty).Trim() + ")" : "");
        currentEquation = formattedResult;
        isOperatorClicked = false;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private bool TryShowEasterEgg()
    {
        if (hpMode != HpCalculatorMode.None)
            return false;

        string topLine = "";
        string message;
        switch (currentEquation)
        {
            case "67":
                message = AdvanceResetEasterEgg();
                break;
            case "4221":
                message = "Раз, два, три, прийом! Ця штука працює?";
                break;
            case "666":
                message = "О, так!!!";
                break;
            case "69":
                message = "Нааайс";
                break;
            case "1984":
                topLine = "Це як у 1984";
                message = "Але я не читав";
                break;
            default:
                return false;
        }

        ResetCalculator();
        easterEggTopSource = topLine;
        easterEggMessageSource = message;
        if (resultText != null)
        {
            showingEasterEgg = true;
            resultBestFitBeforeEasterEgg = resultText.resizeTextForBestFit;
            resultMinSizeBeforeEasterEgg = resultText.resizeTextMinSize;
            resultMaxSizeBeforeEasterEgg = resultText.resizeTextMaxSize;
            resultText.resizeTextForBestFit = true;
            resultText.resizeTextMinSize = 10;
            resultText.resizeTextMaxSize = Mathf.Max(10, resultText.fontSize);
        }
        RefreshEasterEggText();
        return true;
    }

    private void RefreshEasterEggText()
    {
        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        string topLine = localization.Translate(easterEggTopSource);
        string message = localization.Translate(easterEggMessageSource);
        if (equationText != null)
            equationText.text = topLine;
        if (resultText != null)
            resultText.text = resultText == equationText && topLine.Length > 0
                ? topLine + "\n" + message : message;
    }

    private string AdvanceResetEasterEgg()
    {
        DndSaveManager saveManager = DndSaveManager.Instance;
        CharacterData character = saveManager != null ? saveManager.GetActiveCharacter() : null;
        if (character == null)
            return "Спочатку обери персонажа.";

        int.TryParse(character.GetSharedString(EasterEgg67CountKey, "0"), out int count);
        count = Mathf.Clamp(count, 0, 2) + 1;
        if (count == 3)
        {
            // Call exactly the same handler as the scene's Reset button.
            CharacterSheetManagerScene1 sheet = UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
            CharacterSceneAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<CharacterSceneAutoSave>();
            if (sheet != null)
                sheet.ResetSceneData();
            else if (autoSave != null)
                autoSave.ResetSceneData();
            else
                return "Не вдалося знайти Reset для цього листа.";
        }

        character.SetSharedString(EasterEgg67CountKey, (count == 3 ? 0 : count).ToString(CultureInfo.InvariantCulture));
        saveManager.SaveData();
        switch (count)
        {
            case 1: return "Якщо ти ще раз це введеш, я тобі видалю персонажа.";
            case 2: return "Я взагалі-то серйозно.";
            default: return "Я попереджував.";
        }
    }

    private void ApplyHpMode(int value)
    {
        HealthBar healthBar = FindActiveHealthBar();
        if (healthBar == null)
        {
            ShowHpResult(GetCalculatorText("hpBarNotFound"));
            hpMode = HpCalculatorMode.None;
            currentEquation = "";
            return;
        }

        value = Mathf.Max(0, value);
        if (hpMode == HpCalculatorMode.MaxHp)
        {
            healthBar.SetMaxHealthAndFill(value);

            ShowHpResult("Max HP =  " + value);
        }
        else if (hpMode == HpCalculatorMode.TemporaryHp)
        {
            healthBar.SetTemporaryHealth(value);

            ShowHpResult(GetCalculatorText("tempHp") + " =  " + value);
        }
        else if (hpMode == HpCalculatorMode.Damage)
        {
            int applied = healthBar.ApplyDamage(value);
            ShowHpResult(GetDamageText(applied));
        }
        else if (hpMode == HpCalculatorMode.Heal)
        {
            int applied = healthBar.ApplyHeal(value);
            ShowHpResult(GetHealedText(applied));
        }

        hpMode = HpCalculatorMode.None;
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private void ApplyLongRest()
    {
        HealthBar healthBar = FindActiveHealthBar();

        int healed = 0;
        if (healthBar != null)
            healed = healthBar.RestoreToMaxHealth();

        CharacterRestService.Apply(true);
        ShowHpResult(healthBar != null ? GetHealedText(healed) : GetCalculatorText("longRest"));
        ResetHpInputState();
    }

    private void ApplyShortRest()
    {
        HealthBar healthBar = FindActiveHealthBar();

        if (healthBar != null)
            healthBar.ClearTemporaryHealth();

        CharacterRestService.Apply(false);

        if (healthBar == null)
        {
            ShowHpResult(GetCalculatorText("shortRest"));
            ResetHpInputState();
            return;
        }

        if (!TryGetHitDice(out int diceCount, out int diceSides))
        {
            ShowHpResult(GetCalculatorText("hitDiceNotFound"));
            ResetHpInputState();
            return;
        }

        int diceToRoll = Mathf.CeilToInt(diceCount / AppConfig.Calculator.ShortRestDiceDivisor);
        int roll = 0;
        for (int i = 0; i < diceToRoll; i++)
            roll += UnityEngine.Random.Range(1, diceSides + 1);

        int healed = healthBar.ApplyHeal(roll);
        ShowHpResult(GetHealedText(healed) + " (" + diceToRoll + "d" + diceSides + "=" + roll + ")");
        ResetHpInputState();
    }

    private void ResetHpInputState()
    {
        hpMode = HpCalculatorMode.None;
        hpModeLabel = "";
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private bool HasDisplayedResult()
    {
        return resultText != null && resultText != equationText && !string.IsNullOrEmpty(resultText.text);
    }

    private void ResetCalculator()
    {
        if (showingEasterEgg && resultText != null)
        {
            resultText.resizeTextForBestFit = resultBestFitBeforeEasterEgg;
            resultText.resizeTextMinSize = resultMinSizeBeforeEasterEgg;
            resultText.resizeTextMaxSize = resultMaxSizeBeforeEasterEgg;
        }
        showingEasterEgg = false;
        easterEggTopSource = "";
        easterEggMessageSource = "";
        currentEquation = "";
        hpModeLabel = "";
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        hpMode = HpCalculatorMode.None;
    }

    private void ClearEntry()
    {
        if (currentEquation.Length == 0)
            return;

        currentEquation = currentEquation.Substring(0, currentEquation.Length - 1);
        RecalculateInputFlags();
        RefreshEquationText();
    }

    private HealthBar FindActiveHealthBar()
    {
        HealthBar[] bars = UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Exclude);
        foreach (HealthBar bar in bars)
            if (bar != null && bar.IsUsableForCalculator)
                return bar;

        return bars.Length > 0 ? bars[0] : null;
    }

    private bool TryGetHitDice(out int diceCount, out int diceSides)
    {
        diceCount = 0;
        diceSides = 0;

        InputField allDiceField = FindInputFieldByName("alldise", "alldaise");
        InputField diceValueField = FindInputFieldByName("daicevalueperson");
        if (allDiceField == null || diceValueField == null)
            return false;

        if (!int.TryParse(ExtractFirstNumber(allDiceField.text), out diceCount))
            return false;

        if (!int.TryParse(ExtractFirstNumber(diceValueField.text), out diceSides))
            return false;

        diceCount = Mathf.Clamp(diceCount, 0, AppConfig.Calculator.MaximumDiceCount);
        diceSides = Mathf.Clamp(
            diceSides,
            AppConfig.Calculator.MinimumDiceSides,
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
            {
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
            }
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;

            foreach (string objectName in objectNames)
            {
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
            }
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;

            foreach (string objectName in objectNames)
            {
                if (field.gameObject.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return field;
            }
        }

        return null;
    }

    private void ApplyHpTextColor()
    {
        if (!hasHpTextColor)
            return;

        if (equationText != null)
            equationText.color = hpTextColor;

        if (resultText != null)
            resultText.color = hpTextColor;
    }

    private void ShowHpResult(string message)
    {
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = message;
        ApplyHpTextColor();
    }

    private string ExtractFirstNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        Match match = Regex.Match(value, @"\d+");
        return match.Success ? match.Value : "";
    }

    private bool IsNumberLabel(string label)
    {
        return double.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private bool IsDiceLabel(string label)
    {
        return Regex.IsMatch(label, @"^[dD]\d+$");
    }

    private bool IsOperator(string label)
    {
        return label.Length == 1 && "+-*/".Contains(label);
    }

    private bool IsLastCharOperator()
    {
        return currentEquation.Length > 0 && "+-*/".Contains(currentEquation[currentEquation.Length - 1].ToString());
    }

    private string TrimTrailingOperators(string expression)
    {
        while (expression.Length > 0 && "+-*/".Contains(expression[expression.Length - 1].ToString()))
            expression = expression.Substring(0, expression.Length - 1);

        return expression;
    }

    private void RecalculateInputFlags()
    {
        isOperatorClicked = IsLastCharOperator();
        isLastInputDice = Regex.IsMatch(currentEquation, @"[dD]\d+$");
    }

    private void RefreshEquationText()
    {
        if (equationText == null || showingEasterEgg)
            return;

        equationText.text = hpMode != HpCalculatorMode.None ? hpModeLabel + currentEquation : currentEquation;
        if (hpMode == HpCalculatorMode.None && ExhaustionEffects.IsD20Roll(currentEquation) && ExhaustionEffects.Level > 0)
            equationText.text += ExhaustionEffects.PenaltyLabel(2 * ExhaustionEffects.Level);
    }

    private string FormatNumber(double value)
    {
        if (Math.Abs(value % 1) < 0.000001)
            return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

}
