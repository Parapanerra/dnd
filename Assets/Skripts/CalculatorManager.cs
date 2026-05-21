using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class CalculatorManager : MonoBehaviour
{
    private const string PotionSaveKeyPrefix = "PotionCount_";

    public List<Button> buttons;
    public Text equationText;
    public Text resultText;

    private readonly string[] potionNames = { "Звичайна", "Велика", "Чудова", "Найвища" };
    private readonly string[] potionFormulas = { "2d4+2", "4d4+4", "8d4+8", "10d4+20" };
    private readonly int[] potionCounts = new int[4];
    private string currentEquation = "";
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
        AssignButtonFunctions();
        AssignHpButtonFunctions();
        AssignPotionControls();
    }

    private void AssignButtonFunctions()
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            Text labelText = button.GetComponentInChildren<Text>();
            if (labelText == null)
                continue;

            string label = labelText.text.Trim();
            button.onClick.AddListener(() => OnButtonClick(label));
        }
    }

    private void AssignHpButtonFunctions()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
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
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        potionCounts[index] = Mathf.Max(0, potionCounts[index] + delta);
        SavePotionCounts();
        RefreshPotionDropdownOptions();
    }

    private void UseSelectedPotion()
    {
        CaptureHpTextColorFromButton(potionUseButton);

        int index = GetSelectedPotionIndex();
        if (potionCounts[index] <= 0)
        {
            ShowHpResult("Немає зілля");
            return;
        }

        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult("HP бар не знайдено");
            return;
        }

        string rolledExpression = ProcessDiceNotation(potionFormulas[index]);
        if (!TryEvaluateExpression(rolledExpression, out double rollResult))
        {
            ShowHpResult("Помилка зілля");
            return;
        }

        int roll = Mathf.Max(0, Mathf.RoundToInt((float)rollResult));
        int healed = healthBar != null ? healthBar.ApplyHeal(roll) : healthBar1.ApplyHeal(roll);
        potionCounts[index]--;
        SavePotionCounts();
        RefreshPotionDropdownOptions();
        ShowHpResult("Зцілено  " + healed + " HP (" + potionFormulas[index] + "=" + roll + ")");
        ResetHpInputState();
    }

    private int GetSelectedPotionIndex()
    {
        if (potionDropdown == null)
            return 0;

        return Mathf.Clamp(potionDropdown.value, 0, potionCounts.Length - 1);
    }

    private void RefreshPotionDropdownOptions()
    {
        if (potionDropdown == null)
            return;

        int selectedIndex = GetSelectedPotionIndex();
        potionDropdown.options.Clear();
        for (int i = 0; i < potionNames.Length; i++)
            potionDropdown.options.Add(new Dropdown.OptionData(potionNames[i] + " x" + potionCounts[i]));

        potionDropdown.SetValueWithoutNotify(selectedIndex);
        potionDropdown.RefreshShownValue();
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
            SetHpMode(HpCalculatorMode.MaxHp, "Max HP:");
            return true;
        }

        if (normalized == "folslive")
        {
            SetHpMode(HpCalculatorMode.TemporaryHp, "Тимч. HP:");
            return true;
        }

        if (normalized == "damage")
        {
            SetHpMode(HpCalculatorMode.Damage, "Урон:");
            return true;
        }

        if (normalized == "heal")
        {
            SetHpMode(HpCalculatorMode.Heal, "Зцілення:");
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

    private void SetHpMode(HpCalculatorMode mode, string label)
    {
        hpMode = mode;
        hpModeLabel = label;
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        if (equationText != null)
            equationText.text = label;
        if (resultText != null)
            resultText.text = "";
        ApplyHpTextColor();
    }

    private void AddNumber(string label)
    {
        if (!string.IsNullOrEmpty(resultText.text))
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
        if (!string.IsNullOrEmpty(resultText.text))
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

        string expression = TrimTrailingOperators(currentEquation);
        if (string.IsNullOrWhiteSpace(expression))
            return;

        expression = ProcessDiceNotation(expression);
        if (!TryEvaluateExpression(expression, out double result))
        {
            resultText.text = "=0";
            return;
        }

        if (hpMode != HpCalculatorMode.None)
        {
            ApplyHpMode(Mathf.RoundToInt((float)result));
            return;
        }

        string formattedResult = FormatNumber(result);
        resultText.text = "=" + formattedResult;
        currentEquation = formattedResult;
        isOperatorClicked = false;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void ApplyHpMode(int value)
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult("HP бар не знайдено");
            hpMode = HpCalculatorMode.None;
            currentEquation = "";
            return;
        }

        value = Mathf.Max(0, value);
        if (hpMode == HpCalculatorMode.MaxHp)
        {
            if (healthBar != null)
                healthBar.SetMaxHealth(value);
            else
                healthBar1.SetMaxHealth(value);

            ShowHpResult("Max HP =  " + value);
        }
        else if (hpMode == HpCalculatorMode.TemporaryHp)
        {
            if (healthBar != null)
                healthBar.SetTemporaryHealth(value);
            else
                healthBar1.SetTemporaryHealth(value);

            ShowHpResult("Тимч. HP =  " + value);
        }
        else if (hpMode == HpCalculatorMode.Damage)
        {
            int applied = healthBar != null ? healthBar.ApplyDamage(value) : healthBar1.ApplyDamage(value);
            ShowHpResult("Отримано  " + applied + " урону");
        }
        else if (hpMode == HpCalculatorMode.Heal)
        {
            int applied = healthBar != null ? healthBar.ApplyHeal(value) : healthBar1.ApplyHeal(value);
            ShowHpResult("Зцілено  " + applied + " HP");
        }

        hpMode = HpCalculatorMode.None;
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private void ApplyLongRest()
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult("HP бар не знайдено");
            return;
        }

        int healed = healthBar != null ? healthBar.RestoreToMaxHealth() : healthBar1.RestoreToMaxHealth();
        ShowHpResult("Зцілено  " + healed + " HP");
        ResetHpInputState();
    }

    private void ApplyShortRest()
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult("HP бар не знайдено");
            return;
        }

        if (healthBar != null)
            healthBar.ClearTemporaryHealth();
        else
            healthBar1.ClearTemporaryHealth();

        if (!TryGetHitDice(out int diceCount, out int diceSides))
        {
            ShowHpResult("Кістки хітів не знайдено");
            ResetHpInputState();
            return;
        }

        int diceToRoll = Mathf.CeilToInt(diceCount / 2f);
        int roll = 0;
        for (int i = 0; i < diceToRoll; i++)
            roll += UnityEngine.Random.Range(1, diceSides + 1);

        int healed = healthBar != null ? healthBar.ApplyHeal(roll) : healthBar1.ApplyHeal(roll);
        ShowHpResult("Зцілено  " + healed + " HP (" + diceToRoll + "d" + diceSides + "=" + roll + ")");
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

    private void ResetCalculator()
    {
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

    private string ProcessDiceNotation(string equation)
    {
        return Regex.Replace(equation, @"(\d*)[dD](\d+)", match =>
        {
            int diceCount = 1;
            if (!string.IsNullOrEmpty(match.Groups[1].Value))
                int.TryParse(match.Groups[1].Value, out diceCount);

            if (!int.TryParse(match.Groups[2].Value, out int diceSides))
                return "0";

            diceCount = Mathf.Clamp(diceCount, 1, 100);
            diceSides = Mathf.Clamp(diceSides, 1, 1000);

            int total = 0;
            for (int i = 0; i < diceCount; i++)
                total += UnityEngine.Random.Range(1, diceSides + 1);

            return total.ToString(CultureInfo.InvariantCulture);
        });
    }

    private bool TryEvaluateExpression(string expression, out double result)
    {
        result = 0;
        expression = expression.Replace(" ", "");

        try
        {
            ExpressionParser parser = new ExpressionParser(expression);
            result = parser.ParseExpression();
            return parser.IsAtEnd && !double.IsNaN(result) && !double.IsInfinity(result);
        }
        catch
        {
            result = 0;
            return false;
        }
    }

    private HealthBar FindActiveHealthBar()
    {
        HealthBar[] bars = UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return bars.Length > 0 ? bars[0] : null;
    }

    private HealthBar1 FindActiveHealthBar1()
    {
        HealthBar1[] bars = UnityEngine.Object.FindObjectsByType<HealthBar1>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

        diceCount = Mathf.Clamp(diceCount, 0, 100);
        diceSides = Mathf.Clamp(diceSides, 1, 1000);
        return diceCount > 0;
    }

    private InputField FindInputFieldByName(params string[] objectNames)
    {
        InputField[] fields = UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        if (equationText == null)
            return;

        equationText.text = hpMode != HpCalculatorMode.None ? hpModeLabel + currentEquation : currentEquation;
    }

    private string FormatNumber(double value)
    {
        if (Math.Abs(value % 1) < 0.000001)
            return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private class ExpressionParser
    {
        private readonly string expression;
        private int index;

        public bool IsAtEnd
        {
            get
            {
                SkipWhitespace();
                return index >= expression.Length;
            }
        }

        public ExpressionParser(string expression)
        {
            this.expression = expression;
        }

        public double ParseExpression()
        {
            double value = ParseTerm();

            while (true)
            {
                SkipWhitespace();
                if (Match('+'))
                    value += ParseTerm();
                else if (Match('-'))
                    value -= ParseTerm();
                else
                    return value;
            }
        }

        private double ParseTerm()
        {
            double value = ParseFactor();

            while (true)
            {
                SkipWhitespace();
                if (Match('*'))
                    value *= ParseFactor();
                else if (Match('/'))
                    value /= ParseFactor();
                else
                    return value;
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();

            if (Match('+'))
                return ParseFactor();

            if (Match('-'))
                return -ParseFactor();

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            int start = index;

            while (index < expression.Length &&
                   (char.IsDigit(expression[index]) || expression[index] == '.'))
            {
                index++;
            }

            if (start == index)
                throw new FormatException("Expected number.");

            string number = expression.Substring(start, index - start);
            return double.Parse(number, CultureInfo.InvariantCulture);
        }

        private bool Match(char symbol)
        {
            if (index >= expression.Length || expression[index] != symbol)
                return false;

            index++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (index < expression.Length && char.IsWhiteSpace(expression[index]))
                index++;
        }
    }
}
