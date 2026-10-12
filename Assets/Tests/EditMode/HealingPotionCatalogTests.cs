using System;
using System.Reflection;
using NUnit.Framework;

public class HealingPotionCatalogTests
{
    [Test]
    public void FormulasAndTranslatedNamesKeepLegacyPotionOrder()
    {
        Type catalogType = Type.GetType("HealingPotionCatalog, Assembly-CSharp");
        Assert.NotNull(catalogType);
        object catalog = Activator.CreateInstance(catalogType, true);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        PropertyInfo count = catalogType.GetProperty("Count", flags);
        MethodInfo formula = catalogType.GetMethod("GetFormula", flags);
        MethodInfo name = catalogType.GetMethod("GetName", flags);
        Assert.AreEqual(4, count.GetValue(catalog));
        string[] expected = { "2d4+2", "4d4+4", "8d4+8", "10d4+20" };
        for (int index = 0; index < expected.Length; index++)
            Assert.AreEqual(expected[index], formula.Invoke(catalog, new object[] { index }));
        Type languageType = Type.GetType("AppLanguage, Assembly-CSharp");
        Assert.NotNull(languageType);
        Assert.AreEqual("Зілля лікування", name.Invoke(catalog, new object[] { 0, Enum.Parse(languageType, "Ukrainian") }));
        Assert.AreEqual("Potion of Supreme Healing", name.Invoke(catalog, new object[] { 3, Enum.Parse(languageType, "English") }));
        Assert.AreEqual("Высшее зелье лечения", name.Invoke(catalog, new object[] { 3, Enum.Parse(languageType, "Russian") }));
    }
}