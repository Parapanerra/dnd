using System;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;

// Owns inventory item import/export dialogs and the verified replacement flow.
public sealed class InventoryItemTransferPanel
{
    private InventoryItemCell owner;
    private Button exportButton;
    private Button importButton;

    public void Initialize(InventoryItemCell cell, Button export, Button import)
    {
        owner = cell;
        exportButton = export;
        importButton = import;
    }

    public void BindButtons()
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

    private void ExportItem()
    {
        InventoryItemExportData data = owner.ReadCurrentData();
        if (string.IsNullOrWhiteSpace(data.itemName))
        {
            TaruckImportReviewDialog.Show("Експорт",
                TaruckImportReviewDialog.Text("Спочатку введіть назву предмета, щоб зберегти його у файл.",
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
                    InventoryItemSerializer.WriteFile(paths[0], data, Application.version);
                    TaruckImportReviewDialog.Show("Експорт", "Предмет збережено.", "Закрити", () => { });
                }
                catch (Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            fileName,
            "Save item",
            "Save"
        );
    }

    private void ImportItem()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            TaruckImportReviewDialog.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".titem", ".taruck-item", ".json"));
        FileBrowser.ShowLoadDialog(
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                byte[] bytes = null;
                try
                {
                    InventoryItemExportData data = InventoryItemSerializer.ReadFile(paths[0], out bytes);
                    InventoryItemExportData previous = owner.ReadCurrentData();
                    string current = string.IsNullOrWhiteSpace(previous.itemName)
                        ? TaruckImportReviewDialog.Text("порожню комірку", "empty cell")
                        : "«" + previous.itemName + "»";
                    string preview = TaruckImportReviewDialog.Text("Предмет: «", "Item: “") + data.itemName +
                        TaruckImportReviewDialog.Text("»\nКатегорія: ", "”\nCategory: ") + data.category +
                        TaruckImportReviewDialog.Text("\nОпис: ", "\nDescription: ") + data.itemDescription +
                        TaruckImportReviewDialog.Text("\n\nЗамінити ", "\n\nReplace ") + current + "?";
                    TaruckImportReviewDialog.Show("Перевірка предмета", preview, "Замінити",
                        () => ApplyImportedItem(data, previous));
                }
                catch (Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка імпорту",
                        TaruckImportReviewDialog.ImportError(exception, bytes, TaruckFileType.ItemExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckTransferFileUtility.DefaultFileBrowserPath(),
            null,
            "Select item",
            "Select"
        );
    }

    private void ApplyImportedItem(InventoryItemExportData imported, InventoryItemExportData previous)
    {
        try
        {
            owner.ApplyData(imported, true);
            // An import is an explicit operation: verify its disk write before reporting success.
            DndSaveManager saveManager = owner.SaveManager;
            saveManager?.FlushPendingSave();
            if (saveManager != null && !string.IsNullOrEmpty(saveManager.SaveError))
            {
                owner.ApplyData(previous, false);
                InventoryItemSerializer.WriteScene(saveManager.GetActiveSceneData(), owner.CellKey, previous);
                throw new System.IO.IOException(saveManager.SaveError);
            }
            TaruckImportReviewDialog.Show("Імпорт завершено", "Предмет завантажено.", "Закрити", () => { });
        }
        catch (Exception exception)
        {
            owner.ApplyData(previous, false);
            TaruckImportReviewDialog.Show("Помилка імпорту", exception.Message, "Закрити", () => { });
        }
    }

}
