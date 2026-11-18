using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Taruck.CharacterCreation;

// This is an action menu. Its transient selection never belongs to a saved form.
[DefaultExecutionOrder(250)]
[RequireComponent(typeof(TransientUiAction))]
public sealed class WildShapePresetPicker : MonoBehaviour
{
    [SerializeField] private WildShapeFormList formList;
    [SerializeField] private Dropdown choices;
    [SerializeField] private Text heading;
    [SerializeField] private Text hint;
    [SerializeField] private CreationSheetBindings bindings;
    private RuntimeLocalization localization;
    private ReadyFormTemplate[] templates = Array.Empty<ReadyFormTemplate>();
    private ReadyFormTemplate[] available = Array.Empty<ReadyFormTemplate>();
    private AppLanguage language;
    private bool adding;

    private void Start()
    {
        if (formList == null || choices == null || bindings == null || formList.Character == null) return;
        localization = new RuntimeManagerResolver().Localization();
        templates = ReadyFormCatalog.All(formList.Character);
        bool updated = ReadyFormCatalog.IdentifyLegacy(formList.Character, bindings, templates);
        var save = new RuntimeManagerResolver().SaveManager();
        if (ReadyFormProgressionNotes.UpgradeExisting(formList.Character, bindings, templates, localization.CurrentLanguage))
        {
            var state = formList.Character.GetSceneData(save.GetActiveSceneDataName(), false);
            if (state != null) RefreshVisibleNotes(state);
            updated = true;
        }
        if (updated) save.RequestSaveData();
        formList.FormsChanged += Refresh;
        choices.onValueChanged.AddListener(Choose);
        Refresh();
    }

    private void RefreshVisibleNotes(CharacterSceneData state)
    {
        var catalog = new TranslationCatalog();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var input in root.GetComponentsInChildren<InputField>(true))
        {
            var field = PersistentFieldId.For(input);
            if (field == null || !ReadyFormProgressionNotes.IsNoteField(bindings, field.Id)) continue;
            string key = "StableField_v1_" + field.Id;
            if (state.HasString(key) && ReadyFormProgressionNotes.IsGeneratedLegacyText(input.text, catalog))
                input.SetTextWithoutNotify(state.GetString(key));
        }
    }
    private void OnDestroy()
    {
        if (formList != null) formList.FormsChanged -= Refresh;
        if (choices != null) choices.onValueChanged.RemoveListener(Choose);
    }
    private void Update()
    {
        if (localization != null && language != localization.CurrentLanguage) Refresh();
    }
    private void Refresh()
    {
        if (localization == null || formList.Character == null) return;
        language = localization.CurrentLanguage;
        available = ReadyFormCatalog.Available(formList.Character, templates);
        heading.text = localization.Translate("Готові форми");
        hint.text = localization.Translate(templates.Length == 0 ? "Для цього персонажа готових форм ще немає. Порожню форму можна додати кнопкою +."
            : available.Length == 0 ? "Усі доступні форми вже додані." : "Обери форму, щоб додати її до списку.");
        choices.ClearOptions();
        choices.AddOptions(new[] { localization.Translate("Обрати готову форму…") }.Concat(available.Select(template => localization.Translate(template.name))).ToList());
        choices.SetValueWithoutNotify(0);
        choices.RefreshShownValue();
        choices.interactable = available.Length > 0 && !adding;
    }
    private void Choose(int index)
    {
        if (adding || index <= 0 || index > available.Length) return;
        var template = available[index - 1];
        adding = true;
        choices.interactable = false;
        choices.Hide();
        try { formList.AddReadyForm(template, bindings); }
        finally { adding = false; if (this != null) Refresh(); }
    }
}
