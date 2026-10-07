using System;
using System.Reflection;
using NUnit.Framework;

public class A7CalculatorTests
{
    [TestCase("2+3*4", 14d)]
    [TestCase("-5+8/2", -1d)]
    [TestCase("1d1+2", 3d)]
    public void ExtractedEvaluatorKeepsCalculatorRules(string expression, double expected)
    {
        Type evaluator = Type.GetType("DiceExpressionEvaluator, Assembly-CSharp");
        Assert.NotNull(evaluator);
        string rolled = (string)evaluator.GetMethod("RollDice").Invoke(null, new object[] { expression });
        object[] arguments = { rolled, 0d };
        bool valid = (bool)evaluator.GetMethod("TryEvaluate").Invoke(null, arguments);
        Assert.IsTrue(valid);
        Assert.AreEqual(expected, (double)arguments[1], 0.000001);
    }

    [Test]
    public void ExtractedEvaluatorRejectsTrailingOperator()
    {
        Type evaluator = Type.GetType("DiceExpressionEvaluator, Assembly-CSharp");
        Assert.NotNull(evaluator);
        object[] arguments = { "2+", 0d };
        bool valid = (bool)evaluator.GetMethod("TryEvaluate", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, arguments);
        Assert.IsFalse(valid);
    }
}
