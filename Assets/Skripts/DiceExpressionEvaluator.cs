using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DiceExpressionEvaluator
{
    public static string RollDice(string equation)
    {
        return Regex.Replace(equation, @"(\d*)[dD](\d+)", match =>
        {
            int diceCount = 1;
            if (!string.IsNullOrEmpty(match.Groups[1].Value))
                int.TryParse(match.Groups[1].Value, out diceCount);
            if (!int.TryParse(match.Groups[2].Value, out int diceSides))
                return "0";

            diceCount = Mathf.Clamp(diceCount,
                AppConfig.Calculator.MinimumDiceCount,
                AppConfig.Calculator.MaximumDiceCount);
            diceSides = Mathf.Clamp(diceSides,
                AppConfig.Calculator.MinimumDiceSides,
                AppConfig.Calculator.MaximumDiceSides);

            int total = 0;
            for (int i = 0; i < diceCount; i++)
                total += UnityEngine.Random.Range(1, diceSides + 1);
            return total.ToString(CultureInfo.InvariantCulture);
        });
    }

    public static bool TryEvaluate(string expression, out double result)
    {
        result = 0;
        expression = expression.Replace(" ", "");
        try
        {
            var parser = new Parser(expression);
            result = parser.ParseExpression();
            return parser.IsAtEnd && !double.IsNaN(result) && !double.IsInfinity(result);
        }
        catch
        {
            result = 0;
            return false;
        }
    }

    private sealed class Parser
    {
        private readonly string expression;
        private int index;

        public Parser(string expression) { this.expression = expression; }

        public bool IsAtEnd
        {
            get
            {
                SkipWhitespace();
                return index >= expression.Length;
            }
        }

        public double ParseExpression()
        {
            double value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) value += ParseTerm();
                else if (Match('-')) value -= ParseTerm();
                else return value;
            }
        }

        private double ParseTerm()
        {
            double value = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (Match('*')) value *= ParseFactor();
                else if (Match('/')) value /= ParseFactor();
                else return value;
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();
            if (Match('+')) return ParseFactor();
            if (Match('-')) return -ParseFactor();
            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            int start = index;
            while (index < expression.Length &&
                   (char.IsDigit(expression[index]) || expression[index] == '.'))
                index++;
            if (start == index)
                throw new FormatException("Expected number.");
            return double.Parse(expression.Substring(start, index - start), CultureInfo.InvariantCulture);
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
