using System;
using System.Reflection;
using NUnit.Framework;

public class ToggleSequenceConfigurationTests
{
    [Test]
    public void SpellToggleOrderMatchesLegacySceneOrderAndReturnsIndependentCopies()
    {
        Type installerType = Type.GetType("ToggleSequenceSceneInstaller, Assembly-CSharp");
        Assert.NotNull(installerType);
        object installer = Activator.CreateInstance(installerType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
        MethodInfo factory = installerType.GetMethod("CreateSpellCheckToggleOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(factory);

        int[] order = (int[])factory.Invoke(installer, null);
        CollectionAssert.AreEqual(new[] { 2, 1, 0, 3, 4, 5, 6, 7 }, order);
        order[0] = -1;
        Assert.AreEqual(2, ((int[])factory.Invoke(installer, null))[0]);
    }
}
