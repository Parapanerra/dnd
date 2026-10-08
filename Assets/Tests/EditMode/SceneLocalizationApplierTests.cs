using System;
using System.Reflection;
using NUnit.Framework;
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
            object applier = Activator.CreateInstance(applierType, new object[] { localization });
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
            Assert.IsFalse((bool)shouldSkip.Invoke(applier, new object[] { label }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(labelObject);
            UnityEngine.Object.DestroyImmediate(inputObject);
            UnityEngine.Object.DestroyImmediate(localizationObject);
        }
    }
}
