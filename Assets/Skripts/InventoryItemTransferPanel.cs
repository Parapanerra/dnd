using System;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;

// Owns inventory item import/export dialogs and the verified replacement flow.
internal sealed class InventoryItemTransferPanel
{
    private InventoryItemCell owner;
    private RuntimeLocalization localization;
    private TaruckDialogPresenter dialogPresenter;
    private readonly InventoryItemFileTransfer fileTransfer = new InventoryItemFileTransfer();
    private readonly InventoryItemSerializer serializer = new InventoryItemSerializer();
    private Button exportButton;
    private Button importButton;

    internal void Initialize(InventoryItemCell cell, Button export, Button import, RuntimeLocalization textCatalog)
    {
        owner = cell;
        localization = textCatalog;
        dialogPresenter = new TaruckDialogPresenter(() => localization.CurrentLanguage);
        exportButton = export;
        importButton = import;
    }

    internal void BindButtons()
    {
        if (exportButton != null)
        {
            exportButton.onClick.RemoveListener(ExportItem);
            exportButton.onClick.AddListener(ExportItem);
        }

        if (importButton != null)
        {
            importButton.onClick.RemoveListener(ImportItem);
            importButton.onClick.AddListener(ImportItem);
        }
    }

    private string DialogText(string ukrainian)
    {
        return localization != null ? localization.Translate(ukrainian) : ukrainian;
    }
    private void ExportItem()
    {
        InventoryItemExportData data = owner.ReadCurrentData();
        if (string.IsNullOrWhiteSpace(data.itemName))
        {
            dialogPresenter.Show("Експорт",
                dialogPresenter.Text("Спочатку введіть назву предмета, щоб зберегти його у файл.",
                    "Enter an item name before saving it to a file."), "Закрити", () => { });
            return;
        }
        string fileName = TaruckTransferFileUtility.SafeFileName(data.itemName, "DnDItem") + ".titem";

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".titem"));
        FileBrowser.ShowSaveDialog(
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                try
                {
                    fileTransfer.Write(paths[0], data, Application.version);
                    dialogPresenter.Show("Експорт", "Предмет збережено.", "Закрити", () => { });
                }
                catch (Exception exception)
                {
                    dialogPresenter.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            fileName,
            DialogText("Зберегти предмет"),
            DialogText("Зберегти")
        );
    }

    private void ImportItem()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            dialogPresenter.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".titem", ".taruck-item", ".json"));
        FileBrowser.ShowLoadDialog(
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                byte[] bytes = null;
                try
                {
                    InventoryItemExportData data = fileTransfer.Read(paths[0], out bytes);
                    owner.ValidateImportedData(data);
                    InventoryItemExportData previous = owner.ReadCurrentData();
                    string current = string.IsNullOrWhiteSpace(previous.itemName)
                        ? dialogPresenter.Text("порожню комірку", "empty cell")
                        : "«" + previous.itemName + "»";
                    string preview = dialogPresenter.Text("Предмет: «", "Item: “") + data.itemName +
                        dialogPresenter.Text("»\nКатегорія: ", "”\nCategory: ") + InventoryItemCategoryView.LabelFor(data.category, localization) +
                        dialogPresenter.Text("\nОпис: ", "\nDescription: ") + data.itemDescription +
                        dialogPresenter.Text("\n\nЗамінити ", "\n\nReplace ") + current + "?";
                    dialogPresenter.Show("Перевірка предмета", preview, "Замінити",
                        () => ApplyImportedItem(data, previous));
                }
                catch (Exception exception)
                {
                    dialogPresenter.Show("Помилка імпорту",
                        dialogPresenter.ImportError(exception, bytes, TaruckFileType.ItemExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            DialogText("Виберіть предмет"),
            DialogText("Вибрати")
        );
    }

    private void ApplyImportedItem(InventoryItemExportData imported, InventoryItemExportData previous)
    {
        try
        {
            InventoryItemImportCommitter.Commit(imported, previous, owner.ValidateImportedData,
                data => owner.ApplyData(data, true),
                () =>
                {
                    DndSaveManager saveManager = owner.SaveManager;
                    saveManager?.FlushPendingSave();
                    if (saveManager != null && !string.IsNullOrEmpty(saveManager.SaveError))
                        throw new System.IO.IOException(saveManager.SaveError);
                },
                data =>
                {
                    owner.ApplyData(data, false);
                    DndSaveManager saveManager = owner.SaveManager;
                    if (saveManager != null)
                        serializer.WriteScene(saveManager.GetActiveSceneData(), owner.CellKey, data);
                });
            dialogPresenter.Show("Імпорт завершено", "Предмет завантажено.", "Закрити", () => { });
        }
        catch (Exception exception)
        {
            dialogPresenter.Show("Помилка імпорту", exception.Message, "Закрити", () => { });
        }
    }

}
