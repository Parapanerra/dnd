using System;
using System.Reflection;
using NUnit.Framework;

public class CalculatorResultEvaluatorTests
{
    private static readonly Type EvaluatorType = Type.GetType("CalculatorResultEvaluator, Assembly-CSharp");

    [Test]
    public void OrdinaryExpressionDoesNotReadExhaustionLevel()
    {
        Assert.NotNull(EvaluatorType);
        object evaluator = Activator.CreateInstance(EvaluatorType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new Func<int>(() => throw new InvalidOperationException("Level was read")) }, null);

        object[] arguments = { "8+2", true, 0d, 0 };
        Assert.IsTrue((bool)EvaluatorType.GetMethod("TryEvaluate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(evaluator, arguments));
        Assert.AreEqual(10d, arguments[2]);
        Assert.AreEqual(0, arguments[3]);
    }

    [Test]
    public void D20RollAppliesPenaltyOnlyOutsideHealthMode()
    {
        Assert.NotNull(EvaluatorType);
        object evaluator = Activator.CreateInstance(EvaluatorType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new Func<int>(() => 2) }, null);
        MethodInfo evaluate = EvaluatorType.GetMethod("TryEvaluate", BindingFlags.Instance | BindingFlags.NonPublic);

        object[] normal = { "1d20", true, 0d, 0 };
        Assert.IsTrue((bool)evaluate.Invoke(evaluator, normal));
        Assert.AreEqual(4, normal[3]);
        Assert.That((double)normal[2], Is.InRange(-3d, 16d));

        object[] health = { "1d20", false, 0d, 0 };
        Assert.IsTrue((bool)evaluate.Invoke(evaluator, health));
        Assert.AreEqual(0, health[3]);
        Assert.That((double)health[2], Is.InRange(1d, 20d));
    }
}
