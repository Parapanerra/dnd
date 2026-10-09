using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class A7HealthBarTests
{
    [Test]
    public void LegacyHealthBarPrefabRetainsReferencesThroughSharedComponent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/hitbar.prefab");
        Assert.NotNull(prefab);

        Component legacy = prefab.GetComponent("HealthBar1");
        Assert.NotNull(legacy);
        Type sharedType = Type.GetType("HealthBar, Assembly-CSharp");
        Assert.NotNull(sharedType);
        Assert.AreSame(legacy, prefab.GetComponent(sharedType));
        Assert.NotNull(sharedType.GetField("healthSlider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(legacy));
        Assert.NotNull(sharedType.GetField("healthText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(legacy));
        Assert.NotNull(sharedType.GetField("maxHealthInputField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(legacy));
        Assert.NotNull(sharedType.GetField("damageButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(legacy));
        Assert.NotNull(sharedType.GetField("healButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(legacy));
    }

    [Test]
    public void SharedHealthBarSearchIncludesLegacyComponent()
    {
        GameObject instance = new GameObject("A7 legacy health bar");
        instance.SetActive(false);
        try
        {
            Type sharedType = Type.GetType("HealthBar, Assembly-CSharp");
            Type legacyType = Type.GetType("HealthBar1, Assembly-CSharp");
            Assert.NotNull(sharedType);
            Assert.NotNull(legacyType);
            Component legacy = instance.AddComponent(legacyType);
            Assert.AreSame(legacy, instance.GetComponent(sharedType));
            Assert.IsTrue(Array.Exists(UnityEngine.Object.FindObjectsByType(sharedType, FindObjectsInactive.Include,
                FindObjectsSortMode.None), found => found == legacy));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
