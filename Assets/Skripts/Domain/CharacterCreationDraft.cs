using System;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    public enum RulesEdition { Rules2014, Rules2024 }
    public enum AbilityMethod { StandardArray, PointBuy, Manual }
    public enum FighterStyle { Defense, Dueling, Archery, GreatWeapon, Protection, TwoWeapon, BlindFighting, Interception, ThrownWeapon, Unarmed, BlessedWarrior, DruidicWarrior }

    [Serializable]
    public sealed class CharacterOriginFeatChoice
    {
        public string id = "";
        public int ability = -1;
        public string[] cantrips = { "", "" };
        public string spell = "";
        // Stable skill:<index> / tool:<canonical name> values, separate from class proficiency choices.
        public string[] selections = { "", "", "" };
    }

    [Serializable]
    public sealed class CharacterCreationDraft : CharacterClassChoices
    {
        public RulesEdition edition = (RulesEdition)(-1);
        public string characterClass = "";
        public int level = -1;
        public CharacterSecondaryClass secondaryClass = new CharacterSecondaryClass();
        public bool preferWideMagicResourceRows;
        public int TotalLevel => multiclassTotalLevel ?? level + (secondaryClass?.IsSelected == true ? secondaryClass.level : 0);
        [NonSerialized] internal int? multiclassTotalLevel;
        [NonSerialized] internal int[] multiclassExistingSkills;
        [NonSerialized] internal string[] multiclassExistingLanguages;
        [NonSerialized] internal int[] multiclassScores;
        [NonSerialized] internal CharacterCreationDraft multiclassOwner;
        public CharacterCreationDraft PrimaryCharacter => multiclassOwner ?? this;
        public bool IsSecondaryTrack => multiclassExistingSkills != null;
    }

    [Serializable]
    public class CharacterClassChoices
    {
        private static readonly System.Reflection.FieldInfo[] ChoiceFields = typeof(CharacterClassChoices).GetFields(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        internal void CopyChoicesTo(CharacterClassChoices target)
        {
            foreach (var field in ChoiceFields) field.SetValue(target, field.GetValue(this));
        }

        // One entry per level after the first: 0 = unset, -1 = fixed value, positive = rolled die.
        public int[] hitPointChoices = new int[19];
        public string subclass = "";
        public string characterName = "";
        public string species = "";
        public string speciesLineage = "";
        public int originSkillReplacement = -1;
        public int speciesSkill = -1;
        public int speciesMagicAbility = -1;
        public string speciesCantrip = "";
        public string speciesLanguage = "";
        public int speciesAncestor = -1;
        public string speciesSize = "";
        public string speciesTool = "";
        public int expandedBonusMode=-1;
        public int[] expandedIncreases={-1,-1,-1};
        public int[] expandedSkills={-1,-1};
        public string expandedTranceProficiency="";
        public string expandedSecondTranceProficiency="";
        public string expandedInstrument="";
        public string expandedCantrip="";
        public string expandedGift="";
        public bool expandedUnarmored;
        public string[] simicEnhancements={"",""};
        public string[] expandedTools={"",""};
        public int expandedMagicAbility=-1;
        public int secondOriginSkillReplacement=-1;
        public int[] speciesIncreases = { -1, -1 };
        public int[] speciesSkills = { -1, -1 };
        public string originToolReplacement = "";
        public string background = "";
        public string backgroundTool = "";
        public string backgroundItem = "";
        public string[] backgroundLanguages = { "", "" };
        public string[] backgroundToolReplacements = { "", "" };
        public CharacterOriginFeatChoice backgroundFeat = new CharacterOriginFeatChoice();
        public CharacterOriginFeatChoice humanFeat = new CharacterOriginFeatChoice();
        public AbilityMethod abilityMethod = (AbilityMethod)(-1);
        // STR, DEX, CON, INT, WIS, CHA. Origin bonuses are calculated separately.
        public int[] abilities = new int[6];
        // Six 4d6-drop-lowest results and the result index assigned to each ability (-1 = unassigned).
        public int[] rolledScores = Array.Empty<int>();
        public int[] rolledDice = Array.Empty<int>();
        public int rolledCount;
        public int[] rolledAssignment = { -1, -1, -1, -1, -1, -1 };
        public int increaseTwo = -1;
        public int increaseOne = -1;
        public int bonusMode = -1;
        public bool threeIncreases;
        public int thirdIncrease = -1;
        public List<int> classSkills = new List<int>();
        // -1 represents Thieves' Tools expertise (available to the 2014 Rogue).
        public List<int> expertise = new List<int>();
        public List<int> weaponMasteries = new List<int>();
        public List<string> preparedSpells = new List<string>();
        public List<string> cantrips = new List<string>();
        public List<int> bardLoreSkills = new List<int>();
        public List<string> bardSecrets = new List<string>();
        public List<string> bardInstruments = new List<string>();
        public string bardWeapon = "";
        public int bardPack = -1;
        public string bardCarriedInstrument = "";
        public string primalOrder = "";
        public string druidFirstItem = "";
        public string druidWeapon = "";
        public string druidLand = "";
        public string druidFury = "";
        public List<string> druidKnownForms = new List<string>();
        public string divineOrder = "";
        public List<int> knowledgeSkills = new List<int>();
        public List<string> knowledgeLanguages = new List<string>();
        public int natureSkill = -1;
        public string natureCantrip = "";
        public string clericWeapon = "";
        public string clericArmor = "";
        public string clericSecondWeapon = "";
        public int clericPack = -1;
        public List<string> spellbookSpells = new List<string>();
        public string wizardWeapon = "";
        public string wizardFocus = "";
        public int wizardPack = -1;
        public int wizardScholar = -1;
        public List<string> wizardSavant = new List<string>();
        public string wizardMasteryOne = "";
        public string wizardMasteryTwo = "";
        public List<string> wizardSignature = new List<string>();
        public int dragonAncestor = -1;
        public int sorcererAffinity = -1;
        public List<string> sorcererMetamagic = new List<string>();
        public string sorcererWeapon = "";
        public string sorcererFocus = "";
        public int sorcererPack = -1;
        public string invocation = ""; // Legacy first-invocation draft compatibility.
        public List<string> warlockInvocations = new List<string>();
        public List<string> warlockArcana = new List<string>();
        public List<CharacterOriginFeatChoice> warlockLessons = new List<CharacterOriginFeatChoice>();
        public string warlockPact = "";
        public int warlockResistance = -1;
        public int pactWeapon = -1;
        public List<string> tomeCantrips = new List<string>();
        public List<string> tomeRituals = new List<string>();
        public string warlockWeapon = "";
        public string warlockSecondWeapon = "";
        public string warlockFocus = "";
        public int warlockPack = -1;
        public string artificerTool = "";
        public string artificerModel = "";
        public string artificerStoredSpell = "";
        public List<string> artificerPlans = new List<string>();
        public List<string> artificerPlanDetails = new List<string>();
        // Empty = learned plan only. Explicit physical targets used for creation-time equipment.
        public List<string> artificerEquippedTargets = new List<string>();
        public List<string> artificerToolReplacements = new List<string>();
        public string artificerWeapon = "";
        public string artificerSecondWeapon = "";
        public string artificerArmor = "";
        public int bloodAbility = -1;
        public string bloodCurse = "";
        public List<string> bloodCurses = new List<string>();
        public List<string> bloodRites = new List<string>();
        public List<string> bloodFormulas = new List<string>();
        public string bloodPatron = "";
        public int bloodWeaponMode = -1;
        public string bloodWeapon = "";
        public string bloodSecondWeapon = "";
        public string bloodArmor = "";
        public string clericStrike = "";
        public int primalKnowledgeSkill = -1;
        public int humanSkill = -1;
        public FighterStyle fightingStyle = (FighterStyle)(-1);
        public int fighterSecondStyle = -1;
        public string fighterTool = "";
        public int fighterSkill = -1;
        public List<string> fighterManeuvers = new List<string>();
        public int equipment = -1;
        public int backgroundEquipmentChoice = -1;
        public string language = "";
        public string secondLanguage = "";
        public string rogueLanguage = "";
        public string rangerEnemy = "";
        public string rangerHumanoids = "";
        public string rangerTerrain = "";
        public string rangerLanguage = "";
        public string[] rangerExtraEnemies = { "", "" }, rangerExtraHumanoids = { "", "" }, rangerExtraTerrains = { "", "" }, rangerExtraLanguages = { "", "" };
        public string[] rangerDeftLanguages = { "", "" };
        public string rangerHunterPrey = "", rangerHunterDefense = "", rangerHunterMultiattack = "", rangerHunterSuperior = "";
        public int rangerFeySkill = -1;
        public string artisanTool = "";
        public string monkTool = "";
        public int monkPack = -1;
        public string monkWeapon = "";
        public string gamingSet = "";
        public string alignment = "";
        // Explicit level-up choices. Indexes correspond to levels 4, 8, 12, 16, 19.
        public int[] advancementModes = { -1, -1, -1, -1, -1 };
        public int[] advancementFirst = { -1, -1, -1, -1, -1 };
        public int[] advancementSecond = { -1, -1, -1, -1, -1 };
        public string epicBoon = "";
        public int epicAbility = -1;
        public int epicSkill = -1;
        public int[] epicResistances = { -1, -1 };
        public string barbarianSpirit = "", barbarianAspect = "", barbarianAttunement = "";
        public List<string> monkDisciplines = new List<string>();
        public int[] mercySkillReplacements = { -1, -1 };
    }

    public sealed class CreatedCharacterStats
    {
        public IReadOnlyList<int> SkillModifiers { get; internal set; }
        public int[] scores;
        public int hitPoints, armorClass, initiative, speed, passivePerception;
        public int secondWindUses;
        public List<int> skills;
        public List<int> expertise;
        public string speciesName, backgroundName, equipment, features, proficiencies;
    }
}
