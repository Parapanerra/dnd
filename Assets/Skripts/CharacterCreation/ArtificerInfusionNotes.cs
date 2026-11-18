using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Taruck.CharacterCreation
{
    // Upgrade blank or untouched generated notes; never replace a player's own text.
    [DefaultExecutionOrder(350)]
    public sealed class ArtificerInfusionNotes : MonoBehaviour
    {
        [SerializeField] private CreationSheetBindings bindings;
        private RuntimeLocalization localization;
        private AppLanguage shownLanguage;
        private bool ready;

        private static CharacterCreationDraft ArtificerTrack(CharacterCreationDraft root)
        {
            if (root == null) return null;
            if (CharacterArtificerRules.Active(root)) return root;
            return root.secondaryClass?.classId == "artificer" ? root.secondaryClass.ClassDraft(root) : null;
        }

        private void Start()
        {
            localization=new RuntimeManagerResolver().Localization();
            FitResourceLabels();
            Refresh();
            BindEquipmentToggles();
            ready=true;
        }

        private void FitResourceLabels()
        {
            if (bindings == null) return;
            var ids = bindings.Entries.Where(CreationMagicResources.IsResourceName).Select(entry => entry.stableId).ToHashSet();
            foreach (var field in GetComponentsInChildren<InputField>(true))
            {
                if (!ids.Contains(PersistentFieldId.For(field)?.Id) || field.textComponent == null) continue;
                var label = field.textComponent;
                field.lineType = InputField.LineType.MultiLineSubmit;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextMaxSize = label.fontSize;
                label.resizeTextMinSize = Mathf.Min(11, label.fontSize);
                label.resizeTextForBestFit = true;
                field.ForceLabelUpdate();
            }
        }

        private void BindEquipmentToggles()
        {
            if (bindings == null) return;
            var save = new RuntimeManagerResolver().SaveManager();
            var character = save.GetActiveCharacter();
            var draft = ArtificerTrack(CharacterLevelUp.ReadDraft(character));
            if (draft == null) return;
            CharacterArtificerEquipment.Repair(draft);
            var toggles = GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < draft.artificerPlans.Count; i++)
            {
                int slot = i;
                if (CharacterArtificerEquipment.Targets(draft, slot).Length == 0) continue;
                string key = "magic.infusion." + slot + ".prepared";
                var entry = bindings.Entries.FirstOrDefault(field => field.key == key);
                var toggle = toggles.FirstOrDefault(control => PersistentFieldId.For(control)?.Id == entry?.stableId);
                if (toggle == null) continue;
                bool equipped = CharacterArtificerEquipment.Target(draft, slot).Length > 0;
                toggle.SetIsOnWithoutNotify(equipped);
                bindings.Toggle(character, key, equipped);
                toggle.onValueChanged.AddListener(value =>
                {
                    var active = save.GetCharacter(character.id);
                    var current = ArtificerTrack(CharacterLevelUp.ReadDraft(active));
                    if (current == null || slot >= current.artificerPlans.Count) return;
                    CharacterArtificerEquipment.Repair(current);
                    void Apply(string target = null)
                    {
                        var root = CharacterLevelUp.ReadDraft(active);
                        var latest = ArtificerTrack(root);
                        if (latest == null || slot >= latest.artificerPlans.Count || latest.artificerPlans[slot] != current.artificerPlans[slot]) return;
                        CharacterArtificerEquipment.Repair(latest);
                        if (latest.IsSecondaryTrack)
                        {
                            string original = CharacterArtificerEquipment.Target(latest, slot);
                            var options = CharacterArtificerEquipment.AvailableTargets(latest, slot);
                            string selected = target ?? (original.Length > 0 ? original : active.GetSharedString("ArtificerTarget_" + slot));
                            if (value && !options.Contains(selected))
                                selected = options.Length == 1 ? options[0] : "";
                            if (!value || selected.Length > 0)
                            {
                                var before = CharacterLevelUp.ReadDraft(active);
                                latest.artificerEquippedTargets[slot] = value ? selected : "";
                                root.secondaryClass.SaveChoices(latest);
                                try
                                {
                                    CreationArtificerResources.ApplyEquipmentChange(active, before, root, bindings, localization.Translate);
                                    if (value) active.SetSharedString("ArtificerTarget_" + slot, selected);
                                }
                                catch (Exception error)
                                {
                                    latest.artificerEquippedTargets[slot] = original;
                                    Debug.LogWarning(error.Message, this);
                                }
                            }
                        }
                        else CreationArtificerResources.ApplyEquipmentToggle(active, bindings, latest, slot, value, localization.Translate, target);
                        toggle.SetIsOnWithoutNotify(CharacterArtificerEquipment.Target(latest, slot).Length > 0);
                        bindings.Toggle(active, key, toggle.isOn);
                        save.RequestSaveData();
                    }
                    var options = CharacterArtificerEquipment.AvailableTargets(current, slot);
                    string remembered = CharacterArtificerEquipment.Target(current, slot);
                    if (remembered.Length == 0) remembered = active.GetSharedString("ArtificerTarget_" + slot);
                    if (value && options.Length > 1 && !options.Contains(remembered))
                    {
                        toggle.SetIsOnWithoutNotify(false);
                        bindings.Toggle(active, key, false);
                        ChooseTarget(current, options, 0, Apply);
                        return;
                    }
                    Apply();
                });
            }
        }

        private void ChooseTarget(CharacterCreationDraft draft, string[] options, int index, Action<string> chosen)
        {
            string Label(string target) => localization.Translate(CharacterArtificerEquipment.Label(draft, target)) +
                CharacterArtificerEquipment.TargetNumber(draft, target);
            var dialog = new TaruckDialogPresenter(() => localization.CurrentLanguage);
            bool more = index + 2 < options.Length;
            string target = options[index];
            string next = options[index + 1];
            dialog.Show(localization.Translate("Обери предмет для спорядження"), "", Label(target), () => chosen(target),
                more ? localization.Translate("Інші варіанти") : Label(next),
                () => { if (more) ChooseTarget(draft, options, index + 1, chosen); else chosen(next); });
        }
        private void Update()
        {
            if(ready&&localization!=null&&shownLanguage!=localization.CurrentLanguage)Refresh();
        }
        private void Refresh()
        {
            if(bindings==null||localization==null)return;
            shownLanguage=localization.CurrentLanguage;
            var save=new RuntimeManagerResolver().SaveManager();
            var character=save.EnsureActiveCharacter();
            string json=character?.GetSharedString("CreationWizard_v1","");
            if(string.IsNullOrEmpty(json))return;
            CharacterCreationDraft draft;
            try{draft=JsonUtility.FromJson<CharacterCreationDraft>(json);}catch(ArgumentException){return;}
            if(draft==null)return;
            var artificer = ArtificerTrack(draft);
            var catalog=new TranslationCatalog();
            var features = CharacterCreationRules.Calculate(draft).features;
            var languages = Enum.GetValues(typeof(AppLanguage)).Cast<AppLanguage>()
                .ToDictionary(language => language, language => (Func<string, string>)(source => catalog.Translate(source, language)));
            var labels = languages.Values.ToArray();
            var variants = languages.ToDictionary(pair => pair.Key, pair =>
                CreationMagicResources.NoteTexts(character, bindings, draft, features, pair.Value, labels));
            var legacy=artificer != null ? Enum.GetValues(typeof(AppLanguage)).Cast<AppLanguage>()
                .SelectMany(language=>new[] {
                    CreationArtificerResources.LegacyInfusionNoteTexts(artificer,s=>catalog.Translate(s,language)),
                    CreationArtificerResources.InfusionNoteTexts(artificer,s=>catalog.Translate(s,language))
                }).ToArray() : Array.Empty<string[]>();
            var state=character.GetSceneData("Spels");
            bool changed=false;
            for(int row=0;row<4;row++)
            {
                var entry=bindings.Entries.First(e=>e.key=="magic.note."+row);
                string key="StableField_v1_"+entry.stableId;
                string old=state.GetString(key,entry.index<state.inputData.Count?state.inputData[entry.index]:"");
                string next=variants[shownLanguage][row];
                string stampKey=CreationMagicResources.NoteStamp(draft,row);
                bool unchanged=character.GetSharedString(stampKey,"")==CreationDescriptionLayout.Fingerprint(old);
                if(old==next)
                {
                    if(!unchanged){character.SetSharedString(stampKey,CreationDescriptionLayout.Fingerprint(next));changed=true;}
                    continue;
                }
                if(!string.IsNullOrWhiteSpace(old)&&!unchanged&&!variants.Values.Any(notes=>notes[row]==old)&&!legacy.Any(notes=>notes[row]==old))continue;
                bindings.Text(character,entry.key,next);bindings.Toggle(character,entry.key+".open",next.Length>0);
                character.SetSharedString(stampKey,CreationDescriptionLayout.Fingerprint(next));
                foreach(var field in GetComponentsInChildren<InputField>(true))
                    if(PersistentFieldId.For(field)?.Id==entry.stableId)field.SetTextWithoutNotify(next);
                var toggleEntry=bindings.Entries.First(e=>e.key==entry.key+".open");
                foreach(var toggle in GetComponentsInChildren<Toggle>(true))
                    if(PersistentFieldId.For(toggle)?.Id==toggleEntry.stableId)toggle.isOn=next.Length>0;
                changed=true;
            }
            if(changed)save.RequestSaveData();
        }
    }
}
