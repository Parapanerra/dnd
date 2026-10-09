using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CalculatorEasterEggTests
{
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
            localizationComponent = (Component)localizationType.GetMethod("EnsureExists").Invoke(null, null);
            Assert.AreSame(localizationComponent,
                localizationType.GetMethod("EnsureExists").Invoke(null, null));
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
