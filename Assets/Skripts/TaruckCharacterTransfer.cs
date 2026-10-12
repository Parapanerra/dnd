using SimpleFileBrowser;

internal sealed class TaruckCharacterTransfer
{
    private readonly TaruckTransferFileReader fileReader = new TaruckTransferFileReader();
    private readonly LegacyJsonImporter legacyImporter = new LegacyJsonImporter();

    internal AppSaveData ReadCollection(string path, out bool binary, out byte[] bytes)
    {
        bytes = fileReader.Read(path);
        binary = TaruckBinaryCodec.HasMagic(bytes);
        return binary
            ? TaruckBinaryCodec.DecodeFullSave(bytes)
            : legacyImporter.FullSave(legacyImporter.DecodeFile(bytes));
    }

    internal CharacterData ReadCharacter(string path, out byte[] bytes)
    {
        bytes = fileReader.Read(path);
        return TaruckBinaryCodec.HasMagic(bytes)
            ? TaruckBinaryCodec.DecodeCharacterExport(bytes)
            : legacyImporter.Character(legacyImporter.DecodeFile(bytes));
    }

    internal void WriteCollection(string path, DndSaveManager manager, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(TaruckTransferFileUtility.EnsureExtension(path, ".tall"),
            TaruckBinaryCodec.EncodeFullSave(manager.GetCollectionForExport(), version));
    }

    internal void WriteCharacter(string path, CharacterData character, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(TaruckTransferFileUtility.EnsureExtension(path, ".tchar"),
            TaruckBinaryCodec.EncodeCharacterExport(character, version));
    }

}
