using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Translates generated descriptions while leaving draft choices and rule data canonical.
    internal static class CharacterCreationPresentation
    {
        private sealed class TemplateMatcher
        {
            internal readonly string text, prefix;
            internal readonly System.Text.RegularExpressions.Regex regex;
            internal TemplateMatcher(string template)
            {
                text=template;
                int placeholder=template.IndexOf('{');prefix=placeholder<0?template:template.Substring(0,placeholder);
                string pattern="^"+System.Text.RegularExpressions.Regex.Escape(template).Replace("\\{0}","(.+?)").Replace("\\{1}","(.+?)").Replace("\\{2}","(.+?)")+"$";
                regex=new System.Text.RegularExpressions.Regex(pattern,System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            }
        }
        private static readonly TemplateMatcher[] FeatureTemplates = CharacterBloodHunterText.Templates
            .Concat(CharacterWizardText.Templates).Concat(CharacterWarlockText.Templates).Concat(CharacterExpandedSpeciesText.Templates).Concat(CharacterClericText.Templates).Concat(CharacterDruidText.Templates).Concat(CharacterDescriptionText.Templates)
            .Select(template=>new TemplateMatcher(template)).ToArray();

        internal static string FeatureLines(string source, Func<string, string> translate) =>
            string.Join("\n", source.Split('\n').Select(line => {
                foreach(var template in FeatureTemplates) {
                    if(!line.StartsWith(template.prefix,StringComparison.Ordinal))continue;
                    var match=template.regex.Match(line);
                    if(match.Success)return string.Format(translate(template.text),match.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Select(g=>(object)translate(g.Value)).ToArray());
                }
                if (line.StartsWith("Винахідник ", StringComparison.Ordinal) && line.Contains(" • D&D ")) return translate("Винахідник") + line.Substring("Винахідник".Length);
                foreach (string id in new[] { "plans", "infusions", "attunement", "dwarf24" })
                {
                    string template = CharacterArtificerText.Get(id);
                    string pattern = "^" + System.Text.RegularExpressions.Regex.Escape(template).Replace("\\{0}", "([0-9]+)").Replace("\\{1}", "([0-9]+)") + "$";
                    var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                    if (match.Success) return string.Format(translate(template), match.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Select(group => (object)group.Value).ToArray());
                }
                const string masteryPrefix = "Майстерність зброї: ";
                const string expertisePrefix = "Експертиза: ";
                const string enemyPrefix = "Обраний ворог: ";
                const string enemySuffix = ". Перевага на Виживання для вистежування та перевірки Інтелекту для згадування відомостей.";
                const string terrainPrefix = "Природний дослідник: ";
                const string terrainSuffix = ". Подвійна майстерність на пов’язані перевірки Інтелекту й Мудрості; додаткові переваги під час подорожі цією місцевістю.";
                const string dragonPrefix = "Драконячий предок: ";
                const string disciplinePrefix = "Стихійна дисципліна: ";
                const string disciplineSuffix = " очок ци; без матеріальних компонентів.";
                if (line.StartsWith(disciplinePrefix, StringComparison.Ordinal) && line.EndsWith(disciplineSuffix, StringComparison.Ordinal))
                    return string.Format(translate("Стихійна дисципліна: {0} очок ци; без матеріальних компонентів."), line.Substring(disciplinePrefix.Length, line.Length - disciplinePrefix.Length - disciplineSuffix.Length));
                const string affinityPrefix = "Стихійна спорідненість: ";
                if (line.StartsWith(affinityPrefix, StringComparison.Ordinal) && line.EndsWith(".", StringComparison.Ordinal) && CharacterSorcererRules.Elements.Contains(line.Substring(affinityPrefix.Length).TrimEnd('.'))) return translate("Стихійна спорідненість:") + " " + translate(line.Substring(affinityPrefix.Length).TrimEnd('.')) + ".";
                if (line.StartsWith(dragonPrefix, StringComparison.Ordinal)) return translate("Драконячий предок:") + " " + translate(line.Substring(dragonPrefix.Length).TrimEnd('.')) + ".";
                if (line.StartsWith(enemyPrefix, StringComparison.Ordinal) && line.EndsWith(enemySuffix, StringComparison.Ordinal))
                    return translate("Обраний ворог:") + " " + translate(line.Substring(enemyPrefix.Length, line.Length - enemyPrefix.Length - enemySuffix.Length)) + ". " + translate(enemySuffix.Substring(2));
                if (line.StartsWith(terrainPrefix, StringComparison.Ordinal) && line.EndsWith(terrainSuffix, StringComparison.Ordinal))
                    return translate("Природний дослідник:") + " " + translate(line.Substring(terrainPrefix.Length, line.Length - terrainPrefix.Length - terrainSuffix.Length)) + ". " + translate(terrainSuffix.Substring(2));
                if (line.StartsWith(expertisePrefix, StringComparison.Ordinal))
                    return translate("Експертиза:") + " " + string.Join(", ", line.Substring(expertisePrefix.Length).TrimEnd('.').Split(new[] { ", " }, StringSplitOptions.None).Select(translate)) + ".";
                foreach (string prefix in new[] { "Обрані вороги: ", "Обрані місцевості: " })
                    if (line.StartsWith(prefix, StringComparison.Ordinal)) return translate(prefix.TrimEnd()) + " " + string.Join(", ", line.Substring(prefix.Length).Split(new[] { ", " }, StringSplitOptions.None).Select(translate));
                if (!line.StartsWith(masteryPrefix, StringComparison.Ordinal)) return translate(line);
                string names = line.Substring(masteryPrefix.Length).TrimEnd('.');
                return translate("Майстерність зброї:") + " " +
                    string.Join(", ", names.Split(new[] { ", " }, StringSplitOptions.None).Select(translate)) + ".";
            }));

        internal static string Proficiencies(CharacterCreationDraft draft, CreatedCharacterStats stats, Func<string, string> translate)
        {
            bool modern = draft.edition == RulesEdition.Rules2024;
            bool dwarf = CharacterSpeciesRules.IsDwarf(draft);
            string tools = string.Join(", ", CharacterBackgroundRules.Tools(draft).Select(translate)) + ".";
            if (dwarf && !modern)
                tools += " " + translate(draft.artisanTool) + translate("; бойова сокира, ручна сокира, легкий і бойовий молоти.");
            if (CharacterWarforgedRules.Warforged(draft) && draft.speciesTool.Length > 0) tools += " " + translate(draft.speciesTool) + ".";
            if (CharacterOriginFeatRules.Tools(draft).Length > 0) tools += " " + string.Join(", ", CharacterOriginFeatRules.Tools(draft).Select(translate)) + ".";
            if (CharacterMonkSubclassRules.Is(draft, "mercy")) tools += " " + translate("Набір травника") + ".";
            if (CharacterRogueRules.Is(draft, "assassin")) tools += " " + string.Join(", ", CharacterRogueRules.Tools(draft).Skip(1).Select(translate)) + ".";
            if (CharacterFighterRules.Is(draft, "battlemaster")) tools += " " + translate(draft.fighterTool) + ".";
            if (draft.characterClass == "monk") tools += " " + translate(draft.monkTool) + ".";
            if (draft.characterClass == "bard") tools += " " + string.Join(", ", draft.bardInstruments.Select(translate)) + ".";
            if (draft.characterClass == "druid") tools += " " + translate("Набір травника") + ".";
            if (draft.characterClass == "artificer") tools += " " + string.Join(", ", CharacterArtificerRules.GrantedTools(draft).Select(translate)) + ".";
            if (draft.characterClass == "bloodhunter") tools += " " + translate("Алхімічне приладдя") + ".";
            if (CharacterGnomeRules.ClassicRock(draft) && draft.characterClass != "artificer") tools += " " + translate("Інструменти лудильника") + ".";
            if (CharacterGnomeRules.NeedsToolReplacement(draft)) tools += " " + translate(draft.originToolReplacement) + ".";
            string languages = string.Join(", ", CharacterSpeciesRules.AutomaticLanguages(draft).Concat(CharacterSpeciesRules.AdditionalLanguages(draft)).Distinct().Select(translate));
            if (CharacterElfRules.Elf(draft) && !CharacterElfRules.Modern(draft)) tools += " " + translate(draft.speciesLineage == "drow"
                ? "Ельфійська зброя: рапіри, короткі мечі й ручні арбалети." : "Ельфійська зброя: довгі й короткі мечі, короткі й довгі луки.");
            string result = translate(draft.characterClass == "bloodhunter"
                    ? "Легкі й середні обладунки, щити, проста й військова зброя. Ряткидки: Спритність, Інтелект."
                    : draft.characterClass == "artificer"
                    ? "Легкі й середні обладунки, щити й проста зброя. Ряткидки: Статура, Інтелект."
                    : draft.characterClass == "warlock"
                    ? "Легкі обладунки; проста зброя. Ряткидки: Мудрість, Харизма."
                    : draft.characterClass == "sorcerer"
                    ? modern ? "Без обладунків; проста зброя. Ряткидки: Статура, Харизма."
                        : "Без обладунків; кинджали, дротики, пращі, посохи й легкі арбалети. Ряткидки: Статура, Харизма."
                    : draft.characterClass == "wizard"
                    ? modern ? "Без обладунків; проста зброя. Ряткидки: Інтелект, Мудрість."
                        : "Без обладунків; кинджали, дротики, пращі, посохи й легкі арбалети. Ряткидки: Інтелект, Мудрість."
                    : draft.characterClass == "cleric"
                    ? CharacterClericRules.MartialWeapons(draft) ? "Усі обладунки, щити, проста й військова зброя. Ряткидки: Мудрість, Харизма."
                        : CharacterClericRules.HeavyArmor(draft) ? "Усі обладунки, щити й проста зброя. Ряткидки: Мудрість, Харизма."
                        : "Легкі й середні обладунки, щити й проста зброя. Ряткидки: Мудрість, Харизма."
                    : draft.characterClass == "druid"
                    ? modern ? draft.primalOrder == "warden" ? "Легкі й середні обладунки, щити, проста й військова зброя. Ряткидки: Інтелект, Мудрість."
                        : "Легкі обладунки, щити й проста зброя. Ряткидки: Інтелект, Мудрість."
                        : "Легкі й середні неметалеві обладунки, неметалеві щити; дубини, кинджали, дротики, списи, булави, посохи, скімітари, серпи й пращі. Ряткидки: Інтелект, Мудрість."
                    : draft.characterClass == "bard"
                    ? CharacterBardRules.Is(draft,"valor") ? "Легкі й середні обладунки, щити, проста й військова зброя. Ряткидки: Спритність, Харизма." : modern ? "Легкі обладунки; проста зброя. Ряткидки: Спритність, Харизма."
                        : "Легкі обладунки; проста зброя, ручні арбалети, довгі й короткі мечі, рапіри. Ряткидки: Спритність, Харизма."
                    : draft.characterClass == "monk"
                    ? modern ? "Без обладунків; проста та легка військова зброя. Ряткидки: Сила, Спритність."
                        : "Без обладунків; проста зброя й короткий меч. Ряткидки: Сила, Спритність."
                    : draft.characterClass == "rogue"
                    ? "Легкі обладунки; злодійські інструменти. Ряткидки: Спритність, Інтелект."
                    : draft.characterClass == "barbarian"
                    ? "Легкі та середні обладунки, щити, проста й військова зброя. Ряткидки: Сила, Статура."
                    : draft.characterClass == "paladin"
                    ? "Усі обладунки, щити, проста й військова зброя. Ряткидки: Мудрість, Харизма."
                    : draft.characterClass == "ranger"
                    ? "Легкі й середні обладунки, щити, проста й військова зброя. Ряткидки: Сила, Спритність."
                    : "Усі обладунки, щити, проста й військова зброя. Ряткидки: Сила, Статура.") +
                "\n" + translate("Навички:") + " " + string.Join(", ", stats.skills.Select(index => translate(CharacterCreationRules.SkillNames[index]))) +
                ".\n" + tools + "\n" + translate("Мови:") + " " + languages + ".";
            if (CharacterArtificerRules.Is(draft, "armorer")) result += "\n" + translate(CharacterArtificerText.Get("proficiency.heavy"));
            if (CharacterArtificerRules.Is(draft, "battlesmith")) result += "\n" + translate(CharacterArtificerText.Get("proficiency.martial"));
            if (CharacterArtificerRules.Revised(draft) && CharacterArtificerRules.Is(draft, "artillerist")) result += "\n" + translate(CharacterArtificerText.Get("proficiency.ranged"));
            if(CharacterExpandedSpeciesRules.TranceCount(draft)>0)result+="\n"+translate(CharacterExpandedSpeciesRules.Text(draft.species=="githyanki"?"ui.temporaryprofs":"ui.tranceprofs"))+": "+string.Join(", ",Enumerable.Range(0,CharacterExpandedSpeciesRules.TranceCount(draft)).Select(i=>CharacterExpandedSpeciesRules.Trance(draft,i)).Where(key=>CharacterExpandedSpeciesRules.TranceOptions().Contains(key)).Select(key=>translate(key.Substring(key.IndexOf(':')+1))))+".";
            if(draft.species=="giff")result+="\n"+translate(CharacterExpandedSpeciesRules.Text("proficiency.firearms"));
            if(draft.species=="satyr"&&!string.IsNullOrWhiteSpace(draft.expandedInstrument))result+="\n"+translate(draft.expandedInstrument)+".";
            if (draft.species == "mountaindwarf") result = result.Replace(translate("Без обладунків") + ";", translate("Легкі й середні обладунки") + ";")
                .Replace(translate("Легкі обладунки") + ";", translate("Легкі й середні обладунки") + ";");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"[ \t]*(?:Ряткидки|Saving throws|Спасброски):[^\r\n]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string skillLabel = translate("Навички:");
            return string.Join("\n", result.Split('\n').Where(line => !line.TrimStart().StartsWith(skillLabel, StringComparison.Ordinal))).Trim();
        }

        internal static string Equipment(CharacterCreationDraft draft, Func<string, string> translate)
        {
            bool modern = draft.edition == RulesEdition.Rules2024;
            string kit = draft.characterClass == "bloodhunter"
                ? Format(translate, "{0}; легкий арбалет; 20 болтів; {1}; набір мандрівника; алхімічне приладдя.", translate(draft.bloodWeapon) + (draft.bloodWeaponMode == 1 ? "; " + translate(draft.bloodSecondWeapon) : ""), translate(draft.bloodArmor))
                : draft.characterClass == "artificer"
                ? modern ? translate(draft.equipment == 0 ? "Проклепана шкіра; кинджал; злодійські інструменти; інструменти лудильника; набір підземельника; 16 зм." : "150 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; легкий арбалет; 20 болтів; {2}; злодійські інструменти; набір підземельника.", translate(draft.artificerWeapon), translate(draft.artificerSecondWeapon), translate(draft.artificerArmor))
                : draft.characterClass == "warlock"
                ? modern ? translate(draft.equipment == 0 ? "Шкіряний обладунок; серп; 2 кинджали; арканний фокус (сфера); книга окультних знань; набір ученого; 15 зм." : "100 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; {2}; шкіряний обладунок; 2 кинджали; {3}.", translate(draft.warlockWeapon) + (draft.warlockWeapon == "Легкий арбалет" ? "; " + translate("20 болтів") : ""), translate(draft.warlockSecondWeapon), translate(draft.warlockFocus), translate(draft.warlockPack == 0 ? "Набір ученого" : "Набір підземельника"))
                : draft.characterClass == "sorcerer"
                ? modern ? translate(draft.equipment == 0 ? "Спис; 2 кинджали; арканний фокус (кристал); набір підземельника; 28 зм." : "50 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; 2 кинджали; {2}.", translate(draft.sorcererWeapon) + (draft.sorcererWeapon == "Легкий арбалет" ? "; " + translate("20 болтів") : ""), translate(draft.sorcererFocus), translate(draft.sorcererPack == 0 ? "Набір підземельника" : "Набір мандрівника"))
                : draft.characterClass == "wizard"
                ? modern ? translate(draft.equipment == 0 ? "2 кинджали; арканний фокус (посох); мантія; книга заклинань; набір ученого; 5 зм." : "55 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; книга заклинань; {2}.", translate(draft.wizardWeapon), translate(draft.wizardFocus), translate(draft.wizardPack == 0 ? "Набір ученого" : "Набір мандрівника"))
                : draft.characterClass == "cleric"
                ? modern ? translate(draft.equipment == 0 ? "Кольчужна сорочка; щит; булава; священний символ; набір жерця; 7 зм." : "110 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; {2}; щит; священний символ; {3}.", translate(draft.clericWeapon), translate(draft.clericArmor),
                        translate(draft.clericSecondWeapon) + (draft.clericSecondWeapon == "Легкий арбалет" ? "; " + translate("20 болтів") : ""), translate(draft.clericPack == 0 ? "Набір жерця" : "Набір мандрівника"))
                : draft.characterClass == "druid"
                ? modern ? translate(draft.equipment == 0 ? "Шкіряний обладунок; щит; серп; фокус друїда (посох); набір мандрівника; набір травника; 9 зм." : "50 зм замість класового спорядження.")
                    : Format(translate, "{0}; {1}; шкіряний обладунок; набір мандрівника; фокус друїда.", translate(draft.druidFirstItem), translate(draft.druidWeapon))
                : draft.characterClass == "bard"
                ? modern ? draft.equipment == 0
                    ? Format(translate, "Шкіряний обладунок; 2 кинджали; {0}; набір артиста; 19 зм.", translate(draft.bardCarriedInstrument))
                    : translate("90 зм замість класового спорядження.")
                    : Format(translate, "{0}; шкіряний обладунок; кинджал; {1}; {2}.", translate(draft.bardWeapon), translate(draft.bardCarriedInstrument), translate(draft.bardPack == 0 ? "Набір дипломата" : "Набір артиста"))
                : draft.characterClass == "rogue"
                ? translate(modern
                    ? draft.equipment == 0 ? "Шкіряний обладунок; 2 кинджали; короткий меч; короткий лук; 20 стріл; сагайдак; злодійські інструменти; набір зломщика; 8 зм." : "100 зм замість класового спорядження."
                    : draft.equipment == 0 ? "Рапіра; короткий лук; 20 стріл; набір зломщика; шкіряний обладунок; 2 кинджали; злодійські інструменти." : "Короткий меч; ще один короткий меч; набір мандрівника; шкіряний обладунок; 2 кинджали; злодійські інструменти.")
                : draft.characterClass == "paladin"
                ? translate(modern
                    ? draft.equipment == 0 ? "Кольчуга; щит; довгий меч; 6 метальних списів; священний символ; набір жерця; 9 зм." : "150 зм замість класового спорядження."
                    : draft.equipment == 0 ? "Кольчуга; довгий меч; щит; 5 метальних списів; набір жерця; священний символ."
                    : "Кольчуга; два довгі мечі; 5 метальних списів; набір мандрівника; священний символ.")
                : draft.characterClass == "ranger"
                ? translate(modern
                    ? draft.equipment == 0 ? "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; фокус друїда; набір мандрівника; 7 зм." : "150 зм замість класового спорядження."
                    : draft.equipment == 0 ? "Лускатий обладунок; два короткі мечі; набір підземельника; довгий лук; сагайдак; 20 стріл."
                    : "Шкіряний обладунок; два короткі мечі; набір мандрівника; довгий лук; сагайдак; 20 стріл.")
                : draft.characterClass == "monk"
                ? translate(modern
                    ? draft.equipment == 0 ? "Спис; 5 кинджалів; {0}; набір мандрівника; 11 зм." : "50 зм замість класового спорядження."
                    : "{0}; {1}; 10 дротиків.")
                    .Replace("{0}", translate(modern ? draft.monkTool : draft.equipment == 0 ? "Короткий меч" : draft.monkWeapon))
                    .Replace("{1}", translate(draft.monkPack == 0 ? "Набір підземельника" : "Набір мандрівника"))
                : draft.characterClass == "barbarian"
                ? translate(modern
                    ? draft.equipment == 0 ? "Сокира дворучна; 4 ручні сокири; набір мандрівника; 15 зм." : "75 зм замість класового спорядження."
                    : draft.equipment == 0 ? "Сокира дворучна; 2 ручні сокири; набір мандрівника; 4 метальні списи." : "Довгий меч; 2 ручні сокири; набір мандрівника; 4 метальні списи.")
                : CharacterFighterRules.TakesStartingGold(draft) ? translate(CharacterFighterRules.GoldOnlyDescription)
                : modern
                ? translate(draft.equipment == 0 ? "Кольчуга; дворучний меч; ціп; 8 метальних списів; набір підземельника; 4 зм."
                    : "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; набір підземельника; 11 зм.")
                : translate(draft.equipment == 0 ? "Кольчуга; довгий меч; щит; 2 ручні сокири; набір підземельника."
                    : "Шкіряний обладунок; довгий лук; 20 стріл; довгий меч; щит; 2 ручні сокири; набір підземельника.");
            if (CharacterRogueRules.Is(draft, "assassin") && modern) kit += "\n" + translate("Набір гримувальника; набір отруйника.");
            string origin = CharacterBackgroundRules.Equipment(draft, translate);
            if (CharacterWarlockRules.Blade(draft) && draft.pactWeapon >= 0 && draft.pactWeapon < CharacterWarlockRules.PactWeapons.Length)
                kit += "\n" + Format(translate, "Зброя договору: {0}.", translate(CharacterWarlockRules.PactWeapons[draft.pactWeapon]));
            var artificer = draft.secondaryClass?.classId == "artificer" ? draft.secondaryClass.ClassDraft(draft) : draft;
            if(CharacterArtificerRules.Active(artificer)&&CharacterArtificerEquipment.ChoiceError(artificer)==null)
                foreach(int slot in CharacterArtificerEquipment.Selected(artificer))
                {
                    string target = CharacterArtificerEquipment.Target(artificer, slot);
                    kit += "\n" + Format(translate, CharacterDescriptionText.Get("equip.record"),
                        translate(CharacterArtificerPlans.All.First(p => p.edition == (modern ? 2024 : 2014) && p.id == artificer.artificerPlans[slot]).name),
                        translate(CharacterArtificerEquipment.Label(artificer, target)) + CharacterArtificerEquipment.TargetNumber(artificer, target));
                }
            return kit + "\n" + translate("Передісторія:") + " " + origin;
        }

        internal static Dictionary<int, string> GeneratedSheetFields(CharacterCreationDraft draft, CreatedCharacterStats stats, Func<string, string> translate)
        {
            // Omit facts already visible in the sheet's class, skills, resource, or Sneak Attack fields.
            string translatedFeatures = FeatureLines(string.Join("\n", stats.features.Split('\n').Where(line =>
                !System.Text.RegularExpressions.Regex.IsMatch(line, @"^[^:\r\n]+ \d+ • D&D (2014|2024)$") &&
                !line.StartsWith("Експертиза:", StringComparison.Ordinal) &&
                !line.StartsWith("Майстерність зброї:", StringComparison.Ordinal) &&
                !line.StartsWith("Заклинання:", StringComparison.Ordinal) &&
                !line.StartsWith("Походження людини —", StringComparison.Ordinal))), translate);
            string[] features = CreationDescriptionLayout.Paragraphs(translatedFeatures);
            var classTracks = draft.secondaryClass?.IsSelected == true
                ? new[] { draft, draft.secondaryClass.ClassDraft(draft) }
                : new[] { draft };
            foreach (var track in classTracks)
            {
                var alwaysPrepared = CreationSpellPreparation.Automatic(track)
                    .Distinct().Where(spell => CharacterSpellCatalog.Find(track.edition, spell)?.level > 0)
                    .Select(translate).ToArray();
                if (alwaysPrepared.Length > 0)
                    features = features.Append(Format(translate, "Завжди підготовлені заклинання ({0}): {1}.",
                        translate(CharacterClassCatalog.Find(track.characterClass).name), string.Join(", ", alwaysPrepared))).ToArray();
                if (!CharacterArtificerRules.Active(track)) continue;
                var specialistTools = CharacterArtificerRules.SubclassTools(track)
                    .Concat(track.artificerToolReplacements ?? Enumerable.Empty<string>())
                    .Where(tool => !string.IsNullOrWhiteSpace(tool)).Distinct().Select(translate).ToArray();
                if (specialistTools.Length > 0)
                    features = features.Append(Format(translate, "Інструменти спеціалізації: володієш такими наборами — {0}.",
                        string.Join(", ", specialistTools))).ToArray();
            }
            if (stats.expertise != null && stats.expertise.Count > 0)
                features = features.Concat(new[]{translate(CharacterDescriptionText.Get("expertise"))}).ToArray();
            features=features.Concat(CreationMasteryDescriptions.For(draft,translate)).ToArray();
            if (draft.secondaryClass?.IsSelected == true)
            {
                features = features.Concat(CreationMasteryDescriptions.For(draft.secondaryClass.ClassDraft(draft.edition), translate))
                    .Concat(CharacterClassHistory.FromCreation(draft).HitDice().Select(pool => Format(translate,
                        "Кістки ОЗ: {0} × к{1}.", pool.count, pool.die))).Distinct().ToArray();
            }
            if (draft.species == "dragonborn")
            {
                bool modern = draft.edition == RulesEdition.Rules2024;
                string shape = modern ? "Конус 15 футів або лінія 30×5 футів" : draft.speciesAncestor <= 4 ? "Лінія 30×5 футів" : "Конус 15 футів";
                string save = !modern && (draft.speciesAncestor == 6 || draft.speciesAncestor >= 8) ? "Статура" : "Спритність";
                features = features.Concat(new[] { Format(translate, "Дихання: {0}; {1}, ряткидок {2}, СК {3}. Опір відповідній шкоді.", translate(CharacterLegacySpeciesRules.BreathType(draft)), translate(shape), translate(save), 8 + CharacterLegacySpeciesRules.Proficiency(draft) + CharacterCreationRules.Modifier(stats.scores[2])) }).ToArray();
            }
            if (CharacterLegacySpeciesRules.NeedsSize(draft)) features = features.Concat(new[] { Format(translate, "Розмір: {0}.", translate(draft.speciesSize == "small" ? "Малий" : "Середній")) }).ToArray();
            if (CharacterLevelAdvancement.HasBoon(draft, "energy")) features = features.Concat(new[] { Format(translate, "Опори: {0}.", string.Join(", ", CharacterLevelAdvancement.BoonTrack(draft).epicResistances.Where(i => i >= 0 && i < CharacterLevelAdvancement.DamageTypes.Length).Select(i => translate(CharacterLevelAdvancement.DamageTypes[i])))) }).ToArray();
            if(CharacterBloodHunterRules.Profane(draft)) {
                var spells=CharacterBloodHunterRules.AutomaticSpells(draft);
                if(spells.Length>0)features=features.Concat(new[]{Format(translate,CharacterBloodHunterRules.Text("profane.revealed"),translate(spells[0]))}).ToArray();
                if(spells.Length>1)features=features.Concat(new[]{Format(translate,CharacterBloodHunterRules.Text("profane.unsealed"),translate(spells[1]))}).ToArray();
            }
            string primary = CreationDescriptionLayout.Primary(features, out string[] remainingFeatures);
            string[] notes = CreationDescriptionLayout.Pack(remainingFeatures);
            var fields = new Dictionary<int, string> {
                [54] = draft.secondaryClass?.IsSelected == true ? draft.secondaryClass.level.ToString() : "",
                [55] = draft.secondaryClass?.IsSelected == true ? translate(CharacterClassCatalog.Find(draft.secondaryClass.classId)?.name ?? "") : "",
                [56] = draft.secondaryClass?.IsSelected == true ? SubclassName(draft.secondaryClass.ClassDraft(draft.edition), translate) : "",
                [60] = translate(CharacterClassCatalog.Find(draft.characterClass).name),
                [61] = SubclassName(draft, translate),
                [62] = translate(CharacterSpeciesRules.LineageName(draft)),
                [63] = translate(stats.speciesName),
                [64] = translate(stats.backgroundName),
                [65] = translate(draft.alignment),
                [67] = draft.characterClass == "bloodhunter" ? translate(draft.bloodWeapon)
                    : draft.characterClass == "artificer"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.artificerWeapon : draft.equipment == 0 ? "Кинджал" : "")
                    : draft.characterClass == "warlock"
                    ? translate(CharacterWarlockRules.Blade(draft) && draft.pactWeapon >= 0 && draft.pactWeapon < CharacterWarlockRules.PactWeapons.Length ? CharacterWarlockRules.PactWeapons[draft.pactWeapon] : draft.edition == RulesEdition.Rules2014 ? draft.warlockWeapon : draft.equipment == 0 ? "Серп" : "")
                    : draft.characterClass == "sorcerer"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.sorcererWeapon : draft.equipment == 0 ? "Спис" : "")
                    : draft.characterClass == "wizard"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.wizardWeapon : draft.equipment == 0 ? "Бойовий посох" : "")
                    : draft.characterClass == "cleric"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.clericWeapon : draft.equipment == 0 ? "Булава" : "")
                    : draft.characterClass == "druid"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.druidWeapon : draft.equipment == 0 ? "Серп" : "")
                    : draft.characterClass == "bard"
                    ? translate(draft.edition == RulesEdition.Rules2014 ? draft.bardWeapon : draft.equipment == 0 ? "Кинджал" : "")
                    : draft.characterClass == "monk"
                    ? translate(draft.edition == RulesEdition.Rules2024 && draft.equipment == 1 ? "Беззбройний удар" : draft.edition == RulesEdition.Rules2024 ? "Спис" : draft.equipment == 1 ? draft.monkWeapon : "Короткий меч")
                    : draft.characterClass == "paladin" ? translate(draft.edition == RulesEdition.Rules2024 && draft.equipment == 1 ? "" : "Довгий меч")
                    : draft.characterClass == "ranger" ? translate(draft.edition == RulesEdition.Rules2024 && draft.equipment == 1 ? "" : "Довгий лук")
                    : draft.characterClass == "rogue"
                    ? translate(draft.edition == RulesEdition.Rules2024 && draft.equipment == 1 ? "" : draft.equipment == 0 && draft.edition == RulesEdition.Rules2014 ? "Рапіра" : "Короткий меч")
                    : draft.characterClass == "barbarian"
                    ? translate(draft.edition == RulesEdition.Rules2024 && draft.equipment == 1 ? "" : draft.equipment == 0 ? "Сокира дворучна" : "Довгий меч")
                    : CharacterFighterRules.TakesStartingGold(draft) ? ""
                    : translate(draft.equipment == 1 ? "Довгий лук" : draft.edition == RulesEdition.Rules2024 ? "Дворучний меч" : "Довгий меч"),
                [75] = draft.characterClass == "ranger" ? Format(translate, "Атак у дії Атака: {0}.", draft.level >= 5 ? 2 : 1) : draft.characterClass == "fighter" ? Format(translate, "Атак у дії Атака: {0}.", CharacterFighterRules.Attacks(draft)) + (CharacterFighterRules.Is(draft, "battlemaster") ? "\n" + Format(translate, "СК маневрів: {0}; кістка переваги к{1}.", 8 + CharacterLegacySpeciesRules.Proficiency(draft) + Math.Max(CharacterCreationRules.Modifier(stats.scores[0]), CharacterCreationRules.Modifier(stats.scores[1])), CharacterFighterRules.SuperiorityDie(draft)) : "") : draft.characterClass == "ranger" || draft.characterClass == "paladin" ? "" : draft.characterClass == "monk" ? translate(CharacterMonkRules.AttackNotes(draft)) + (draft.level >= 2 ? "\n" + Format(translate, "СК ци: {0} (Мудрість).", 8 + CharacterLegacySpeciesRules.Proficiency(draft) + CharacterCreationRules.Modifier(stats.scores[4])) : "") : draft.characterClass == "rogue" ? translate(CharacterRogueRules.Features(draft).Split('\n')[0]) + (draft.edition == RulesEdition.Rules2024 && draft.level >= 5 ? "\n" + Format(translate, "СК хитрого удару: {0} (Спритність).", 8 + CharacterLegacySpeciesRules.Proficiency(draft) + CharacterCreationRules.Modifier(stats.scores[1])) : "") : draft.characterClass == "barbarian" ? Format(translate, "Лють: +{0} шкоди при атаці Силою за умов класу; використання відстежуються в ресурсах.", CharacterBarbarianRules.RageDamage(draft)) : translate(draft.fightingStyle == FighterStyle.Archery ? "Стрільба: +2 до атаки далекобійною зброєю (бонус уже враховано для лука)." :
                    draft.fightingStyle == FighterStyle.Dueling ? "Дуелі: +2 шкоди при атаці відповідною одноручною зброєю." :
                    draft.fightingStyle == FighterStyle.GreatWeapon ? "Велика зброя: особлива дія кубиків шкоди дворучної зброї за правилами вибраної редакції." : "Захист: +1 КО у обладунку."),
                [76] = primary,
                [77] = Proficiencies(draft, stats, translate) + CreationMulticlassResources.Proficiencies(draft, translate),
                [82] = Equipment(draft, translate),
                [101] = translate(draft.characterClass == "barbarian" ? "Лють варвара" : draft.characterClass == "paladin" ? "" : draft.characterClass == "ranger" && draft.edition == RulesEdition.Rules2024 ? "Мітка мисливця" : draft.characterClass == "rogue" || draft.characterClass == "monk" || draft.characterClass == "ranger" ? "" : "Друге дихання")
            };
            if (draft.characterClass == "bard" || draft.characterClass == "druid" || draft.characterClass == "cleric" || draft.characterClass == "wizard" || draft.characterClass == "sorcerer" || draft.characterClass == "warlock" || draft.characterClass == "artificer" || draft.characterClass == "bloodhunter") { fields[75] = ""; fields[101] = ""; }
            for (int i = 0; i < notes.Length; i++) fields[i < 4 ? 78 + i : 98 + i] = notes[i];
            if(CharacterBloodHunterRules.Active(draft))fields[75]=Format(translate,CharacterBloodHunterRules.Text("attacks"),draft.level>=5?2:1)+"\n"+Format(translate,CharacterBloodHunterRules.Text("dc"),8+CharacterLegacySpeciesRules.Proficiency(draft)+CharacterBloodHunterRules.HemocraftModifier(draft));
            if (draft.secondaryClass?.IsSelected == true)
            {
                var secondary = draft.secondaryClass.ClassDraft(draft.edition);
                int Attacks(CharacterCreationDraft track) => track.characterClass switch
                {
                    "fighter" => CharacterFighterRules.Attacks(track),
                    "barbarian" or "bloodhunter" or "monk" or "paladin" or "ranger" => track.level >= 5 ? 2 : 1,
                    "artificer" => track.level >= 5 &&
                        (CharacterArtificerRules.Is(track, "armorer") || CharacterArtificerRules.Is(track, "battlesmith")) ? 2 : 1,
                    "bard" => CharacterBardRules.Is(track, "valor") && track.level >= 6 ? 2 : 1,
                    _ => 1
                };
                int primaryAttacks = Attacks(draft);
                int count = Math.Max(primaryAttacks, Attacks(secondary));
                if (count > primaryAttacks)
                {
                    string currentLine = Format(translate, "Атак у дії Атака: {0}.", primaryAttacks);
                    string newLine = Format(translate, "Атак у дії Атака: {0}.", count);
                    fields[75] = fields[75].StartsWith(currentLine, StringComparison.Ordinal)
                        ? newLine + fields[75].Substring(currentLine.Length)
                        : string.Join("\n", new[] { newLine, fields[75] }.Where(line => !string.IsNullOrWhiteSpace(line)));
                }
                if (CharacterFighterRules.Is(secondary, "battlemaster"))
                    fields[75] += "\n" + Format(translate, "СК маневрів: {0}; кістка переваги к{1}.",
                        8 + CharacterLegacySpeciesRules.Proficiency(draft) +
                        Math.Max(CharacterCreationRules.Modifier(stats.scores[0]), CharacterCreationRules.Modifier(stats.scores[1])),
                        CharacterFighterRules.SuperiorityDie(secondary));
            }
            return fields;
        }

        private static string SubclassName(CharacterCreationDraft draft, Func<string, string> translate)
        {
            if (CharacterWizardRules.HasSchool(draft)) return translate(CharacterWizardText.Get(draft.subclass));
            if (CharacterArtificerRules.Active(draft) && draft.level >= 3 && CharacterArtificerRules.Options(draft).Contains(draft.subclass)) return translate(CharacterArtificerText.Get("name." + draft.subclass));
            if (CharacterBloodHunterRules.Active(draft) && draft.level>=3 && CharacterBloodHunterRules.Orders.Contains(draft.subclass))return translate(CharacterBloodHunterRules.Text("order."+draft.subclass));
            if (CharacterBardRules.Active(draft) && draft.level >= 3 && CharacterBardRules.Options(draft).Contains(draft.subclass)) return translate(CharacterBardRules.Names[Array.IndexOf(CharacterBardRules.Modern,draft.subclass)]);
            if (CharacterSorcererRules.Active(draft) && draft.level >= CharacterSorcererRules.SubclassLevel(draft) && CharacterSorcererRules.Options(draft).Contains(draft.subclass)) return translate(CharacterSorcererRules.Names[Array.IndexOf(CharacterSorcererRules.Modern, draft.subclass)]);
            if (draft.characterClass == "ranger" && draft.level >= 3 && CharacterRangerRules.Options(draft).Contains(draft.subclass)) return translate(CharacterRangerRules.Names[Array.IndexOf(CharacterRangerRules.Modern, draft.subclass)]);
            if (draft.characterClass == "paladin" && draft.level >= 3 && CharacterPaladinRules.Options(draft).Contains(draft.subclass)) return translate(CharacterPaladinRules.Names[Array.IndexOf(CharacterPaladinRules.Modern, draft.subclass)]);
            if (draft.characterClass == "fighter" && CharacterFighterRules.Options(draft).Contains(draft.subclass)) return translate(CharacterFighterRules.Names[Array.IndexOf(CharacterFighterRules.Modern, draft.subclass)]);
            if (draft.characterClass == "warlock" && draft.level >= (CharacterWarlockRules.Modern(draft)?3:1))
            {
                int index = Array.IndexOf(CharacterWarlockRules.Patrons, draft.subclass);
                return index < 0 ? "" : translate(CharacterWarlockRules.PatronNames[index]);
            }
            if (draft.characterClass == "sorcerer" && draft.edition == RulesEdition.Rules2014)
                return translate(draft.subclass == "draconic" ? "Драконяче походження" : draft.subclass == "wildmagic" ? "Дика магія" : "");
            if(CharacterDruidRules.Active(draft)&&draft.level>=CharacterDruidRules.SubclassLevel(draft))return CharacterDruidRules.Options(draft).Contains(draft.subclass)?translate(CharacterDruidRules.Text("circle."+draft.subclass)):"";
            if (CharacterClericRules.Active(draft) && draft.level>=CharacterClericRules.SubclassLevel(draft))
            {
                int index = Array.IndexOf(CharacterClericRules.Domains, draft.subclass);
                return index < 0 ? "" : translate(CharacterClericRules.DomainNames[index]);
            }
            if (draft.level < 3) return "";
            if (CharacterRogueRules.Active(draft)) return translate(CharacterRogueRules.Name(draft.subclass));
            if (CharacterBarbarianRules.Active(draft)) return translate(CharacterBarbarianRules.Name(draft.subclass));
            return translate(draft.subclass == "champion" ? "Чемпіон" : draft.subclass == "berserker" ? "Берсерк" : draft.subclass == "thief" ? "Злодій" : draft.characterClass == "monk" && CharacterMonkSubclassRules.Options(draft).Contains(draft.subclass) ? CharacterMonkSubclassRules.Names[Array.IndexOf(CharacterMonkSubclassRules.Modern, draft.subclass)] : "");
        }

        private static string Format(Func<string, string> translate, string source, params object[] values) =>
            string.Format(translate(source), values);
    }
}
