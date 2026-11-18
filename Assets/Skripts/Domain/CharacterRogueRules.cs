using System;
using System.Linq;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    public static class CharacterRogueRules
    {
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "rogue";
        internal static bool Modern(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static string[] Options(CharacterCreationDraft d) => Modern(d) ? new[] { "thief", "assassin", "trickster", "soulknife" } : new[] { "thief", "assassin", "trickster" };
        public static string Name(string id) => id == "thief" ? "Злодій" : id == "assassin" ? "Убивця" : id == "trickster" ? "Містичний шахрай" : id == "soulknife" ? "Психічний клинок" : "";
        public static bool Is(CharacterCreationDraft d, string id) => Active(d) && d.level >= 3 && d.subclass == id && Options(d).Contains(id);
        public static bool Trickster(CharacterCreationDraft d) => Is(d, "trickster");
        internal static int SpellCount(CharacterCreationDraft d) => !Trickster(d) ? 0 : new[] {0,0,3,4,4,4,5,6,6,7,8,8,9,10,10,11,11,11,12,13}[Math.Clamp(d.level,1,20)-1];
        internal static int Cantrips(CharacterCreationDraft d) => !Trickster(d) ? 0 : d.level >= 10 ? 3 : 2;
        internal static int SlotCount(CharacterCreationDraft d, int circle)
        {
            if (!Trickster(d)) return 0;
            int l=d.level;
            return circle == 1 ? l >= 7 ? 4 : l >= 4 ? 3 : 2 : circle == 2 ? l >= 10 ? 3 : l >= 7 ? 2 : 0 : circle == 3 ? l >= 16 ? 3 : l >= 13 ? 2 : 0 : circle == 4 && l >= 19 ? 1 : 0;
        }
        public static int ExpertiseCount(CharacterCreationDraft d) => d.level >= 6 ? 4 : 2;
        public static string SneakDie(CharacterCreationDraft d) => ((Math.Max(1, d.level) + 1) / 2) + "d6";
        public static bool SavingThrow(CharacterCreationDraft d, int ability) => Active(d) && (ability == 1 || ability == 3 || d.level >= 15 && (ability == 4 || Modern(d) && ability == 5));
        public static int PsiDice(CharacterCreationDraft d) => d.level >= 17 ? 12 : d.level >= 13 ? 10 : d.level >= 9 ? 8 : d.level >= 5 ? 6 : 4;
        public static int PsiFaces(CharacterCreationDraft d) => d.level >= 17 ? 12 : d.level >= 11 ? 10 : d.level >= 5 ? 8 : 6;
        public static string[] Tools(CharacterCreationDraft d) => Is(d, "assassin") ? new[] { "Злодійські інструменти", "Набір гримувальника", "Набір отруйника" } : new[] { "Злодійські інструменти" };
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] s = CharacterCreationRules.Scores(d);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Where(x => x >= 0).Distinct().ToList();
            if (Modern(d) && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            return new CreatedCharacterStats {
                scores = s, skills = skills.Distinct().ToList(), expertise = d.expertise.Where(x => x >= 0).ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, CharacterCreationRules.Modifier(s[2])),
                armorClass = (Modern(d) && d.equipment == 0 ? 12 : Modern(d) && d.equipment == 1 ? 10 : 11) + CharacterCreationRules.Modifier(s[1]),
                initiative = CharacterCreationRules.Modifier(s[1]), speed = CharacterSpeciesRules.IsDwarf(d) && !Modern(d) ? 25 : 30,
                passivePerception = 10 + CharacterCreationRules.Modifier(s[4]) + (d.expertise.Contains(11) ? 2 : skills.Contains(11) ? 1 : 0) * CharacterLegacySpeciesRules.Proficiency(d),
                speciesName = CharacterSpeciesRules.Name(d), backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "",
                equipment = CharacterCreationRules.Equipment(d), proficiencies = "Легкі обладунки; злодійські інструменти.",
                features = Features(d) + "\n" + SubclassFeatures(d) + CharacterBackgroundRules.Features(d)
            };
        }
        public static string Features(CharacterCreationDraft d)
        {
            int level = d.level; bool modern = Modern(d);
            var lines = new List<string> { "Прихована атака: раз за хід, фехтувальна або далекобійна зброя; потрібна перевага або інший не недієздатний ворог цілі в 5 футах від неї без перешкоди на атаку. Кістки записані в окремому полі.", "Злодійський жаргон: таємні знаки й умовна мова." };
            if (modern) lines.Add("Майстерність зброї: " + string.Join(", ", d.weaponMasteries.Where(i => i >= 0 && i < CharacterCreationRules.MasteryWeapons.Count).Select(i => CharacterCreationRules.MasteryWeapons[i])) + ".");
            if (level >= 2) lines.Add("Хитра дія: бонусною дією Ривок, Відхід або Сховатися.");
            if (modern && level >= 3) lines.Add("Влучна ціль: якщо не рухався, бонусною дією отримай перевагу на наступну атаку цього ходу; швидкість стає 0 до кінця ходу.");
            if (level >= 5) lines.Add("Неймовірне ухилення: реакцією на влучання видимого нападника зменш шкоду цієї атаки наполовину.");
            if (modern && level >= 5) {
                lines.Add(level >= 11 ? "Хитрий удар: обмінюй кістки Прихованої атаки на два різні ефекти. СК = 8 + Спритність + майстерність; оплати вартість кожного ефекту." : "Хитрий удар: обміняй кістки Прихованої атаки на один ефект. СК = 8 + Спритність + майстерність.");
                lines.Add("За 1к6: Отрута — Статура проти Отруєного на 1 хв, повтор наприкінці ходів; потрібен набір отруйника. Підніжка — Спритність або Велика чи менша ціль падає. Відступ — половина швидкості без провокованих атак.");
                if (level >= 14) lines.Add("Складні хитрі удари: 2к6 — Статура, наступного ходу тільки рух, дія або бонусна дія; 3к6 — Спритність проти Сліпоти до кінця наступного ходу цілі; 6к6 — Статура проти Непритомності на 1 хв, повтор наприкінці ходів, шкода припиняє ефект.");
            }
            if (level >= 7) lines.Add(modern ? "Уникнення: ряткидок Спритності проти половини шкоди дає 0 при успіху й половину при провалі; не діє при недієздатності." : "Уникнення: ряткидок Спритності проти половини шкоди дає 0 при успіху й половину при провалі.");
            if (level >= (modern ? 7 : 11)) lines.Add("Надійний талант: при перевірці з бонусом майстерності результат к20 1–9 вважай 10.");
            if (!modern && level >= 14) lines.Add("Сліпе чуття: якщо чуєш, знаєш розташування прихованих і невидимих істот у 10 футах.");
            if (level >= 15) lines.Add(modern ? "Гнучкий розум: володіння ряткидками Мудрості й Харизми; бонуси вже враховано." : "Гнучкий розум: володіння ряткидками Мудрості; бонус уже враховано.");
            if (level >= 18) lines.Add("Невловимість: атаки проти тебе не мають переваги, поки ти не недієздатний.");
            if (level >= 20) lines.Add(modern ? "Удача: невдалий кидок к20 перетвори на 20; раз за короткий або довгий відпочинок." : "Удача: промах по цілі в досяжності перетвори на влучання або невдалу перевірку характеристики — на к20 = 20; раз за короткий або довгий відпочинок.");
            return string.Join("\n", lines);
        }
        internal static string SubclassFeatures(CharacterCreationDraft d)
        {
            var lines = new List<string>(); int level = d.level; bool modern = Modern(d);
            if (Is(d, "thief")) {
                lines.Add(modern ? "Спритні руки: бонусною дією Спритність рук зі злодійськими інструментами чи кишенькова крадіжка, Використання або магічна дія для активації магічного предмета. Робота на висоті: швидкість лазіння = ходьбі, стрибки можуть використовувати Спритність." : "Спритні руки: бонусною дією Спритність рук, злодійські інструменти або Використання предмета. Робота на висоті: лазіння без додаткового руху; стрибок із розбігу довший на модифікатор Спритності у футах.");
                if (level >= 9) lines.Add(modern ? "Досконала скритність: за 1к6 Прихованої атаки атака не припиняє Невидимість від Сховатися, якщо закінчиш хід за укриттям на три чверті чи повним укриттям." : "Досконала скритність: перевага на Скритність, якщо цього ходу рухаєшся не більше половини швидкості.");
                if (level >= 13) lines.Add(modern ? "Використання магічних пристроїв: налаштування на 4 предмети; при витраті зарядів к6 = 6 зберігає їх. Будь-які сувої з Інтелектом; заговір і круг 1 без перевірки, вищі — Магія СК 10 + круг, провал знищує сувій." : "Використання магічних пристроїв: ігноруй вимоги класу, раси й рівня при використанні магічних предметів.");
                if (level >= 17) lines.Add(modern ? "Рефлекси злодія: у першому раунді два ходи — за своєю ініціативою та ініціативою мінус 10." : "Рефлекси злодія: у першому раунді два ходи — за своєю ініціативою та ініціативою мінус 10; не діє при несподіванці.");
            }
            if (Is(d, "assassin")) {
                lines.Add(CharacterDescriptionText.Get(modern ? "rogue.assassin.tools24" : "rogue.assassin.tools14"));
                lines.Add(modern ? "Убивство: перевага на ініціативу; у першому раунді перевага на атаки істот, які ще не ходили. Прихована атака в першому раунді додає шкоду = рівень плута." : "Убивство: перевага на атаки істот, які ще не ходили; влучання по захопленій зненацька істоті критичне.");
                if (level >= 9) lines.Add(modern ? "Майстер проникнення: за годину вивчення точно наслідуй мову або почерк. Влучна ціль більше не зменшує швидкість до 0." : "Майстер проникнення: за 7 днів і 25 зм створи нову вигадану особу з історією та зв’язками; її приймають за справжню, доки немає очевидних доказів протилежного.");
                if (level >= 13) lines.Add(modern ? "Отруєна зброя: ефект Отрута хитрого удару додає 2к6 отруйної шкоди за кожен провал ряткидка, ігноруючи опір отруті." : "Самозванець: після 3 годин вивчення наслідуй мову, почерк і поведінку; перевага на Обман проти підозрілих спостерігачів.");
                if (level >= 17) lines.Add(modern ? "Смертельний удар: Прихована атака першого раунду — ряткидок Статури СК 8 + Спритність + майстерність, провал подвоює шкоду атаки." : "Смертельний удар: влучання по захопленій зненацька цілі — ряткидок Статури СК 8 + Спритність + майстерність, провал подвоює шкоду атаки.");
            }
            if (Is(d, "trickster")) {
                lines.Add(modern ? "Спритна магічна рука: накладання й керування бонусною дією; рука може бути невидимою й робити перевірки Спритності рук." : "Спритна магічна рука: може бути невидимою, ховати й діставати предмети в чужих контейнерах, працювати злодійськими інструментами. Для непомітності Спритність рук проти Уважності. Керування Хитрою дією.");
                if (level >= 9) lines.Add(modern ? "Магічна засідка: коли накладаєш спел, будучи Невидимим, ціль має перешкоду на ряткидки проти нього цього ходу." : "Магічна засідка: коли ти прихований від цілі при накладанні спела, вона має перешкоду на ряткидки проти нього цього ходу.");
                if (level >= 13) lines.Add(modern ? "Універсальний шахрай: застосовуючи Підніжку хитрого удару, також застосуй її до іншої істоти в 5 футах від Магічної руки." : "Універсальний шахрай: бонусною дією Магічна рука відволікає істоту в 5 футах від себе; до кінця ходу ти атакуєш її з перевагою.");
                if (level >= 17) lines.Add(modern ? "Крадій спелів: реакція на спел, що зачіпає тебе; Інтелект проти твоєї СК магії. Провал — спел не діє на тебе; якщо його круг доступний, підготуй його на 8 годин і заборони його цілі на цей час. Після успішної крадіжки — довгий відпочинок." : "Крадій спелів: реакція на спел, що зачіпає тебе; ряткидок базовою характеристикою мага проти твоєї СК. Провал — спел не діє на тебе; якщо його круг доступний, знай його 8 годин і заборони його цілі на цей час. Раз за довгий відпочинок.");
            }
            if (Is(d, "soulknife")) {
                lines.Add("Психічні клинки: вільною рукою атакуй при дії Атака або провокованій атаці; фехтувальні, метальні 60/120 футів, шкода 1к6 психічна, майстерність Уразливість. Бонусною дією другою вільною рукою — другий клинок із 1к4.");
                lines.Add("Псіонічна вправність: додай кістку енергії до невдалої перевірки навички чи інструмента з володінням; кістка витрачається лише при успіху. Короткий відпочинок повертає одну кістку, довгий — усі.");
                lines.Add("Психічний шепіт: магічною дією зв’яжись із видимими істотами числом до майстерності, на кістку енергії годин у межах милі. Перше використання за довгий відпочинок без витрати кістки; решта витрачають кістку.");
                if (level >= 9) lines.Add("Клинки душі: при промаху психічним клинком додай кістку енергії до атаки, витративши її лише при влучанні. Бонусною дією витрать кістку й телепортуйся у видиме місце до 10 × її результат футів.");
                if (level >= 13) lines.Add("Психічна завіса: магічна дія, Невидимість до години; завершується після шкоди істоті чи примусового ряткидка. Раз за довгий відпочинок; витрать кістку енергії для відновлення використання.");
                if (level >= 17) lines.Add("Розрив розуму: після Прихованої атаки психічними клинками Мудрість СК 8 + Спритність + майстерність проти Приголомшеного на 1 хв, повтор наприкінці ходів. Раз за довгий відпочинок; витрать 3 кістки енергії для відновлення використання.");
            }
            return string.Join("\n", lines);
        }
    }
}
