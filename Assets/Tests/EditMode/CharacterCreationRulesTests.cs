using System.Linq;
using NUnit.Framework;
using Taruck.CharacterCreation;

public class CharacterCreationRulesTests
{
    private static CharacterCreationDraft Draft() => new CharacterCreationDraft { characterName = "Тест" };
    [Test] public void Dwarf2014UsesRacialBonusesAndSingleSecondWind()
    {
        var d = Draft(); d.edition = RulesEdition.Rules2014;
        var s = CharacterCreationRules.Calculate(d);
        Assert.That(s.scores, Is.EqualTo(new[] {15,13,16,8,13,10}));
        Assert.That(s.hitPoints, Is.EqualTo(14)); Assert.That(s.speed, Is.EqualTo(25));
        Assert.That(s.secondWindUses, Is.EqualTo(1)); Assert.That(s.armorClass, Is.EqualTo(19));
    }
    [Test] public void Dwarf2024UsesBackgroundBonusesAndDifferentEquipment()
    {
        var s = CharacterCreationRules.Calculate(Draft());
        Assert.That(s.scores, Is.EqualTo(new[] {17,13,15,8,12,10}));
        Assert.That(s.hitPoints, Is.EqualTo(13)); Assert.That(s.armorClass, Is.EqualTo(17));
        Assert.That(s.secondWindUses, Is.EqualTo(2)); Assert.That(s.speed, Is.EqualTo(30));
    }
    [Test] public void EditionSwitchDoesNotAccumulateBonuses()
    {
        var d = Draft(); var original = CharacterCreationRules.Scores(d);
        d.edition = RulesEdition.Rules2014; CharacterCreationRules.Scores(d);
        d.edition = RulesEdition.Rules2024;
        Assert.That(CharacterCreationRules.Scores(d), Is.EqualTo(original));
        Assert.That(d.abilities, Is.EqualTo(new[] {15,13,14,8,12,10}));
    }
    [Test] public void DuplicateSkillAndOverspentPointsAreRejected()
    {
        var d = Draft(); d.classSkills[0] = 3; d.abilityMethod = AbilityMethod.PointBuy; d.abilities = new[] {15,15,15,15,15,15};
        Assert.That(CharacterCreationRules.Validate(d).Count, Is.GreaterThanOrEqualTo(2));
    }
    [Test] public void BackgroundChangeRepairsOnlyDependentChoices()
    {
        var d = Draft(); d.background = "criminal"; d.increaseTwo = 0;
        CharacterCreationRules.RepairDependentChoices(d);
        Assert.That(new[] {1,2,3}, Does.Contain(d.increaseTwo));
        Assert.That(d.classSkills, Is.EqualTo(new[] {11,17}));
    }
    [Test] public void Human2024GetsToughAndExtraUniqueSkill()
    {
        var d = Draft(); d.species = "human";
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(14));
        Assert.That(CharacterCreationRules.Calculate(d).skills, Has.Count.EqualTo(5));
        d.humanSkill = 3; Assert.That(CharacterCreationRules.Validate(d), Is.Not.Empty);
    }
    [Test] public void ThreeBonusesAndCriminalAlertAreAppliedOnce()
    {
        var d = Draft(); d.background = "criminal"; d.threeIncreases = true; CharacterCreationRules.RepairDependentChoices(d);
        var s = CharacterCreationRules.Calculate(d);
        Assert.That(s.scores, Is.EqualTo(new[] {15,14,15,9,12,10}));
        Assert.That(s.initiative, Is.EqualTo(4));
    }
    [Test] public void StandardArrayMustUseEachNumberOnce()
    {
        var d = Draft(); d.abilities[0] = 14;
        Assert.That(CharacterCreationRules.Validate(d).Any(x=>x.Contains("Стандартний")), Is.True);
    }
    [Test] public void PointBuyAcceptsTwentySevenAndRejectsOutOfRange()
    {
        var d = Draft(); d.abilityMethod = AbilityMethod.PointBuy;
        Assert.That(d.abilities.Sum(CharacterCreationRules.PointCost), Is.EqualTo(27));
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        d.abilities[0] = 16; Assert.That(CharacterCreationRules.Validate(d), Is.Not.Empty);
    }
    [Test] public void AbilityModifiersRoundDownForOddScoresBelowTen()
    { Assert.That(CharacterCreationRules.Modifier(7), Is.EqualTo(-2)); }
    [Test] public void HeavyArmorStrengthPenaltyRespects2014DwarfException()
    {
        var d = Draft(); d.abilities = new[] {8,13,14,15,12,10};
        Assert.That(CharacterCreationRules.Calculate(d).speed, Is.EqualTo(20));
        d.edition = RulesEdition.Rules2014;
        Assert.That(CharacterCreationRules.Calculate(d).speed, Is.EqualTo(25));
    }
    [Test] public void DiceScoresRequireSixUniqueAssignments()
    {
        var d = Draft(); d.abilityMethod = AbilityMethod.Manual;
        d.rolledScores = new[] { 15, 14, 13, 12, 10, 8 };
        d.rolledAssignment = new[] { 0, 1, 2, 3, 4, -1 };
        d.abilities = new[] { 15, 14, 13, 12, 10, 10 };
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("Кубики")), Is.True);
        d.rolledAssignment[5] = 5; d.abilities[5] = 8;
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        d.rolledAssignment[5] = 4;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("Кубики")), Is.True);
    }
}
