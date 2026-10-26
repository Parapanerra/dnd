using System;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    public enum RulesEdition { Rules2014, Rules2024 }
    public enum AbilityMethod { StandardArray, PointBuy, Manual }
    public enum FighterStyle { Defense, Dueling, Archery, GreatWeapon }

    [Serializable]
    public sealed class CharacterCreationDraft
    {
        public RulesEdition edition = (RulesEdition)(-1);
        public string characterName = "";
        public string species = "";
        public string background = "";
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
        public int humanSkill = -1;
        public FighterStyle fightingStyle = (FighterStyle)(-1);
        public int equipment = -1;
        public int backgroundEquipmentChoice = -1;
        public string language = "";
        public string secondLanguage = "";
        public string artisanTool = "";
        public string gamingSet = "";
        public string alignment = "";
    }

    public sealed class CreatedCharacterStats
    {
        public int[] scores;
        public int hitPoints, armorClass, initiative, speed, passivePerception;
        public int secondWindUses;
        public List<int> skills;
        public string speciesName, backgroundName, equipment, features, proficiencies;
    }
}
