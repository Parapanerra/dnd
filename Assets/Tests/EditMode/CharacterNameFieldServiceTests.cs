using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterNameFieldServiceTests
{
    [Test]
    public void FindsLegacyNameFieldWhenModernObjectIsMissing()
    {
        Type serviceType = Type.GetType("CharacterNameFieldService, Assembly-CSharp");
        Assert.NotNull(serviceType);

        GameObject root = new GameObject("playerInfo");
        GameObject group = new GameObject("inputPises");
        GameObject legacy = new GameObject("mmpises1", typeof(RectTransform), typeof(InputField));
        try
        {
            group.transform.SetParent(root.transform);
            legacy.transform.SetParent(group.transform);
            InputField input = legacy.GetComponent<InputField>();

            object service = Activator.CreateInstance(serviceType);
            serviceType.GetMethod("Cache").Invoke(service, new object[]
            {
                new List<InputField> { input }, new List<TMP_InputField>(), true
            });

            Assert.AreSame(input, serviceType.GetProperty("InputField").GetValue(service));
            Assert.AreEqual("Hero", serviceType.GetMethod("CleanName").Invoke(null, new object[] { " Hero " }));
            Assert.AreEqual("", serviceType.GetMethod("CleanName").Invoke(null, new object[] { "42" }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
