using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalculatorManager : MonoBehaviour
{
    public List<Button> buttons;
    public Text equationText;
    public Text resultText;

    private string currentEquation = "";
    private readonly CalculatorEasterEgg easterEgg = new CalculatorEasterEgg();
    private readonly CalculatorHealthController healthController = new CalculatorHealthController();
    private bool isOperatorClicked;
    private bool isLastInputDice;
    private CalculatorPotionController potionController;

    private void Start()
    {
        RuntimeLocalization.EnsureExists();
        EnsureDisplayTexts();
        AssignButtonFunctions();
        AssignHpButtonFunctions();
        potionController = GetComponent<CalculatorPotionController>();
        if (potionController == null)
            potionController = gameObject.AddComponent<CalculatorPotionController>();
        potionController.Initialize(this);
        potionController.Wire();
    }

    public void RefreshLocalization()
    {
        if (healthController.IsActive)
            healthController.RefreshLabel(GetCalculatorText, RuntimeLocalization.EnsureExists().CurrentLanguage);

        if (potionController != null)
            potionController.RefreshPotionDropdownOptions();
        RefreshEquationText();
        if (easterEgg.IsShowing)
            easterEgg.Refresh();
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
        return CalculatorHealthController.IsButtonName(buttonName) ||
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
            if (!CalculatorHealthController.IsButtonName(buttonName))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnHpButtonClick(buttonName, button));
        }
    }

    internal string GetCalculatorText(string key)
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

    internal string GetHealedText(int value)
    {
        return CalculatorHealthController.FormatHealed(value, GetCalculatorText);
    }

    private void OnHpButtonClick(string buttonName, Button button)
    {
        CaptureHpTextColorFromButton(button);

        OnButtonClick(buttonName);
    }

    internal void CaptureHpTextColorFromButton(Button button)
    {
        healthController.CaptureTextColor(button);
        healthController.ApplyTextColor(equationText, resultText);
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
        if (normalized == "shortrest")
        {
            ShowHpResult(healthController.ApplyShortRest(FindActiveHealthBar(), GetCalculatorText));
            ResetHpInputState();
            return true;
        }

        if (normalized == "longrest")
        {
            ShowHpResult(healthController.ApplyLongRest(FindActiveHealthBar(), GetCalculatorText));
            ResetHpInputState();
            return true;
        }

        if (!CalculatorHealthController.IsButtonName(normalized))
            return false;
        if (easterEgg.IsShowing)
            ResetCalculator();
        healthController.TrySelectMode(normalized, GetCalculatorText,
            RuntimeLocalization.EnsureExists().CurrentLanguage);
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        if (equationText != null)
            equationText.text = healthController.ModeLabel;
        if (resultText != null)
            resultText.text = "";
        healthController.ApplyTextColor(equationText, resultText);
        return true;
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

        if (!healthController.IsActive && easterEgg.TryShow(currentEquation, equationText, resultText, ResetCalculator))
            return;

        string expression = TrimTrailingOperators(currentEquation);
        if (string.IsNullOrWhiteSpace(expression))
            return;

        int exhaustionPenalty = !healthController.IsActive && ExhaustionEffects.IsD20Roll(expression)
            ? 2 * ExhaustionEffects.Level : 0;
        expression = DiceExpressionEvaluator.RollDice(expression);
        if (!DiceExpressionEvaluator.TryEvaluate(expression, out double result))
        {
            if (resultText != null)
                resultText.text = "=0";
            return;
        }

        if (healthController.IsActive)
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

    private void ApplyHpMode(int value)
    {
        ShowHpResult(healthController.Apply(value, FindActiveHealthBar(), GetCalculatorText));
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    internal void ResetHpInputState()
    {
        healthController.Reset();
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
        easterEgg.ResetDisplay();
        currentEquation = "";
        healthController.Reset();
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private void ClearEntry()
    {
        if (currentEquation.Length == 0)
            return;

        currentEquation = currentEquation.Substring(0, currentEquation.Length - 1);
        RecalculateInputFlags();
        RefreshEquationText();
    }

    internal HealthBar FindActiveHealthBar()
    {
        return CalculatorHealthController.FindActiveHealthBar();
    }

    internal void ShowHpResult(string message)
    {
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = message;
        healthController.ApplyTextColor(equationText, resultText);
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
        if (equationText == null || easterEgg.IsShowing)
            return;

        equationText.text = healthController.IsActive ? healthController.ModeLabel + currentEquation : currentEquation;
        if (!healthController.IsActive && ExhaustionEffects.IsD20Roll(currentEquation) && ExhaustionEffects.Level > 0)
            equationText.text += ExhaustionEffects.PenaltyLabel(2 * ExhaustionEffects.Level);
    }

    private string FormatNumber(double value)
    {
        if (Math.Abs(value % 1) < 0.000001)
            return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

}
