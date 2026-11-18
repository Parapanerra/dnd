using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    [Serializable]
    internal sealed class CharacterSpellRecord
    {
        public int edition;
        public string source;
        public string name;
        public int level;
        public string school;
        public string[] classes;
        public string castingUnit;
        public int castingAmount;
        public string durationUnit;
        public int durationAmount;
        public bool concentration;
        public bool ritual;
        public string rangeUnit;
        public string scaling5, scaling11, scaling17;
        public bool scalingExtra;
        public int rangeAmount;
        public int materialCost;
        public bool materialConsumed;
    }

    [Serializable]
    internal sealed class CharacterSpellCatalogData
    {
        public CharacterSpellRecord[] entries;
    }

    // PHB/XPHB cantrips, level-one class lists and Wizard spells through circle four.
    // Display names/descriptions are localized separately before a class exposes them.
    internal static class CharacterSpellCatalog
    {
        private static readonly Lazy<CharacterSpellRecord[]> Index = new Lazy<CharacterSpellRecord[]>(Load);
        private static readonly SupplementalSpellText WizardHigherSpellText = new SupplementalSpellText("WizardHigherSpellText");
        private static readonly SupplementalSpellText PaladinSpellText = new SupplementalSpellText("PaladinSpellText");
        private static readonly SupplementalSpellText RangerSpellText = new SupplementalSpellText("RangerSpellText");
        private static readonly SupplementalSpellText SorcererSpellText = new SupplementalSpellText("SorcererSpellText");
        private static readonly SupplementalSpellText BardHigherSpellText = new SupplementalSpellText("BardHigherSpellText");

        internal static IReadOnlyList<CharacterSpellRecord> Available(RulesEdition edition, string classId, int level)
        {
            if (level < 0 || level > 9 || string.IsNullOrEmpty(classId)) return Array.Empty<CharacterSpellRecord>();
            string className = char.ToUpperInvariant(classId[0]) + classId.Substring(1);
            int year = edition == RulesEdition.Rules2024 ? 2024 : 2014;
            return Index.Value.Where(spell => spell.edition == year && spell.level == level &&
                spell.classes != null && spell.classes.Contains(className, StringComparer.Ordinal)).ToArray();
        }

        internal static IReadOnlyList<CharacterSpellRecord> AnyClass(RulesEdition edition, int level)
        {
            int year = edition == RulesEdition.Rules2024 ? 2024 : 2014;
            return Index.Value.Where(spell => spell.edition == year && spell.level == level).ToArray();
        }

        internal static IReadOnlyList<CharacterSpellRecord> AvailableFor(CharacterCreationDraft draft, int level)
        {
            if (CharacterBloodHunterRules.Profane(draft)) return (level == 0 ? Available(RulesEdition.Rules2014, "warlock", 0) : Enumerable.Range(1, CharacterBloodHunterRules.PactCircle(draft)).SelectMany(circle => Available(RulesEdition.Rules2014, "warlock", circle))).OrderBy(s => s.level).ThenBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (CharacterWarlockRules.Active(draft)) {
                var warlock=level==0?Available(draft.edition,"warlock",0):Enumerable.Range(1,CharacterWarlockRules.SlotCircle(draft)).SelectMany(circle=>Available(draft.edition,"warlock",circle));
                return warlock.Concat(level==0?Enumerable.Empty<CharacterSpellRecord>():CharacterWarlockRules.ExpandedSpells(draft).Select(name=>Find(draft.edition,name)).Where(spell=>spell!=null)).Where(spell=>!CharacterWarlockRules.AutomaticSpells(draft).Contains(spell.name)).GroupBy(spell=>spell.name).Select(g=>g.First()).OrderBy(spell=>spell.level).ThenBy(spell=>spell.name,StringComparer.Ordinal).ToArray();
            }
            if (draft.characterClass == "wizard")
                return level == 0 ? Available(draft.edition, "wizard", 0).Where(spell => !CharacterWizardRules.Automatic(draft).Contains(spell.name)).ToArray() : Enumerable.Range(1, CharacterWizardRules.BookCircle(CharacterWizardRules.BookCount(draft) - 1))
                    .SelectMany(circle => Available(draft.edition, "wizard", circle)).OrderBy(spell => spell.level).ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
            if (CharacterBardRules.Active(draft)) return BardChoices(draft, level);
            if (CharacterDruidRules.Active(draft))return (level==0?Available(draft.edition,"druid",0):Enumerable.Range(1,CharacterDruidRules.MaximumCircle(draft)).SelectMany(circle=>Available(draft.edition,"druid",circle))).Where(spell=>!CharacterDruidRules.AutomaticSpells(draft).Contains(spell.name)).OrderBy(spell=>spell.level).ThenBy(spell=>spell.name,StringComparer.Ordinal).ToArray();
            if (CharacterClericRules.Active(draft))return (level==0?Available(draft.edition,"cleric",0):Enumerable.Range(1,CharacterClericRules.MaximumCircle(draft)).SelectMany(circle=>Available(draft.edition,"cleric",circle))).Where(spell=>!CharacterClericRules.DomainSpells(draft).Contains(spell.name)).OrderBy(spell=>spell.level).ThenBy(spell=>spell.name,StringComparer.Ordinal).ToArray();
            if (CharacterArtificerRules.Active(draft))
                return (level == 0 ? Available(draft.edition, "artificer", 0) : Enumerable.Range(1, CharacterArtificerRules.MaximumCircle(draft)).SelectMany(circle => Available(draft.edition, "artificer", circle)))
                    .Where(spell => !CharacterArtificerRules.AutomaticSpells(draft).Contains(spell.name)).OrderBy(spell => spell.level).ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
            if (CharacterTricksterSpells.ThirdCaster(draft)) return CharacterTricksterSpells.Available(draft, level);
            if (CharacterSorcererRules.Active(draft))
                return (level == 0 ? Available(draft.edition, "sorcerer", 0) : Enumerable.Range(1, CharacterSorcererRules.MaximumCircle(draft)).SelectMany(circle => Available(draft.edition, "sorcerer", circle)))
                    .Where(spell => !CharacterSorcererRules.AutomaticSpells(draft).Contains(spell.name)).OrderBy(spell => spell.level).ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
            if (CharacterRangerRules.Active(draft))
            {
                if (level == 0) return CharacterRangerRules.Druidic(draft) ? Available(draft.edition, "druid", 0) : Array.Empty<CharacterSpellRecord>();
                return Enumerable.Range(1, CharacterRangerRules.MaximumCircle(draft)).SelectMany(circle => Available(draft.edition, "ranger", circle))
                    .Where(spell => !CharacterRangerRules.AutomaticSpells(draft).Contains(spell.name)).OrderBy(spell => spell.level).ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
            }
            if (CharacterPaladinRules.Active(draft))
            {
                if (level == 0) return CharacterPaladinRules.Blessed(draft) ? Available(draft.edition, "cleric", 0) : Array.Empty<CharacterSpellRecord>();
                return Enumerable.Range(1, CharacterPaladinRules.MaximumCircle(draft)).SelectMany(circle => Available(draft.edition, "paladin", circle))
                    .Where(spell => !CharacterPaladinRules.AutomaticSpells(draft).Contains(spell.name)).OrderBy(spell => spell.level).ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
            }
            var result = Available(draft.edition, draft.characterClass, level).ToList();
            if (level == 1) foreach (string name in CharacterWarlockRules.ExpandedSpells(draft))
            {
                var spell = Find(draft.edition, name);
                if (spell != null && !result.Any(item => item.name == name)) result.Add(spell);
            }
            return result.OrderBy(spell => spell.name, StringComparer.Ordinal).ToArray();
        }

        internal static CharacterSpellRecord[] ArtificerStoredSpellChoices(CharacterCreationDraft d)
        {
            if (!CharacterArtificerRules.Active(d) || d.level < 11) return Array.Empty<CharacterSpellRecord>();

            bool revised = CharacterArtificerRules.Revised(d);
            int maximum = revised ? 3 : 2;
            var candidates = Enumerable.Range(1, maximum).SelectMany(level => Available(d.edition, "artificer", level));
            if (revised)
                candidates = candidates.Concat(CharacterArtificerRules.AutomaticSpells(d)
                    .Select(name => Find(d.edition, name)).Where(spell => spell != null && spell.level > 0 && spell.level <= maximum));

            return candidates.Where(spell => spell.castingUnit == "action" && spell.castingAmount == 1 && (!revised || !spell.materialConsumed))
                .GroupBy(spell => spell.name).Select(group => group.First()).OrderBy(spell => spell.level)
                .ThenBy(spell => spell.name, StringComparer.Ordinal).ToArray();
        }

        internal static IReadOnlyList<CharacterSpellRecord> BardChoices(CharacterCreationDraft d, int level)
        {
            if (level==0) return Available(d.edition,"bard",0).Where(s=>!CharacterBardRules.AutomaticSpells(d).Contains(s.name)&&!d.bardSecrets.Contains(s.name)).ToArray();
            var classes=CharacterBardRules.Revised(d)&&d.level>=10?new[]{"bard","cleric","druid","wizard"}:new[]{"bard"};
            return classes.SelectMany(id=>Enumerable.Range(1,CharacterBardRules.MaximumCircle(d)).SelectMany(circle=>Available(d.edition,id,circle)))
                .GroupBy(s=>s.name).Select(g=>g.First()).Where(s=>!CharacterBardRules.AutomaticSpells(d).Contains(s.name)&&!d.bardSecrets.Contains(s.name)).OrderBy(s=>s.level).ThenBy(s=>s.name,StringComparer.Ordinal).ToArray();
        }
        internal static IReadOnlyList<CharacterSpellRecord> BardSecrets(CharacterCreationDraft d,int slot)
        {
            var result=Enumerable.Range(0,CharacterBardRules.MaximumCircle(d)+1).SelectMany(circle=>AnyClass(d.edition,circle))
                .Where(spell=>spell.level<=CharacterBardRules.SecretCircle(d,slot)||spell.classes.Contains("Bard"));
            if(CharacterBardRules.Revised(d))result=result.Where(s=>s.classes.Any(id=>new[]{"Cleric","Druid","Wizard"}.Contains(id)));
            return result.Where(s=>BardSecretCanLearn(d,slot,s)&&!d.cantrips.Contains(s.name)&&!d.preparedSpells.Contains(s.name)&&!CharacterBardRules.AutomaticSpells(d).Contains(s.name))
                .OrderBy(s=>s.level).ThenBy(s=>s.name,StringComparer.Ordinal).ToArray();
        }
        private static KeyValuePair<int,bool> BardLearningEntry(CharacterCreationDraft d,string name) {
            var spell=Find(d.edition,name);return new KeyValuePair<int,bool>(spell?.level??99,spell?.classes?.Contains("Bard")==true);
        }
        internal static bool BardCanLearn(CharacterCreationDraft d,IEnumerable<string> names) {
            if(!CharacterBardRules.Revised(d)) names=names.Concat(d.bardSecrets.Where(name=>!string.IsNullOrEmpty(name)&&(Find(d.edition,name)?.level??0)>0));
            return CharacterBardRules.CanLearn(d,names.Select(name=>BardLearningEntry(d,name)));
        }
        private static bool BardSecretCanLearn(CharacterCreationDraft d,int slot,CharacterSpellRecord candidate) {
            var secrets=d.bardSecrets.Where((name,index)=>index!=slot&&!string.IsNullOrEmpty(name)).Select(name=>Find(d.edition,name)).Where(spell=>spell!=null).Concat(new[]{candidate}).ToArray();
            if(CharacterBardRules.Revised(d)) {
                for(int circle=4;circle<=CharacterBardRules.MaximumCircle(d);circle++)
                    if(secrets.Count(spell=>spell.level>=circle)>Math.Max(0,Math.Min(2,d.level-(circle*2-1)+1)))return false;
                return true;
            }
            return CharacterBardRules.CanLearn(d,d.preparedSpells.Where(name=>!string.IsNullOrEmpty(name)).Select(name=>BardLearningEntry(d,name)).Concat(secrets.Where(spell=>spell.level>0).Select(spell=>new KeyValuePair<int,bool>(spell.level,spell.classes.Contains("Bard")))));
        }

        // Compact 2024 Paladin card text. Each line is a paraphrase of the pinned XPHB data.
        internal static string Paladin2024Summary(string name)
        {
            switch (name)
            {
                case "Bless": return "Дія; 30 футів; концентрація до 1 хв. До трьох істот додають 1к4 до атак і ряткидків.";
                case "Command": return "Дія; 60 футів. Істота робить ряткидок Мудрості; за провалу виконує однослівний наказ наступного ходу.";
                case "Compelled Duel": return "Бонусна дія; 30 футів; концентрація до 1 хв. За провалу ряткидка Мудрості ціль атакує інших із завадою та не відходить далі 30 футів.";
                case "Cure Wounds": return "Дія; дотик. Відновлює 2к8 + модифікатор Харизми ОЗ.";
                case "Detect Evil and Good": return "Дія; концентрація до 10 хв. Відчуває певних надприродних істот і освячені місця в межах 30 футів.";
                case "Detect Magic": return "Дія; концентрація до 10 хв. Відчуває магію в межах 30 футів; дією можна побачити ауру й школу заклинання.";
                case "Detect Poison and Disease": return "Дія; концентрація до 10 хв. Відчуває отрути, отруйних істот і магічні хвороби в межах 30 футів.";
                case "Divine Favor": return "Бонусна дія; 1 хв. Влучання зброєю завдають додатково 1к4 променистої шкоди.";
                case "Divine Smite": return "Бонусна дія одразу після влучання ближньою зброєю або беззбройним ударом. Додає 2к8 променистої шкоди; ще 1к8 проти породження або нежиті.";
                case "Heroism": return "Дія; дотик; концентрація до 1 хв. Істота не може бути Наляканою і на початку ходу отримує тимчасові ОЗ за модифікатором Харизми.";
                case "Protection from Evil and Good": return "Дія; дотик; концентрація до 10 хв. Захищає від певних надприродних істот: їхні атаки з завадою, ціль не можна зачарувати, налякати чи одержати.";
                case "Purify Food and Drink": return "Дія; 10 футів. Прибирає отруту й псування з немагічної їжі та напоїв у сфері радіусом 5 футів.";
                case "Searing Smite": return "Бонусна дія після влучання в ближньому бою. Додає 1к6 вогняної шкоди; ціль отримує 1к6 щохідно, доки не зробить ряткидок Статури.";
                case "Shield of Faith": return "Бонусна дія; 60 футів; концентрація до 10 хв. Дає цілі +2 КО.";
                case "Thunderous Smite": return "Бонусна дія після влучання в ближньому бою. Додає 2к6 громової шкоди; ряткидок Сили або ціль відштовхує на 10 футів і збиває долілиць.";
                case "Wrathful Smite": return "Бонусна дія після влучання в ближньому бою; до 1 хв. Додає 1к6 некротичної шкоди; ряткидок Мудрості або ціль Налякана.";
                default: return "";
            }
        }

        internal static bool Paladin2024BonusAction(string name) =>
            name == "Compelled Duel" || name == "Divine Favor" || name == "Divine Smite" ||
            name == "Searing Smite" || name == "Shield of Faith" || name == "Thunderous Smite" ||
            name == "Wrathful Smite";

        internal static string Summary2024(string name)
        {
            string shared = Paladin2024Summary(name);
            if (!string.IsNullOrEmpty(shared)) return shared;
            switch (name)
            {
                case "Alarm": return "Ритуал; 30 футів; 8 год. Попереджає, коли істота входить у захищену ділянку.";
                case "Animal Friendship": return "Дія; 30 футів; 24 год. За провалу ряткидка Мудрості звір стає Зачарованим тобою.";
                case "Ensnaring Strike": return "Бонусна дія після влучання зброєю; концентрація до 1 хв. Ціль робить ряткидок Сили або стає Скутою лозою.";
                case "Entangle": return "Дія; 90 футів; концентрація до 1 хв. Рослини утворюють складну місцевість; істоти можуть стати Скутими.";
                case "Fog Cloud": return "Дія; 120 футів; концентрація до 1 год. Створює сферу густого туману радіусом 20 футів.";
                case "Goodberry": return "Дія. Створює 10 ягід; кожна відновлює 1 ОЗ і насичує істоту на день.";
                case "Hail of Thorns": return "Бонусна дія після влучання далекобійною зброєю. Ціль та істоти поруч роблять ряткидок Спритності проти 1к10 колючої шкоди.";
                case "Hunter's Mark": return "Бонусна дія; 90 футів; концентрація до 1 год. Познач ціль; влучання зброєю завдають їй додатково 1к6 силової шкоди.";
                case "Jump": return "Бонусна дія; дотик; 1 хв. Дальність стрибка істоти збільшується за правилами заклинання.";
                case "Longstrider": return "Дія; дотик; 1 год. Швидкість істоти збільшується на 10 футів.";
                case "Speak with Animals": return "Ритуал; 10 хв. Дає змогу розуміти мову звірів і розмовляти з ними.";
                default: return "";
            }
        }

        internal static bool BonusAction2024(string name) => Paladin2024BonusAction(name) ||
            name == "Ensnaring Strike" || name == "Hail of Thorns" || name == "Hunter's Mark" || name == "Jump";

        internal static CharacterSpellRecord Find(RulesEdition edition, string name)
        {
            int year = edition == RulesEdition.Rules2024 ? 2024 : 2014;
            return Index.Value.FirstOrDefault(spell => spell.edition == year && spell.name == name);
        }

        internal static string CardDescription2024(string name, string classId, Func<string, string> translate)
            => CardDescription(RulesEdition.Rules2024, name, classId, translate);

        internal static string CardDescription(RulesEdition edition, string name, string classId, Func<string, string> translate, int spellAbility = -1, bool ignoreMaterial = false, int casterLevel = 1)
        {
            var spell = Find(edition, name);
            string summary = name == "Cure Wounds" && classId == "ranger"
                ? "Дія; дотик. Відновлює 2к8 + модифікатор Мудрості ОЗ." : Summary2024(name);
            int effectStart = summary.IndexOf(". ", StringComparison.Ordinal);
            string effect = effectStart >= 0 ? summary.Substring(effectStart + 2) : summary;
            string bardEffect = CharacterBardSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(bardEffect)) effect = bardEffect;
            string druidEffect = CharacterDruidSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(druidEffect)) effect = druidEffect;
            string clericEffect = CharacterClericSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(clericEffect)) effect = clericEffect;
            string wizardEffect = CharacterWizardSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(wizardEffect)) effect = wizardEffect;
            string warlockEffect = CharacterWarlockSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(warlockEffect)) effect = warlockEffect;
            string artificerEffect = CharacterArtificerSpellText.Effect(name);
            if (!string.IsNullOrEmpty(artificerEffect)) effect = artificerEffect;
            string monkEffect = CharacterMonkSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(monkEffect)) effect = monkEffect;
            string higherEffect = WizardHigherSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(higherEffect)) effect = higherEffect;
            if (name == "Telekinesis" && edition == RulesEdition.Rules2024) effect = CharacterFighterText.Get("telekinesis.spell");
            if (name == "Ensnaring Strike") effect = "Ряткидок Сили або ціль Скута лозою та отримує 1к6 колючої шкоди на початку кожного свого ходу; дією можна звільнитися.";
            if (name == "Hunter's Mark") effect = "Атаки з кидком на влучання завдають позначеній цілі додатково 1к6 силової шкоди. Перевага на Уважність і Виживання для її пошуку; після її падіння до 0 ОЗ мітку можна перенести бонусною дією.";
            if (name == "Jump") effect = edition == RulesEdition.Rules2024
                ? "Раз за свій хід істота може витратити 10 футів руху, щоб стрибнути на відстань до 30 футів."
                : "Дальність стрибків істоти потроюється.";
            if (name == "Goodberry") effect = edition == RulesEdition.Rules2024
                ? "Створює 10 ягід. Бонусною дією істота з’їдає ягоду, відновлює 1 ОЗ і насичується на день."
                : "Створює до 10 ягід; дією істота з’їдає ягоду, відновлює 1 ОЗ і насичується на день. Магія ягід зникає за 24 год.";
            string paladinEffect = PaladinSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(paladinEffect)) effect = paladinEffect;
            string rangerEffect = RangerSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(rangerEffect)) effect = rangerEffect;
            string sorcererEffect = SorcererSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(sorcererEffect)) effect = sorcererEffect;
            string bardHigherEffect = BardHigherSpellText.Effect(edition, name);
            if (!string.IsNullOrEmpty(bardHigherEffect)) effect=bardHigherEffect;
            if ((classId == "druid" || classId == "ranger" || classId == "cleric") &&
                (name == "Cure Wounds" || name == "Healing Word" || name == "Heroism"))
                effect = effect.Replace("Харизми", "Мудрості");
            if (classId == "artificer" && name == "Cure Wounds") effect = effect.Replace("Харизми", "Інтелекту");
            if (classId == "warlock" && name == "Shillelagh") effect = effect.Replace("Мудрість", "Харизму");
            if (spellAbility >= 3 && spellAbility <= 5)
            {
                if (name == "Cure Wounds" || name == "Healing Word" || name == "Heroism")
                    effect = effect.Replace("Харизми", "{ability}").Replace("Мудрості", "{ability}").Replace("Інтелекту", "{ability}")
                        .Replace("{ability}", spellAbility == 3 ? "Інтелекту" : spellAbility == 4 ? "Мудрості" : "Харизми");
                if (name == "Shillelagh") effect = effect.Replace("Мудрість", "{ability}").Replace("Харизму", "{ability}")
                    .Replace("{ability}", spellAbility == 3 ? "Інтелект" : spellAbility == 4 ? "Мудрість" : "Харизму");
            }
            var details = new List<string>();
            if (spell != null)
            {
                string range = spell.rangeUnit == "touch" ? translate("Дотик") : spell.rangeUnit == "self" ? translate("На себе") :
                    spell.rangeUnit == "feet" ? spell.rangeAmount + " " + translate("футів") :
                    spell.rangeUnit == "miles" ? spell.rangeAmount + " " + translate("миль") : "";
                if (range.Length > 0) details.Add(translate("Дальність:") + " " + range + ".");
                if (!(classId=="druid" && edition==RulesEdition.Rules2024 && name=="Find Familiar") && (spell.castingUnit == "minute" || spell.castingUnit == "hour")) details.Add(string.Format(translate("Час накладання: {0} {1}."), spell.castingAmount, translate(spell.castingUnit == "minute" ? "хвилин" : "годин")));
                if (!ignoreMaterial && spell.materialCost > 0) details.Add(string.Format(translate("Матеріальний компонент: {0} зм. {1}"), spell.materialCost / 100f, translate(spell.materialConsumed ? "Витрачається." : "Не витрачається.")));
                if (DurationOption(spell) == 15 && spell.durationAmount > 0) details.Add(string.Format(translate("Тривалість: {0} {1}."), spell.durationAmount, translate(spell.durationUnit == "minute" ? "хвилин" : spell.durationUnit == "hour" ? "годин" : spell.durationUnit == "day" ? "днів" : "раундів")));
                if (spell.concentration) details.Add(translate("Потребує концентрації."));
                if (!(classId=="druid" && edition==RulesEdition.Rules2024 && name=="Find Familiar") && spell.ritual && (edition == RulesEdition.Rules2024 || new[] { "bard", "wizard", "druid", "cleric", "artificer" }.Contains(classId))) details.Add(translate("Можна накласти ритуалом."));
            }
            if (edition == RulesEdition.Rules2024 && name == "Ensnaring Strike") details.Add(translate("Одразу після влучання зброєю."));
            else if (edition == RulesEdition.Rules2024 && name == "Hail of Thorns") details.Add(translate("Одразу після влучання далекобійною зброєю."));
            else if (edition == RulesEdition.Rules2024 && new[] { "Divine Smite", "Searing Smite", "Thunderous Smite", "Wrathful Smite", "Blinding Smite", "Shining Smite", "Staggering Smite", "Banishing Smite" }.Contains(name))
                details.Add(translate("Одразу після влучання ближньою зброєю або беззбройним ударом."));
            details.Add(translate(effect));
            if(spell != null && spell.level == 0 && casterLevel >= 5) {
                int tier=casterLevel>=17?17:casterLevel>=11?11:5;
                string dice=tier==17?spell.scaling17:tier==11?spell.scaling11:spell.scaling5;
                if(name!="Eldritch Blast"&&!string.IsNullOrEmpty(dice))details.Add(string.Format(translate(spell.scalingExtra?"Додаткова шкода заговору на рівні {0}: {1}.":"Кубики заговору на рівні {0}: {1}."),casterLevel,dice));
                if(name=="Eldritch Blast")details.Add(string.Format(translate("Кількість променів на рівні {0}: {1}; окрема атака на кожен."),casterLevel,tier==17?4:tier==11?3:2));
            }
            return string.Join("\n", details);
        }

        // The existing left selector is action type; all time values belong on the right.
        internal static int CastingOption(CharacterSpellRecord spell) => spell.castingUnit == "bonus" ? 1 :
            spell.castingUnit == "reaction" ? 2 : 0;

        internal static int DurationOption(CharacterSpellRecord spell)
        {
            if (spell.durationUnit == "special" || spell.durationUnit == "permanent") return 15;
            if (spell.durationUnit == "round") return spell.durationAmount == 1 ? 2 : 15;
            if (spell.durationUnit == "minute") return spell.durationAmount == 1 ? 3 : spell.durationAmount == 10 ? 4 : 15;
            if (spell.durationUnit == "hour")
                switch (spell.durationAmount) { case 1: return 5; case 2: return 6; case 3: return 7; case 6: return 8; case 8: return 13; case 10: return 9; case 12: return 10; case 24: return 11; }
            if (spell.durationUnit == "day" && spell.durationAmount == 1) return 11;
            if (spell.durationUnit == "day" && spell.durationAmount == 10) return 14;
            return spell.durationUnit == "instant" ? 0 : 15;
        }

        private static CharacterSpellRecord[] Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("CharacterCreation/SpellIndexLevel01");
            if (asset == null) throw new InvalidOperationException("Character creation spell index is missing.");
            CharacterSpellCatalogData data = JsonUtility.FromJson<CharacterSpellCatalogData>(asset.text);
            if (data?.entries == null || data.entries.Length < 150)
                throw new InvalidOperationException("Character creation spell index is invalid.");
            return data.entries;
        }
    }
}
