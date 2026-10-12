using System;
using System.Reflection;
using NUnit.Framework;

public class CharacterRestSavedStateServiceTests
{
    [Test]
    public void ShortAndLongRestUpdateSavedPagesWithoutClearingShortRestSpellSlots()
    {
        var character = new CharacterData("rest-test");
        CharacterSceneData active = character.GetSceneData("Spels");
        CharacterSceneData other = character.GetSceneData("cartaPersonaj");
        Mark(active, "wild", "WildShape", 0);
        Mark(active, "spell", "SpellSlots", 0);
        Mark(active, "death", "DeathSaves", 0);
        Mark(active, "exhaustion", "Exhaustion", 0);
        Mark(other, "spell2", "SpellSlots", 0);
        Mark(other, "blood0", "BloodCurse", 0);
        Mark(other, "blood7", "BloodCurse", 7);
        Mark(other, "blood8", "BloodCurse", 8);
        Mark(other, "blood11", "BloodCurse", 11);
        Mark(other, "blood12", "BloodCurse", 12);
        Mark(other, "exhaustion2", "Exhaustion", 0);
        other.SetInt("HealthBar_test_maxHealth", 12);
        other.SetInt("HealthBar_test_currentHealth", 4);
        other.SetInt("HealthBar_test_currentTemporaryHealth", 3);

        Type serviceType = Type.GetType("CharacterRestSavedStateService, Assembly-CSharp");
        Assert.NotNull(serviceType);
        object service = Activator.CreateInstance(serviceType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
        MethodInfo apply = serviceType.GetMethod("ApplyToCharacter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(apply);

        apply.Invoke(service, new object[] { character, false, active });
        Assert.AreEqual(0, active.GetInt("StableField_v1_wild"));
        Assert.AreEqual(1, active.GetInt("StableField_v1_spell"));
        Assert.AreEqual(1, other.GetInt("StableField_v1_spell2"));
        Assert.AreEqual(4, other.GetInt("HealthBar_test_currentHealth"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_blood0"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_blood7"));
        Assert.AreEqual(1, other.GetInt("StableField_v1_blood8"));
        Assert.AreEqual(1, other.GetInt("StableField_v1_blood11"));

        apply.Invoke(service, new object[] { character, true, active });
        Assert.AreEqual(0, active.GetInt("StableField_v1_spell"));
        Assert.AreEqual(0, active.GetInt("StableField_v1_death"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_spell2"));
        Assert.AreEqual(1, active.GetInt("StableField_v1_exhaustion"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_exhaustion2"));
        Assert.AreEqual(12, other.GetInt("HealthBar_test_currentHealth"));
        Assert.AreEqual(0, other.GetInt("HealthBar_test_currentTemporaryHealth"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_blood8"));
        Assert.AreEqual(0, other.GetInt("StableField_v1_blood11"));
        Assert.AreEqual(1, other.GetInt("StableField_v1_blood12"));
    }

    private static void Mark(CharacterSceneData scene, string id, string role, int number)
    {
        scene.SetString("RestRole_" + id, role + "|" + number);
        scene.SetInt("StableField_v1_" + id, 1);
    }
}