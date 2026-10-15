using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns the calculator's hidden messages and their display state.
internal sealed class CalculatorEasterEgg
{
    private const string ResetCountKey = "Calculator.EasterEgg67.Count";
    private const string ResetTriggerCode = "67";
    private const string RadioCode = "4221";
    private const string DevilCode = "666";
    private const string NiceCode = "69";
    private const string BookCode = "1984";
    private const int WarningsBeforeReset = 2;
    private const int ResetTriggerCount = WarningsBeforeReset + 1;
    private const int MinimumMessageFontSize = 10;
    private readonly RuntimeLocalization localization;
    private readonly DndSaveManager saveManager;
    private Text equationText;
    private Text resultText;
    private string topSource = "";
    private string messageSource = "";
    private bool splitResetMessage;
    private bool bestFitBefore;
    private int minSizeBefore;
    private int maxSizeBefore;

    public bool IsShowing { get; private set; }

    public CalculatorEasterEgg(RuntimeLocalization textCatalog, DndSaveManager dataManager)
    {
        localization = textCatalog;
        saveManager = dataManager;
    }

    public bool TryShow(string equation, Text equationDisplay, Text resultDisplay, Action resetCalculator)
    {
        string topLine = "";
        string message;
        switch (equation)
        {
            case ResetTriggerCode:
                message = AdvanceResetMessage();
                break;
            case RadioCode:
                message = "Раз, два, три, прийом! Ця штука працює?";
                break;
            case DevilCode:
                message = "Hell Yeah!!!";
                break;
            case NiceCode:
                message = "Нааайс";
                break;
            case BookCode:
                topLine = "Це як у 1984";
                message = "Але я не читав";
                break;
            default:
                return false;
        }

        resetCalculator();
        equationText = equationDisplay;
        resultText = resultDisplay;
        topSource = topLine;
        messageSource = message;
        splitResetMessage = equation == ResetTriggerCode;
        if (resultText != null)
        {
            IsShowing = true;
            bestFitBefore = resultText.resizeTextForBestFit;
            minSizeBefore = resultText.resizeTextMinSize;
            maxSizeBefore = resultText.resizeTextMaxSize;
            resultText.resizeTextForBestFit = true;
            resultText.resizeTextMinSize = MinimumMessageFontSize;
            resultText.resizeTextMaxSize = Mathf.Max(MinimumMessageFontSize, resultText.fontSize);
        }

        Refresh();
        return true;
    }

    public void Refresh()
    {
        string topLine = localization.Translate(topSource);
        string message = localization.Translate(messageSource);
        if (splitResetMessage)
        {
            int separatorIndex = message.IndexOf('—');
            if (separatorIndex < 0)
                separatorIndex = message.IndexOf(',');
            if (separatorIndex >= 0)
            {
                topLine = message.Substring(0, separatorIndex + 1).Trim();
                message = message.Substring(separatorIndex + 1).Trim();
            }
        }
        if (equationText != null)
            equationText.text = topLine;
        if (resultText != null)
            resultText.text = resultText == equationText && topLine.Length > 0
                ? topLine + "\n" + message : message;
    }

    public void ResetDisplay()
    {
        if (IsShowing && resultText != null)
        {
            resultText.resizeTextForBestFit = bestFitBefore;
            resultText.resizeTextMinSize = minSizeBefore;
            resultText.resizeTextMaxSize = maxSizeBefore;
        }

        IsShowing = false;
        topSource = "";
        messageSource = "";
        splitResetMessage = false;
    }

    private string AdvanceResetMessage()
    {
        CharacterData character = saveManager != null ? saveManager.GetActiveCharacter() : null;
        if (character == null)
            return "Спочатку обери персонажа.";

        int.TryParse(character.GetSharedString(ResetCountKey, "0"), out int count);
        count = Mathf.Clamp(count, 0, WarningsBeforeReset) + 1;
        if (count == ResetTriggerCount)
        {
            CharacterSheetManagerScene1 sheet = UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
            if (sheet != null)
                sheet.ResetSceneData();
            else
                return "Не вдалося знайти Reset для цього листа.";
        }

        character.SetSharedString(ResetCountKey, (count == ResetTriggerCount ? 0 : count).ToString(CultureInfo.InvariantCulture));
        saveManager.SaveData();
        switch (count)
        {
            case 1: return "Ще раз напишеш 67 — видалю персонажа.";
            case 2: return "Я взагалі-то серйозно.";
            default: return "Я попереджував.";
        }
    }
}

// Character-name surprise lives beside the calculator easter eggs.
internal sealed class CharacterNameEasterEgg
{
    private readonly TaruckDialogPresenter dialogs;
    private bool shownForCurrentEntry;

    internal CharacterNameEasterEgg(RuntimeLocalization localization)
    {
        dialogs = new TaruckDialogPresenter(() => localization.CurrentLanguage);
    }

    internal void Bind(InputField input, TMP_InputField tmpInput)
    {
        if (input != null)
        {
            shownForCurrentEntry = IsTriggerName(input.text);
            input.onValueChanged.AddListener(HandleValueChanged);
            input.onEndEdit.AddListener(HandleEndEdit);
        }
        else if (tmpInput != null)
        {
            shownForCurrentEntry = IsTriggerName(tmpInput.text);
            tmpInput.onValueChanged.AddListener(HandleValueChanged);
            tmpInput.onEndEdit.AddListener(HandleEndEdit);
        }
    }

    private void HandleValueChanged(string value)
    {
        if (!IsTriggerName(value))
            shownForCurrentEntry = false;
    }

    private void HandleEndEdit(string value)
    {
        if (shownForCurrentEntry || !IsTriggerName(value))
            return;

        Texture2D photo = Resources.Load<Texture2D>("UI/TaruckShipEasterEgg");
        if (photo == null)
        {
            Debug.LogError("Missing Resources/UI/TaruckShipEasterEgg image");
            return;
        }

        shownForCurrentEntry = true;
        dialogs.ShowImage("Taruck ship", photo);
    }

    internal static bool IsTriggerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string name = value.Trim();
        return name.Equals("Taruck ship", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Taruck", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Тарук", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Тарук Шип", StringComparison.OrdinalIgnoreCase);
    }
}
