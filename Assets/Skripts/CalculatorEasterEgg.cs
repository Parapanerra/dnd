using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// Owns the calculator's hidden messages and their display state.
public sealed class CalculatorEasterEgg
{
    private const string ResetCountKey = "Calculator.EasterEgg67.Count";
    private Text equationText;
    private Text resultText;
    private string topSource = "";
    private string messageSource = "";
    private bool splitResetMessage;
    private bool bestFitBefore;
    private int minSizeBefore;
    private int maxSizeBefore;

    public bool IsShowing { get; private set; }

    public bool TryShow(string equation, Text equationDisplay, Text resultDisplay, Action resetCalculator)
    {
        string topLine = "";
        string message;
        switch (equation)
        {
            case "67":
                message = AdvanceResetMessage();
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

        resetCalculator();
        equationText = equationDisplay;
        resultText = resultDisplay;
        topSource = topLine;
        messageSource = message;
        splitResetMessage = equation == "67";
        if (resultText != null)
        {
            IsShowing = true;
            bestFitBefore = resultText.resizeTextForBestFit;
            minSizeBefore = resultText.resizeTextMinSize;
            maxSizeBefore = resultText.resizeTextMaxSize;
            resultText.resizeTextForBestFit = true;
            resultText.resizeTextMinSize = 10;
            resultText.resizeTextMaxSize = Mathf.Max(10, resultText.fontSize);
        }

        Refresh();
        return true;
    }

    public void Refresh()
    {
        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
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

    private static string AdvanceResetMessage()
    {
        DndSaveManager saveManager = DndSaveManager.Instance;
        CharacterData character = saveManager != null ? saveManager.GetActiveCharacter() : null;
        if (character == null)
            return "Спочатку обери персонажа.";

        int.TryParse(character.GetSharedString(ResetCountKey, "0"), out int count);
        count = Mathf.Clamp(count, 0, 2) + 1;
        if (count == 3)
        {
            CharacterSheetManagerScene1 sheet = UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
            CharacterSceneAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<CharacterSceneAutoSave>();
            if (sheet != null)
                sheet.ResetSceneData();
            else if (autoSave != null)
                autoSave.ResetSceneData();
            else
                return "Не вдалося знайти Reset для цього листа.";
        }

        character.SetSharedString(ResetCountKey, (count == 3 ? 0 : count).ToString(CultureInfo.InvariantCulture));
        saveManager.SaveData();
        switch (count)
        {
            case 1: return "Ще раз напишеш 67 — видалю персонажа.";
            case 2: return "Я взагалі-то серйозно.";
            default: return "Я попереджував.";
        }
    }
}
