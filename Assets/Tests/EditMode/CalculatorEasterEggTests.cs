using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CalculatorEasterEggTests
{
    [Test]
    public void CharacterNameAliasesTriggerOnlyForTheRequestedNames()
    {
        Type eggType = Type.GetType("CharacterNameEasterEgg, Assembly-CSharp");
        Assert.NotNull(eggType);
        MethodInfo matches = eggType.GetMethod("IsTriggerName",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(matches);

        foreach (string name in new[] { "Taruck ship", "Taruck", "Тарук", "Тарук Шип",
                     " taruck SHIP ", "тарук шип" })
            Assert.IsTrue((bool)matches.Invoke(null, new object[] { name }), name);

        foreach (string name in new[] { "", "Taruck ships", "Тарук Шипа", "Другий персонаж" })
            Assert.IsFalse((bool)matches.Invoke(null, new object[] { name }), name);
    }

    [Test]
    public void CharacterNamePhotoUsesImportDialogFrame()
    {
        Texture2D photo = Resources.Load<Texture2D>("UI/TaruckShipEasterEgg");
        GameObject prefab = Resources.Load<GameObject>("UI/TaruckImportReviewDialog");
        Assert.NotNull(photo);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            Type dialogType = Type.GetType("TaruckImportReviewDialog, Assembly-CSharp");
            Type messagesType = Type.GetType("TaruckDialogText, Assembly-CSharp");
            Type languageType = Type.GetType("AppLanguage, Assembly-CSharp");
            Assert.NotNull(dialogType);
            Assert.NotNull(messagesType);
            Assert.NotNull(languageType);
            Component dialog = instance.GetComponent(dialogType);
            Assert.NotNull(dialog);
            MethodInfo getLanguage = typeof(CalculatorEasterEggTests)
                .GetMethod(nameof(DefaultLanguage), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(languageType);
            Type getterType = typeof(Func<>).MakeGenericType(languageType);
            Delegate getter = Delegate.CreateDelegate(getterType, getLanguage);
            object messages = Activator.CreateInstance(messagesType,
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { getter }, null);
            MethodInfo configure = dialogType.GetMethod("ConfigureImage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(configure);
            configure.Invoke(dialog, new[] { "Taruck ship", photo, messages });

            RawImage displayed = instance.GetComponentInChildren<RawImage>(true);
            Assert.NotNull(displayed);
            Assert.AreSame(photo, displayed.texture);
            RectTransform imageRect = displayed.rectTransform;
            RectTransform viewport = (RectTransform)imageRect.parent;
            Assert.AreEqual(viewport.rect.width, imageRect.rect.width, 0.1f);
            Assert.AreEqual(viewport.rect.height, imageRect.rect.height, 0.1f);
            Assert.AreEqual((float)photo.width / photo.height,
                imageRect.rect.width / imageRect.rect.height, 0.001f);
            Image footer = Array.Find(instance.GetComponentsInChildren<Image>(true),
                item => item.name == "EasterEggButtonFrame");
            Image mainFrame = Array.Find(instance.GetComponentsInChildren<Image>(true),
                item => item.name == "PanelFrame");
            Assert.NotNull(footer);
            Assert.NotNull(mainFrame);
            Assert.AreSame(mainFrame.sprite, footer.sprite);
            Assert.AreEqual(mainFrame.type, footer.type);
            Assert.AreSame(mainFrame.transform.parent, footer.transform.parent);
            RectTransform mainRect = mainFrame.rectTransform;
            RectTransform footerRect = footer.rectTransform;
            Assert.AreEqual(1f, mainRect.localScale.x, 0.001f);
            Assert.AreEqual(1f, footerRect.localScale.x, 0.001f);
            float mainBottom = mainRect.anchoredPosition.y - mainRect.rect.height * mainRect.localScale.y * 0.5f;
            float footerTop = footerRect.anchoredPosition.y + footerRect.rect.height * footerRect.localScale.y * 0.5f;
            Assert.AreEqual(mainBottom, footerTop, 0.1f);
            Button close = Array.Find(instance.GetComponentsInChildren<Button>(true),
                item => item.name == "PrimaryButton");
            Assert.NotNull(close);
            Assert.AreSame(footer.transform, close.transform.parent.parent);
            Text title = Array.Find(instance.GetComponentsInChildren<Text>(true),
                item => item.name == "Title");
            Assert.NotNull(title);
            Assert.IsFalse(title.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static T DefaultLanguage<T>() => default;

    [Test]
    public void ShowingAndClosingMessageRestoresResultTextSizing()
    {
        GameObject equationObject = new GameObject("Equation", typeof(RectTransform), typeof(Text));
        GameObject resultObject = new GameObject("Result", typeof(RectTransform), typeof(Text));
        Component localizationComponent = null;
        bool createdLocalization = false;
        FieldInfo catalogField = null;
        object previousCatalog = null;
        try
        {
            Type localizationType = Type.GetType("RuntimeLocalization, Assembly-CSharp");
            Type catalogType = Type.GetType("TranslationCatalog, Assembly-CSharp");
            Assert.NotNull(localizationType);
            Assert.NotNull(catalogType);
            createdLocalization = UnityEngine.Object.FindObjectsByType(localizationType,
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0;
            Type resolverType = Type.GetType("RuntimeManagerResolver, Assembly-CSharp");
            Assert.NotNull(resolverType);
            object resolver = Activator.CreateInstance(resolverType, true);
            MethodInfo resolveLocalization = resolverType.GetMethod("Localization",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(resolveLocalization);
            localizationComponent = (Component)resolveLocalization.Invoke(resolver, null);
            Assert.AreSame(localizationComponent,
                resolveLocalization.Invoke(resolver, null));
            catalogField = localizationType.GetField("catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            previousCatalog = catalogField.GetValue(localizationComponent);
            if (previousCatalog == null)
                catalogField.SetValue(localizationComponent, Activator.CreateInstance(catalogType));

            Text equation = equationObject.GetComponent<Text>();
            Text result = resultObject.GetComponent<Text>();
            result.fontSize = 18;
            result.resizeTextForBestFit = false;
            result.resizeTextMinSize = 7;
            result.resizeTextMaxSize = 32;
            int resetCalls = 0;
            Type eggType = Type.GetType("CalculatorEasterEgg, Assembly-CSharp");
            Assert.NotNull(eggType);
            object easterEgg = Activator.CreateInstance(eggType, localizationComponent, null);

            Assert.IsTrue((bool)eggType.GetMethod("TryShow").Invoke(easterEgg,
                new object[] { "1984", equation, result, (Action)(() => resetCalls++) }));
            Assert.AreEqual(1, resetCalls);
            Assert.IsTrue((bool)eggType.GetProperty("IsShowing").GetValue(easterEgg));
            Assert.IsTrue(result.resizeTextForBestFit);
            Assert.IsNotEmpty(result.text);

            eggType.GetMethod("ResetDisplay").Invoke(easterEgg, null);
            Assert.IsFalse((bool)eggType.GetProperty("IsShowing").GetValue(easterEgg));
            Assert.IsFalse(result.resizeTextForBestFit);
            Assert.AreEqual(7, result.resizeTextMinSize);
            Assert.AreEqual(32, result.resizeTextMaxSize);
        }
        finally
        {
            if (localizationComponent != null)
            {
                catalogField?.SetValue(localizationComponent, previousCatalog);
                if (createdLocalization)
                    UnityEngine.Object.DestroyImmediate(localizationComponent.gameObject);
            }
            UnityEngine.Object.DestroyImmediate(equationObject);
            UnityEngine.Object.DestroyImmediate(resultObject);
        }
    }
}
