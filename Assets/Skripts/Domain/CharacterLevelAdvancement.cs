using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Scores are applied in level order, before the level-20 Monk capstone.
    public static class CharacterLevelAdvancement
    {
        public static readonly int[] Levels = { 4, 8, 12, 16, 19 };
        public static int[] LevelsFor(CharacterCreationDraft d) => d.characterClass == "fighter" ? new[] { 4, 6, 8, 12, 14, 16, 19 } : d.characterClass == "rogue" ? new[] { 4, 8, 10, 12, 16, 19 } : Levels;
        public static readonly string[] Boons = { "asi", "combat", "travel", "energy", "fate", "fortitude", "offense", "recovery", "skill", "speed", "night", "truesight", "recall" };
        public static readonly string[] BoonNames = { "Підвищення характеристик", "Дар бойової майстерності", "Дар міжпросторових подорожей", "Дар опору енергії", "Дар долі", "Дар міцності", "Дар невідворотного наступу", "Дар відновлення", "Дар умінь", "Дар швидкості", "Дар нічного духа", "Дар істинного зору", "Дар збереження заклинань" };
        public static readonly string[] DamageTypes = { "Кислотна", "Холодна", "Вогняна", "Блискавична", "Некротична", "Отруйна", "Психічна", "Промениста", "Громова" };
        public static bool Active(CharacterCreationDraft d) => CharacterWizardRules.Active(d) || CharacterWarlockRules.Active(d) || CharacterDruidRules.Active(d) || CharacterClericRules.Active(d) || CharacterBloodHunterRules.Active(d) || CharacterArtificerRules.Active(d) || CharacterBardRules.Active(d) || d.characterClass == "sorcerer" || d.characterClass == "ranger" || d.characterClass == "paladin" || d.characterClass == "monk" || d.characterClass == "barbarian" || d.characterClass == "rogue" || d.characterClass == "fighter";
        public static bool Epic(CharacterCreationDraft d) => Active(d) && d.edition == RulesEdition.Rules2024 && d.level >= 19;
        public static CharacterCreationDraft BoonTrack(CharacterCreationDraft d) =>
            Epic(d) || d.secondaryClass?.IsSelected != true ? d : d.secondaryClass.ClassDraft(d.edition);
        public static bool HasBoon(CharacterCreationDraft d, string id)
        {
            var track = BoonTrack(d);
            return Epic(track) && track.epicBoon == id;
        }
        public static int Count(CharacterCreationDraft d) => Active(d) ? LevelsFor(d).Count(level => level <= d.level) : 0;
        internal static bool IsAsi(CharacterCreationDraft d, int index) => LevelsFor(d)[index] != 19 || !Epic(d) || d.epicBoon == "asi";
        public static string[] BoonsFor(CharacterCreationDraft d) => Boons.Where(id => id != "recall" || CharacterWizardRules.Active(d) || CharacterWarlockRules.Active(d) || CharacterDruidRules.Active(d) || CharacterClericRules.Active(d) || CharacterArtificerRules.Active(d) || CharacterBardRules.Active(d) || CharacterSorcererRules.Active(d) || CharacterRangerRules.Active(d) || CharacterPaladinRules.Active(d) || CharacterRogueRules.Trickster(d) || CharacterFighterRules.Caster(d)).ToArray();
        internal static int MaximumScore(CharacterCreationDraft d, int ability)
        {
            int cap = 20;
            if (d.level >= 20 && d.characterClass == "barbarian" && (ability == 0 || ability == 2)) cap = d.edition == RulesEdition.Rules2024 ? 25 : 24;
            if (d.level >= 20 && d.characterClass == "monk" && d.edition == RulesEdition.Rules2024 && (ability == 1 || ability == 4)) cap = 25;
            var boon = BoonTrack(d);
            if (Epic(boon) && boon.epicBoon != "asi" && Boons.Contains(boon.epicBoon) && ability == boon.epicAbility) cap = Math.Max(cap, 30);
            return cap;
        }
        public static int[] EpicAbilities(CharacterCreationDraft d, CharacterCreationDraft track = null)
        {
            track ??= d;
            int[] scores = ScoresWithoutCapstone(d, secondary: track != d ? track : null);
            return (track.epicBoon == "offense" ? new[] { 0, 1 } : track.epicBoon == "recall" ? new[] { 3, 4, 5 } : Enumerable.Range(0, 6).ToArray()).Where(i => scores[i] < 30).ToArray();
        }
        private static int[] BaseScores(CharacterCreationDraft d) => CharacterCreationRules.Scores(new CharacterCreationDraft {
            edition = d.edition, species = d.species, speciesLineage = d.speciesLineage, speciesIncreases = d.speciesIncreases, expandedBonusMode=d.expandedBonusMode, expandedIncreases=d.expandedIncreases,
            abilities = d.abilities, background = d.background, bonusMode = d.bonusMode, threeIncreases = d.threeIncreases,
            increaseTwo = d.increaseTwo, increaseOne = d.increaseOne, characterClass = "", level = 1
        });
        private static int[] ScoresWithoutCapstone(CharacterCreationDraft d, int excludedIndex = -1, bool excludedSecond = false, CharacterCreationDraft secondary = null)
        {
            int[] scores = BaseScores(d);
            AddIncreases(d, scores, secondary == null ? excludedIndex : -1, excludedSecond);
            var secondTrack = secondary ?? (d.secondaryClass?.IsSelected == true ? d.secondaryClass.ClassDraft(d.edition) : null);
            if (secondTrack != null) AddIncreases(secondTrack, scores, secondary == null ? -1 : excludedIndex, excludedSecond);
            return scores;
        }
        private static void AddIncreases(CharacterCreationDraft d, int[] scores, int excludedIndex = -1, bool excludedSecond = false)
        {
            EnsureArrays(d);
            for (int i = 0; i < Count(d); i++)
            {
                if (!IsAsi(d, i) || d.advancementModes[i] < 0) continue;
                int a = d.advancementFirst[i], b = d.advancementSecond[i];
                if ((i != excludedIndex || excludedSecond) && a >= 0 && a < 6) scores[a] += d.advancementModes[i] == 0 ? 2 : 1;
                if ((i != excludedIndex || !excludedSecond) && d.advancementModes[i] == 1 && b >= 0 && b < 6 && b != a) scores[b]++;
            }
        }
        public static int[] AbilityOptions(CharacterCreationDraft d, int index, bool second = false, CharacterCreationDraft track = null)
        {
            track ??= d;
            int[] scores = ScoresWithoutCapstone(d, index, second, track != d ? track : null);
            int amount = !second && track.advancementModes[index] == 0 ? 2 : 1;
            return Enumerable.Range(0, 6).Where(i => scores[i] + amount <= 20 && (second ? i != track.advancementFirst[index] : track.advancementModes[index] != 1 || i != track.advancementSecond[index])).ToArray();
        }
        public static void RepairChoices(CharacterCreationDraft d, CharacterCreationDraft track = null)
        {
            track ??= d;
            if (d.IsSecondaryTrack || !Active(track) || d.abilities == null || d.abilities.Length != 6) return;
            EnsureArrays(track);
            int[] scores = BaseScores(d);
            if (track != d) AddIncreases(d, scores);
            else if (d.secondaryClass?.IsSelected == true) AddIncreases(d.secondaryClass.ClassDraft(d.edition), scores);
            for (int i = 0; i < Count(track); i++)
            {
                if (!IsAsi(track, i) || track.advancementModes[i] < 0) continue;
                int a = track.advancementFirst[i], b = track.advancementSecond[i], amount = track.advancementModes[i] == 0 ? 2 : 1;
                if (a < 0 || a >= 6 || scores[a] + amount > 20) track.advancementFirst[i] = -1;
                else scores[a] += amount;
                if (track.advancementModes[i] != 1 || b < 0 || b >= 6 || b == a || scores[b] >= 20) track.advancementSecond[i] = -1;
                else scores[b]++;
            }
            if (Epic(track) && !EpicAbilities(d, track).Contains(track.epicAbility)) track.epicAbility = -1;
        }

        public static void EnsureArrays(CharacterCreationDraft d)
        {
            d.advancementModes = Resize(d.advancementModes, LevelsFor(d).Length);
            d.advancementFirst = Resize(d.advancementFirst, LevelsFor(d).Length);
            d.advancementSecond = Resize(d.advancementSecond, LevelsFor(d).Length);
            if (d.epicResistances == null || d.epicResistances.Length != 2) d.epicResistances = new[] { -1, -1 };
        }
        private static int[] Resize(int[] values, int size)
        {
            if (values != null && values.Length == size) return values;
            var result = Enumerable.Repeat(-1, size).ToArray();
            if (values != null) Array.Copy(values, result, Math.Min(values.Length, result.Length));
            return result;
        }
        internal static void ApplyScores(CharacterCreationDraft d, int[] scores)
        {
            if (!Active(d)) return;
            EnsureArrays(d);
            for (int i = 0; i < Count(d); i++)
            {
                if (!IsAsi(d, i)) continue;
                int a = d.advancementFirst[i], b = d.advancementSecond[i];
                if (a >= 0 && a < 6 && d.advancementModes[i] >= 0)
                    scores[a] = Math.Min(20, scores[a] + (d.advancementModes[i] == 0 ? 2 : 1));
                if (d.advancementModes[i] == 1 && b >= 0 && b < 6 && b != a) scores[b] = Math.Min(20, scores[b] + 1);
            }
            var secondary = d.secondaryClass?.IsSelected == true ? d.secondaryClass.ClassDraft(d.edition) : null;
            if (secondary != null) AddIncreases(secondary, scores);
            var boon = Epic(d) ? d : secondary;
            if (boon != null && Epic(boon) && Boons.Contains(boon.epicBoon) && boon.epicBoon != "asi" &&
                EpicAbilities(d, boon).Contains(boon.epicAbility))
                scores[boon.epicAbility] = Math.Min(30, scores[boon.epicAbility] + 1);
            if (d.level >= 20 && d.characterClass == "barbarian")
            {
                int cap = d.edition == RulesEdition.Rules2024 ? 25 : 24;
                scores[0] = Math.Min(cap, scores[0] + 4);
                scores[2] = Math.Min(cap, scores[2] + 4);
            }
            else if (d.characterClass == "monk" && d.edition == RulesEdition.Rules2024 && d.level >= 20)
            {
                scores[1] = Math.Min(25, scores[1] + 4);
                scores[4] = Math.Min(25, scores[4] + 4);
            }
        }
        public static string ChoiceError(CharacterCreationDraft d, CharacterCreationDraft track = null)
        {
            track ??= d;
            if (!Active(track)) return null;
            EnsureArrays(track);
            if (Epic(track))
            {
                if (!BoonsFor(track).Contains(track.epicBoon)) return "Обери дар або підвищення характеристик на 19 рівні.";
                if (track.epicBoon != "asi" && !EpicAbilities(d, track).Contains(track.epicAbility)) return "Обери характеристику для епічного дару.";
                if (track.epicBoon == "skill" && (track.epicSkill < 0 || track.epicSkill >= 18)) return "Обери експертизу дару умінь.";
                if (track.epicBoon == "energy" && (track.epicResistances.Any(x => x < 0 || x >= DamageTypes.Length) || track.epicResistances[0] == track.epicResistances[1]))
                    return "Обери два різні опори епічного дару.";
            }
            for (int i = 0; i < Count(track); i++)
            {
                if (!IsAsi(track, i)) continue;
                int a = track.advancementFirst[i], b = track.advancementSecond[i], mode = track.advancementModes[i];
                if (mode < 0 || mode > 1 || a < 0 || a >= 6 || mode == 1 && (b < 0 || b >= 6 || a == b))
                    return "Обери підвищення характеристик для кожного доступного рівня.";
            }
            if (ScoresWithoutCapstone(d, secondary: track != d ? track : null).Any(x => x > 20))
                return "Підвищення характеристик не може перевищувати 20.";
            return null;
        }
        internal static CreatedCharacterStats Apply(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            if (HasBoon(d, "fortitude")) stats.hitPoints += 40;
            if (HasBoon(d, "speed")) stats.speed += 30;
            if (HasBoon(d, "skill"))
            {
                stats.skills = Enumerable.Range(0, 18).ToList();
                stats.expertise ??= new System.Collections.Generic.List<int>();
                int skill = BoonTrack(d).epicSkill;
                if (skill >= 0 && skill < 18 && !stats.expertise.Contains(skill)) stats.expertise.Add(skill);
            }
            var boon = BoonTrack(d);
            string feature = BoonFeature(boon.epicBoon);
            if (Epic(boon) && feature.Length > 0) stats.features += "\n" + feature;
            return stats;
        }
        public static string BoonFeature(string id)
        {
            switch (id)
            {
                case "combat": return "Дар бойової майстерності: перетвори промах на влучання; відновлення на початку твого наступного ходу.";
                case "travel": return "Дар міжпросторових подорожей: одразу після дії Атака або Магія телепортуйся на 30 футів у видиме вільне місце.";
                case "energy": return "Дар опору енергії: два обрані опори можна змінювати після довгого відпочинку. Реакція на таку шкоду — видима ціль у 60 футах без повного укриття: ряткидок Спритності (8 + Статура + майстерність) або 2к12 + Статура шкоди того самого типу.";
                case "fate": return "Дар долі: після успіху або провалу кидка к20 у 60 футах додай або відніми 2к4. Відновлення при ініціативі, короткому чи довгому відпочинку.";
                case "fortitude": return "Дар міцності: +40 максимальних ОЗ; при відновленні ОЗ можна додати модифікатор Статури раз до початку наступного ходу.";
                case "offense": return "Дар невідворотного наступу: дробильна, колюча й рубана шкода ігнорують опір. Натуральна 20 на атаці додає шкоду за значенням характеристики, підвищеної цим даром.";
                case "recovery": return "Дар відновлення: замість падіння до 0 ОЗ залишся з 1 й віднови половину максимальних ОЗ (раз за довгий відпочинок). Також запас 10к10: бонусною дією витрачай кубики на лікування; довгий відпочинок повертає всі.";
                case "skill": return "Дар умінь: володіння всіма навичками та експертиза однієї обраної навички.";
                case "speed": return "Дар швидкості: +30 футів швидкості; Відхід бонусною дією також завершує твоє захоплення.";
                case "night": return "Дар нічного духа: у тьмяному світлі чи темряві бонусною дією стань Невидимим до наступної дії, бонусної дії або реакції; там же опір усій шкоді, крім психічної та променистої.";
                case "recall": return "Дар збереження заклинань: при накладанні спела коміркою 1–4 круга кинь к4; якщо число дорівнює кругу комірки, вона не витрачається.";
                case "truesight": return "Дар істинного зору: істинний зір на 60 футів.";
                default: return "";
            }
        }
    }
}
