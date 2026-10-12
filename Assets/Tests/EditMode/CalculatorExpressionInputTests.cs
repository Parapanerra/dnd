using System;
using System.Reflection;
using NUnit.Framework;

public class CalculatorExpressionInputTests
{
    private static readonly Type InputType = Type.GetType("CalculatorExpressionInput, Assembly-CSharp");
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void OperatorReplacementAndTrailingOperatorKeepSameEvaluationExpression()
    {
        Assert.NotNull(InputType);
        object input = Activator.CreateInstance(InputType, true);
        Invoke(input, "AddNumber", "12");
        Invoke(input, "AddOperator", '+');
        Invoke(input, "AddOperator", '-');
        Assert.AreEqual("12-", Read(input));
        Assert.AreEqual("12", Invoke(input, "ExpressionForEvaluation"));

        Invoke(input, "AddNumber", "3");
        Assert.AreEqual("12-3", Invoke(input, "ExpressionForEvaluation"));
        Invoke(input, "SetResult", "9");
        Assert.AreEqual("9", Read(input));
        Invoke(input, "Reset");
        Assert.AreEqual("", Read(input));
    }

    [Test]
    public void DiceEntryAndBackspacePreserveInputRules()
    {
        Assert.NotNull(InputType);
        object input = Activator.CreateInstance(InputType, true);
        Invoke(input, "AddNumber", "2");
        Invoke(input, "AddDice", "D20");
        Invoke(input, "AddDice", "d6");
        Assert.AreEqual("2d20", Read(input));
        Invoke(input, "ClearEntry");
        Assert.AreEqual("2d2", Read(input));
        Invoke(input, "AddDice", "d6");
        Assert.AreEqual("2d2", Read(input));
        Invoke(input, "AddOperator", '+');
        Invoke(input, "AddDice", "d6");
        Assert.AreEqual("2d2+d6", Read(input));
    }

    private static object Invoke(object input, string method, params object[] arguments)
    {
        return InputType.GetMethod(method, InstanceFlags).Invoke(input, arguments);
    }

    private static string Read(object input)
    {
        return (string)InputType.GetProperty("Value", InstanceFlags).GetValue(input);
    }
}