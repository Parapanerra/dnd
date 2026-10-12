using System;
using System.Text.RegularExpressions;

// Evaluates a calculator expression and applies the d20 exhaustion penalty.
internal sealed class CalculatorResultEvaluator
{
    private readonly Func<int> exhaustionLevel;

    internal CalculatorResultEvaluator(Func<int> exhaustionLevel)
    {
        this.exhaustionLevel = exhaustionLevel ?? throw new ArgumentNullException(nameof(exhaustionLevel));
    }

    internal bool TryEvaluate(string expression, bool applyExhaustion,
        out double result, out int exhaustionPenalty)
    {
        exhaustionPenalty = applyExhaustion && IsD20Roll(expression)
            ? ExhaustionEffects.DicePenaltyPerLevel * exhaustionLevel()
            : 0;

        string rolledExpression = DiceExpressionEvaluator.RollDice(expression);
        if (!DiceExpressionEvaluator.TryEvaluate(rolledExpression, out result))
            return false;

        result -= exhaustionPenalty;
        return true;
    }

    internal bool IsD20Roll(string expression)
    {
        return Regex.IsMatch(expression ?? "", @"(?<![\w.])\d*[dD]20(?!\d)");
    }
}
