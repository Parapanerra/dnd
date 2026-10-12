using System;
using System.Reflection;
using NUnit.Framework;

public class A7HealthBarPersistenceTests
{
    [Test]
    public void LegacyKeysRoundTripCurrentAndTemporaryHealth()
    {
        Type storeType = Type.GetType("HealthBarPersistence, Assembly-CSharp");
        Type valuesType = Type.GetType("HealthBarValues, Assembly-CSharp");
        Assert.NotNull(storeType);
        Assert.NotNull(valuesType);

        CharacterSceneData scene = new CharacterSceneData("cartaPersonaj");
        object store = Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { null, 3 }, null);
        storeType.GetMethod("BindScene", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, new object[] { scene });
        object values = Activator.CreateInstance(valuesType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { 10, 7, 5, 2 }, null);
        storeType.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, new[] { values });

        Assert.AreEqual(10, scene.GetInt("HealthBar_3_maxHealth"));
        Assert.AreEqual(7, scene.GetInt("HealthBar_3_currentHealth"));
        Assert.AreEqual(5, scene.GetInt("HealthBar_3_maxTemporaryHealth"));
        Assert.AreEqual(2, scene.GetInt("HealthBar_3_currentTemporaryHealth"));

        object[] result = { null };
        Assert.IsTrue((bool)storeType.GetMethod("TryLoad", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, result));
        Assert.AreEqual(7, valuesType.GetField("CurrentHealth", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(result[0]));
        Assert.AreEqual(2, valuesType.GetField("CurrentTemporaryHealth", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(result[0]));
    }
}