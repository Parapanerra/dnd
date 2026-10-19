using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalculatorManager : MonoBehaviour
{
    [SerializeField] private List<Button> buttons;
    [SerializeField] private Text equationText;
    [SerializeField] private Text resultText;

    private readonly CalculatorExpressionInput expressionInput = new CalculatorExpressionInput();
    private readonly CalculatorTextCatalog textCatalog = new CalculatorTextCatalog();
    private CalculatorResultEvaluator resultEvaluator;
    private CalculatorEasterEgg easterEgg;
    private readonly CalculatorHealthController healthController = new CalculatorHealthController();
    private CalculatorPotionController potionController;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private ExhaustionEffects exhaustionEffects;

    private void Start()
    {
        localization = new RuntimeManagerResolver().Localization();
        saveManager = new RuntimeManagerResolver().SaveManager();
        healthController.Initialize(new CharacterRestService(saveManager));
        easterEgg = new CalculatorEasterEgg(localization, saveManager);
        exhaustionEffects = new ExhaustionEffects(saveManager);
        resultEvaluator = new CalculatorResultEvaluator(() => exhaustionEffects.Level);
        CalculatorControlBinder controls = new CalculatorControlBinder(transform, expressionInput.NormalizeLabel,
            expressionInput.IsCalculatorButtonLabel, healthController.IsButtonName);
        controls.ResolveDisplayTexts(ref equationText, ref resultText);
        if (buttons == null)
            buttons = new List<Button>();
        controls.BindButtons(buttons, OnButtonClick, OnHpButtonClick);
        potionController = GetComponent<CalculatorPotionController>();
        if (potionController == null)
            potionController = gameObject.AddComponent<CalculatorPotionController>();
        potionController.Initialize(this, saveManager, localization);
        potionController.Wire();
    }

    internal void RefreshLocalization()
    {
        if (localization == null || easterEgg == null)
            return;

        if (healthController.IsActive)
            healthController.RefreshLabel(GetCalculatorText, localization.CurrentLanguage);

        if (potionController != null)
            potionController.RefreshPotionDropdownOptions();
        RefreshEquationText();
        if (easterEgg.IsShowing)
            easterEgg.Refresh();
    }

    internal void RefreshExhaustionDisplay()
    {
        RefreshEquationText();
    }

    internal string GetCalculatorText(string key)
    {
        return textCatalog.Get(key, localization.CurrentLanguage);
    }

    internal string GetHealedText(int value)
    {
        return healthController.FormatHealed(value, GetCalculatorText);
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
        string label = expressionInput.NormalizeLabel(rawLabel);
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

        if (expressionInput.IsOperator(label))
        {
            AddOperator(label[0]);
            return;
        }

        if (expressionInput.IsDiceLabel(label))
        {
            AddDice(label);
            return;
        }

        if (expressionInput.IsNumberLabel(label))
        {
            AddNumber(label);
        }
    }

    private bool HandleHpModeButton(string label)
    {
        string normalized = label.ToLowerInvariant();
        if ((normalized == "heal" || normalized == "shortrest" || normalized == "longrest") &&
            ShowDeadCharacterAlertIfNeeded())
            return true;
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

        if (!healthController.IsButtonName(normalized))
            return false;
        if (easterEgg.IsShowing)
            ResetCalculator();
        healthController.TrySelectMode(normalized, GetCalculatorText,
            localization.CurrentLanguage);
        expressionInput.Reset();
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
        expressionInput.AddNumber(label);
        RefreshEquationText();
    }

    private void AddOperator(char operatorChar)
    {
        expressionInput.AddOperator(operatorChar);
        RefreshEquationText();
    }

    private void AddDice(string label)
    {
        if (HasDisplayedResult())
            ResetCalculator();
        expressionInput.AddDice(label);
        RefreshEquationText();
    }

    private void CalculateResult()
    {
        if (string.IsNullOrWhiteSpace(expressionInput.Value))
            return;

        if (!healthController.IsActive && easterEgg.TryShow(expressionInput.Value, equationText, resultText, ResetCalculator))
            return;

        string expression = expressionInput.ExpressionForEvaluation();
        if (string.IsNullOrWhiteSpace(expression))
            return;

        if (!resultEvaluator.TryEvaluate(expression, !healthController.IsActive,
            out double result, out int exhaustionPenalty))
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

        string formattedResult = expressionInput.FormatNumber(result);
        if (resultText != null)
            resultText.text = "=" + formattedResult + (exhaustionPenalty > 0 ? " (" + textCatalog.PenaltyLabel(exhaustionPenalty, localization.CurrentLanguage).Trim() + ")" : "");
        expressionInput.SetResult(formattedResult);
        RefreshEquationText();
    }

    private void ApplyHpMode(int value)
    {
        if (healthController.IsHealing && ShowDeadCharacterAlertIfNeeded())
        {
            ResetHpInputState();
            return;
        }
        ShowHpResult(healthController.Apply(value, FindActiveHealthBar(), GetCalculatorText));
        expressionInput.Reset();
    }

    internal bool ShowDeadCharacterAlertIfNeeded()
    {
        if (exhaustionEffects == null || exhaustionEffects.Level < 6)
            return false;

        new TaruckDialogPresenter(() => localization.CurrentLanguage).ShowCompact(
            localization.Translate("Персонаж мертвий"),
            localization.Translate("Спочатку зніміть шостий рівень виснаження, щоб зцілити персонажа."),
            localization.Translate("Закрити"), null);
        return true;
    }

    internal void ResetHpInputState()
    {
        healthController.Reset();
        expressionInput.Reset();
    }

    private bool HasDisplayedResult()
    {
        return resultText != null && resultText != equationText && !string.IsNullOrEmpty(resultText.text);
    }

    private void ResetCalculator()
    {
        easterEgg.ResetDisplay();
        expressionInput.Reset();
        healthController.Reset();
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = "";
    }

    private void ClearEntry()
    {
        if (expressionInput.IsEmpty)
            return;
        expressionInput.ClearEntry();
        RefreshEquationText();
    }

    internal HealthBar FindActiveHealthBar()
    {
        return healthController.FindActiveHealthBar();
    }

    internal void ShowHpResult(string message)
    {
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = message;
        healthController.ApplyTextColor(equationText, resultText);
    }

    private void RefreshEquationText()
    {
        if (equationText == null || easterEgg == null || exhaustionEffects == null || resultEvaluator == null ||
            localization == null || easterEgg.IsShowing)
            return;

        equationText.text = healthController.IsActive ? healthController.ModeLabel + expressionInput.Value : expressionInput.Value;
        if (!healthController.IsActive && resultEvaluator.IsD20Roll(expressionInput.Value) && exhaustionEffects.Level > 0)
            equationText.text += textCatalog.PenaltyLabel(
                ExhaustionEffects.DicePenaltyPerLevel * exhaustionEffects.Level, localization.CurrentLanguage);
    }
}
