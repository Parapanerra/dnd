using System.Collections;
using System.Collections.Generic;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Owns the import/export panel and its selection state.
public class MainMenuTransferPanel : MonoBehaviour
{
    private MainMenuManager owner;
    private Button exportOneCharacterButton;
    private Button importOneCharacterButton;
    private Button openSavePanelButton;
    private Dropdown oneCharacterDropdown;
    private TMP_Dropdown oneCharacterTmpDropdown;
    private GameObject savePanel;
    private bool oneCharacterDropdownHasSelection;

    public void Initialize(MainMenuManager manager)
    {
        owner = manager;
    }

    public void HideSavePanel()
    {
        if (savePanel != null)
            savePanel.SetActive(false);
    }

    public void BindButtons()
    {
        if (owner.exportButton != null)
        {
            owner.exportButton.onClick.RemoveAllListeners();
            owner.exportButton.onClick.AddListener(ExportFile);
        }

        if (owner.importButton != null)
        {
            owner.importButton.onClick.RemoveAllListeners();
            owner.importButton.onClick.AddListener(ImportFile);
        }

        if (exportOneCharacterButton != null)
        {
            exportOneCharacterButton.onClick.RemoveAllListeners();
            exportOneCharacterButton.onClick.AddListener(ExportSelectedCharacterFile);
        }

        if (importOneCharacterButton != null)
        {
            importOneCharacterButton.onClick.RemoveAllListeners();
            importOneCharacterButton.onClick.AddListener(ImportCharacterFile);
        }

        if (oneCharacterDropdown != null)
        {
            oneCharacterDropdown.onValueChanged.RemoveAllListeners();
            oneCharacterDropdown.onValueChanged.AddListener(OnOneCharacterDropdownChanged);
            AddOneCharacterDropdownClickListener(oneCharacterDropdown.gameObject);
        }

        if (oneCharacterTmpDropdown != null)
        {
            oneCharacterTmpDropdown.onValueChanged.RemoveAllListeners();
            oneCharacterTmpDropdown.onValueChanged.AddListener(OnOneCharacterDropdownChanged);
            AddOneCharacterDropdownClickListener(oneCharacterTmpDropdown.gameObject);
        }

        if (openSavePanelButton != null && savePanel != null)
        {
            openSavePanelButton.onClick.RemoveAllListeners();
            openSavePanelButton.onClick.AddListener(ToggleSavePanel);
        }

    }

    public void Wire()
    {
        Button downloadButton = MainMenuSceneLookup.FindFirstButtonInScene("dowload", "download");
        if (downloadButton != null)
            owner.exportButton = downloadButton;

        Button uploadButton = MainMenuSceneLookup.FindButtonInScene("upload");
        if (uploadButton != null)
            owner.importButton = uploadButton;

        exportOneCharacterButton = MainMenuSceneLookup.FindButtonInScene("downLoadOne");
        importOneCharacterButton = MainMenuSceneLookup.FindButtonInScene("upLoadOne");
        oneCharacterDropdown = MainMenuSceneLookup.FindDropdownInScene("choisSeveFailPersonsj");
        oneCharacterTmpDropdown = MainMenuSceneLookup.FindTmpDropdownInScene("choisSeveFailPersonsj");
        openSavePanelButton = MainMenuSceneLookup.FindButtonInScene("openPanelSaveBatton");

        Transform panelTransform = MainMenuSceneLookup.FindTransformInScene("openPanelSavePanel");
        savePanel = panelTransform != null ? panelTransform.gameObject : null;

        RefreshOneCharacterDropdown();
    }

