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
    private bool isLastInputDice;
    private CalculatorPotionController potionController;

    private void Start()
    {
        RuntimeLocalization.EnsureExists();
        CalculatorControlBinder controls = new CalculatorControlBinder(transform, NormalizeLabel, IsCalculatorButtonLabel);
        controls.ResolveDisplayTexts(ref equationText, ref resultText);
        if (buttons == null)
            buttons = new List<Button>();
        controls.BindButtons(buttons, OnButtonClick, OnHpButtonClick);
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

    internal string GetCalculatorText(string key)
    {
        return CalculatorTextCatalog.Get(key, RuntimeLocalization.EnsureExists().CurrentLanguage);
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
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void ApplyHpMode(int value)
    {
        ShowHpResult(healthController.Apply(value, FindActiveHealthBar(), GetCalculatorText));
        currentEquation = "";
        isLastInputDice = false;
    }

    internal void ResetHpInputState()
    {
        healthController.Reset();
        currentEquation = "";
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

    private bool IsCalculatorButtonLabel(string label)
    {
        return IsNumberLabel(label) || IsOperator(label) || IsDiceLabel(label) ||
               label == "C" || label == "CE" || label == "=";
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
