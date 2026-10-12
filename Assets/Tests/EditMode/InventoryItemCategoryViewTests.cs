using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemCategoryViewTests
{
    [Test]
    public void ShowCategorySwitchesOnlyMatchingDropdownAndCustomImage()
    {
        Type viewType = Type.GetType("InventoryItemCategoryView, Assembly-CSharp");
        Assert.NotNull(viewType);

        GameObject weapon = new GameObject("weapon");
        GameObject armor = new GameObject("armor");
        GameObject custom = new GameObject("custom");
        try
        {
            Dropdown[] dropdowns = new Dropdown[6];
            dropdowns[0] = weapon.AddComponent<Dropdown>();
            dropdowns[1] = armor.AddComponent<Dropdown>();
            TMP_Dropdown[] tmpDropdowns = new TMP_Dropdown[6];
            object view = Activator.CreateInstance(viewType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { null, null, dropdowns, tmpDropdowns, custom, null }, null);
            MethodInfo show = viewType.GetMethod("ShowCategory", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(show);

            Type categories = Type.GetType("AppConfig+Inventory, Assembly-CSharp");
            Assert.NotNull(categories);
            int armorCategory = (int)categories.GetField("ArmorCategory").GetRawConstantValue();
            int customCategory = (int)categories.GetField("CustomCategory").GetRawConstantValue();
            show.Invoke(view, new object[] { armorCategory });
            Assert.IsFalse(weapon.activeSelf);
            Assert.IsTrue(armor.activeSelf);
            Assert.IsFalse(custom.activeSelf);

            show.Invoke(view, new object[] { customCategory });
            Assert.IsFalse(weapon.activeSelf);
            Assert.IsFalse(armor.activeSelf);
            Assert.IsTrue(custom.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(weapon);
            UnityEngine.Object.DestroyImmediate(armor);
            UnityEngine.Object.DestroyImmediate(custom);
        }
    }
}