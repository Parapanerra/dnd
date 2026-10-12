using System;
using System.Globalization;
using System.Text.RegularExpressions;

// Owns calculator expression input independently of UI and result modes.
internal sealed class CalculatorExpressionInput
{
    private const double WholeNumberTolerance = 0.000001d;
    private string value = "";
    private bool lastInputWasDice;

    internal string Value => value;
    internal bool IsEmpty => value.Length == 0;

    internal void Reset()
    {
        value = "";
        lastInputWasDice = false;
    }

    internal void SetResult(string result)
    {
        value = result ?? "";
        lastInputWasDice = false;
    }

    internal void AddNumber(string number)
    {
        value += number;
        lastInputWasDice = false;
    }

    internal void AddOperator(char operatorChar)
    {
        if (value.Length == 0)
        {
            if (operatorChar == '-')
                value = "-";
            return;
        }

        if (IsOperator(value[value.Length - 1].ToString()))
            value = value.Substring(0, value.Length - 1) + operatorChar;
        else
            value += operatorChar;

        lastInputWasDice = false;
    }

    internal void AddDice(string dice)
    {
        if (lastInputWasDice)
            return;
        value += dice.ToLowerInvariant();
        lastInputWasDice = true;
    }

    internal void ClearEntry()
    {
        if (value.Length == 0)
            return;
        value = value.Substring(0, value.Length - 1);
        lastInputWasDice = Regex.IsMatch(value, @"[dD]\d+$");
    }

    internal string ExpressionForEvaluation()
    {
        string expression = value;
        while (expression.Length > 0 && IsOperator(expression[expression.Length - 1].ToString()))
            expression = expression.Substring(0, expression.Length - 1);
        return expression;
    }

    internal string NormalizeLabel(string label)
    {
        if (label == "×" || label == "x" || label == "X") return "*";
        if (label == "÷") return "/";
        return label.Replace(" ", "");
    }

    internal bool IsCalculatorButtonLabel(string label)
    {
        return IsNumberLabel(label) || IsOperator(label) || IsDiceLabel(label) ||
               label == "C" || label == "CE" || label == "=";
    }

    internal bool IsNumberLabel(string label) =>
        double.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    internal bool IsDiceLabel(string label) => Regex.IsMatch(label, @"^[dD]\d+$");

    internal bool IsOperator(string label) =>
        label.Length == 1 && "+-*/".Contains(label);

    internal string FormatNumber(double number)
    {
        if (Math.Abs(number % 1) < WholeNumberTolerance)
            return ((long)Math.Round(number)).ToString(CultureInfo.InvariantCulture);
        return number.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
