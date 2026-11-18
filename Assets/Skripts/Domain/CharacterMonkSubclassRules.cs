using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterMonkSubclassRules
    {
        public static readonly string[] Classic = { "openhand", "shadow", "elements" };
        public static readonly string[] Modern = { "openhand", "shadow", "elements", "mercy" };
        public static readonly string[] Names = { "Відкрита долоня", "Тінь", "Стихії", "Милосердя" };
        public static bool Is(CharacterCreationDraft d, string id) => d.characterClass == "monk" && d.level >= 3 && d.subclass == id;
        public static string[] Options(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024 ? Modern : Classic;
        public static readonly string[] DisciplineIds = { "winter", "north", "mountain", "fangs", "thunder", "phoenix", "gong", "mist", "wind", "flame", "gale", "river", "cinder", "air", "whip", "earth" };
        public static readonly string[] DisciplineNames = { "Подих зими", "Стиск північного вітру", "Захист вічної гори", "Ікла вогняної змії", "Кулак чотирьох громів", "Полум’я фенікса", "Гонг вершини", "Стійка туману", "Осідлати вітер", "Ріка голодного полум’я", "Порив духів вітру", "Змінити плин ріки", "Удар попелу", "Незламне повітря", "Водяний батіг", "Хвиля рухомої землі" };
        public static readonly int[] DisciplineLevels = { 17, 6, 17, 3, 3, 11, 6, 11, 11, 17, 3, 3, 3, 3, 3, 17 };
        public static readonly string[] DisciplineSpells = { "Cone of Cold", "Hold Person", "Stoneskin", "", "Thunderwave", "Fireball", "Shatter", "Gaseous Form", "Fly", "Wall of Fire", "Gust of Wind", "", "Burning Hands", "", "", "Wall of Stone" };
        public static readonly int[] DisciplineCosts = { 6, 3, 5, 1, 2, 4, 3, 4, 4, 5, 2, 1, 2, 2, 2, 6 };
        public static bool ClassicElements(CharacterCreationDraft d) => Is(d, "elements") && d.edition == RulesEdition.Rules2014;
        public static int DisciplineCount(CharacterCreationDraft d) => !ClassicElements(d) ? 0 : 1 + (d.level >= 6 ? 1 : 0) + (d.level >= 11 ? 1 : 0) + (d.level >= 17 ? 1 : 0);
        internal static int[] MercySkills(CharacterCreationDraft d)
        {
            if (!Is(d, "mercy")) return Array.Empty<int>();
            var prior = CharacterCreationRules.BackgroundSkills(d).Concat(CharacterSpeciesRules.GrantedSkills(d)).ToArray();
            return new[] { 6, 9 }.Select((skill, i) => prior.Contains(skill) && d.mercySkillReplacements != null && d.mercySkillReplacements.Length > i ? d.mercySkillReplacements[i] : skill).Where(skill => skill >= 0 && skill < 18).Distinct().ToArray();
        }
        public static string ChoiceError(CharacterCreationDraft d)
        {
            if (d.characterClass != "monk" || d.level < 3) return null;
            if (!Options(d).Contains(d.subclass)) return "Обери підклас персонажа.";
            if (ClassicElements(d))
            {
                if (d.monkDisciplines == null || d.monkDisciplines.Count != DisciplineCount(d) || d.monkDisciplines.Distinct().Count() != d.monkDisciplines.Count ||
                    d.monkDisciplines.Any(id => Array.IndexOf(DisciplineIds, id) < 0 || DisciplineLevels[Array.IndexOf(DisciplineIds, id)] > d.level))
                    return "Обери доступні стихійні дисципліни без повторів.";
                foreach (int threshold in new[] { 6, 11, 17 })
                {
                    int changes = new[] { 6, 11, 17 }.Count(level => level >= threshold && level <= d.level) * 2;
                    if (d.monkDisciplines.Count(id => DisciplineLevels[Array.IndexOf(DisciplineIds, id)] >= threshold) > changes)
                        return "На кожному рівні нової дисципліни можна вивчити одну й замінити одну стару.";
                }
            }
            if (Is(d, "mercy"))
            {
                int[] skills = MercySkills(d);
                if (skills.Length != 2 || skills.Any(skill => CharacterCreationRules.BackgroundSkills(d).Contains(skill) || CharacterSpeciesRules.GrantedSkills(d).Contains(skill) || d.classSkills.Contains(skill)))
                    return "Обери дві різні навички Милосердя без повторів попередніх володінь.";
            }
            return null;
        }
        public static string[] Spells(CharacterCreationDraft d)
        {
            var spells = new List<string>();
            if (d.characterClass != "monk") return spells.ToArray();
            if (Is(d, "shadow"))
            {
                spells.Add("Minor Illusion"); spells.Add("Darkness");
                if (d.edition == RulesEdition.Rules2014) spells.AddRange(new[] { "Darkvision", "Pass without Trace", "Silence" });
            }
            if (Is(d, "elements") && d.edition == RulesEdition.Rules2024) spells.Add("Elementalism");
            if (ClassicElements(d)) spells.AddRange((d.monkDisciplines ?? new List<string>()).Select(id => Array.IndexOf(DisciplineIds, id)).Where(i => i >= 0).Select(i => DisciplineSpells[i]).Where(name => name.Length > 0));
            if (d.edition == RulesEdition.Rules2014 && d.level >= 18) spells.Add("Astral Projection");
            if (d.edition == RulesEdition.Rules2014 && Is(d, "openhand") && d.level >= 11) spells.Add("Sanctuary");
            return spells.Distinct().ToArray();
        }
        public static string SpellUse(CharacterCreationDraft d, string name)
        {
            if (name == "Astral Projection") return "Порожнє тіло: 8 очок ци, лише для себе, без матеріальних компонентів.";
            if (name == "Sanctuary") return "Умиротворення: автоматично після довгого відпочинку до наступного; завершується за звичайних умов спела.";
            if (name == "Minor Illusion" || name == "Elementalism") return "Магія монаха: характеристика Мудрість; без витрати очок ци.";
            if (Is(d, "shadow")) return d.edition == RulesEdition.Rules2024
                ? "Мистецтво тіні: 1 очко ци, без компонентів. Бачиш крізь власну Темряву; на початку ходу можеш пересунути її в межах 60 футів."
                : "Мистецтво тіні: дією за 2 очки ци, без матеріальних компонентів.";
            int index = Array.IndexOf(DisciplineSpells, name);
            return index < 0 ? "" : "Стихійна дисципліна: " + DisciplineCosts[index] + " очок ци; без матеріальних компонентів.";
        }
        internal static string Features(CharacterCreationDraft d)
        {
            if (d.characterClass != "monk" || d.level < 3 || d.subclass == "openhand") return "";
            bool modern = d.edition == RulesEdition.Rules2024;
            var lines = new List<string>();
            if (Is(d, "shadow"))
            {
                lines.Add(modern ? "Мистецтво тіні: Темрява за 1 очко ци без компонентів; бачиш у власній зоні й рухаєш її на початку ходу в межах 60 футів. Темнозір 60 футів або +60 до наявного; Мала ілюзія за Мудрістю."
                    : "Мистецтво тіні: дією за 2 очки ци — Темрява, Темнозір, Безслідний крок або Тиша без матеріальних компонентів; також Мала ілюзія.");
                if (d.level >= 6) lines.Add("Крок тіні: бонусною дією з тьмяного світла чи темряви телепортуйся на 60 футів у видиме вільне місце також у тіні; перевага наступної ближньої атаки до кінця ходу.");
                if (d.level >= 11) lines.Add(modern ? "Покращений крок тіні: за 1 очко ци телепортація не потребує тіні на початку й наприкінці; одразу після неї один беззбройний удар у межах тієї ж бонусної дії."
                    : "Плащ тіней: дією в тьмяному світлі чи темряві стань Невидимим до атаки, спела або виходу на яскраве світло.");
                if (d.level >= 17) lines.Add(modern ? "Плащ тіней: дією Магія у тіні за 3 очки ци на 1 хвилину — Невидимість, рух крізь зайняті місця як складною місцевістю й безкоштовний Шквал ударів. Завершується при Недієздатності або завершенні ходу на яскравому світлі; із зайнятого місця виштовхує назад."
                    : "Нагода: якщо інша істота влучила атакою по цілі в 5 футах від тебе, реакцією виконай ближню атаку по цій цілі.");
            }
            if (Is(d, "elements") && modern)
            {
                lines.Add("Стихійне налаштування: на початку ходу 1 очко ци на 10 хвилин або до Недієздатності. Досяжність беззбройних ударів +10 футів; кислота, холод, вогонь, блискавка або грім замість звичайної шкоди. Після такої шкоди — ряткидок Сили цілі проти переміщення на 10 футів до тебе чи від тебе. Заговір Стихійність за Мудрістю.");
                if (d.level >= 6) lines.Add("Стихійний вибух: дією Магія за 2 очки ци сфера радіусом 20 футів у 120 футах. Спритність проти трьох кісток бойових мистецтв кислотної, холодної, вогняної, блискавичної або громової шкоди; успіх — половина.");
                if (d.level >= 11) lines.Add("Стихійний рух: під час Стихійного налаштування швидкості польоту й плавання дорівнюють швидкості ходьби.");
                if (d.level >= 17) lines.Add("Втілення стихій: під час налаштування один змінний щохідно стихійний опір; раз за свій хід влучання без зброї додає кістку шкоди. Крок вітру дає ще +20 футів на хід; при проході в 5 футах обрані істоти отримують кістку стихійної шкоди, кожна раз за хід.");
            }
            if (ClassicElements(d))
            {
                lines.Add("Стихійне налаштування: дією в 30 футах створи нешкідливий стихійний прояв, запали чи згаси малий вогонь, нагрій/охолоди фунт речовини на годину або зміни форму стихії в кубі 1 фут на хвилину.");
                lines.Add("Стихійні спели не потребують матеріальних компонентів. З 5 рівня додаткове очко ци підвищує круг спела на один; максимум очок на спел: 3 на рівнях 5–8, 4 на 9–12, 5 на 13–16, 6 на 17–20.");
                foreach (string id in d.monkDisciplines ?? new List<string>()) { string text = DisciplineFeature(id); if (text.Length > 0) lines.Add(text); }
            }
            if (Is(d, "mercy"))
            {
                lines.Add("Долоня зцілення: дія Магія, дотик, 1 очко ци — кістка бойових мистецтв + Мудрість ОЗ; один удар Шквалу можна замінити цим лікуванням без додаткових очок.");
                lines.Add("Долоня шкоди: раз за хід після шкоди беззбройним ударом витрать 1 очко ци на додаткову кістку бойових мистецтв + Мудрість некротичної шкоди.");
                if (d.level >= 6) lines.Add("Дотик лікаря: Долоня шкоди також отруює до кінця твого наступного ходу; Долоня зцілення прибирає Засліплений, Оглухлий, Паралізований, Отруєний або Приголомшений.");
                if (d.level >= 11) lines.Add("Шквал зцілення й шкоди: усі удари Шквалу можна замінити лікуванням; Долоня шкоди в його ударі не коштує додаткового ци, усе ще раз за хід. Ці переваги — модифікатор Мудрості разів, мінімум 1, за довгий відпочинок.");
                if (d.level >= 17) lines.Add("Долоня найвищого милосердя: дія Магія, дотик, 5 очок ци — воскреси померлого не більш ніж 24 години тому з 4к10 + Мудрість ОЗ; прибирає стани з Дотику лікаря. Раз за довгий відпочинок.");
            }
            return string.Join("\n", lines);
        }
        internal static string DisciplineFeature(string id)
        {
            switch (id)
            {
                case "fangs": return "Ікла вогняної змії: дія Атака, 1 очко ци — до кінця ходу досяжність беззбройних ударів +10 футів, вогонь замість дробильної шкоди; при влучанні ще 1 очко ци дає +1к10 вогню.";
                case "river": return "Змінити плин ріки: дія, 1 очко ци — перетвори воду/лід у 120 футах у ділянці до 30 футів завширшки; зміна висоти до половини ширини. Не можна ув’язнювати чи травмувати істот.";
                case "air": return "Незламне повітря: дія, 2 очки ци, ціль у 30 футах; Сила проти 3к10 дробильної шкоди, відштовхування на 20 футів і падіння. Успіх — половина, без руху й падіння; +1к10 за кожне додаткове очко ци.";
                case "whip": return "Водяний батіг: дія, 2 очки ци, видима ціль у 30 футах; Спритність проти 3к10 дробильної шкоди й падіння або притягнення на 25 футів. Успіх — половина без додаткового ефекту; +1к10 за додаткове очко ци.";
                default: return "";
            }
        }
    }
}
