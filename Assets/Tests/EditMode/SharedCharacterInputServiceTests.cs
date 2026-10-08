using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SharedCharacterInputServiceTests
{
    [Test]
    public void LegacySharedFieldKeySurvivesSaveAndLoad()
    {
        Type service = Type.GetType("SharedCharacterInputService, Assembly-CSharp");
        Assert.NotNull(service);

        GameObject container = new GameObject("magMod (Clone)");
        GameObject fieldObject = new GameObject("Input");
        fieldObject.transform.SetParent(container.transform);
        try
        {
            InputField field = fieldObject.AddComponent<InputField>();
            field.SetTextWithoutNotify("7");
            CharacterData character = new CharacterData("test-id");
            InputField[] fields = { field };
            TMP_InputField[] tmpFields = Array.Empty<TMP_InputField>();

            service.GetMethod("Save").Invoke(null, new object[] { character, fields, tmpFields });
            Assert.AreEqual("7", character.GetSharedString("SharedInput_magMod", ""));

            field.SetTextWithoutNotify("0");
            service.GetMethod("Load").Invoke(null, new object[] { character, fields, tmpFields });
            Assert.AreEqual("7", field.text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(container);
        }
    }
}
