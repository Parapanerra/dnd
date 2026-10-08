using System;
using NUnit.Framework;

public class TranslationCatalogTests
{
    [Test]
    public void SwitchingLanguagesRecoversTheOriginalSource()
    {
        Type catalogType = Type.GetType("TranslationCatalog, Assembly-CSharp");
        Type languageType = Type.GetType("AppLanguage, Assembly-CSharp");
        Assert.NotNull(catalogType);
        Assert.NotNull(languageType);

        object catalog = Activator.CreateInstance(catalogType);
        var translate = catalogType.GetMethod("Translate");
        object english = Enum.Parse(languageType, "English");
        object russian = Enum.Parse(languageType, "Russian");
        object ukrainian = Enum.Parse(languageType, "Ukrainian");

        Assert.AreEqual("Eldritch Invocations", translate.Invoke(catalog,
            new[] { "Таємничі заклики", english }));
        Assert.AreEqual("Таємничі заклики", translate.Invoke(catalog,
            new[] { "Eldritch Invocations", ukrainian }));
        Assert.AreEqual("Мистические воззвания", translate.Invoke(catalog,
            new[] { "Eldritch Invocations", russian }));
    }
}
