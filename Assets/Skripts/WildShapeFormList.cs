using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Uses the scene's existing form, add, and delete artwork inside a scrollable list.
[DefaultExecutionOrder(200)]
public sealed class WildShapeFormList : MonoBehaviour
{
    internal const string SlotsKey = "WildShapeFormSlots";
    private const int DefaultFormCount = 3;
    private const string NameKeyPrefix = "WildShapeFormName_";
    private const string PendingDeleteKey = "WildShapePendingDeleteSlot";
    private static string pendingCameraTarget;
    private static Vector3 pendingCameraPosition;
    private static float pendingCameraSize;

    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private RectTransform content;
    [SerializeField] private Button formTemplate;
    [SerializeField] private Button deleteTemplate;
    [SerializeField] private Button addButton;

    private readonly List<int> slots = new List<int>();
    private DndSaveManager saveManager;
    private CharacterData character;
    private RuntimeLocalization localization;
    private InputField nameInput;
    private AppLanguage lastLanguage;
    private float rowStep;
    private Vector2 addStartPosition;
    private int selectedSlot;

    private void Start()
    {
        if (scrollView == null || content == null || formTemplate == null ||
            deleteTemplate == null || addButton == null)
        {
            Debug.LogError("WildShapeFormList: scene list references are missing.", this);
            return;
        }

        saveManager = new RuntimeManagerResolver().SaveManager();
        character = saveManager.EnsureActiveCharacter();
        localization = new RuntimeManagerResolver().Localization();
        if (character == null)
        {
            Debug.LogError("WildShapeFormList: active character is missing.", this);
            return;
        }

        CompletePendingDelete();
        nameInput = transform.Find("petoForma/plauerInfo (1)/inputPises/mmpises2")?.GetComponent<InputField>();
        lastLanguage = localization.CurrentLanguage;

        addStartPosition = ((RectTransform)addButton.transform).anchoredPosition;
        RectTransform formRect = (RectTransform)formTemplate.transform;
        RectTransform deleteRect = (RectTransform)deleteTemplate.transform;
        float formHeight = formRect.rect.height * Mathf.Abs(formRect.localScale.y);
        float deleteHeight = deleteRect.rect.height * Mathf.Abs(deleteRect.localScale.y);
        rowStep = Mathf.Max(formHeight, deleteHeight) + 8f;

        LoadSlots();
        selectedSlot = SlotFromSceneName(saveManager.GetActiveSceneDataName());
        RestoreCamera();
        if (!slots.Contains(selectedSlot))
        {
            SelectSlot(slots[0]);
            return;
        }

        if (nameInput != null)
        {
            string currentName = nameInput.text.Trim();
            if (!character.HasSharedString(NameKey(selectedSlot)) && currentName.Length > 0)
            {
                character.SetSharedString(NameKey(selectedSlot), currentName);
                saveManager.RequestSaveData();
            }
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        DisablePersistentActions(formTemplate);
        DisablePersistentActions(deleteTemplate);
        DisablePersistentActions(addButton);
        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(AddForm);
        formTemplate.gameObject.SetActive(false);
        deleteTemplate.gameObject.SetActive(false);
        RefreshRows();
        FindAnyObjectByType<WildShapeTitleUpdater>()?.Apply();
    }

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    private void Update()
    {
        if (localization == null || localization.CurrentLanguage == lastLanguage)
            return;
        lastLanguage = localization.CurrentLanguage;
        RefreshLabels();
    }

    private void LoadSlots()
    {
        slots.Clear();
        string stored = character.GetSharedString(SlotsKey, "");
        if (!string.IsNullOrWhiteSpace(stored))
        {
            foreach (string part in stored.Split(','))
                if (int.TryParse(part, out int slot) && slot >= 0 && !slots.Contains(slot))
                    slots.Add(slot);
        }

        if (slots.Count > 0)
            return;

        for (int i = 0; i < DefaultFormCount; i++)
            slots.Add(i);

        // Existing characters may already have data in more than three old pages.
        if (character.sceneStates != null)
        {
            foreach (CharacterSceneData state in character.sceneStates)
            {
                int slot = SlotFromSceneName(state != null ? state.sceneName : null);
                if (slot >= DefaultFormCount && !slots.Contains(slot))
                    slots.Add(slot);
            }
        }

        slots.Sort();
        PersistSlots();
    }

    private void RefreshRows()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (!child.name.StartsWith("WildShapeForm_", StringComparison.Ordinal) &&
                !child.name.StartsWith("WildShapeDelete_", StringComparison.Ordinal))
                continue;
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        RectTransform formRect = (RectTransform)formTemplate.transform;
        RectTransform deleteRect = (RectTransform)deleteTemplate.transform;
        for (int i = 0; i < slots.Count; i++)
        {
            int slot = slots[i];
            Button form = Instantiate(formTemplate, content);
            Button delete = Instantiate(deleteTemplate, content);
            form.gameObject.name = "WildShapeForm_" + (i + 1);
            delete.gameObject.name = "WildShapeDelete_" + (i + 1);
            Text label = form.GetComponentInChildren<Text>(true);
            if (label != null && label.GetComponent<LocalizedIgnore>() == null)
                label.gameObject.AddComponent<LocalizedIgnore>();
            form.gameObject.SetActive(true);
            delete.gameObject.SetActive(true);

            ((RectTransform)form.transform).anchoredPosition =
                formRect.anchoredPosition + Vector2.down * (i * rowStep);
            ((RectTransform)delete.transform).anchoredPosition =
                deleteRect.anchoredPosition + Vector2.down * (i * rowStep);

            DisablePersistentActions(form);
            DisablePersistentActions(delete);
            form.onClick.RemoveAllListeners();
            delete.onClick.RemoveAllListeners();
            form.onClick.AddListener(() => SelectSlot(slot));
            delete.onClick.AddListener(() => DeleteForm(slot));
            delete.interactable = slots.Count > 1;

            if (label != null)
                label.text = GetLabel(slot, i);
        }

        ((RectTransform)addButton.transform).anchoredPosition =
            new Vector2(addStartPosition.x, formRect.anchoredPosition.y - slots.Count * rowStep);
        float requiredHeight = Mathf.Abs(((RectTransform)addButton.transform).anchoredPosition.y) +
                               ((RectTransform)addButton.transform).rect.height + 24f;
        content.sizeDelta = new Vector2(content.sizeDelta.x,
            Mathf.Max(scrollView.viewport.rect.height, requiredHeight));
        scrollView.verticalNormalizedPosition = 1f;
    }

