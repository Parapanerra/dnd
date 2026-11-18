using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // User-provided pre-2024 Blood Hunter PDF, not an invented 2024 revision.
    public static partial class CharacterBloodHunterRules
    {
        public static readonly string[] CurseIds = { "exposure", "anxious", "marked", "puppet", "agony", "mind", "binding", "eyeless" };
        public static readonly string[] CurseNames = { "Прокляття вразливості", "Прокляття тривоги", "Прокляття мітки", "Прокляття впалої маріонетки", "Прокляття роздутої агонії", "Прокляття затьмареного розуму", "Прокляття зв’язування", "Прокляття сліпоти" };
        public static string[] MartialWeapons => CharacterWarlockRules.PactWeapons.Skip(10).Concat(new[] { "Ручний арбалет", "Важкий арбалет", "Довгий лук", "Духова трубка", "Сітка" }).ToArray();
        internal static string PrimaryWeapon(CharacterCreationDraft d) => d.bloodWeapon;
        public static string Damage(string weapon)
        {
            switch (weapon)
            {
                case "Кавалерійський спис": return "1d12";
                case "Тризуб": return "1d6";
                case "Ручний арбалет": return "1d6";
                case "Важкий арбалет": return "1d10";
                case "Довгий лук": return "1d8";
                case "Духова трубка": return "1";
                case "Сітка": return "";
            }
            int index = Array.IndexOf(CharacterWarlockRules.PactWeapons, weapon);
            return index < 0 ? CharacterCreationRules.MonkWeaponDamage(weapon) : CharacterWarlockRules.PactDamage[index];
        }
        public static int WeaponModifier(CharacterCreationDraft d, int[] scores) => WeaponModifier(d.bloodWeapon, scores);
        public static int WeaponModifier(string weapon, int[] scores)
        {
            bool ranged = new[] { "Ручний арбалет", "Важкий арбалет", "Довгий лук", "Духова трубка", "Сітка", "Легкий арбалет", "Короткий лук", "Праща" }.Contains(weapon);
            bool finesse = new[] { "Рапіра", "Скімітар", "Короткий меч", "Батіг", "Кинджал", "Дротик" }.Contains(weapon);
            return CharacterCreationRules.Modifier(ranged ? scores[1] : finesse ? Math.Max(scores[0], scores[1]) : scores[0]);
        }
        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (d.characterClass != "bloodhunter") return null;
            if (page == 0 && (d.bloodAbility != 3 && d.bloodAbility != 4 || d.bloodCurses.Count != CursesKnown(d) || d.bloodCurses.Any(id => !CurseIds.Contains(id)) || d.bloodCurses.Distinct().Count() != CursesKnown(d))) return Text("error.curses");
            if (page == 0 && (d.bloodRites.Count != RitesKnown(d) || !ValidRites(d, d.bloodRites))) return Text("error.rites");
            if (page == 0 && Is(d, "mutant") && (d.bloodFormulas.Count != FormulasKnown(d) || !ValidFormulas(d, d.bloodFormulas))) return Text("error.formulas");
            if (page == 0 && Profane(d) && !Patrons.Contains(d.bloodPatron)) return Text("error.patron");
            if (page == 4 && d.level >= 2 && !Styles.Contains((int)d.fightingStyle)) return Text("error.style");
            if (page == 4 && !d.IsSecondaryTrack && (d.bloodWeaponMode < 0 || d.bloodWeaponMode > 1 ||
                !(d.bloodWeaponMode == 0 ? MartialWeapons : CharacterCreationRules.MonkSimpleWeapons).Contains(d.bloodWeapon) ||
                d.bloodWeaponMode == 1 && !CharacterCreationRules.MonkSimpleWeapons.Contains(d.bloodSecondWeapon) ||
                d.bloodArmor != "Проклепана шкіра" && d.bloodArmor != "Лускатий обладунок")) return "Обери зброю й обладунок мисливця крові.";
            return null;
        }
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d);
            int Mod(int i) => CharacterCreationRules.Modifier(scores[i]);
            bool dwarf = CharacterSpeciesRules.IsDwarf(d);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Distinct().ToList();
            return new CreatedCharacterStats {
                scores = scores, skills = skills, hitPoints = CharacterCreationRules.ClassHitPoints(d, Mod(2)),
                armorClass = d.bloodArmor == "Лускатий обладунок" ? 14 + Math.Min(2, Mod(1)) : 12 + Mod(1),
                initiative = Mod(1), speed = (dwarf ? 25 : 30) + (d.level >= 10 ? 5 : 0) + (Is(d, "lycan") && d.level >= 7 ? 10 : 0), passivePerception = 10 + Mod(4) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = dwarf ? "Дварф (пагорбовий)" : "Людина", backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "",
                features = Features(d) +
                    CharacterBackgroundRules.Features(d) +
                    (dwarf ? CharacterSpeciesRules.Dwarf2014Features(d) : ""),
                equipment = Equipment(d)
            };
        }
        internal static string Equipment(CharacterCreationDraft d) => d.bloodWeapon + (d.bloodWeaponMode == 1 ? "; " + d.bloodSecondWeapon : "") + "; легкий арбалет; 20 болтів; " + d.bloodArmor + "; набір мандрівника; алхімічне приладдя.";
        internal static string CurseText(string id)
        {
            switch (id)
            {
                case "exposure": return "Прокляття вразливості: реакція на шкоду видимій істоті за 30 футів; прибирає опір цим типам шкоди до кінця її наступного ходу. Посилення: замість імунітету дає опір.";
                case "anxious": return "Прокляття тривоги: бонусна дія, 30 футів; перевага на Залякування цілі до кінця твого наступного ходу. Посилення: завада на її наступний ряткидок Мудрості до завершення прокляття.";
                case "marked": return "Прокляття мітки: бонусна дія, 30 футів; до кінця твого ходу додає кістку гемокрафту до шкоди обряду по цілі. Посилення: перевага на твою наступну атаку по ній цього ходу. Потребує активного обряду з 2 рівня.";
                case "puppet": return "Прокляття впалої маріонетки: реакція на падіння істоти за 30 футів до 0 ОЗ; вона робить одну атаку зброєю, потім знов непритомна або мертва. Посилення: спершу рух на половину швидкості та бонус гемокрафту до атаки й шкоди, мінімум +1.";
                case "agony": return "Прокляття роздутої агонії: бонусна дія, 30 футів; до кінця твого наступного ходу завада на перевірки Сили й Спритності, 1к8 некротичної шкоди за понад одну атаку за хід. Посилення: 1 хвилина, ряткидок Статури наприкінці кожного ходу.";
                case "mind": return "Прокляття затьмареного розуму: бонусна дія, 30 футів; завада на наступний ряткидок концентрації до кінця твого наступного ходу. Посилення: завада на всі такі ряткидки за цей час.";
                case "binding": return "Прокляття зв’язування: бонусна дія, 30 футів; ряткидок Сили або швидкість 0 і немає реакцій до кінця твого наступного ходу; ціль максимум на один розмір більша. Посилення: будь-який розмір, 1 хвилина, повторний ряткидок наприкінці ходу.";
                case "eyeless": return "Прокляття сліпоти: реакція на атаку істоти за 30 футів; після кидка, до результату, відніми 1к4 від атаки. Імунітет до сліпоти захищає. Посилення: кожна атака до кінця твого наступного ходу, окремий кидок для кожної.";
                default: return "";
            }
        }
    }
}
