using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationSpellPreparation
    {
        internal static IEnumerable<string> ClassSpells(CharacterCreationDraft d) =>
            (CharacterWizardRules.Active(d) ? CharacterWizardRules.Book(d) : d.preparedSpells)
            .Concat(d.cantrips).Concat(Automatic(d))
            .Concat(CharacterDruidRules.FeatureSpells(d)).Concat(CharacterArtificerRules.FeatureSpells(d))
            .Concat(CharacterBloodHunterRules.AutomaticSpells(d))
            .Concat(CharacterWarlockInvocations.FeatureSpells(d)).Concat(CharacterBarbarianRules.Spells(d))
            .Concat(CharacterMonkSubclassRules.Spells(d))
            .Concat(CharacterWarlockRules.Tome(d) ? d.tomeCantrips.Concat(d.tomeRituals) : Array.Empty<string>())
            .Concat(CharacterTricksterSpells.Active(d) ? new[] { "Mage Hand" } : Array.Empty<string>())
            .Concat(CharacterFighterRules.Is(d, "psiwarrior") && d.level >= 18 ? new[] { "Telekinesis" } : Array.Empty<string>())
            .Concat(CharacterWarlockRules.Active(d) ? d.warlockArcana : Array.Empty<string>().AsEnumerable())
            .Concat(CharacterBardRules.Active(d) ? d.bardSecrets : Array.Empty<string>().AsEnumerable())
            .Concat(CharacterArtificerRules.Active(d) && CharacterArtificerRules.Revised(d) ? new[] { "Mending" } : Array.Empty<string>())
            .Concat(CharacterClericRules.Classic(d) && d.subclass == "light" ? new[] { "Light" } : Array.Empty<string>())
            .Concat(CharacterClericRules.Classic(d) && d.subclass == "nature" ? new[] { d.natureCantrip } : Array.Empty<string>())
            .Where(name => !string.IsNullOrEmpty(name)).Distinct();
        internal static IEnumerable<string> Automatic(CharacterCreationDraft d)=>CharacterClericRules.DomainSpells(d)
            .Concat(CharacterDruidRules.AutomaticSpells(d)).Concat(CharacterPaladinRules.AutomaticSpells(d))
            .Concat(CharacterRangerRules.AutomaticSpells(d)).Concat(CharacterSorcererRules.AutomaticSpells(d))
            .Concat(CharacterBardRules.AutomaticSpells(d)).Concat(CharacterArtificerRules.AutomaticSpells(d))
            .Concat(CharacterWarlockRules.AutomaticSpells(d)).Concat(CharacterWizardRules.Automatic(d));
        private static bool ClassPrepared(CharacterCreationDraft d,string spell)=>d.preparedSpells.Contains(spell)||Automatic(d).Contains(spell)
            ||CharacterBardRules.Active(d)&&d.bardSecrets.Contains(spell)
            ||CharacterWarlockRules.Modern(d)&&CharacterWarlockRules.Tome(d)&&d.tomeRituals.Contains(spell)
            ||d.edition==RulesEdition.Rules2024&&CharacterFighterRules.Is(d,"psiwarrior")&&d.level>=18&&spell=="Telekinesis";
        internal static bool RacialPrepared(CharacterCreationDraft d,string spell)=>d.edition==RulesEdition.Rules2024&&(!CharacterExpandedSpeciesRules.Active(d)||d.species=="rimekin")&&CharacterSpeciesRules.Spells(d).Contains(spell);
        internal static bool IsPrepared(CharacterCreationDraft d,string spell,int level)=>level==0||ClassPrepared(d,spell)
            ||RacialPrepared(d,spell)
            ||CharacterOriginFeatRules.Choices(d).Any(feat=>CharacterOriginFeatRules.Spells(feat).Contains(spell));
        internal static bool UsesClassAbility(CharacterCreationDraft d,string spell)=>ClassPrepared(d,spell)||d.cantrips.Contains(spell)
            ||CharacterWizardRules.Active(d)&&CharacterWizardRules.Book(d).Contains(spell)||CharacterWarlockRules.Tome(d)&&d.tomeCantrips.Contains(spell)
            ||CharacterDruidRules.FeatureSpells(d).Contains(spell)||CharacterArtificerRules.FeatureSpells(d).Contains(spell)
            ||CharacterBloodHunterRules.AutomaticSpells(d).Contains(spell)
            ||CharacterWarlockRules.Active(d)&&d.warlockArcana.Contains(spell)
            ||CharacterMonkSubclassRules.Spells(d).Contains(spell)||CharacterBarbarianRules.Spells(d).Contains(spell)
            ||CharacterFighterRules.Is(d,"psiwarrior")&&d.level>=18&&spell=="Telekinesis"
            ||CharacterTricksterSpells.Active(d)&&spell=="Mage Hand"
            ||CharacterWarlockInvocations.FeatureSpells(d).Contains(spell);
        internal static int ClassAbility(CharacterCreationDraft d)=>d.characterClass=="bloodhunter"?d.bloodAbility:
            new[]{"wizard","artificer","fighter","rogue"}.Contains(d.characterClass)?3:
            new[]{"cleric","druid","ranger","monk","barbarian"}.Contains(d.characterClass)?4:5;
    }
}
