using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private string advancementClass;
        private readonly Dictionary<string, int> secondaryHitDieRolls = new Dictionary<string, int>();
        private bool showSecondaryHitPoints;

        private void DrawMulticlassCreation()
        {
            if ((int)draft.edition < 0 || draft.level < 1) return;
            var options = CharacterSecondaryClass.Options(draft).ToArray();
            if (draft.secondaryClass?.IsSelected == true && !options.Any(option => option.id == draft.secondaryClass.classId))
                draft.secondaryClass = new CharacterSecondaryClass();
            if (draft.level >= CharacterClassHistory.MaximumCharacterLevel) return;
            DropdownKey("Мультиклас", options.Select(option => option.id).ToArray(), options.Select(option => L(option.name)).ToArray(),
                draft.secondaryClass?.classId ?? "", value => draft.secondaryClass = new CharacterSecondaryClass { classId = value }, 300);
            if (draft.secondaryClass?.IsSelected != true) return;
            int max = Math.Min(CharacterSecondaryClass.MaximumLevel, CharacterClassHistory.MaximumCharacterLevel - draft.level);
            var secondary = draft.secondaryClass;
            DropdownInt("Рівень другого класу", Enumerable.Range(1, max).ToArray(), secondary.level,
                value => secondary.level = Math.Clamp(value, 1, max), value => value.ToString(), 160);
            Note("Підклас, навички й заклинання другого класу обираються на кроці «Спорядження».");
            DrawSecondClassChoices(0);
        }

        private bool DrawMulticlassAdvancement()
        {
            var original = advancementOriginal;
            var existing = original.secondaryClass;
            var history = CharacterClassHistory.FromCreation(original);
            var scores = CharacterCreationRules.Scores(original, includeEquipment: false);
            var options = existing?.IsSelected == true
                ? new[] { CharacterClassCatalog.Find(existing.classId) }
                : CharacterSecondaryClass.Options(original).Where(option => history.CanTakeLevel(original.edition, option.id, scores)).ToArray();
            if (options.Length == 0) return false;
            var primary = CharacterClassCatalog.Find(original.characterClass);
            var keys = new[] { primary.id }.Concat(options.Select(option => option.id)).ToArray();
            var labels = new[] { L(primary.name) }.Concat(options.Select(option => L(option.name))).ToArray();
            ui.Text(content, "Клас для підвищення", 22, CreationWizardUi.Gold, 36);
            ui.Dropdown(content, "Клас для підвищення", labels, Array.IndexOf(keys, advancementClass ?? primary.id), selected =>
            {
                string selection = keys[selected];
                if (selection == (advancementClass ?? primary.id)) return;
                advancementClass = selection;
                if (selection == primary.id)
                {
                    draft.level = original.level + 1;
                    draft.secondaryClass = CopySecondaryClass(existing);
                }
                else
                {
                    draft.level = original.level;
                    draft.secondaryClass = existing?.IsSelected == true ? CopySecondaryClass(existing) : new CharacterSecondaryClass { classId = selection, level = 0 };
                    draft.secondaryClass.level++;
                }
                Draw(false);
            }, 300, 42);
            if (!IsAdvancingSecondary) return false;
            DrawSecondClassChoices(existing?.IsSelected == true ? existing.level : 0);
            return true;
        }

        private bool IsAdvancingSecondary => IsLevelUp && advancementClass != null && advancementClass != advancementOriginal.characterClass;

        private static CharacterSecondaryClass CopySecondaryClass(CharacterSecondaryClass value) => value == null
            ? new CharacterSecondaryClass() : JsonUtility.FromJson<CharacterSecondaryClass>(JsonUtility.ToJson(value));

        private void DrawSecondClassChoices(int firstLevel)
        {
            var secondary = draft.secondaryClass;
            secondary.ResizeHitPointChoices();
            var definition = CharacterClassCatalog.Find(secondary.classId);
            var track = secondary.ClassDraft(draft);
            int die = definition.hitDie;
            if (!IsLevelUp && secondary.level > 1)
            {
                ui.Text(content, LF("Кидки ОЗ — {0}", L(definition.name)), 24, CreationWizardUi.Gold, 42);
                var actions = compact ? content : ui.Row("SecondaryHealthActions", content, 54);
                if (!compact) LimitLevelUpButtonRow(actions);
                ui.Button(actions, "Середнє для всіх", () =>
                {
                    Array.Fill(secondary.hitPointChoices, -1, 0, secondary.level);
                    Draw(false);
                }, secondary.hitPointChoices.Take(secondary.level).All(choice => choice == -1), 54, showCheck: true);
                ui.Button(actions, "Кинути для всіх", () =>
                {
                    for (int slot = 0; slot < secondary.level; slot++)
                        secondary.hitPointChoices[slot] = UnityEngine.Random.Range(1, die + 1);
                    Draw(false);
                }, secondary.hitPointChoices.Take(secondary.level).All(choice => choice > 0), 54, showCheck: true);
                ui.Text(content, LF("Вибрано рівнів: {0}/{1}.",
                    secondary.hitPointChoices.Take(secondary.level).Count(choice => choice != 0), secondary.level),
                    22, CreationWizardUi.Gold, 36);
                ui.Button(content, showSecondaryHitPoints ? "Згорнути окремі рівні" : "Налаштувати окремі рівні", () =>
                {
                    showSecondaryHitPoints = !showSecondaryHitPoints;
                    Draw(false);
                }, false, 48);
                if (!showSecondaryHitPoints) return;
            }
            for (int i = firstLevel; i < secondary.level; i++)
            {
                int slot = i;
                ui.Text(content, LF("ОЗ другого класу на рівні {0}", i + 1), 22, CreationWizardUi.Gold, 40);
                Transform row = compact ? content : ui.Row("SecondaryClassHealth", content, 54);
                if (!compact) LimitLevelUpButtonRow(row);
                ui.Button(row, LF("Взяти середнє: {0}", die / 2 + 1), () => { secondary.hitPointChoices[slot] = -1; Draw(false); },
                    secondary.hitPointChoices[slot] == -1, 54, showCheck: true);
                secondaryHitDieRolls.TryGetValue(secondary.classId, out int previousRoll);
                int rolled = secondary.hitPointChoices[slot] > 0 ? secondary.hitPointChoices[slot] : previousRoll;
                ui.Button(row, rolled > 0 ? LF("Випало: {0}", rolled) : LF("Кинути к{0}", die), () =>
                {
                    int result = rolled > 0 ? rolled : UnityEngine.Random.Range(1, die + 1);
                    if (IsLevelUp) secondaryHitDieRolls[secondary.classId] = result;
                    secondary.hitPointChoices[slot] = result;
                    Draw(false);
                }, secondary.hitPointChoices[slot] > 0, 54, showCheck: true);
            }

            if (IsLevelUp) DrawSecondaryTrackChoices(secondary, track, firstLevel);
        }

        private bool drawingSecondaryTrack;
        private bool redrawSecondaryTrack;

        private void DrawSecondaryTrackChoices(CharacterSecondaryClass secondary, CharacterCreationDraft track, int firstLevel)
        {
            var primary = draft;
            var original = advancementOriginal;
            var previous = original?.secondaryClass?.IsSelected == true
                ? original.secondaryClass.ClassDraft(original)
                : new CharacterCreationDraft { characterClass = track.characterClass, edition = track.edition, level = firstLevel };

            void InTrack(Action action)
            {
                draft = track;
                advancementOriginal = previous;
                drawingSecondaryTrack = true;
                try
                {
                    action();
                    secondary.SaveChoices(track);
                }
                finally
                {
                    draft = primary;
                    advancementOriginal = original;
                    drawingSecondaryTrack = false;
                }
                if (!redrawSecondaryTrack) return;
                redrawSecondaryTrack = false;
                Draw(false);
            }

            ui.BindChoice = action => () => InTrack(action);
            drawingNewClassChoices = IsLevelUp;
            try
            {
                InTrack(() =>
                {
                    CharacterCreationRules.RepairDependentChoices(track);
                    CharacterClericSpellChoices.Repair(track);
                    CharacterDruidFormChoices.Repair(track);
                    DrawClassProgressionChoices();
                    SkillsPage();
                    EquipmentPage();
                });
            }
            finally
            {
                ui.BindChoice = null;
                drawingNewClassChoices = false;
            }
        }
        private string ClassSummary()
        {
            string primary = L(CharacterClassCatalog.Find(draft.characterClass).name);
            if (draft.secondaryClass?.IsSelected != true) return primary;
            var secondary = draft.secondaryClass;
            return $"{primary} {draft.level} / {L(CharacterClassCatalog.Find(secondary.classId).name)} {secondary.level}";
        }
    }
}
