using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    internal sealed class ReadyFormTemplate
    {
        internal readonly string id, name;
        private readonly Action<CharacterData, CreationSheetBindings, int, Func<string, string>> write;
        internal ReadyFormTemplate(string id, string name, Action<CharacterData, CreationSheetBindings, int, Func<string, string>> write)
        { this.id = id; this.name = name; this.write = write; }
        internal void Write(CharacterData character, CreationSheetBindings bindings, int slot, Func<string, string> translate)
            => write(character, bindings, slot, translate);
    }

    internal static class ReadyFormCatalog
    {
        private const string Prefix = "WildShapeTemplateId_";
        internal static string Key(int slot) => Prefix + slot;
        internal static ReadyFormTemplate[] All(CharacterData character)
        {
            string json = character?.GetSharedString("CreationWizard_v1", "");
            if (string.IsNullOrEmpty(json)) return Array.Empty<ReadyFormTemplate>();
            CharacterCreationDraft draft;
            try { draft = JsonUtility.FromJson<CharacterCreationDraft>(json); }
            catch (ArgumentException) { return Array.Empty<ReadyFormTemplate>(); }
            if (draft == null || draft.level < 1 || draft.abilities == null || draft.abilities.Length != 6)
                return Array.Empty<ReadyFormTemplate>();
            var stats = CharacterCreationRules.Calculate(draft);
            var templates = Templates(draft, stats);
            if (draft.secondaryClass?.IsSelected == true)
                templates = templates.Concat(Templates(draft.secondaryClass.ClassDraft(draft), stats));
            return templates.GroupBy(template => template.id, StringComparer.Ordinal).Select(group => group.First()).ToArray();
        }

        private static IEnumerable<ReadyFormTemplate> Templates(CharacterCreationDraft draft, CreatedCharacterStats stats) =>
            CreationSummonedSpirits.Templates(draft, stats).Concat(CreationPaladinCompanions.Templates(draft, stats))
                .Concat(CreationRangerCompanions.Templates(draft, stats)).Concat(CreationSorcererCompanions.Templates(draft, stats))
                .Concat(CreationBardCompanions.Templates(draft, stats)).Concat(CreationArtificerCompanions.Templates(draft, stats))
                .Concat(CreationClericCompanions.Templates(draft, stats)).Concat(CreationDruidForms.Templates(draft, stats));
        internal static ReadyFormTemplate[] Available(CharacterData character, ReadyFormTemplate[] templates)
        {
            var used = new HashSet<string>(character.sharedStringData.Where(entry => entry.key.StartsWith(Prefix, StringComparison.Ordinal)).Select(entry => entry.value));
            return templates.Where(template => !used.Contains(template.id)).ToArray();
        }
        internal static bool Add(CharacterData character, CreationSheetBindings bindings, ReadyFormTemplate template, int slot, Func<string, string> translate)
        {
            if (template == null || character.GetSceneData(WildShapeFormList.SceneNameForSlot(slot), false) != null ||
                character.sharedStringData.Any(entry => entry.key.StartsWith(Prefix, StringComparison.Ordinal) && entry.value == template.id)) return false;
            if (bindings == null || string.IsNullOrEmpty(bindings.FormHealthPrefix) || !bindings.Entries.Any(entry => entry.key == "form.input.1"))
                throw new InvalidOperationException("Companion form bindings are missing.");
            // Stage the new form before attaching it, so a failed fill cannot damage an existing slot.
            var candidate = new CharacterData("ready-form");
            candidate.SetSharedString("CreationWizard_v1", character.GetSharedString("CreationWizard_v1", ""));
            template.Write(candidate, bindings, slot, translate);
            var state = candidate.GetSceneData(WildShapeFormList.SceneNameForSlot(slot), false);
            if (state == null) throw new InvalidOperationException("Ready form template produced no data.");
            character.sceneStates.Add(state);
            character.SetSharedString("WildShapeFormName_" + slot, translate(template.name));
            character.SetSharedString(Key(slot), template.id);
            return true;
        }
        internal static void Forget(CharacterData character, int slot) => character.DeleteSharedString(Key(slot));

        internal static void ApplyLevelUp(CharacterData current, CharacterData before, CharacterData after, CreationSheetBindings bindings)
        {
            var forms = current.sceneStates.Where(state => WildShapeFormList.SlotFromSceneName(state.sceneName) >= 0).ToArray();
            if (forms.Length == 0 || string.IsNullOrEmpty(bindings.FormHealthPrefix)) return;
            var previous = All(before);
            if (previous.Length == 0) return;
            IdentifyLegacy(current, bindings, previous);
            var upgraded = All(after);
            var fields = bindings.Entries.Where(field => field.key.StartsWith("form.input.", StringComparison.Ordinal) &&
                field.key != "form.input.1").ToArray();
            var catalog = new TranslationCatalog();

            foreach (var form in forms)
            {
                int slot = WildShapeFormList.SlotFromSceneName(form.sceneName);
                string id = current.GetSharedString(Key(slot), "");
                var oldTemplate = previous.FirstOrDefault(template => template.id == id);
                var newTemplate = upgraded.FirstOrDefault(template => template.id == id);
                if (oldTemplate == null || newTemplate == null) continue;

                CharacterSceneData Render(ReadyFormTemplate template, CharacterData source, AppLanguage language)
                {
                    var probe = new CharacterData("level-up-form");
                    probe.SetSharedString(CharacterLevelUp.DraftKey, source.GetSharedString(CharacterLevelUp.DraftKey));
                    template.Write(probe, bindings, slot, text => catalog.Translate(text, language));
                    return probe.GetSceneData(form.sceneName, false);
                }

                var languages = (AppLanguage[])Enum.GetValues(typeof(AppLanguage));
                var oldForms = languages.Select(language => Render(oldTemplate, before, language)).ToArray();
                var newForms = languages.Select(language => Render(newTemplate, after, language)).ToArray();
                var feature = fields.First(field => field.key == "form.input.2");
                int preferredLanguage = Array.FindIndex(oldForms, state => Read(state, feature) == Read(form, feature));
                if (preferredLanguage < 0) preferredLanguage = Array.IndexOf(languages,
                    new RuntimeManagerResolver().FindLocalization()?.CurrentLanguage ?? AppLanguage.Ukrainian);

                foreach (var field in fields)
                {
                    string live = Read(form, field);
                    int match = Read(oldForms[preferredLanguage], field) == live ? preferredLanguage :
                        Array.FindIndex(oldForms, state => Read(state, field) == live);
                    if (match < 0) continue;
                    string next = Read(newForms[match], field);
                    if (next != live) bindings.Text(current, field.key, next, form.sceneName);
                }

                string maxKey = bindings.FormHealthPrefix + "maxHealth";
                string hpKey = bindings.FormHealthPrefix + "currentHealth";
                int oldMax = oldForms[0].GetInt(maxKey);
                int newMax = newForms[0].GetInt(maxKey);
                if (oldMax == newMax || form.GetInt(maxKey, oldMax) != oldMax) continue;
                int hp = form.GetInt(hpKey, oldMax);
                form.SetInt(maxKey, newMax);
                form.SetInt(hpKey, hp <= 0 ? 0 : Mathf.Clamp(hp + newMax - oldMax, 0, newMax));
            }
        }

        private static string Read(CharacterSceneData state, CreationSheetBindings.Entry field)
            => state.GetString("StableField_v1_" + field.stableId,
                field.index >= 0 && field.index < state.inputData.Count ? state.inputData[field.index] : "");

        // Older generators wrote every variant without IDs. Recognize unchanged stat blocks,
        // independently of names, but never delete or replace their data.
        internal static bool IdentifyLegacy(CharacterData character, CreationSheetBindings bindings, ReadyFormTemplate[] templates)
        {
            if (templates.Length == 0 || bindings == null) return false;
            var featureField = bindings.Entries.First(entry => entry.key == "form.input.2");
            var legacy = character.sceneStates.Where(state => {
                int slot = WildShapeFormList.SlotFromSceneName(state.sceneName);
                return slot >= 0 && !character.HasSharedString(Key(slot)) &&
                    !string.IsNullOrWhiteSpace(state.GetString("StableField_v1_" + featureField.stableId,
                        featureField.index >= 0 && featureField.index < state.inputData.Count ? state.inputData[featureField.index] : ""));
            }).ToArray();
            if (legacy.Length == 0) return false;
            bool changed = false;
            var catalog = new TranslationCatalog();
            foreach (var template in templates)
            {
                if (character.sharedStringData.Any(entry => entry.key.StartsWith(Prefix, StringComparison.Ordinal) && entry.value == template.id)) continue;
                foreach (AppLanguage language in Enum.GetValues(typeof(AppLanguage)))
                {
                    var probe = new CharacterData("legacy-form");
                    probe.SetSharedString("CreationWizard_v1", character.GetSharedString("CreationWizard_v1", ""));
                    template.Write(probe, bindings, 0, text => catalog.Translate(text, language));
                    var expected = probe.GetSceneData(WildShapeFormList.SceneNameForSlot(0), false);
                    var match = legacy.FirstOrDefault(state => {
                        int slot = WildShapeFormList.SlotFromSceneName(state.sceneName);
                        if (slot < 0 || character.HasSharedString(Key(slot))) return false;
                        return new[] { 2, 8, 10, 12, 14, 16, 18, 20, 21, 23 }.All(index => {
                            var field = bindings.Entries.First(entry => entry.key == "form.input." + index);
                            string key = "StableField_v1_" + field.stableId;
                            return state.GetString(key, field.index >= 0 && field.index < state.inputData.Count ? state.inputData[field.index] : "") == expected.GetString(key);
                        });
                    });
                    if (match == null) continue;
                    character.SetSharedString(Key(WildShapeFormList.SlotFromSceneName(match.sceneName)), template.id);
                    changed = true;
                    break;
                }
            }
            return changed;
        }
    }
}
