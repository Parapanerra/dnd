using System;
using NUnit.Framework;

public class CalculatorHealthControllerTests
{
    [Test]
    public void MissingHealthBarReportsErrorAndLeavesCalculatorOutOfHpMode()
    {
        Type controllerType = Type.GetType("CalculatorHealthController, Assembly-CSharp");
        Type languageType = Type.GetType("AppLanguage, Assembly-CSharp");
        Assert.NotNull(controllerType);
        Assert.NotNull(languageType);
        object controller = Activator.CreateInstance(controllerType);
        Func<string, string> localize = key => key == "hpBarNotFound" ? "HP bar not found" : key;
        object english = Enum.Parse(languageType, "English");

        bool selected = (bool)controllerType.GetMethod("TrySelectMode").Invoke(controller,
            new object[] { "damage", localize, english });
        Assert.IsTrue(selected);
        Assert.IsTrue((bool)controllerType.GetProperty("IsActive").GetValue(controller));
        Assert.AreEqual("Damage:", controllerType.GetProperty("ModeLabel").GetValue(controller));

        string result = (string)controllerType.GetMethod("Apply").Invoke(controller,
            new object[] { 5, null, localize });
        Assert.AreEqual("HP bar not found", result);
        Assert.IsFalse((bool)controllerType.GetProperty("IsActive").GetValue(controller));
    }
}