    private void ExportFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        saveManager.SaveData();

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".tall"));
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    try
                    {
                        TaruckCharacterTransfer.WriteCollection(paths[0], saveManager.saveData, Application.version);
                        TaruckImportReviewDialog.Show("Експорт", "Колекцію персонажів збережено.", "Закрити", () => { });
                    }
                    catch (System.Exception exception)
                    {
                        TaruckImportReviewDialog.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                    }
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            "AllCharacters.tall",
            TaruckImportReviewDialog.Text("Зберегти файл", "Save file"),
            TaruckImportReviewDialog.Text("Зберегти", "Save")
        );
    }

    private void ImportFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            TaruckImportReviewDialog.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tall", ".taruck-save", ".json"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

                byte[] bytes = null;
                try
                {
                    AppSaveData importedData = TaruckCharacterTransfer.ReadCollection(paths[0], out bool binary, out bytes);
                    string preview = (binary ? "Taruck v1" : TaruckImportReviewDialog.Text("Старий JSON", "Legacy JSON")) +
                        TaruckImportReviewDialog.Text("\nПерсонажів: ", "\nCharacters: ") + importedData.characters.Count;
                    for (int i = 0; i < Mathf.Min(importedData.characters.Count, 50); i++)
                        preview += "\n• " + importedData.characters[i].characterName;
                    if (importedData.characters.Count > 50)
                        preview += TaruckImportReviewDialog.Text("\n… ще ", "\n… and ") + (importedData.characters.Count - 50);
                    TaruckImportReviewDialog.Show("Перевірка імпорту", preview +
                        TaruckImportReviewDialog.Text("\n\nЗамінити всю колекцію чи додати персонажів?",
                            "\n\nReplace the whole collection or add these characters?"),
                        "Замінити все", () => ApplyCollectionImport(saveManager, importedData, false),
                        "Додати", () => ApplyCollectionImport(saveManager, importedData, true));
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка імпорту",
                        TaruckImportReviewDialog.ImportError(exception, bytes, TaruckFileType.FullSaveExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            TaruckImportReviewDialog.Text("Виберіть файл Taruck або JSON", "Select Taruck or JSON file"),
            TaruckImportReviewDialog.Text("Вибрати", "Select")
        );
    }

    private void ExportSelectedCharacterFile()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string fileName = TaruckTransferFileUtility.SafeFileName(character.characterName, "DnDCharacter") + ".tchar";

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".tchar"));
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                try
                {
                    TaruckCharacterTransfer.WriteCharacter(paths[0], character, Application.version);
                    TaruckImportReviewDialog.Show("Експорт", "Персонажа збережено.", "Закрити", () => { });
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            fileName,
            TaruckImportReviewDialog.Text("Зберегти персонажа", "Save character"),
            TaruckImportReviewDialog.Text("Зберегти", "Save")
        );
    }

    private void ImportCharacterFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            TaruckImportReviewDialog.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tchar", ".taruck-character", ".json", ".dndchar"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                byte[] bytes = null;
                try
                {
                    CharacterData importedCharacter = TaruckCharacterTransfer.ReadCharacter(paths[0], out bytes);
                    TaruckImportReviewDialog.Show("Перевірка персонажа",
                        TaruckImportReviewDialog.Text("Імпортувати персонажа «", "Import character “") +
                            importedCharacter.characterName + TaruckImportReviewDialog.Text("»?", "”?"),
                        "Додати", () => ApplyCharacterImport(saveManager, importedCharacter));
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка імпорту",
                        TaruckImportReviewDialog.ImportError(exception, bytes, TaruckFileType.CharacterExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            TaruckImportReviewDialog.Text("Виберіть файл персонажа", "Select character file"),
            TaruckImportReviewDialog.Text("Вибрати", "Select")
        );
    }

    private void ApplyCollectionImport(DndSaveManager manager, AppSaveData imported, bool merge)
    {
        if (!manager.TryImportCollection(imported, merge, out string error))
        {
            TaruckImportReviewDialog.Show("Помилка імпорту", error, "Закрити", () => { });
            return;
        }
        owner.RefreshCharacterList();
        TaruckImportReviewDialog.Show("Імпорт завершено",
            TaruckImportReviewDialog.Text("Персонажів у колекції: ", "Characters in collection: ") + manager.saveData.characters.Count,
            "Закрити", () => { });
    }

    private void ApplyCharacterImport(DndSaveManager manager, CharacterData imported)
    {
        try
        {
            string importedId = TaruckCharacterTransfer.ImportCharacter(manager, imported, Application.version);
            owner.RefreshCharacterList();
            owner.OpenImportedCharacter(importedId);
        }
        catch (System.Exception exception)
        {
            TaruckImportReviewDialog.Show("Помилка імпорту", exception.Message, "Закрити", () => { });
        }
    }

    private CharacterData GetSelectedCharacterForExport()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        if (saveManager.saveData == null || saveManager.saveData.characters == null || saveManager.saveData.characters.Count == 0)
            return null;

        if (!oneCharacterDropdownHasSelection)
            return null;

        int index = oneCharacterDropdown != null
            ? oneCharacterDropdown.value
            : oneCharacterTmpDropdown != null ? oneCharacterTmpDropdown.value : 0;

        index = Mathf.Clamp(index, 0, saveManager.saveData.characters.Count - 1);
        return saveManager.saveData.characters[index];
    }

    public void RefreshOneCharacterDropdown()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        List<string> options = new List<string>();
        bool hasCharacters = saveManager.saveData != null &&
                             saveManager.saveData.characters != null &&
                             saveManager.saveData.characters.Count > 0;
        oneCharacterDropdownHasSelection = false;

        if (hasCharacters)
        {
            foreach (CharacterData character in saveManager.saveData.characters)
                options.Add(MainMenuCharacterLabels.GetDisplayName(character));
        }
        else
        {
            options.Add(MainMenuCharacterLabels.Localize("Немає персонажів"));
        }

        if (oneCharacterDropdown != null)
        {
            oneCharacterDropdown.ClearOptions();
            oneCharacterDropdown.AddOptions(options);
            oneCharacterDropdown.SetValueWithoutNotify(0);
            oneCharacterDropdown.RefreshShownValue();
            oneCharacterDropdown.interactable = hasCharacters;
            if (hasCharacters)
                ApplyOneCharacterDropdownPlaceholder();
        }

        if (oneCharacterTmpDropdown != null)
        {
            oneCharacterTmpDropdown.ClearOptions();
            oneCharacterTmpDropdown.AddOptions(options);
            oneCharacterTmpDropdown.SetValueWithoutNotify(0);
            oneCharacterTmpDropdown.RefreshShownValue();
            oneCharacterTmpDropdown.interactable = hasCharacters;
            if (hasCharacters)
                ApplyOneCharacterDropdownPlaceholder();
        }

        UpdateOneCharacterExportButtonState();
    }

    private void UpdateOneCharacterExportButtonState()
    {
        if (exportOneCharacterButton == null)
            return;

        bool hasSelection = oneCharacterDropdownHasSelection && (oneCharacterDropdown != null
            ? oneCharacterDropdown.interactable
            : oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.interactable);

        exportOneCharacterButton.interactable = hasSelection;
    }

    private void ToggleSavePanel()
    {
        if (savePanel == null)
            return;

        bool shouldShow = !savePanel.activeSelf;
        savePanel.SetActive(shouldShow);

        if (shouldShow)
            StartCoroutine(RestoreOneCharacterDropdownCaptionNextFrame());
    }

    private IEnumerator RestoreOneCharacterDropdownCaptionNextFrame()
    {
        RestoreOneCharacterDropdownCaption();
        yield return null;
        RestoreOneCharacterDropdownCaption();
    }

    private void RestoreOneCharacterDropdownCaption()
    {
        if (!HasOneCharacterDropdownOptions())
            return;

        if (oneCharacterDropdownHasSelection)
            ApplySelectedOneCharacterDropdownCaption();
        else
            ApplyOneCharacterDropdownPlaceholder();
    }

    private void OnOneCharacterDropdownChanged(int value)
    {
        oneCharacterDropdownHasSelection = HasOneCharacterDropdownOptions();
        ApplySelectedOneCharacterDropdownCaption();
        UpdateOneCharacterExportButtonState();
    }

    private void OnOneCharacterDropdownClicked()
    {
        if (!HasOneCharacterDropdownOptions())
            return;

        oneCharacterDropdownHasSelection = true;
        ApplySelectedOneCharacterDropdownCaption();
        UpdateOneCharacterExportButtonState();
    }

    private bool HasOneCharacterDropdownOptions()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        return saveManager.saveData != null &&
               saveManager.saveData.characters != null &&
               saveManager.saveData.characters.Count > 0;
    }

    private void ApplyOneCharacterDropdownPlaceholder()
    {
        if (oneCharacterDropdown != null && oneCharacterDropdown.captionText != null)
            oneCharacterDropdown.captionText.text = MainMenuCharacterLabels.Localize("Оберіть персонажа");

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = MainMenuCharacterLabels.Localize("Оберіть персонажа");
    }

    private void ApplySelectedOneCharacterDropdownCaption()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string label = MainMenuCharacterLabels.GetDisplayName(character);

        if (oneCharacterDropdown != null && oneCharacterDropdown.captionText != null)
            oneCharacterDropdown.captionText.text = label;

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = label;
    }

    private void AddOneCharacterDropdownClickListener(GameObject dropdownObject)
    {
        if (dropdownObject == null)
            return;

        EventTrigger trigger = dropdownObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = dropdownObject.AddComponent<EventTrigger>();

        EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(delegate { OnOneCharacterDropdownClicked(); });
        trigger.triggers.Add(clickEntry);
    }

}
