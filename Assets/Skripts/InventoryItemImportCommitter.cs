using System;

// One preview/commit boundary. Validation happens before mutation; failures restore the previous cell.
internal static class InventoryItemImportCommitter
{
    internal static void Commit(InventoryItemExportData candidate, InventoryItemExportData previous,
        Action<InventoryItemExportData> validate, Action<InventoryItemExportData> apply,
        Action flush, Action<InventoryItemExportData> restore)
    {
        validate(candidate);
        try
        {
            apply(candidate);
            flush();
        }
        catch
        {
            restore(previous);
            throw;
        }
    }
}
