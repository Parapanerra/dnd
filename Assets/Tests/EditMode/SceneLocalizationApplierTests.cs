using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneLocalizationApplierTests
{
    [Test]
    public void EditableUserTextIsSkippedButOrdinaryLabelIsNot()
    {
        Type localizationType = Type.GetType("RuntimeLocalization, Assembly-CSharp");
        Type catalogType = Type.GetType("TranslationCatalog, Assembly-CSharp");
        Type applierType = Type.GetType("SceneLocalizationApplier, Assembly-CSharp");
        Assert.NotNull(localizationType);
        Assert.NotNull(catalogType);
        Assert.NotNull(applierType);

        GameObject localizationObject = new GameObject("TestLocalization");
        GameObject inputObject = new GameObject("PlayerNameInput", typeof(RectTransform), typeof(InputField));
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        GameObject labelObject = new GameObject("StaticLabel", typeof(RectTransform), typeof(Text));
        try
        {
            Component localization = localizationObject.AddComponent(localizationType);
            localizationType.GetField("catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(localization, Activator.CreateInstance(catalogType));
            object applier = Activator.CreateInstance(applierType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { localization }, null);
            MethodInfo shouldSkip = applierType.GetMethod("ShouldSkip", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Text) }, null);
            Assert.NotNull(shouldSkip);

            textObject.transform.SetParent(inputObject.transform);
            Text userText = textObject.GetComponent<Text>();
            userText.text = "My custom character name";
            inputObject.GetComponent<InputField>().textComponent = userText;
            Text label = labelObject.GetComponent<Text>();
            label.text = "Оберіть зілля";

            Assert.IsTrue((bool)shouldSkip.Invoke(applier, new object[] { userText }));
            userText.text = "Оберіть зілля";
            Assert.IsTrue((bool)shouldSkip.Invoke(applier, new object[] { userText }),
                "A player-entered phrase must stay unchanged even if it matches a translation key");
            Assert.IsFalse((bool)shouldSkip.Invoke(applier, new object[] { label }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(labelObject);
            UnityEngine.Object.DestroyImmediate(inputObject);
            UnityEngine.Object.DestroyImmediate(localizationObject);
        }
    }

    [Test]
    public void FileBrowserTitleIsNotRewrittenBySceneLocalization()
    {
        Type applierType = Type.GetType("SceneLocalizationApplier, Assembly-CSharp");
        Type browserType = Type.GetType("SimpleFileBrowser.FileBrowser, SimpleFileBrowser.Runtime");
        Assert.NotNull(applierType);
        Assert.NotNull(browserType);
        GameObject browser = new GameObject("FileBrowser");
        browser.SetActive(false);
        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        try
        {
            browser.AddComponent(browserType);
            title.transform.SetParent(browser.transform);
            object applier = Activator.CreateInstance(applierType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
            MethodInfo shouldSkip = applierType.GetMethod("ShouldSkip", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(TMP_Text) }, null);
            Assert.NotNull(shouldSkip);
            Assert.IsTrue((bool)shouldSkip.Invoke(applier, new object[] { title.GetComponent<TMP_Text>() }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(browser);
        }
    }
}
