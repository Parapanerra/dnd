using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterSpellSlotProgression
    {
        internal static (int[] ordinary, (string classId, int circle, int count)[] pact) ForClasses(params CharacterCreationDraft[] classes)
        {
            if (classes.Length == 0 || classes.Any(track => track == null || track.level < 1 ||
                    track.level > CharacterCreationRules.MaxSupportedLevel(track)) ||
                classes.Select(track => track.edition).Distinct().Count() != 1 ||
                classes.Select(track => track.characterClass).Distinct().Count() != classes.Length ||
                classes.Sum(track => track.level) > CharacterClassHistory.MaximumCharacterLevel)
                throw new ArgumentException("Expected distinct class tracks in one edition with a total level from 1 to 20.", nameof(classes));

            var spellcasters = classes.Where(track => ClassSlots(track, 1) > 0).ToArray();
            int casterLevel = spellcasters.Sum(CasterLevel);
            var ordinary = Enumerable.Range(1, 9).Select(circle => spellcasters.Length == 1
                ? ClassSlots(spellcasters[0], circle) : CharacterFullCasterSlots.Count(casterLevel, circle)).ToArray();

            var warlock = classes.FirstOrDefault(CharacterWarlockRules.Active);
            var profaneSoul = classes.FirstOrDefault(CharacterBloodHunterRules.Profane);
            if (warlock != null && profaneSoul != null)
            {
                // Profane Soul contributes one third of its levels to the Warlock Pact Magic table.
                var pactTrack = new CharacterCreationDraft { characterClass = warlock.characterClass,
                    edition = warlock.edition, level = warlock.level + profaneSoul.level / 3 };
                return (ordinary, new[] { (pactTrack.characterClass, PactCircle(pactTrack), PactCount(pactTrack)) });
            }

            var pact = classes.Select(track =>
                (classId: track.characterClass, circle: PactCircle(track), count: PactCount(track)))
                .Where(pool => pool.count > 0).ToArray();
            return (ordinary, pact);
        }

        // The existing single-class sheet has one set of rows. Keep its saved row indexes for Pact Magic.
        internal static int[] ForSingleClassSheet(CharacterCreationDraft draft)
        {
            var (ordinary, pact) = ForClasses(draft);
            foreach (var pool in pact) ordinary[pool.circle - 1] = pool.count;
            return ordinary;
        }

        public static (int[] slots, (string classId, int circle, int count)[] separatePact) ForSheet(CharacterCreationDraft draft)
        {
            var (ordinary, pact) = ForDraft(draft);
            if (ordinary.Any(count => count > 0)) return (ordinary, pact);
            foreach (var pool in pact) ordinary[pool.circle - 1] = pool.count;
            return (ordinary, Array.Empty<(string, int, int)>());
        }

        public static (int[] ordinary, (string classId, int circle, int count)[] pact) ForDraft(CharacterCreationDraft draft) =>
            draft.secondaryClass?.IsSelected == true
                ? ForClasses(draft, draft.secondaryClass.ClassDraft(draft.edition))
                : ForClasses(draft);

        private static int ClassSlots(CharacterCreationDraft track, int circle) => track.characterClass switch
        {
            "bard" or "cleric" or "druid" or "sorcerer" or "wizard" => CharacterFullCasterSlots.Count(track.level, circle),
            "artificer" => CharacterArtificerRules.Slots(track, circle),
            "paladin" => CharacterPaladinRules.Slots(track, circle),
            "ranger" => CharacterRangerRules.Slots(track, circle),
            "fighter" => CharacterFighterRules.Slots(track, circle),
            "rogue" => CharacterRogueRules.SlotCount(track, circle),
            _ => 0
        };

        private static int CasterLevel(CharacterCreationDraft track) => track.characterClass switch
        {
            "artificer" => (track.level + 1) / 2,
            "paladin" or "ranger" => (track.level + (track.edition == RulesEdition.Rules2024 ? 1 : 0)) / 2,
            "fighter" or "rogue" => track.level / 3,
            _ => track.level
        };

        private static int PactCircle(CharacterCreationDraft track) => CharacterWarlockRules.Active(track)
            ? CharacterWarlockRules.SlotCircle(track) : CharacterBloodHunterRules.PactCircle(track);

        private static int PactCount(CharacterCreationDraft track) => CharacterWarlockRules.Active(track)
            ? CharacterWarlockRules.SlotCount(track) : CharacterBloodHunterRules.PactSlots(track);
    }
}
