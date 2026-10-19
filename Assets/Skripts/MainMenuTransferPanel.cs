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
    private readonly TaruckCharacterTransfer characterTransfer = new TaruckCharacterTransfer();
    private readonly CharacterSingleImportService singleImport = new CharacterSingleImportService();
    private readonly MainMenuSceneLookup sceneLookup = new MainMenuSceneLookup();
    private MainMenuManager owner;
    private DndSaveManager saveManager;
    private TaruckDialogPresenter dialogPresenter;
    private MainMenuCharacterLabels characterLabels;
    private Button exportButton;
    private Button importButton;
    private Button exportOneCharacterButton;
    private Button importOneCharacterButton;
    private Button openSavePanelButton;
    private Dropdown oneCharacterDropdown;
    private TMP_Dropdown oneCharacterTmpDropdown;
    private GameObject savePanel;
    private bool oneCharacterDropdownHasSelection;

    internal void Initialize(MainMenuManager manager, DndSaveManager dataManager, MainMenuCharacterLabels labels,
        RuntimeLocalization localization)
    {
        owner = manager;
        saveManager = dataManager;
        characterLabels = labels;
        dialogPresenter = new TaruckDialogPresenter(() => localization.CurrentLanguage);
    }

    internal void HideSavePanel()
    {
        if (savePanel != null)
            savePanel.SetActive(false);
    }

    internal void BindButtons()
    {
        if (exportButton != null)
        {
            exportButton.onClick.RemoveAllListeners();
            exportButton.onClick.AddListener(ExportFile);
        }

        if (importButton != null)
        {
            importButton.onClick.RemoveAllListeners();
            importButton.onClick.AddListener(ImportFile);
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

    internal void Wire()
    {
        Button downloadButton = sceneLookup.FindFirstButtonInScene("dowload", "download");
        if (downloadButton != null)
            exportButton = downloadButton;

        Button uploadButton = sceneLookup.FindButtonInScene("upload");
        if (uploadButton != null)
            importButton = uploadButton;

        exportOneCharacterButton = sceneLookup.FindButtonInScene("downLoadOne");
        importOneCharacterButton = sceneLookup.FindButtonInScene("upLoadOne");
        oneCharacterDropdown = sceneLookup.FindDropdownInScene("choisSeveFailPersonsj");
        oneCharacterTmpDropdown = sceneLookup.FindTmpDropdownInScene("choisSeveFailPersonsj");
        openSavePanelButton = sceneLookup.FindButtonInScene("openPanelSaveBatton");

        Transform panelTransform = sceneLookup.FindTransformInScene("openPanelSavePanel");
        savePanel = panelTransform != null ? panelTransform.gameObject : null;

        RefreshOneCharacterDropdown();
    }

    private void ExportFile()
    {
        saveManager.SaveData();

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".tall"));
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    try
                    {
                        int localOnly = characterTransfer.WriteCollection(paths[0], saveManager, Application.version);
                        string message = dialogPresenter.Text("Колекцію персонажів збережено.", "Character collection saved.");
                        if (localOnly > 0)
                            message += dialogPresenter.Text("\nФото лише локально, не перенесено: ",
                                "\nPhotos kept only locally, not transferred: ") + localOnly;
                        dialogPresenter.Show("Експорт", message, "Закрити", () => { });
                    }
                    catch (System.Exception exception)
                    {
                        dialogPresenter.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                    }
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            "AllCharacters.tall",
            dialogPresenter.Text("Зберегти файл", "Save file"),
            dialogPresenter.Text("Зберегти", "Save")
        );
    }

    private void ImportFile()
    {

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            dialogPresenter.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tall", ".taruck-save", ".json"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

                byte[] bytes = null;
                try
                {
                    AppSaveData importedData = characterTransfer.ReadCollection(paths[0], out bool binary, out bytes);
                    string preview = (binary ? "Taruck v1" : dialogPresenter.Text("Старий JSON", "Legacy JSON")) +
                        dialogPresenter.Text("\nПерсонажів: ", "\nCharacters: ") + importedData.characters.Count;
                    for (int i = 0; i < Mathf.Min(importedData.characters.Count, 50); i++)
                        preview += "\n• " + importedData.characters[i].characterName;
                    if (importedData.characters.Count > 50)
                        preview += dialogPresenter.Text("\n… ще ", "\n… and ") + (importedData.characters.Count - 50);
                    dialogPresenter.Show("Перевірка імпорту", preview +
                        dialogPresenter.Text("\n\nЗамінити всю колекцію чи додати персонажів?",
                            "\n\nReplace the whole collection or add these characters?"),
                        "Замінити все", () => ApplyCollectionImport(saveManager, importedData, false),
                        "Додати", () => ApplyCollectionImport(saveManager, importedData, true),
                        () => TaruckAlbumArchive.Cleanup(importedData));
                }
                catch (System.Exception exception)
                {
                    dialogPresenter.Show("Помилка імпорту",
                        dialogPresenter.ImportError(exception, bytes, TaruckFileType.FullSaveExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            dialogPresenter.Text("Виберіть файл Taruck або JSON", "Select Taruck or JSON file"),
            dialogPresenter.Text("Вибрати", "Select")
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
                    int localOnly = characterTransfer.WriteCharacter(paths[0], character, Application.version);
                    string message = dialogPresenter.Text("Персонажа збережено.", "Character saved.");
                    if (localOnly > 0)
                        message += dialogPresenter.Text("\nФото лише локально, не перенесено: ",
                            "\nPhotos kept only locally, not transferred: ") + localOnly;
                    dialogPresenter.Show("Експорт", message, "Закрити", () => { });
                }
                catch (System.Exception exception)
                {
                    dialogPresenter.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            fileName,
            dialogPresenter.Text("Зберегти персонажа", "Save character"),
            dialogPresenter.Text("Зберегти", "Save")
        );
    }

    private void ImportCharacterFile()
    {

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            dialogPresenter.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tchar", ".taruck-character", ".json", ".dndchar"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                byte[] bytes = null;
                try
                {
                    CharacterData importedCharacter = characterTransfer.ReadCharacter(paths[0], out bytes);
                    dialogPresenter.Show("Перевірка персонажа",
                        dialogPresenter.Text("Імпортувати персонажа «", "Import character “") +
                            importedCharacter.characterName + dialogPresenter.Text("»?", "”?"),
                        "Додати", () => ApplyCharacterImport(saveManager, importedCharacter),
                        onDismissed: () => TaruckAlbumArchive.Cleanup(importedCharacter));
                }
                catch (System.Exception exception)
                {
                    dialogPresenter.Show("Помилка імпорту",
                        dialogPresenter.ImportError(exception, bytes, TaruckFileType.CharacterExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            dialogPresenter.Text("Виберіть файл персонажа", "Select character file"),
            dialogPresenter.Text("Вибрати", "Select")
        );
    }

    private void ApplyCollectionImport(DndSaveManager manager, AppSaveData imported, bool merge)
    {
        try
        {
            if (!manager.TryImportCollection(imported, merge, out string error))
            {
                dialogPresenter.Show("Помилка імпорту", error, "Закрити", () => { });
                return;
            }
            owner.RefreshCharacterList();
            dialogPresenter.Show("Імпорт завершено",
                dialogPresenter.Text("Персонажів у колекції: ", "Characters in collection: ") + manager.GetCharacterCount(),
                "Закрити", () => { });
        }
        finally { TaruckAlbumArchive.Cleanup(imported); }
    }

    private void ApplyCharacterImport(DndSaveManager manager, CharacterData imported)
    {
        try
        {
            string importedId = singleImport.Import(manager, imported, Application.version);
            owner.RefreshCharacterList();
            owner.OpenImportedCharacter(importedId);
        }
        catch (System.Exception exception)
        {
            dialogPresenter.Show("Помилка імпорту", exception.Message, "Закрити", () => { });
        }
        finally { TaruckAlbumArchive.Cleanup(imported); }
    }

    private CharacterData GetSelectedCharacterForExport()
    {
        if (saveManager.GetCharacterCount() == 0)
            return null;

        if (!oneCharacterDropdownHasSelection)
            return null;

        int index = oneCharacterDropdown != null
            ? oneCharacterDropdown.value
            : oneCharacterTmpDropdown != null ? oneCharacterTmpDropdown.value : 0;

        index = Mathf.Clamp(index, 0, saveManager.GetCharacterCount() - 1);
        return saveManager.GetCharacterAt(index);
    }

    internal void RefreshOneCharacterDropdown()
    {
        List<string> options = new List<string>();
        bool hasCharacters = saveManager.GetCharacterCount() > 0;
        oneCharacterDropdownHasSelection = false;

        if (hasCharacters)
        {
            foreach (CharacterData character in saveManager.GetCharactersSnapshot())
                options.Add(characterLabels.GetDisplayName(character));
        }
        else
        {
            options.Add(characterLabels.Localize("Немає персонажів"));
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
        return saveManager.GetCharacterCount() > 0;
    }

    private void ApplyOneCharacterDropdownPlaceholder()
    {
        if (oneCharacterDropdown != null && oneCharacterDropdown.captionText != null)
            oneCharacterDropdown.captionText.text = characterLabels.Localize("Оберіть персонажа");

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = characterLabels.Localize("Оберіть персонажа");
    }

    private void ApplySelectedOneCharacterDropdownCaption()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string label = characterLabels.GetDisplayName(character);

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
