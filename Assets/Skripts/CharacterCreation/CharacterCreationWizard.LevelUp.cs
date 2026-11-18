using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private string advancementId;
        private string advancementBaseline;
        private int advancementFrom;
        private CharacterCreationDraft advancementOriginal;
        private readonly HashSet<string> levelChoices = new HashSet<string>();
        private bool drawingNewClassChoices;
        private int levelHitDieRoll;
        private bool IsLevelUp => advancementId != null;

        private void LoadLevelUp()
        {
            var manager = new RuntimeManagerResolver().FindSaveManager();
            string id = manager?.ConsumePendingLevelUpCharacter();
            if (string.IsNullOrEmpty(id)) return;
            var character = manager.GetCharacter(id);
            if (!CharacterLevelUp.CanStart(character)) return;
            draft = CharacterLevelUp.ReadDraft(character);
            advancementOriginal = CharacterLevelUp.ReadDraft(character);
            advancementId = id;
            advancementBaseline = character.GetSharedString(CharacterLevelUp.DraftKey);
            advancementFrom = draft.level;
            draft.level++;
            draft.characterName = character.characterName;
        }

        private void DrawLevelUp()
        {
            heading.text = L("Підвищення рівня");
            subtitle.text = draft.characterName + "  •  " + ClassSummary() + "  •  D&D " +
                (draft.edition == RulesEdition.Rules2024 ? "2024" : "2014");
            progress.text = LF("Рівень {0} → {1}", advancementOriginal.TotalLevel, draft.TotalLevel);
            feedback.text = "";
            back.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = L("<  Назад");
            nextLabel.text = L("Зберегти рівень");
            StyleIcon(next, completeSprite);

            if (DrawMulticlassAdvancement())
            {
                var secondary = draft.secondaryClass;
                var track = secondary.ClassDraft(draft.edition);
                DrawLevelAdvancements(track, track.level - 1, () => secondary.SaveChoices(track));
                drawingNewClassChoices = true;
                try { DrawLevelUpSpecies(); }
                finally { drawingNewClassChoices = false; }
                DrawLevelBenefits();
                return;
            }

            ui.Text(content, LF("ОЗ на рівні {0}", draft.level), 26, CreationWizardUi.Gold, 44);
            int index = draft.level - 2;
            int fixedHp = CharacterCreationRules.FixedHitPoints(draft);
            int hitDie = CharacterCreationRules.HitDie(draft);
            var actions = compact ? content : ui.Row("LevelUpHealthActions", content, 54);
            if (!compact) LimitLevelUpButtonRow(actions);
            ui.Button(actions, LF("Взяти середнє: {0}", fixedHp), () =>
            {
                draft.hitPointChoices[index] = -1;
                Draw(false);
            }, draft.hitPointChoices[index] == -1, 54, showCheck: true);
            ui.Button(actions, levelHitDieRoll == 0 ? LF("Кинути к{0}", hitDie) : LF("Випало: {0}", levelHitDieRoll), () =>
            {
                if (levelHitDieRoll == 0) levelHitDieRoll = UnityEngine.Random.Range(1, hitDie + 1);
                draft.hitPointChoices[index] = levelHitDieRoll;
                Draw(false);
            }, draft.hitPointChoices[index] > 0, 54, showCheck: true);

            DrawLevelAdvancements();
            DrawLevelBenefits();
            DrawArtificerLevelUpSpells();
            drawingNewClassChoices = true;
            try
            {
                DrawLevelUpSpecies();
                DrawClassProgressionChoices();
                SkillsPage();
                EquipmentPage();
            }
            finally { drawingNewClassChoices = false; }
        }

        private void DrawLevelUpSpecies()
        {
            if (draft.species == "simic" && draft.TotalLevel == 5 ||
                draft.species == "aasimarlegacy" && draft.TotalLevel == 3)
                ExpandedOriginChoices();
        }

        private void DrawLevelBenefits()
        {
            string[] Features(CharacterCreationDraft value)
                => CreationDescriptionLayout.Paragraphs(CharacterCreationRules.Calculate(value).features)
                    .Where(line => !line.EndsWith("• D&D 2014", StringComparison.Ordinal) &&
                        !line.EndsWith("• D&D 2024", StringComparison.Ordinal)).ToArray();
            var changes = Features(draft).Except(Features(advancementOriginal))
                .Select(line => CharacterCreationPresentation.FeatureLines(line, L))
                .Concat(CharacterLevelUp.SpellSlotChanges(advancementOriginal, draft, L)).ToArray();
            IEnumerable<string> Prepared(CharacterCreationDraft value)
            {
                var spells = CreationSpellPreparation.Automatic(value);
                return value.secondaryClass?.IsSelected == true
                    ? spells.Concat(CreationSpellPreparation.Automatic(value.secondaryClass.ClassDraft(value))) : spells;
            }
            var newPrepared = Prepared(draft).Except(Prepared(advancementOriginal)).ToArray();
            if (changes.Length == 0 && newPrepared.Length == 0) return;
            ui.Text(content, "Зміни нового рівня", 25, CreationWizardUi.Gold, 42);
            foreach (string change in changes) Note(change);
            if (newPrepared.Length > 0)
                Note(L("Нові автоматично підготовлені заклинання:") + " " + string.Join(", ", newPrepared.Select(L)));
        }

        private void DrawArtificerLevelUpSpells()
        {
            if (!CharacterArtificerRules.Active(draft) || !CharacterArtificerRules.Revised(draft)) return;
            int previousCount = CharacterArtificerRules.SpellCount(advancementOriginal);
            int count = CharacterArtificerRules.SpellCount(draft);
            if (count > previousCount)
            {
                ui.Text(content, LF("Підготовлені заклинання: {0} → {1}", previousCount, count),
                    25, CreationWizardUi.Gold, 42);
                SpellPickers(draft.preparedSpells, 1, count, firstSlot: previousCount);
            }

        }

        private static void LimitLevelUpButtonRow(Transform row)
        {
            row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        }

        private bool ShowLevelChoice(string label, bool unfilled)
        {
            if (!drawingNewClassChoices) return true;
            string key = draft.characterClass + "|" + label;
            if (unfilled) levelChoices.Add(key);
            return levelChoices.Contains(key);
        }

        private bool LevelUpAddsExpertise()
        {
            int Count(CharacterCreationDraft value) => value.level < 1 ? 0 : value.characterClass == "rogue"
                ? CharacterRogueRules.ExpertiseCount(value)
                : CharacterBardRules.ExpertiseCount(value) + CharacterRangerRules.ExpertiseCount(value);
            return Count(draft) > Count(advancementOriginal);
        }

        private void SaveLevelUp()
        {
            string error = IsAdvancingSecondary ? draft.secondaryClass.ChoiceError(draft, false) : StepError(0) ?? CharacterLevelAdvancement.ChoiceError(draft) ??
                StepError(3) ?? StepError(4) ?? CharacterCreationRules.Validate(draft, true).FirstOrDefault();
            if (error != null)
            {
                feedback.text = L(error);
                return;
            }
            if (!Commit(out error))
            {
                feedback.text = LF("Не вдалося зберегти: {0}", L(error));
                return;
            }
            next.interactable = false;
            SceneManager.LoadScene(AppConfig.Scenes.CharacterSheet);
        }

        private bool Commit(out string error)
        {
            var manager = new RuntimeManagerResolver().SaveManager();
            string committedId = advancementId;
            bool saved = manager.TryUpdateCollection(candidate =>
            {
                if (IsLevelUp)
                {
                    var character = candidate.characters.Find(item => item.id == advancementId);
                    CharacterLevelUp.ApplyClassLevel(character, advancementBaseline, draft, bindings, L);
                }
                else
                {
                    var character = new CharacterCreationService(bindings, L).Build(draft);
                    candidate.characters.Add(character);
                    committedId = character.id;
                }
                candidate.lastActiveCharacterId = committedId;
            }, out error);
            if (saved) createdId = committedId;
            return saved;
        }

        private void LeaveWizard()
        {
            SceneManager.LoadScene(IsLevelUp ? AppConfig.Scenes.CharacterSheet : AppConfig.Scenes.Menu);
        }
    }
}
