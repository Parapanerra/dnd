using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class A7UiConfigTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void DropdownConfigOwnsVisibilityAndClearsHiddenToggles()
    {
        GameObject dropdownObject = new GameObject("Dropdown", typeof(RectTransform), typeof(Dropdown));
        GameObject first = new GameObject("First");
        GameObject second = new GameObject("Second", typeof(RectTransform), typeof(Toggle));
        try
        {
            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("None"),
                new Dropdown.OptionData("One"),
                new Dropdown.OptionData("Two")
            };
            dropdown.SetValueWithoutNotify(1);
            second.GetComponent<Toggle>().SetIsOnWithoutNotify(true);
            Type configType = Type.GetType("DropdownManager+DropdownConfig, Assembly-CSharp");
            Assert.NotNull(configType);
            object config = Activator.CreateInstance(configType);
            configType.GetField("dropdown", PrivateInstance).SetValue(config, dropdown);
            configType.GetField("tangles", PrivateInstance).SetValue(config, new List<GameObject> { first, second });
            configType.GetMethod("Bind").Invoke(config, null);
            Assert.IsTrue(first.activeSelf);
            Assert.IsFalse(second.activeSelf);
            Assert.IsFalse(second.GetComponent<Toggle>().isOn);
            configType.GetMethod("Unbind").Invoke(config, null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(dropdownObject);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void IncrementConfigUnbindsItsButtons()
    {
        GameObject inputObject = new GameObject("Input", typeof(RectTransform), typeof(InputField));
        GameObject incrementObject = new GameObject("Increment", typeof(RectTransform), typeof(Button));
        GameObject decrementObject = new GameObject("Decrement", typeof(RectTransform), typeof(Button));
        try
        {
            InputField input = inputObject.GetComponent<InputField>();
            input.SetTextWithoutNotify("5");
            Button increment = incrementObject.GetComponent<Button>();
            Button decrement = decrementObject.GetComponent<Button>();
            Type configType = Type.GetType("InputFieldIncrementer+FieldConfig, Assembly-CSharp");
            Assert.NotNull(configType);
            object config = Activator.CreateInstance(configType);
            configType.GetField("inputField", PrivateInstance).SetValue(config, input);
            configType.GetField("incrementButton", PrivateInstance).SetValue(config, increment);
            configType.GetField("decrementButton", PrivateInstance).SetValue(config, decrement);
            configType.GetMethod("Bind").Invoke(config, null);
            increment.onClick.Invoke();
            Assert.AreEqual("6", input.text);
            decrement.onClick.Invoke();
            Assert.AreEqual("5", input.text);
            configType.GetMethod("Unbind").Invoke(config, null);
            increment.onClick.Invoke();
            Assert.AreEqual("5", input.text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(inputObject);
            UnityEngine.Object.DestroyImmediate(incrementObject);
            UnityEngine.Object.DestroyImmediate(decrementObject);
        }
    }
}