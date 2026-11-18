using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterArtificerRules
    {
        public static string[] Tools => CharacterCreationRules.MonkTools.Take(17).Where(tool => tool != "Інструменти лудильника").ToArray();
        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (d.characterClass != "artificer") return null;
            if (page == 0 && Is(d, "armorer") && !Models(d).Contains(d.artificerModel)) return CharacterArtificerText.Get("error.model");
            if (page == 3 && (d.artificerToolReplacements.Count != ToolReplacementCount(d) || d.artificerToolReplacements.Where((tool, i) => !ToolReplacementOptions(d, i).Contains(tool)).Any() || !d.IsSecondaryTrack && !Tools.Contains(d.artificerTool))) return "Обери ремісничі інструменти винахідника.";
            if (page == 4 && !d.IsSecondaryTrack && d.edition == RulesEdition.Rules2014 &&
                (!CharacterCreationRules.MonkSimpleWeapons.Contains(d.artificerWeapon) || !CharacterCreationRules.MonkSimpleWeapons.Contains(d.artificerSecondWeapon) ||
                 d.artificerArmor != "Проклепана шкіра" && d.artificerArmor != "Лускатий обладунок")) return "Обери дві прості зброї й обладунок винахідника.";
            if (page == 4 && (d.artificerPlans.Count != PlansKnown(d) || d.artificerPlans.Any(string.IsNullOrEmpty) ||
                d.artificerPlans.Distinct().Count() != d.artificerPlans.Count || !CanKnowPlans(d, d.artificerPlans) || d.artificerPlans.Where((id, i) => !PlanOptions(d, i).Any(p => p.id == id)).Any()))
                return CharacterArtificerText.Get("error.plans");
            if (page == 4)
            {
                var names = d.artificerPlans.Select((id, i) => id.StartsWith("custom.", StringComparison.Ordinal) ? i < d.artificerPlanDetails.Count ? d.artificerPlanDetails[i].Trim() : "" : CharacterArtificerPlans.All.FirstOrDefault(p => p.edition == (Revised(d) ? 2024 : 2014) && p.id == id)?.name ?? "").ToArray();
                if (names.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 80) || names.Select(CharacterArtificerPlans.CanonicalName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length) return CharacterArtificerText.Get("error.custom");
            }
            if (page == 4 && d.level >= 11 && string.IsNullOrEmpty(d.artificerStoredSpell)) return CharacterArtificerText.Get("error.store");
            if(page==4)return CharacterArtificerEquipment.ChoiceError(d);
            return null;
        }

        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d);
            bool modern = d.edition == RulesEdition.Rules2024, dwarf = CharacterSpeciesRules.IsDwarf(d);
            int Mod(int ability) => CharacterCreationRules.Modifier(scores[ability]);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if (modern && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            int armor = modern ? d.equipment == 0 ? 12 + Mod(1) : 10 + Mod(1) :
                d.artificerArmor == "Лускатий обладунок" ? 14 + Math.Min(2, Mod(1)) : 12 + Mod(1);
            int createdArmor = CharacterArtificerEquipment.ChoiceError(d) == null ? CharacterArtificerEquipment.CreatedArmorBase(d) : 0;
            if (createdArmor > 0) armor = createdArmor + Math.Min(2, Mod(1));
            var result = new CreatedCharacterStats {
                scores = scores, skills = skills.Distinct().ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, Mod(2)), armorClass = armor+CharacterArtificerEquipment.ArmorBonus(d),
                initiative = Mod(1) + 0, speed = dwarf && !modern ? 25 : 30,
                passivePerception = 10 + Mod(4) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = dwarf ? modern ? "Дварф" : "Дварф (пагорбовий)" : "Людина", backgroundName = CharacterBackgroundRules.Find(d)?.name ?? ""
            };
            result.features = "Винахідник " + d.level + " • D&D " + (modern ? "2024" : "2014") + "\n" +
                (modern ? "Створення предметів: заговір Лагодження. Дією Магія з інструментами лудильника створи немагічну річ із переліку класу; вона зникає після довгого відпочинку. Використання: модифікатор Інтелекту, мінімум 1, за довгий відпочинок."
                : "Магічне майстрування: дією надай крихітному предмету світло 5 футів, запис до 6 секунд, звук чи запах або зображення до 25 слів. Одночасно діє на кількість предметів, рівну модифікатору Інтелекту, мінімум 1.") +
                "\nМагія через інструменти: тримай злодійські або ремісничі інструменти, якими володієш; базова характеристика — Інтелект. Комірки відновлюються після довгого відпочинку.";
            result.features += CharacterBackgroundRules.Features(d);
            if (dwarf) result.features += modern
                ? "\nТемнозір 120 футів; опір отруті та перевага проти стану Отруєний.\n" + string.Format(CharacterArtificerText.Get("dwarf24"), CharacterLegacySpeciesRules.Proficiency(d))
                : CharacterSpeciesRules.Dwarf2014Features(d);
            else if (modern && d.species == "human") result.features += "\nЛюдина: героїчне натхнення після довгого відпочинку; додаткова навичка.";
            result.features += ProgressionFeatures(d);
            if (Is(d, "armorer") && d.artificerModel == "infiltrator" && CharacterArtificerEquipment.WearsArmor(d)) result.speed += 5;
            result.equipment = Equipment(d);
            return result;
        }

        internal static string Equipment(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024
            ? d.equipment == 0 ? "Проклепана шкіра; кинджал; злодійські інструменти; інструменти лудильника; набір підземельника; 16 зм." : "150 зм замість класового спорядження."
            : d.artificerWeapon + "; " + d.artificerSecondWeapon + "; легкий арбалет; 20 болтів; " + d.artificerArmor + "; злодійські інструменти; набір підземельника.";
    }
}
