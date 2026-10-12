using SimpleFileBrowser;

// Owns the file format and file I/O used by item import and export.
internal sealed class InventoryItemFileTransfer
{
    private readonly TaruckTransferFileReader fileReader = new TaruckTransferFileReader();
    private readonly LegacyJsonImporter legacyImporter = new LegacyJsonImporter();

    internal void Write(string path, InventoryItemExportData data, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(
            TaruckTransferFileUtility.EnsureExtension(path, ".titem"),
            TaruckBinaryCodec.EncodeItemExport(data, version));
    }

    internal InventoryItemExportData Read(string path, out byte[] bytes)
    {
        bytes = fileReader.Read(path);
        return TaruckBinaryCodec.HasMagic(bytes)
            ? TaruckBinaryCodec.DecodeItemExport(bytes)
            : legacyImporter.Item(legacyImporter.DecodeFile(bytes));
    }
}
