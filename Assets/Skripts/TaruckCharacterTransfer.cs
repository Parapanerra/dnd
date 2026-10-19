using SimpleFileBrowser;

internal sealed class TaruckCharacterTransfer
{
    private readonly TaruckTransferFileReader fileReader = new TaruckTransferFileReader();
    private readonly LegacyJsonImporter legacyImporter = new LegacyJsonImporter();

    internal AppSaveData ReadCollection(string path, out bool binary, out byte[] bytes)
    {
        bool temporary;
        string local = TaruckAlbumArchive.LocalPath(path, out temporary);
        try
        {
            if (TaruckAlbumArchive.IsArchive(local))
            {
                bytes = null;
                binary = true;
                return TaruckAlbumArchive.ReadCollection(local);
            }
            if (temporary) System.IO.File.Delete(local);
            bytes = fileReader.Read(path);
            binary = TaruckBinaryCodec.HasMagic(bytes);
            return binary
                ? TaruckBinaryCodec.DecodeFullSave(bytes)
                : legacyImporter.FullSave(legacyImporter.DecodeFile(bytes));
        }
        catch
        {
            if (temporary && System.IO.File.Exists(local)) System.IO.File.Delete(local);
            throw;
        }
    }

    internal CharacterData ReadCharacter(string path, out byte[] bytes)
    {
        bool temporary;
        string local = TaruckAlbumArchive.LocalPath(path, out temporary);
        try
        {
            if (TaruckAlbumArchive.IsArchive(local))
            {
                bytes = null;
                return TaruckAlbumArchive.ReadCharacter(local);
            }
            if (temporary) System.IO.File.Delete(local);
            bytes = fileReader.Read(path);
            return TaruckBinaryCodec.HasMagic(bytes)
                ? TaruckBinaryCodec.DecodeCharacterExport(bytes)
                : legacyImporter.Character(legacyImporter.DecodeFile(bytes));
        }
        catch
        {
            if (temporary && System.IO.File.Exists(local)) System.IO.File.Delete(local);
            throw;
        }
    }

    internal int WriteCollection(string path, DndSaveManager manager, string version)
    {
        return TaruckAlbumArchive.WriteCollection(path, manager.GetCollectionForExport(), version);
    }

    internal int WriteCharacter(string path, CharacterData character, string version)
    {
        return TaruckAlbumArchive.WriteCharacter(path, character, version);
    }

}
