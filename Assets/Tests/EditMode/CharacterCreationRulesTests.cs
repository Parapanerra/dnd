using System.Linq;
using NUnit.Framework;
using Taruck.CharacterCreation;

public class CharacterCreationRulesTests
{
    private static CharacterCreationDraft Draft() => new CharacterCreationDraft {
        characterName = "Тест", characterClass = "fighter", edition = RulesEdition.Rules2024, level = 1,
        species = "dwarf", background = "soldier", abilityMethod = AbilityMethod.StandardArray,
        abilities = new[] { 15, 13, 14, 8, 12, 10 }, bonusMode = 0,
        increaseTwo = 0, increaseOne = 2, classSkills = new System.Collections.Generic.List<int> { 11, 17 },
        weaponMasteries = new System.Collections.Generic.List<int> { 0, 1, 2 },
        humanSkill = 6, fightingStyle = FighterStyle.Defense, equipment = 0,
        backgroundEquipmentChoice = 0, language = "Ельфійська", secondLanguage = "Гномська",
        gamingSet = "Гральні кості", artisanTool = "Інструменти коваля", alignment = "Нейтральний"
    };
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
        Assert.That(d.increaseTwo, Is.EqualTo(-1));
        Assert.That(d.classSkills, Is.EqualTo(new[] {11,17}));
    }
    [Test] public void Human2024GetsToughAndExtraUniqueSkill()
    {
        var d = Draft(); d.species = "human"; d.speciesSize = "medium"; d.humanFeat.id = "tough";
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

    [Test]
    public void SecondaryBardAddsHalfProficiencyOnlyToUntrainedSkills()
    {
        var draft = Draft();
        draft.secondaryClass = new CharacterSecondaryClass { classId = "bard", level = 2 };
        var stats = CharacterCreationRules.Calculate(draft);
        const int acrobatics = 0;
        const int perception = 11;

        Assert.That(stats.skills.Contains(acrobatics), Is.False);
        Assert.That(stats.SkillModifiers[acrobatics], Is.EqualTo(CharacterCreationRules.Modifier(stats.scores[1]) + 1));
        Assert.That(stats.SkillModifiers[perception], Is.EqualTo(CharacterCreationRules.Modifier(stats.scores[4]) + 2));
        Assert.That(stats.passivePerception, Is.EqualTo(10 + stats.SkillModifiers[perception]));
    }

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
    [Test] public void FighterLevelTwoUsesFixedOrRolledHitPointsWithoutChangingFirstLevel()
    {
        var d = new CharacterCreationDraft {
            characterClass = "fighter", edition = RulesEdition.Rules2014, species = "human", background = "soldier",
            abilities = new[] { 14, 12, 14, 10, 10, 10 }, equipment = 0,
            fightingStyle = FighterStyle.Defense, level = 1
        };
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(12));
        d.level = 2; d.hitPointChoices[0] = -1;
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(20));
        d.hitPointChoices[0] = 1;
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(15));
    }
    [Test] public void LevelTwoRequiresAnExplicitHitPointChoice()
    {
        var d = new CharacterCreationDraft { characterClass = "fighter", level = 2 };
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("ОЗ для кожного")), Is.True);
        d.hitPointChoices[0] = -1;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("ОЗ для кожного")), Is.False);
    }
    [Test] public void ChampionLevelThreeRequiresTwoHitPointChoicesAndSubclass()
    {
        var d = new CharacterCreationDraft {
            characterClass = "fighter", edition = RulesEdition.Rules2024, species = "dwarf", background = "soldier",
            abilities = new[] { 14, 12, 14, 10, 10, 10 }, equipment = 0,
            fightingStyle = FighterStyle.Defense, level = 3
        };
        d.hitPointChoices[0] = -1;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("ОЗ для кожного")), Is.True);
        d.hitPointChoices[1] = -1;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("підклас")), Is.True);
        d.subclass = "champion";
        var stats = CharacterCreationRules.Calculate(d);
        Assert.That(stats.hitPoints, Is.EqualTo(31));
        Assert.That(stats.features, Does.Contain("Видатний атлет"));
    }
    [Test] public void BarbarianUsesD12RageAndUnarmoredDefense()
    {
        var d = Draft();
        d.characterClass = "barbarian"; d.weaponMasteries = new System.Collections.Generic.List<int> { 0, 1 };
        d.fightingStyle = (FighterStyle)(-1);
        d.classSkills = new System.Collections.Generic.List<int> { 11, 17 };
        d.abilities = new[] { 15, 13, 14, 8, 12, 10 };
        var levelOne = CharacterCreationRules.Calculate(d);
        Assert.That(levelOne.hitPoints, Is.EqualTo(15)); // d12 + Con 2 + Dwarf 1
        Assert.That(levelOne.armorClass, Is.EqualTo(13)); // 10 + Dex 1 + Con 2
        Assert.That(CharacterBarbarianRules.Rages(d), Is.EqualTo(2));
        Assert.That(levelOne.features, Does.Contain("Лють 2024:"));
        d.level = 3;
        d.hitPointChoices[0] = -1;
        d.hitPointChoices[1] = 12;
        d.subclass = "berserker";
        d.primalKnowledgeSkill = 10;
        var levelThree = CharacterCreationRules.Calculate(d);
        Assert.That(levelThree.hitPoints, Is.EqualTo(40));
        Assert.That(levelThree.features, Does.Contain("2к6"));
        Assert.That(levelThree.skills, Does.Contain(10));
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
    }
    [Test] public void BarbarianSubclassAndPrimalKnowledgeAreExplicitChoices()
    {
        var d = Draft();
        d.characterClass = "barbarian"; d.weaponMasteries = new System.Collections.Generic.List<int> { 0, 1 };
        d.classSkills = new System.Collections.Generic.List<int> { 11, 17 };
        d.level = 3; d.hitPointChoices[0] = -1; d.hitPointChoices[1] = -1;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("підклас")), Is.True);
        d.subclass = "berserker";
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("Первісного")), Is.True);
        d.primalKnowledgeSkill = 10;
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        d.classSkills[0] = 10;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("Первісного")), Is.True);
    }

    [Test] public void MonkLevelOneUsesEditionSpecificMartialArtsAndExplicitTool()
    {
        var d = Draft();
        d.characterClass = "monk";
        d.fightingStyle = (FighterStyle)(-1);
        d.classSkills = new System.Collections.Generic.List<int> { 0, 14 };
        d.weaponMasteries.Clear();
        d.monkTool = "Лютня";
        d.monkPack = 1;
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        var modern = CharacterCreationRules.Calculate(d);
        Assert.That(modern.armorClass, Is.EqualTo(12));
        Assert.That(CharacterMonkRules.MartialDie(d), Is.EqualTo("1d6"));
        Assert.That(modern.features, Does.Contain("Кістка бойових мистецтв: 1d6."));
        d.edition = RulesEdition.Rules2014;
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        var classic = CharacterCreationRules.Calculate(d);
        Assert.That(classic.armorClass, Is.EqualTo(12));
        Assert.That(CharacterMonkRules.MartialDie(d), Is.EqualTo("1d4"));
        Assert.That(classic.features, Does.Contain("Кістка бойових мистецтв: 1d4."));
        Assert.That(classic.equipment, Does.Contain("набір мандрівника"));
        d.level = 2;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("Обери ОЗ")), Is.True);
        d.hitPointChoices[0] = -1;
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints - classic.hitPoints,
            Is.EqualTo(5 + CharacterCreationRules.Modifier(classic.scores[2]) + CharacterSpeciesRules.HitPointsPerLevel(d)));
        Assert.That(CharacterCreationRules.Calculate(d).speed, Is.EqualTo(classic.speed + 10));
    }

    [TestCase(RulesEdition.Rules2014)]
    [TestCase(RulesEdition.Rules2024)]
    public void MonkOpenHandRequiresSubclassAndUsesEditionSpecificDeflection(RulesEdition edition)
    {
        var d = Draft(); d.edition = edition; d.characterClass = "monk"; d.level = 3;
        d.classSkills = new System.Collections.Generic.List<int> { 0, 14 };
        d.weaponMasteries.Clear(); d.monkTool = "Лютня"; d.monkPack = 1;
        d.hitPointChoices[0] = -1; d.hitPointChoices[1] = 8;
        Assert.That(CharacterCreationRules.Validate(d).Any(x => x.Contains("підклас")), Is.True);
        d.subclass = "openhand";
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        var stats = CharacterCreationRules.Calculate(d);
        Assert.That(CharacterMonkRules.Points(d), Is.EqualTo(3));
        Assert.That(stats.features, Does.Contain(edition == RulesEdition.Rules2024 ? "Відбиття атак:" : "Відбиття снарядів:"));
        Assert.That(stats.features.Contains("Незвичайний метаболізм:"), Is.EqualTo(edition == RulesEdition.Rules2024));
        Assert.That(CharacterMonkRules.WeaponModifier(d, new[] { 16, 10, 10, 10, 10, 10 }, "Спис"), Is.EqualTo(3));
    }
    [Test] public void HumanOriginFeatIsRequiredAndToughIsNotAutomatic()
    {
        var d = Draft(); d.species = "human"; d.speciesSize = "medium";
        Assert.That(CharacterCreationRules.Validate(d).Any(error => error.Contains("рису походження людини")), Is.True);
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(12));
        d.humanFeat.id = "alert";
        Assert.That(CharacterCreationRules.Validate(d), Is.Empty);
        Assert.That(CharacterCreationRules.Calculate(d).hitPoints, Is.EqualTo(12));
        Assert.That(CharacterCreationRules.Calculate(d).initiative, Is.EqualTo(3));
    }
    [Test] public void BackgroundGoldUsesTheChosenPackageAndEdition()
    {
        var d = Draft(); d.background = "farmer";
        Assert.That(CharacterBackgroundRules.Gold(d), Is.EqualTo(30));
        d.backgroundEquipmentChoice = 1;
        Assert.That(CharacterBackgroundRules.Gold(d), Is.EqualTo(50));
        d.edition = RulesEdition.Rules2014; d.background = "noble";
        Assert.That(CharacterBackgroundRules.Gold(d), Is.EqualTo(25));
        d.background = "hermit";
        Assert.That(CharacterBackgroundRules.Gold(d), Is.EqualTo(5));
    }
    [Test] public void HumanCannotDuplicateNonRepeatableBackgroundFeat()
    {
        var d = Draft(); d.species = "human"; d.background = "farmer"; d.humanFeat.id = "tough";
        CharacterCreationRules.RepairDependentChoices(d);
        Assert.That(d.humanFeat.id, Is.Empty);
        Assert.That(CharacterOriginFeatRules.HumanOptions(d), Does.Not.Contain("tough"));
        d.background = "noble"; CharacterCreationRules.RepairDependentChoices(d);
        Assert.That(CharacterOriginFeatRules.HumanOptions(d), Does.Contain("skilled"));
    }
    [Test] public void ClassicHalfElfHasIndependentBonusAndSkillChoices()
    {
        var d = Draft(); d.edition = RulesEdition.Rules2014; d.species = "halfelf";
        Assert.That(CharacterSpeciesRules.ChoiceError(d), Is.Not.Null);
        d.speciesIncreases = new[] { 0, 2 };
        Assert.That(CharacterCreationRules.Scores(d), Is.EqualTo(new[] { 16, 13, 15, 8, 12, 12 }));
        Assert.That(CharacterSpeciesRules.ChoiceError(d, 3), Is.Not.Null);
        d.speciesSkills = new[] { 4, 14 }; d.speciesLanguage = "Гномська";
        Assert.That(CharacterSpeciesRules.ChoiceError(d, 3), Is.Null);
        d.speciesSkills[1] = 4;
        Assert.That(CharacterSpeciesRules.ChoiceError(d, 3), Is.Not.Null);
    }

}
