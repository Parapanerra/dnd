using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class GeneratedCharacterMerge
    {
        private const string StablePrefix = "StableField_v1_";
        private static readonly int[] FeatureFields = { 76, 78, 79, 80, 81, 102, 103, 104, 105 };

        internal static void Apply(CharacterData current, CharacterData before, CharacterData after,
            CreationSheetBindings bindings, Func<string, string> translate)
        {
            const string stablePrefix = "StableField_v1_";
            VerifyPositionalFields(current, before, after, bindings);
            CarryMagicResourceUses(current, before, after, bindings);
            CarryPactSlots(current, before, after, bindings);
            ReadyFormCatalog.ApplyLevelUp(current, before, after, bindings);
            MergeFeatureText(current, before, after, bindings, translate);
            var magicNoteKeys = MergeMagicNotes(current, before, after, bindings);
            var featureKeys = bindings.Entries.Where(field => field.scene == AppConfig.Scenes.CharacterSheet &&
                field.collection == "inputData" && FeatureFields.Contains(field.index))
                .Select(field => stablePrefix + field.stableId).ToHashSet();
            foreach (var change in after.sceneStates)
            {
                var baseline = before.GetSceneData(change.sceneName, false);
                var actual = current.GetSceneData(change.sceneName);
                foreach (var entry in change.stringData)
                {
                    if (change.sceneName == AppConfig.Scenes.CharacterSheet && featureKeys.Contains(entry.key)) continue;
                    if (change.sceneName == "Spels" && magicNoteKeys.Contains(entry.key)) continue;
                    string previous = baseline?.GetString(entry.key) ?? "";
                    if (entry.value == previous || actual.HasString(entry.key) && actual.GetString(entry.key) != previous) continue;
                    actual.SetString(entry.key, entry.value);
                }
                foreach (var entry in change.intData)
                {
                    if (entry.key.StartsWith(bindings.HealthPrefix, StringComparison.Ordinal)) continue;
                    if (change.sceneName == "Spels" && magicNoteKeys.Contains(entry.key)) continue;
                    bool existed = baseline != null && baseline.HasInt(entry.key);
                    int previous = baseline?.GetInt(entry.key) ?? 0;
                    if (existed && entry.value == previous || actual.HasInt(entry.key) && actual.GetInt(entry.key) != previous) continue;
                    actual.SetInt(entry.key, entry.value);
                }
                actual.stableFieldVersion = Math.Max(actual.stableFieldVersion, change.stableFieldVersion);
            }

            var oldHealth = before.GetSceneData(AppConfig.Scenes.CharacterSheet, false);
            var newHealth = after.GetSceneData(AppConfig.Scenes.CharacterSheet, false);
            var health = current.GetSceneData(AppConfig.Scenes.CharacterSheet);
            string maximum = bindings.HealthPrefix + "maxHealth";
            string remaining = bindings.HealthPrefix + "currentHealth";
            int oldMaximum = oldHealth.GetInt(maximum);
            int gained = newHealth.GetInt(maximum) - oldMaximum;
            int actualMaximum = health.GetInt(maximum, oldMaximum);
            int actualRemaining = health.GetInt(remaining, actualMaximum);
            int newMaximum = Math.Max(1, actualMaximum + gained);
            int newRemaining = actualRemaining == 0 ? 0 : Math.Clamp(actualRemaining + gained, 0, newMaximum);
            health.SetInt(maximum, newMaximum);
            health.SetInt(remaining, newRemaining);
            current.maxHealth = newMaximum;
            current.currentHealth = newRemaining;

            foreach (var entry in after.sharedStringData)
            {
                if (entry.key == CharacterLevelUp.DraftKey || entry.key == CharacterLevelUp.ClassHistoryKey || entry.key.StartsWith("CreationGeneratedText_", StringComparison.Ordinal) ||
                    entry.key.StartsWith("CreationMagicNote_v1_", StringComparison.Ordinal) ||
                    entry.key.StartsWith("CreationInfusionNote_v1_", StringComparison.Ordinal)) continue;
                string previous = before.GetSharedString(entry.key, null);
                if (previous == entry.value) continue;
                if (!current.HasSharedString(entry.key) || current.GetSharedString(entry.key) == previous)
                    current.SetSharedString(entry.key, entry.value);
            }
            foreach (var entry in after.sharedStringData.Where(entry => entry.key.StartsWith("CreationGeneratedText_v1_", StringComparison.Ordinal)))
            {
                string index = entry.key.Substring("CreationGeneratedText_v1_".Length);
                var field = bindings.Entries.FirstOrDefault(field => field.key == "input." + index);
                if (field == null) continue;
                string key = stablePrefix + field.stableId;
                if (current.GetSceneData(field.scene, false)?.GetString(key) == after.GetSceneData(field.scene, false)?.GetString(key))
                    current.SetSharedString(entry.key, entry.value);
            }

            // Keep legacy numeric mirrors in sync for controls still read through the old map.
            foreach (var field in bindings.Entries.Where(field => field.index >= 0))
            {
                var state = current.GetSceneData(field.scene, false);
                if (state == null) continue;
                string key = stablePrefix + field.stableId;
                int i = field.index;
                if (field.collection == "inputData" && state.HasString(key) && i < state.inputData.Count)
                    state.inputData[i] = state.GetString(key);
                else if (field.collection == "toggleData" && state.HasInt(key) && i < state.toggleData.Count)
                    state.toggleData[i] = state.GetInt(key) != 0;
                else if (field.collection == "dropdownData" && state.HasInt(key) && i < state.dropdownData.Count)
                    state.dropdownData[i] = state.GetInt(key);
            }
        }

        private static System.Collections.Generic.HashSet<string> MergeMagicNotes(CharacterData current, CharacterData before,
            CharacterData after, CreationSheetBindings bindings)
        {
            const string scene = "Spels";
            var handled = new System.Collections.Generic.HashSet<string>();
            var draft = CharacterLevelUp.ReadDraft(after);
            var generated = after.GetSceneData(scene, false);
            if (draft == null || generated == null) return handled;
            var previous = before.GetSceneData(scene, false);
            var live = current.GetSceneData(scene);
            var fields = Enumerable.Range(0, 4)
                .Select(i => bindings.Entries.First(field => field.key == "magic.note." + i)).ToArray();
            string Read(CharacterSceneData state, CreationSheetBindings.Entry field) => state == null ? "" :
                state.GetString(StablePrefix + field.stableId,
                    field.index >= 0 && field.index < state.inputData.Count ? state.inputData[field.index] : "");

            var oldNotes = fields.Select(field => Read(previous, field)).ToArray();
            var newNotes = fields.Select(field => Read(generated, field)).ToArray();
            if (oldNotes.SequenceEqual(newNotes)) return handled;
            var existing = fields.Select(field => Read(live, field)).ToArray();
            var manual = existing.Select((value, i) => !string.IsNullOrWhiteSpace(value) && value != oldNotes[i] &&
                current.GetSharedString(CreationMagicResources.NoteStamp(draft, i)) != CreationDescriptionLayout.Fingerprint(value)).ToArray();
            var merged = newNotes.ToArray();
            if (manual.Any(value => value))
            {
                for (int i = 0; i < merged.Length; i++)
                    if (manual[i]) merged[i] = existing[i];
                string[] Blocks(string value) => value.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                var included = merged.SelectMany(Blocks).ToHashSet(StringComparer.Ordinal);
                foreach (string block in newNotes.Where((_, i) => manual[i]).SelectMany(Blocks))
                {
                    if (!included.Add(block)) continue;
                    var available = Enumerable.Range(0, merged.Length).OrderBy(i => manual[i]);
                    int row = available.Where(i => merged[i].Length + (merged[i].Length > 0 ? 2 : 0) + block.Length <=
                        CreationDescriptionLayout.FieldCapacity).DefaultIfEmpty(-1).First();
                    if (row < 0) throw new InvalidOperationException("У нотатках магії немає місця для змін нового рівня.");
                    merged[row] = merged[row].Length == 0 ? block : merged[row] + "\n\n" + block;
                }
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string key = fields[i].key;
                var open = bindings.Entries.First(field => field.key == key + ".open");
                handled.Add(StablePrefix + fields[i].stableId);
                handled.Add(StablePrefix + open.stableId);
                if (merged[i] != existing[i])
                {
                    bindings.Text(current, key, merged[i]);
                    bindings.Toggle(current, open.key, merged[i].Length > 0);
                }
                current.SetSharedString(CreationMagicResources.NoteStamp(draft, i),
                    merged[i] == newNotes[i] ? CreationDescriptionLayout.Fingerprint(merged[i]) : "");
            }
            return handled;
        }

        private static void MergeFeatureText(CharacterData current, CharacterData before, CharacterData after,
            CreationSheetBindings bindings, Func<string, string> translate)
        {
            var fields = FeatureFields.Select(index => bindings.Entries.First(field => field.key == "input." + index)).ToArray();
            string scene = AppConfig.Scenes.CharacterSheet;
            var live = current.GetSceneData(scene);
            string Read(CharacterSceneData data, CreationSheetBindings.Entry field)
                => data.GetString(StablePrefix + field.stableId, field.index < data.inputData.Count ? data.inputData[field.index] : "");

            string[] existing = fields.Select(field => Read(live, field)).ToArray();
            string[] previous = fields.Select(field => Read(before.GetSceneData(scene, false), field)).ToArray();
            string[] next = fields.Select(field => Read(after.GetSceneData(scene, false), field)).ToArray();
            bool generated = existing.Select((value, i) => value == previous[i] ||
                current.GetSharedString("CreationGeneratedText_v1_" + fields[i].index) == CreationDescriptionLayout.Fingerprint(value)).All(match => match);

            string[] merged = generated ? next.ToArray() : existing.ToArray();
            if (!generated)
            {
                var oldParagraphs = previous.SelectMany(CreationDescriptionLayout.Paragraphs).ToArray();
                var newParagraphs = next.SelectMany(CreationDescriptionLayout.Paragraphs).ToArray();
                foreach (var removed in oldParagraphs.Except(newParagraphs))
                    for (int i = 0; i < merged.Length; i++)
                        merged[i] = string.Join("\n\n", merged[i].Split(new[] { "\n\n" }, StringSplitOptions.None)
                            .Where(paragraph => paragraph.Trim() != removed));
                foreach (var added in newParagraphs.Except(oldParagraphs)) AppendNote(added);
            }

            var oldDraft = CharacterLevelUp.ReadDraft(before);
            var newDraft = CharacterLevelUp.ReadDraft(after);
            if (oldDraft != null && newDraft != null)
            {
                foreach (string slots in CharacterLevelUp.SpellSlotChanges(oldDraft, newDraft, translate)) AppendNote(slots);
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (existing[i] == merged[i]) continue;
                bindings.Text(current, fields[i].key, merged[i]);
                if (i > 0 && merged[i].Length > 0) CharacterCreationService.OpenFeatureNote(live, i - 1);
            }

            void AppendNote(string paragraph)
            {
                if (merged.Any(value => CreationDescriptionLayout.Paragraphs(value).Contains(paragraph))) return;
                for (int i = 1; i < merged.Length; i++)
                {
                    string combined = merged[i].Length == 0 ? paragraph : merged[i] + "\n\n" + paragraph;
                    if (combined.Length > CreationDescriptionLayout.FieldCapacity) continue;
                    merged[i] = combined;
                    return;
                }
                throw new InvalidOperationException("У нотатках немає місця для змін нового рівня.");
            }
        }
        private static void VerifyPositionalFields(CharacterData current, CharacterData before, CharacterData after, CreationSheetBindings bindings)
        {
            const string prefix = "StableField_v1_";
            void Verify(string scene, string titleKey, Func<CreationSheetBindings.Entry, bool> belongs)
            {
                var title = bindings.Entries.FirstOrDefault(entry => entry.key == titleKey);
                var live = current.GetSceneData(scene, false);
                var old = before.GetSceneData(scene, false);
                var next = after.GetSceneData(scene, false);
                if (title == null || live == null || next == null) return;
                string key = prefix + title.stableId;
                if (!next.HasString(key) || (old?.GetString(key) ?? "") == next.GetString(key)) return;
                bool edited = bindings.Entries.Where(belongs).Any(field =>
                {
                    string fieldKey = prefix + field.stableId;
                    if (field.collection == "inputData")
                        return live.HasString(fieldKey) && live.GetString(fieldKey) != (old?.GetString(fieldKey) ?? "");
                    return live.HasInt(fieldKey) && live.GetInt(fieldKey) != (old?.GetInt(fieldKey) ?? 0);
                });
                if (edited) throw new InvalidOperationException("Підвищення зачіпає відредагований запис заклинання або ресурсу. Збереження не змінено.");
            }

            for (int page = 0; page < 5; page++)
            for (int card = 0; card < 15; card++)
            {
                string scene = page == 0 ? "spelBook" : "spelBook " + page;
                Verify(scene, "book.input." + (card * 2), field => field.key == "book.input." + (card * 2) ||
                    field.key == "book.input." + (card * 2 + 1) || field.key == "book.prepared." + card ||
                    field.key == "book.dropdown." + (card * 3) || field.key == "book.dropdown." + (card * 3 + 1) ||
                    field.key == "book.dropdown." + (card * 3 + 2));
            }
            foreach (var field in bindings.Entries.Where(field => field.scene == "Spels" && field.key.StartsWith("magic.", StringComparison.Ordinal) && field.key.EndsWith(".name", StringComparison.Ordinal)))
            {
                string row = field.key.Substring(0, field.key.Length - 5);
                string rest = row.Replace("magic.large.resource.", "rest.magic.large.").Replace("magic.resource.", "rest.magic.")
                    .Replace("magic.invocation.", "rest.magic.invocation.");
                if (!rest.StartsWith("rest.", StringComparison.Ordinal)) continue;
                Verify(field.scene, field.key, candidate => candidate.key == field.key || candidate.key == row + ".count" ||
                    row.StartsWith("magic.invocation.", StringComparison.Ordinal) && candidate.key == rest);
            }
        }

        private static void CarryMagicResourceUses(CharacterData current, CharacterData before, CharacterData after,
            CreationSheetBindings bindings)
        {
            const string prefix = "StableField_v1_";
            var actual = current.GetSceneData("Spels", false);
            var old = before.GetSceneData("Spels", false);
            var next = after.GetSceneData("Spels", false);
            if (actual == null || old == null || next == null) return;
            string Name(CharacterSceneData state, string key) => state.GetString(prefix + key, "");
            string Identity(string label)
            {
                label = System.Text.RegularExpressions.Regex.Replace(label, @"\b[кkdд](?:4|6|8|10|12)\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return label.Trim();
            }
            string oldPactRow = before.GetSharedString("CreationPactResourceRow_v1");
            string newPactRow = after.GetSharedString("CreationPactResourceRow_v1");
            var rows = bindings.Entries.Where(field => field.scene == "Spels" &&
                (field.key.StartsWith("magic.resource.", StringComparison.Ordinal) || field.key.StartsWith("magic.large.resource.", StringComparison.Ordinal)) &&
                field.key.EndsWith(".name", StringComparison.Ordinal)).Select(field =>
            {
                string rowKey = field.key.Substring(0, field.key.Length - 5);
                string restKey = rowKey.Replace("magic.large.resource.", "rest.magic.large.").Replace("magic.resource.", "rest.magic.");
                var uses = bindings.Entries.Where(entry => entry.key == restKey && entry.collection == "toggleData")
                    .OrderBy(entry => entry.index).ToArray();
                var capacity = bindings.Entries.First(entry => entry.key == rowKey + ".count");
                var oldLabel = Name(old, field.stableId);
                var newLabel = Name(next, field.stableId);
                var spent = uses.Select(entry => actual.GetInt(prefix + entry.stableId,
                    entry.index < actual.toggleData.Count && actual.toggleData[entry.index] ? 1 : 0) != 0).ToArray();
                return (oldLabel, newLabel, uses, capacity: next.GetInt(prefix + capacity.stableId), spent,
                    oldIdentity: restKey == oldPactRow ? "pact magic" : Identity(oldLabel),
                    newIdentity: restKey == newPactRow ? "pact magic" : Identity(newLabel));
            }).ToArray();
            foreach (var row in rows.Where(row => row.oldLabel != row.newLabel && row.newLabel.Length > 0))
            {
                var oldRows = rows.Where(item => item.oldIdentity == row.newIdentity).Take(2).ToArray();
                var spent = oldRows.Length == 1 ? oldRows[0].spent : Array.Empty<bool>();
                for (int i = 0; i < row.uses.Length; i++)
                {
                    var use = row.uses[i];
                    bool used = i < spent.Length && i < row.capacity && spent[i];
                    actual.SetInt(prefix + use.stableId, used ? 1 : 0);
                    if (use.index < actual.toggleData.Count) actual.toggleData[use.index] = used;
                }
            }
        }

        private static void CarryPactSlots(CharacterData current, CharacterData before, CharacterData after, CreationSheetBindings bindings)
        {
            string separateTarget = after.GetSharedString("CreationPactResourceRow_v1");
            bool wasPactOnly = before.GetSharedString("CreationSpellRecovery") == "short";
            if (!wasPactOnly || after.GetSharedString("CreationSpellRecovery") != "short" && string.IsNullOrEmpty(separateTarget)) return;
            var actual = current.GetSceneData("Spels", false);
            var old = before.GetSceneData("Spels", false);
            var next = after.GetSceneData("Spels", false);
            if (actual == null || old == null || next == null) return;
            const string prefix = "StableField_v1_";
            var rows = Enumerable.Range(0, 9).Select(circle => bindings.Entries.First(field => field.key == "magic.slot." + circle)).ToArray();
            int source = Array.FindIndex(rows, row => old.GetInt(prefix + row.stableId) > 0);
            int target = Array.FindIndex(rows, row => next.GetInt(prefix + row.stableId) > 0);
            if (source < 0 || string.IsNullOrEmpty(separateTarget) && (target < 0 || source == target)) return;

            var fields = RestSpellSlotFields.ForScene("Spels").ToArray();
            if (fields.Length != 72) throw new InvalidOperationException("Не вдалося перенести витрачені комірки договору.");
            int spent = fields.Skip(source * 8).Take(8).Count(field => field.IsSpent(actual));
            foreach (var field in fields.Skip(source * 8).Take(8)) field.SetSpent(actual, false);
            if (!string.IsNullOrEmpty(separateTarget))
            {
                var uses = bindings.Entries.Where(field => field.key == separateTarget && field.collection == "toggleData")
                    .OrderBy(field => field.index).ToArray();
                for (int i = 0; i < uses.Length; i++)
                {
                    var field = uses[i];
                    actual.SetInt(prefix + field.stableId, i < spent ? 1 : 0);
                    if (field.index < actual.toggleData.Count) actual.toggleData[field.index] = i < spent;
                }
                return;
            }
            spent = Math.Min(spent, next.GetInt(prefix + rows[target].stableId));
            for (int i = 0; i < 8; i++) fields[target * 8 + i].SetSpent(actual, i < spent);
        }
    }
}