    private void SelectSlot(int slot)
    {
        if (!slots.Contains(slot))
            return;

        string sceneName = SceneNameForSlot(slot);
        if (saveManager.GetActiveSceneDataName() != sceneName)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                pendingCameraTarget = sceneName;
                pendingCameraPosition = camera.transform.position;
                pendingCameraSize = camera.orthographicSize;
            }
            new SceneNavigator().LoadSceneByName(sceneName);
        }
    }

    private void RestoreCamera()
    {
        if (pendingCameraTarget != saveManager.GetActiveSceneDataName())
            return;
        pendingCameraTarget = null;
        Camera camera = Camera.main;
        if (camera == null)
            return;
        camera.orthographicSize = pendingCameraSize;
        camera.transform.position = pendingCameraPosition;
    }

    private void AddForm()
    {
        int slot = 0;
        foreach (int existing in slots)
            slot = Mathf.Max(slot, existing + 1);
        while (character.GetSceneData(SceneNameForSlot(slot), false) != null)
            slot++;

        slots.Add(slot);
        PersistSlots();
        SelectSlot(slot);
    }

    private void DeleteForm(int slot)
    {
        if (!slots.Contains(slot) || slots.Count <= 1)
            return;

        if (selectedSlot == slot)
        {
            var messages = new TaruckDialogText(() => localization.CurrentLanguage);
            new TaruckDialogPresenter(() => localization.CurrentLanguage).ShowCompact(
                messages.Text("Видалення відкритої форми", "Delete open form", "Удаление открытой формы"),
                messages.Text("Ви намагаєтесь видалити форму, яка зараз відкрита. Видалити її та перейти до іншої форми?",
                    "You are deleting the form that is currently open. Delete it and open another form?",
                    "Вы удаляете открытую форму. Удалить её и открыть другую форму?"),
                messages.Text("Видалити", "Delete", "Удалить"),
                () => DeleteFormConfirmed(slot));
            return;
        }

        DeleteFormConfirmed(slot);
    }

    private void DeleteFormConfirmed(int slot)
    {
        if (!slots.Contains(slot) || slots.Count <= 1)
            return;

        int position = slots.IndexOf(slot);
        int replacement = selectedSlot == slot
            ? slots[position == 0 ? 1 : position - 1]
            : selectedSlot;
        slots.Remove(slot);
        character.DeleteSharedString(NameKey(slot));
        PersistSlots();

        if (selectedSlot == slot)
        {
            // The departing scene saves during unload. Delete its data after the next scene loads.
            character.SetSharedString(PendingDeleteKey, slot.ToString());
            saveManager.SaveData();
            SelectSlot(replacement);
            return;
        }

        CharacterSceneData state = character.GetSceneData(SceneNameForSlot(slot), false);
        if (state != null)
            character.sceneStates.Remove(state);
        saveManager.SaveData();
        RefreshRows();
    }

    private void CompletePendingDelete()
    {
        string pending = character.GetSharedString(PendingDeleteKey, "");
        if (!int.TryParse(pending, out int slot))
            return;
        CharacterSceneData state = character.GetSceneData(SceneNameForSlot(slot), false);
        if (state != null)
            character.sceneStates.Remove(state);
        character.DeleteSharedString(PendingDeleteKey);
        saveManager.SaveData();
    }

    private void OnNameChanged(string value)
    {
        if (character == null || selectedSlot < 0)
            return;
        character.SetSharedString(NameKey(selectedSlot), value.Trim());
        saveManager.RequestSaveData();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Transform row = content.Find("WildShapeForm_" + (i + 1));
            Text label = row != null ? row.GetComponentInChildren<Text>(true) : null;
            if (label != null)
                label.text = GetLabel(slots[i], i);
        }
    }

    private string GetLabel(int slot, int index)
    {
        string customName = character.GetSharedString(NameKey(slot), "").Trim();
        if (customName.Length == 0 && nameInput != null)
        {
            CharacterSceneData state = character.GetSceneData(SceneNameForSlot(slot), false);
            if (state != null)
                customName = StableFieldStorage.ReadText(state, nameInput, "").Trim();
        }
        if (customName.Length > 0)
            return customName;
        return localization.CurrentLanguage == AppLanguage.English
            ? "Form " + (index + 1) : "Форма " + (index + 1);
    }

    private static string NameKey(int slot) => NameKeyPrefix + slot;

    private void PersistSlots()
    {
        character.SetSharedString(SlotsKey, string.Join(",", slots));
        saveManager.RequestSaveData();
    }

    internal static string SceneNameForSlot(int slot)
    {
        return slot <= 0 ? AppConfig.Scenes.WildShape : AppConfig.Scenes.WildShape + " " + slot;
    }

    internal static int SlotFromSceneName(string sceneName)
    {
        if (sceneName == AppConfig.Scenes.WildShape)
            return 0;
        const string prefix = AppConfig.Scenes.WildShape + " ";
        return sceneName != null && sceneName.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(sceneName.Substring(prefix.Length), out int slot) && slot > 0
            ? slot : -1;
    }

    internal static int DisplayNumber(CharacterData character, int slot)
    {
        if (character == null || slot < 0)
            return 1;
        string stored = character.GetSharedString(SlotsKey, "");
        if (!string.IsNullOrWhiteSpace(stored))
        {
            string[] parts = stored.Split(',');
            for (int i = 0; i < parts.Length; i++)
                if (int.TryParse(parts[i], out int value) && value == slot)
                    return i + 1;
        }
        return slot + 1;
    }

    private static void DisablePersistentActions(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
    }
}
