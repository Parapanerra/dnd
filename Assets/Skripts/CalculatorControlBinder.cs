using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Finds calculator controls and connects them to the calculation and HP actions.
public sealed class CalculatorControlBinder
{
    private readonly Transform host;
    private readonly Func<string, string> normalizeLabel;
    private readonly Func<string, bool> isCalculatorInput;

    public CalculatorControlBinder(Transform host, Func<string, string> normalizeLabel,
        Func<string, bool> isCalculatorInput)
    {
        this.host = host;
        this.normalizeLabel = normalizeLabel;
        this.isCalculatorInput = isCalculatorInput;
    }

    public void ResolveDisplayTexts(ref Text equationText, ref Text resultText)
    {
        if (equationText != null && resultText != null)
            return;

        List<Text> displayTexts = new List<Text>();
        foreach (Text text in FindCalculatorRoot().GetComponentsInChildren<Text>(true))
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

    public void BindButtons(List<Button> buttons, Action<string> onMainButton,
        Action<string, Button> onHpButton)
    {
        if (buttons == null)
            throw new ArgumentNullException(nameof(buttons));

        buttons.Clear();
        Transform root = FindCalculatorRoot();
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button == null || IsSpecialCalculatorButton(button))
                continue;

            string label = normalizeLabel(GetButtonLabel(button));
            if (isCalculatorInput(label))
                buttons.Add(button);
        }

        foreach (Button button in buttons)
        {
            string label = GetButtonLabel(button);
            if (string.IsNullOrEmpty(label))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onMainButton(label));
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button == null)
                continue;

            string buttonName = normalizeLabel(button.gameObject.name);
            if (!CalculatorHealthController.IsButtonName(buttonName))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onHpButton(buttonName, button));
        }
    }

    private Transform FindCalculatorRoot()
    {
        Transform current = host;
        while (current != null)
        {
            if (string.Equals(current.name, "kalPanel", StringComparison.OrdinalIgnoreCase))
                return current;
            current = current.parent;
        }
        return host.parent != null ? host.parent : host;
    }

    private static string GetButtonLabel(Button button)
    {
        if (button == null)
            return "";
        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
            return labelText.text.Trim();
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        return tmpText != null ? tmpText.text.Trim() : "";
    }

    private static int CompareDisplayTextCandidates(Text left, Text right)
    {
        bool leftEmpty = left == null || string.IsNullOrWhiteSpace(left.text);
        bool rightEmpty = right == null || string.IsNullOrWhiteSpace(right.text);
        if (leftEmpty != rightEmpty)
            return leftEmpty ? -1 : 1;
        return right.rectTransform.position.y.CompareTo(left.rectTransform.position.y);
    }

    private static bool IsInsideInteractiveControl(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
            if (current.GetComponent<Button>() != null || current.GetComponent<Dropdown>() != null ||
                current.GetComponent<TMP_Dropdown>() != null || current.GetComponent<InputField>() != null ||
                current.GetComponent<TMP_InputField>() != null)
                return true;
        return false;
    }

    private bool IsStaticCalculatorLabel(string text)
    {
        string label = normalizeLabel(text).ToLowerInvariant();
        return !string.IsNullOrEmpty(label) &&
               (label.Contains("калькулятор") || label.Contains("calculator") ||
                label.Contains("меню") || label.Contains("menu") ||
                label.Contains("шкода") || label.Contains("урон") || label.Contains("damage") ||
                label.Contains("зцілення") || label.Contains("heal") ||
                label.Contains("маххп") || label.Contains("maxhp") ||
                label.Contains("відпочинок") || label.Contains("rest") ||
                label.Contains("випити") || label.Contains("зілля") ||
                label.Contains("псевдожиття"));
    }

    private bool IsSpecialCalculatorButton(Button button)
    {
        string name = normalizeLabel(button.gameObject.name).ToLowerInvariant();
        return CalculatorHealthController.IsButtonName(name) ||
               name == "potionplus" || name == "potionminus" || name == "potionuse";
    }
}
