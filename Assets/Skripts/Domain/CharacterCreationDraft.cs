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
        public RulesEdition edition = RulesEdition.Rules2024;
        public string characterName = "";
        public string species = "dwarf";
        public string background = "soldier";
        public AbilityMethod abilityMethod;
        // STR, DEX, CON, INT, WIS, CHA. Origin bonuses are calculated separately.
        public int[] abilities = { 15, 13, 14, 8, 12, 10 };
        // Six 4d6-drop-lowest results and the result index assigned to each ability (-1 = unassigned).
        public int[] rolledScores = Array.Empty<int>();
        public int[] rolledAssignment = { -1, -1, -1, -1, -1, -1 };
        public int increaseTwo = 0;
        public int increaseOne = 2;
        public bool threeIncreases;
        public int thirdIncrease = 1;
        public List<int> classSkills = new List<int> { 11, 17 };
        public int humanSkill = 9;
        public FighterStyle fightingStyle;
        public int equipment;
        public string language = "Ельфійська";
        public string secondLanguage = "Дварфська";
        public string artisanTool = "Інструменти коваля";
        public string gamingSet = "Гральні кості";
        public string alignment = "Нейтральний";
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
