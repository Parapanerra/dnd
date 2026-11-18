using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    internal static class CharacterLevelUp
    {
        internal const string DraftKey = "CreationWizard_v1";
        internal const string ClassHistoryKey = "CreationClassHistory_v1";
        internal const string SpellOrderKey = "CreationSpellOrder_v1";

        internal static CharacterCreationDraft ReadDraft(CharacterData character)
        {
            string json = character?.GetSharedString(DraftKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<CharacterCreationDraft>(json); }
            catch (ArgumentException) { return null; }
        }

        internal static bool CanStart(CharacterData character)
        {
            var draft = ReadDraft(character);
            if (draft == null) return false;
            var history = ReadClassHistory(character, draft);
            return history != null && history.MatchesDraft(draft) &&
                history.CanTakeLevel(draft.edition, draft.characterClass, null);
        }

        internal static CharacterClassHistory ReadClassHistory(CharacterData character, CharacterCreationDraft draft)
        {
            string json = character.GetSharedString(ClassHistoryKey);
            if (string.IsNullOrEmpty(json)) return CharacterClassHistory.FromCreation(draft);
            try { return JsonUtility.FromJson<CharacterClassHistory>(json); }
            catch (ArgumentException) { return null; }
        }

        internal static string[] SpellSlotChanges(CharacterCreationDraft previous, CharacterCreationDraft next, Func<string, string> translate)
        {
            var (before, oldPact) = CharacterSpellSlotProgression.ForDraft(previous);
            var (after, newPact) = CharacterSpellSlotProgression.ForDraft(next);
            var ordinaryChanges = Enumerable.Range(0, before.Length).Select(index => new { circle = index + 1, oldCount = before[index], count = after[index] })
                .Where(row => row.oldCount != row.count)
                .Select(row => string.Format(translate("Комірки {0}-го кола: {1} → {2}"), row.circle, row.oldCount, row.count));
            var pactChanges = newPact.Except(oldPact)
                .Select(pool => string.Format(translate("Комірки договору — коло {0}"), pool.circle) + " · " + pool.count);
            return ordinaryChanges.Concat(pactChanges).ToArray();
        }

        internal static void ApplyClassLevel(CharacterData character, string originalJson,
            CharacterCreationDraft draft, CreationSheetBindings bindings, Func<string, string> translate)
        {
            var previous = ReadDraft(character);
            var history = previous == null ? null : ReadClassHistory(character, previous);
            if (history == null || !history.MatchesDraft(previous) || character.GetSharedString(DraftKey) != originalJson ||
                draft.TotalLevel != previous.TotalLevel + 1 || draft.characterClass != previous.characterClass ||
                draft.edition != previous.edition || draft.species != previous.species || draft.background != previous.background ||
                CharacterCreationRules.HasSubclassChoice(previous) && previous.subclass.Length > 0 && draft.subclass != previous.subclass)
                throw new InvalidOperationException("Дані персонажа змінилися. Відкрий підвищення рівня знову.");

            bool primaryGrows = draft.level == previous.level + 1;
            var secondary = draft.secondaryClass;
            var previousSecondary = previous.secondaryClass;
            string gainingClass = primaryGrows ? draft.characterClass : secondary?.classId;
            int hpChoice = primaryGrows ? draft.hitPointChoices?.ElementAtOrDefault(draft.level - 2) ?? 0
                : secondary?.hitPointChoices?.ElementAtOrDefault(secondary.level - 1) ?? 0;
            bool hadSecondClass = previousSecondary?.IsSelected == true;
            bool hasSecondClass = secondary?.IsSelected == true;
            bool sameSecondClass = !hadSecondClass && !hasSecondClass || hadSecondClass && hasSecondClass &&
                JsonUtility.ToJson(secondary) == JsonUtility.ToJson(previousSecondary);
            if (primaryGrows && !sameSecondClass ||
                !primaryGrows && (draft.level != previous.level || secondary?.IsSelected != true ||
                    hadSecondClass && !secondary.PreservesChoices(previousSecondary, draft.edition)) ||
                !history.TryAppend(draft.edition, gainingClass, CharacterCreationRules.Scores(previous, includeEquipment: false), hpChoice, out var nextHistory) ||
                !nextHistory.MatchesDraft(draft))
                throw new InvalidOperationException("Дані персонажа змінилися. Відкрий підвищення рівня знову.");

            var builder = new CharacterCreationService(bindings, translate);
            string[] spellOrder = character.GetSharedString(SpellOrderKey).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var before = builder.Build(previous, spellOrder, history);
            var after = builder.Build(draft, before.GetSharedString(SpellOrderKey).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), nextHistory);
            GeneratedCharacterMerge.Apply(character, before, after, bindings, translate);
            character.SetSharedString(DraftKey, after.GetSharedString(DraftKey));
            character.SetSharedString(ClassHistoryKey, JsonUtility.ToJson(nextHistory));
            character.SetSharedString(SpellOrderKey, after.GetSharedString(SpellOrderKey));
        }
    }
}
