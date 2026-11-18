using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CharacterCreationValidation
    {
        internal static string StepError(CharacterCreationDraft draft, int page, bool isLevelUp, Func<string, string> translate)
        {
            if (CharacterBardRules.Active(draft)) { string bardError = CharacterBardRules.ChoiceError(draft, page); if (bardError != null) return bardError; }
            if (CharacterRangerRules.Active(draft)) { string rangerError = CharacterRangerRules.ChoiceError(draft, page); if (rangerError != null) return rangerError; }
            if (page == 0)
            {
                if ((int)draft.edition < 0) return "Обери редакцію D&D.";
                if (string.IsNullOrEmpty(draft.characterClass)) return "Обери клас персонажа.";
                if (draft.level < 1 || draft.level > CharacterCreationRules.MaxSupportedLevel(draft)) return "Обери доступний рівень класу.";
                string druidError = CharacterDruidRules.ChoiceError(draft, 0) ?? CharacterDruidFormChoices.Error(draft); if(druidError!=null)return druidError;
                string clericError = CharacterClericRules.ChoiceError(draft, 0);
                if (clericError != null) return clericError;
                string sorcererError = CharacterSorcererRules.ChoiceError(draft, 0);
                if (sorcererError != null) return sorcererError;
                if (CharacterWizardRules.Active(draft) && draft.level >= CharacterWizardRules.SchoolLevel(draft) && !CharacterWizardRules.HasSchool(draft))
                    return CharacterWizardText.Get("school.error");
                string warlockError = CharacterWarlockRules.ChoiceError(draft, 0);
                if (warlockError != null) return warlockError;
                string artificerError = CharacterArtificerRules.ChoiceError(draft, 0);
                if (artificerError != null) return artificerError;
                string bloodError = CharacterBloodHunterRules.ChoiceError(draft, 0);
                if (bloodError != null) return bloodError;
                if (draft.characterClass == "druid" && draft.edition == RulesEdition.Rules2024 && draft.primalOrder != "magician" && draft.primalOrder != "warden") return "Обери первісний шлях друїда.";
                if (draft.characterClass == "ranger" && draft.edition == RulesEdition.Rules2014 &&
                    (!CharacterCreationRules.RangerEnemies.Contains(draft.rangerEnemy) || !CharacterCreationRules.RangerTerrains.Contains(draft.rangerTerrain) ||
                     draft.rangerEnemy == "Два види гуманоїдів" && draft.rangerHumanoids.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().Count() != 2))
                    return "Обери ворога й місцевість слідопита; для гуманоїдів запиши два різні види через кому.";
                int hitLevel = FirstMissingHitPointLevel(draft);
                if (hitLevel > 0) return string.Format(translate("Обери ОЗ для рівня {0}."), hitLevel);
                if (!CharacterCreationRules.HasSubclassChoice(draft)) return "Обери підклас персонажа.";
                if (CharacterBarbarianRules.Active(draft)) return CharacterBarbarianRules.ChoiceError(draft);
                if (CharacterMonkSubclassRules.ClassicElements(draft)) return CharacterMonkSubclassRules.ChoiceError(draft);
                return null;
            }
            if (page == 1)
            {
                string featError = CharacterOriginFeatRules.ChoiceError(draft, 1);
                if (featError != null) return featError;
                string speciesError = CharacterSpeciesRules.ChoiceError(draft);
                if (speciesError != null) return speciesError;
                return CharacterBackgroundRules.Find(draft) == null ? "Обери передісторію." : null;
            }
            if (page == 2)
            {
                if ((int)draft.abilityMethod < 0) return "Обери спосіб визначення характеристик.";
                if (draft.abilities == null || draft.abilities.Length != 6 || draft.abilities.Any(x => x <= 0)) return "Розподіли всі шість значень характеристик.";
                if (draft.abilityMethod == AbilityMethod.StandardArray && !draft.abilities.OrderBy(x => x).SequenceEqual(new[] { 8, 10, 12, 13, 14, 15 })) return "Признач кожне число стандартного набору один раз.";
                if (draft.abilityMethod == AbilityMethod.Manual && (draft.rolledCount != 6 || draft.rolledAssignment.Any(x => x < 0))) return "Зроби шість кидків і розподіли всі результати.";
                if (draft.edition == RulesEdition.Rules2024)
                {
                    if (draft.bonusMode < 0) return "Обери спосіб розподілу бонусів передісторії.";
                    if (!draft.threeIncreases && (draft.increaseTwo < 0 || draft.increaseOne < 0)) return "Обери характеристики для бонусів +2 та +1.";
                }
                return CharacterLevelAdvancement.ChoiceError(draft);
            }
            if (page == 3)
            {
                string speciesError = CharacterSpeciesRules.ChoiceError(draft, 3);
                if (speciesError != null) return speciesError;
                if (CharacterMonkSubclassRules.Is(draft, "mercy")) { string error = CharacterMonkSubclassRules.ChoiceError(draft); if (error != null) return error; }
                if (draft.classSkills.Count != CharacterCreationRules.ClassSkillCount(draft)) return "Обери навички класу.";
                if (draft.characterClass == "rogue" && (draft.expertise == null || draft.expertise.Count != CharacterRogueRules.ExpertiseCount(draft))) return "Обери всі доступні експертизи плута.";
                if (draft.characterClass == "barbarian" && draft.edition == RulesEdition.Rules2024 && draft.level >= 3 && draft.primalKnowledgeSkill < 0) return "Обери навичку Первісного знання.";
                var languagesChosen = CharacterSpeciesRules.AdditionalLanguages(draft);
                if (languagesChosen.Distinct().Count() != languagesChosen.Length || languagesChosen.Any(CharacterSpeciesRules.AutomaticLanguages(draft).Contains)) return "Обери додаткові мови без повторів уже відомих.";
                if (draft.edition == RulesEdition.Rules2024 && draft.species == "human" && draft.humanSkill < 0) return "Обери додаткову навичку людини.";
                if (CharacterSpeciesRules.NeedsExtraLanguage(draft) && string.IsNullOrEmpty(draft.language)) return "Обери мову.";
                if (draft.edition == RulesEdition.Rules2024 && string.IsNullOrEmpty(draft.secondLanguage)) return "Обери другу мову.";
                if (draft.characterClass == "rogue" && draft.edition == RulesEdition.Rules2024 && string.IsNullOrEmpty(draft.rogueLanguage)) return "Обери додаткову мову плута, відмінну від інших.";
                string backgroundError = CharacterBackgroundRules.ChoiceError(draft);
                if (backgroundError != null) return backgroundError;
                if (draft.edition == RulesEdition.Rules2014 && CharacterSpeciesRules.IsDwarf(draft) && string.IsNullOrEmpty(draft.artisanTool)) return "Обери ремесло дварфа.";
                if (draft.characterClass == "monk" && string.IsNullOrEmpty(draft.monkTool)) return "Обери інструмент монаха.";
                if (draft.characterClass == "bard" && (draft.bardInstruments.Count != 3 || draft.bardInstruments.Any(string.IsNullOrEmpty))) return "Обери три різні музичні інструменти.";
                string clericError = CharacterClericRules.ChoiceError(draft, 3);
                if (clericError != null) return clericError;
                string artificerError = CharacterArtificerRules.ChoiceError(draft, 3);
                if (artificerError != null) return artificerError;
                return null;
            }
            if (page == 4)
            {
                string featError = CharacterOriginFeatRules.ChoiceError(draft, 4);
                if (featError != null) return featError;
                string backgroundError = CharacterBackgroundRules.ChoiceError(draft, 4);
                if (backgroundError != null) return backgroundError;
                if (draft.characterClass == "fighter" && (int)draft.fightingStyle < 0) return "Обери бойовий стиль.";
                if (draft.characterClass == "paladin") { string error = CharacterPaladinRules.ChoiceError(draft); if (error != null) return error; }
                if (draft.characterClass == "fighter") { string error = CharacterFighterRules.ChoiceError(draft); if (error != null) return error; }
                if (draft.equipment < 0) return "Обери класовий набір спорядження.";
                if (draft.characterClass == "monk" && draft.edition == RulesEdition.Rules2014 && draft.monkPack < 0) return "Обери набір монаха.";
                if (draft.characterClass == "monk" && draft.edition == RulesEdition.Rules2014 && draft.equipment == 1 && string.IsNullOrEmpty(draft.monkWeapon)) return "Обери просту зброю монаха.";
                if (draft.edition == RulesEdition.Rules2024 && draft.backgroundEquipmentChoice < 0) return "Обери спорядження передісторії або 50 зм.";
                if (draft.edition == RulesEdition.Rules2024 && CharacterCreationRules.MasteryCount(draft) > 0 && (draft.weaponMasteries == null || draft.weaponMasteries.Count != CharacterCreationRules.MasteryCount(draft) || draft.weaponMasteries.Any(x => !CharacterCreationRules.MasteryOptions(draft).Contains(x)) || draft.weaponMasteries.Distinct().Count() != CharacterCreationRules.MasteryCount(draft))) return "Обери майстерності зброї класу.";
                if (draft.characterClass == "wizard" && (draft.spellbookSpells.Count != CharacterWizardRules.BookCount(draft) || draft.spellbookSpells.Any(string.IsNullOrEmpty) || draft.spellbookSpells.Distinct().Count() != CharacterWizardRules.BookCount(draft))) return CharacterWizardText.Get("book.error");
                if (draft.characterClass == "wizard" && draft.edition == RulesEdition.Rules2014 && (string.IsNullOrEmpty(draft.wizardWeapon) || string.IsNullOrEmpty(draft.wizardFocus) || draft.wizardPack < 0)) return "Обери зброю, фокус і набір чарівника.";
                int requiredSpells = CharacterCreationRules.SpellChoiceCount(draft);
                if (requiredSpells > 0 && (draft.preparedSpells == null || draft.preparedSpells.Count != requiredSpells || draft.preparedSpells.Any(string.IsNullOrEmpty) || draft.preparedSpells.Distinct().Count() != requiredSpells)) return "Обери всі заклинання класу без повторів.";
                int requiredCantrips = CharacterCreationRules.CantripCount(draft);
                if (requiredCantrips > 0 && (draft.cantrips == null || draft.cantrips.Count != requiredCantrips || draft.cantrips.Any(string.IsNullOrEmpty) || draft.cantrips.Distinct().Count() != requiredCantrips)) return "Обери всі заговори класу без повторів.";
                if (draft.characterClass == "bard" && (draft.edition == RulesEdition.Rules2014 || draft.equipment == 0) && string.IsNullOrEmpty(draft.bardCarriedInstrument)) return "Обери музичний інструмент у спорядженні.";
                if (draft.characterClass == "bard" && draft.edition == RulesEdition.Rules2014 && (string.IsNullOrEmpty(draft.bardWeapon) || draft.bardPack < 0)) return "Обери зброю та набір барда.";
                if (draft.characterClass == "druid" && draft.edition == RulesEdition.Rules2014 && (string.IsNullOrEmpty(draft.druidFirstItem) || string.IsNullOrEmpty(draft.druidWeapon))) return "Обери щит або зброю та основну зброю друїда.";
                if (CharacterTricksterSpells.ThirdCaster(draft) && !CharacterTricksterSpells.CanLearn(draft, draft.preparedSpells)) return "Обери законний набір заклинань підкласу.";
                string clericError = CharacterClericRules.ChoiceError(draft, 4);
                if (clericError != null) return clericError;
                if (CharacterBardRules.Active(draft) && !CharacterSpellCatalog.BardCanLearn(draft,draft.preparedSpells.Where(name=>!string.IsNullOrEmpty(name)))) return CharacterBardText.Get("error.repertoire");
                if (CharacterSorcererRules.Active(draft) && !CharacterSorcererRules.CanLearn(draft, draft.preparedSpells.Where(name => !string.IsNullOrEmpty(name)).Select(name => CharacterSpellCatalog.Find(draft.edition, name)?.level ?? 99))) return CharacterSorcererText.Get("error.spells");
                string sorcererError = CharacterSorcererRules.ChoiceError(draft, 4);
                if (sorcererError != null) return sorcererError;
                string wizardError = CharacterWizardRules.Active(draft)
                    ? CharacterWizardRules.ChoiceError(draft) ?? CreationWizardMagic.ChoiceError(draft)
                    : null;
                if (wizardError != null) return wizardError;
                string warlockError = CharacterWarlockRules.ChoiceError(draft, 4);
                if (warlockError != null) return warlockError;
                string artificerError = CharacterArtificerRules.ChoiceError(draft, 4);
                if (artificerError != null) return artificerError;
                string bloodError = CharacterBloodHunterRules.ChoiceError(draft, 4);
                if (bloodError != null) return bloodError;
                if (!isLevelUp && draft.secondaryClass?.IsSelected == true)
                    return draft.secondaryClass.ChoiceError(draft, checkRequirements: true);
            }
            return null;
        }

        internal static bool CanSummarize(CharacterCreationDraft draft) =>
            (int)draft.edition >= 0 && !string.IsNullOrEmpty(draft.characterClass) &&
            !string.IsNullOrEmpty(draft.species) && !string.IsNullOrEmpty(draft.background) &&
            draft.abilities != null && draft.abilities.Length == 6 && draft.abilities.All(score => score > 0) &&
            draft.equipment >= 0 && (draft.characterClass != "fighter" || (int)draft.fightingStyle >= 0) &&
            CharacterCreationRules.HasSubclassChoice(draft) && HasHitPointChoices(draft);

        private static bool HasHitPointChoices(CharacterCreationDraft draft) =>
            draft.level >= 1 && draft.level <= CharacterCreationRules.MaxSupportedLevel(draft) &&
            FirstMissingHitPointLevel(draft) == 0;

        private static int FirstMissingHitPointLevel(CharacterCreationDraft draft)
        {
            for (int level = 2; level <= draft.level; level++)
                if (draft.hitPointChoices == null || draft.hitPointChoices.Length < level - 1 || draft.hitPointChoices[level - 2] == 0)
                    return level;
            return 0;
        }

        internal static void ValidateChoices(CharacterCreationDraft draft, bool creating)
        {
            var errors = CharacterCreationRules.Validate(draft);
            if (draft.secondaryClass?.IsSelected == true)
            {
                string secondaryError = draft.secondaryClass.ChoiceError(draft, creating);
                if (secondaryError != null) errors.Add(secondaryError);
            }

            string classOptionsError = ClassOptionsError(draft);
            if (classOptionsError != null) errors.Add(classOptionsError);
            if (draft.species == "kobold" && draft.expandedGift == "magic" &&
                !CharacterSpellCatalog.Available(draft.edition, "sorcerer", 0).Any(spell => spell.name == draft.expandedCantrip))
                errors.Add(CharacterExpandedSpeciesRules.Text("error.koboldcantrip"));

            if (draft.species == "khoravar")
            {
                string cantrip = string.IsNullOrEmpty(draft.expandedCantrip) ? "Friends" : draft.expandedCantrip;
                if (!new[] { "cleric", "druid", "wizard" }.SelectMany(id => CharacterSpellCatalog.Available(draft.edition, id, 0))
                    .Any(spell => spell.name == cantrip))
                    errors.Add(CharacterExpandedSpeciesRules.Text("error.feygift"));
            }
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
        }

        internal static void ValidateSpellChoices(CharacterCreationDraft draft, CharacterCreationDraft secondary)
        {
            ValidateClassSpells(draft);
            if (secondary == null) return;
            ValidateClassSpells(secondary);
            string error = ClassOptionsError(secondary);
            if (error != null) throw new ArgumentException(error);
        }

        private static string ClassOptionsError(CharacterCreationDraft draft)
        {
            string error = CharacterDruidFormChoices.Error(draft);
            if (error != null) return error;
            if (CharacterArtificerRules.Active(draft) && draft.level >= 11 &&
                !CharacterSpellCatalog.ArtificerStoredSpellChoices(draft).Any(spell => spell.name == draft.artificerStoredSpell))
                return CharacterArtificerText.Get("error.store");
            return null;
        }

        private static void ValidateClassSpells(CharacterCreationDraft draft)
        {
            if (CharacterCreationRules.SpellChoiceCount(draft) > 0 &&
                draft.preparedSpells.Any(name => draft.characterClass == "ranger" && draft.edition == RulesEdition.Rules2024 && name == "Hunter's Mark" ||
                    draft.characterClass == "druid" && draft.edition == RulesEdition.Rules2024 && name == "Speak with Animals" ||
                    CharacterClericRules.DomainSpells(draft).Contains(name) ||
                    !CharacterSpellCatalog.AvailableFor(draft, 1).Any(spell => spell.name == name)))
                throw new ArgumentException("Обери заклинання зі списку класу.");
            if (CharacterBardRules.Active(draft)) {
                if(!CharacterSpellCatalog.BardCanLearn(draft,draft.preparedSpells))throw new ArgumentException(CharacterBardText.Get("error.repertoire"));
                for(int i=0;i<draft.bardSecrets.Count;i++)if(!CharacterSpellCatalog.BardSecrets(draft,i).Any(s=>s.name==draft.bardSecrets[i]))throw new ArgumentException(CharacterBardText.Get("error.secrets"));
                if(draft.bardSecrets.Distinct().Count()!=draft.bardSecrets.Count)throw new ArgumentException(CharacterBardText.Get("error.secrets"));
            }
            if (CharacterSorcererRules.Active(draft) && !CharacterSorcererRules.CanLearn(draft, draft.preparedSpells.Select(name => CharacterSpellCatalog.Find(draft.edition, name)?.level ?? 99))) throw new ArgumentException(CharacterSorcererText.Get("error.spells"));
            if(CharacterBloodHunterRules.Profane(draft) && !CharacterBloodHunterRules.CanLearn(draft,draft.preparedSpells.Select(name=>CharacterSpellCatalog.Find(draft.edition,name)?.level??99)))throw new ArgumentException(CharacterBloodHunterRules.Text("error.spells"));
            if (draft.characterClass == "artificer" && draft.edition == RulesEdition.Rules2024 && draft.cantrips.Contains("Mending")) throw new ArgumentException("Лагодження вже надане класом; обери інший заговір.");
            if (CharacterCreationRules.CantripCount(draft) > 0 && draft.cantrips.Any(name => !CharacterSpellCatalog.AvailableFor(draft, 0).Any(spell => spell.name == name)))
                throw new ArgumentException("Обери заговори зі списку класу.");
            if (CharacterClericRules.Classic(draft) && draft.subclass == "nature" && !CharacterSpellCatalog.Available(draft.edition, "druid", 0).Any(spell => spell.name == draft.natureCantrip))
                throw new ArgumentException("Обери додатковий заговір друїда без повторів.");
            string wizardSpellError = CreationWizardMagic.ChoiceError(draft);
            if (wizardSpellError != null) throw new ArgumentException(wizardSpellError);
            if (draft.characterClass == "wizard" && draft.spellbookSpells.Where((name, index) =>
                !CharacterSpellCatalog.AvailableFor(draft, 1).Any(spell => spell.name == name && spell.level <= CharacterWizardRules.BookCircle(index))).Any())
                throw new ArgumentException("Обери заклинання зі списку класу.");
            if(CharacterWarlockRules.Active(draft)&&!CharacterWarlockRules.CanLearn(draft,draft.preparedSpells.Select(name=>CharacterSpellCatalog.Find(draft.edition,name)?.level??99)))throw new ArgumentException(CharacterWarlockRules.Text("error.learning"));
            if(CharacterWarlockRules.Active(draft)&&draft.warlockArcana.Where((name,index)=>!CharacterSpellCatalog.Available(draft.edition,"warlock",index+6).Any(spell=>spell.name==name)).Any())throw new ArgumentException(CharacterWarlockRules.Text("error.arcana"));
            if (CharacterWarlockRules.Tome(draft) && (draft.tomeCantrips.Any(name => !CharacterSpellCatalog.AnyClass(draft.edition, 0).Any(spell => spell.name == name)) ||
                draft.tomeRituals.Any(name => !CharacterSpellCatalog.AnyClass(draft.edition, 1).Any(spell => spell.name == name && spell.ritual))))
                throw new ArgumentException("Обери три додаткові заговори й два ритуали 1 рівня без повторів.");
            if (CharacterElfRules.Elf(draft) && draft.speciesLineage == "high" && !CharacterSpellCatalog.Available(draft.edition, "wizard", 0).Any(spell => spell.name == draft.speciesCantrip)) throw new ArgumentException("Обери заговір високого ельфа.");
            if (draft.cantrips.Any(name => CharacterSpeciesRules.Spells(draft).Contains(name)) || draft.tomeCantrips.Any(name => CharacterSpeciesRules.Spells(draft).Contains(name))) throw new ArgumentException("Расовий заговір уже наданий; обери інший класовий заговір.");
            foreach (var feat in CharacterOriginFeatRules.Choices(draft).Where(CharacterOriginFeatRules.Magic))
                if (feat.cantrips.Any(name => !CharacterSpellCatalog.Available(draft.edition, CharacterOriginFeatRules.SpellClass(feat), 0).Any(spell => spell.name == name)) || !CharacterSpellCatalog.Available(draft.edition, CharacterOriginFeatRules.SpellClass(feat), 1).Any(spell => spell.name == feat.spell))
                    throw new ArgumentException("Обери спели риси зі списку її класу.");
            if (CharacterTricksterSpells.ThirdCaster(draft) && !CharacterTricksterSpells.CanLearn(draft, draft.preparedSpells)) throw new ArgumentException("Обери законний набір заклинань підкласу.");
        }
    }
}
