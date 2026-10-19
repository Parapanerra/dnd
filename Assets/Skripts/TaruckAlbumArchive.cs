using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimpleFileBrowser;
using UnityEngine;

// Transfer files carry album media in a streaming ZIP alongside the existing Taruck metadata.
internal static class TaruckAlbumArchive
{
    private const string MetadataEntry = "taruck/metadata";
    private const int MaxImageBytes = 16 * 1024 * 1024;
    private const long MaxTransferBytes = 300L * 1024 * 1024;
    internal const long AlbumTransferBudgetBytes = 250L * 1024 * 1024;
    private const string TooLargeMessage = "Файл сейва більший за 300 МБ. Його не можна експортувати або імпортувати.";

    internal static bool IsArchive(string path)
    {
        using (var file = File.OpenRead(path))
            return file.ReadByte() == 'P' && file.ReadByte() == 'K';
    }

    internal static string LocalPath(string selectedPath, out bool temporary)
    {
        temporary = !File.Exists(selectedPath);
        if (!temporary)
        {
            EnsureSize(selectedPath);
            return selectedPath;
        }
        string path = Path.Combine(Application.temporaryCachePath,
            "taruck-transfer-" + Guid.NewGuid().ToString("N"));
        try
        {
            FileBrowserHelpers.CopyFile(selectedPath, path);
            EnsureSize(path);
            return path;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    internal static int WriteCollection(string destination, AppSaveData data, string version)
    {
        AppSaveData export = TaruckBinaryCodec.DecodeFullSave(TaruckBinaryCodec.EncodeFullSave(data, version));
        var store = new AlbumMediaStore(Application.persistentDataPath);
        int skipped = ApplyAlbumBudget(export.characters, store);
        Write(destination, ".tall", export.characters.Count,
            TaruckBinaryCodec.EncodeFullSave(export, version),
            index => export.characters[index]);
        return skipped;
    }

    internal static int WriteCharacter(string destination, CharacterData character, string version)
    {
        CharacterData export = TaruckBinaryCodec.DecodeCharacterExport(
            TaruckBinaryCodec.EncodeCharacterExport(character, version));
        var store = new AlbumMediaStore(Application.persistentDataPath);
        int skipped = ApplyAlbumBudget(new[] { export }, store);
        Write(destination, ".tchar", 1,
            TaruckBinaryCodec.EncodeCharacterExport(export, version),
            _ => export);
        return skipped;
    }

    internal static long LocalAlbumBytes(AppSaveData data, AlbumMediaStore store)
    {
        long total = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (CharacterData character in data.characters)
            foreach (AlbumImageData image in character.albumImages)
                foreach (string path in new[] { image.relativeFilePath, image.thumbnailRelativePath })
                    if (seen.Add(path)) total += store.FileSize(path);
        return total;
    }

    private static int ApplyAlbumBudget(System.Collections.Generic.IEnumerable<CharacterData> characters,
        AlbumMediaStore store)
    {
        long total = 0;
        int skipped = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (CharacterData character in characters)
        {
            var included = new System.Collections.Generic.List<AlbumImageData>();
            foreach (AlbumImageData image in character.albumImages)
            {
                long added = 0;
                if (!seen.Contains(image.relativeFilePath)) added += store.FileSize(image.relativeFilePath);
                if (!seen.Contains(image.thumbnailRelativePath)) added += store.FileSize(image.thumbnailRelativePath);
                if (total + added > AlbumTransferBudgetBytes)
                {
                    skipped++;
                    continue;
                }
                total += added;
                seen.Add(image.relativeFilePath);
                seen.Add(image.thumbnailRelativePath);
                included.Add(image);
            }
            character.albumImages = included;
        }
        return skipped;
    }

    private static void Write(string destination, string extension, int characterCount,
        byte[] metadata, Func<int, CharacterData> characterAt)
    {
        string temp = Path.Combine(Application.temporaryCachePath,
            "taruck-export-" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            var store = new AlbumMediaStore(Application.persistentDataPath);
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var written = new HashSet<string>(StringComparer.Ordinal);
                using (var output = zip.CreateEntry(MetadataEntry, System.IO.Compression.CompressionLevel.NoCompression).Open())
                    output.Write(metadata, 0, metadata.Length);
                for (int c = 0; c < characterCount; c++)
                {
                    CharacterData character = characterAt(c);
                    for (int i = 0; i < character.albumImages.Count; i++)
                    {
                        AlbumImageData image = character.albumImages[i];
                        WriteImage(zip, Entry(image.relativeFilePath), image.relativeFilePath, store, written);
                        WriteImage(zip, Entry(image.thumbnailRelativePath), image.thumbnailRelativePath, store, written);
                    }
                }
            }
            EnsureSize(temp);
            string path = TaruckTransferFileUtility.EnsureExtension(destination, extension);
            if (File.Exists(path)) File.Copy(temp, path, true);
            else FileBrowserHelpers.CopyFile(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void WriteImage(ZipArchive zip, string entryName, string relativePath,
        AlbumMediaStore store, HashSet<string> written)
    {
        if (!written.Add(entryName)) return;
        byte[] bytes = store.Read(relativePath);
        if (bytes.Length > MaxImageBytes) throw new InvalidDataException("Album image is too large");
        using (var output = zip.CreateEntry(entryName, System.IO.Compression.CompressionLevel.NoCompression).Open())
            output.Write(bytes, 0, bytes.Length);
    }

    internal static AppSaveData ReadCollection(string path)
    {
        using (var zip = ZipFile.OpenRead(path))
        {
            AppSaveData data = TaruckBinaryCodec.DecodeFullSave(ReadEntry(zip, MetadataEntry, 128 * 1024 * 1024));
            for (int c = 0; c < data.characters.Count; c++)
                Attach(data.characters[c], path, zip, c);
            return data;
        }
    }

    internal static CharacterData ReadCharacter(string path)
    {
        using (var zip = ZipFile.OpenRead(path))
        {
            CharacterData data = TaruckBinaryCodec.DecodeCharacterExport(ReadEntry(zip, MetadataEntry, 128 * 1024 * 1024));
            Attach(data, path, zip, 0);
            return data;
        }
    }

    private static void Attach(CharacterData character, string path, ZipArchive zip, int c)
    {
        for (int i = 0; i < character.albumImages.Count; i++)
        {
            AlbumImageData image = character.albumImages[i];
            image.exportArchivePath = path;
            image.exportImageEntry = Entry(image.relativeFilePath);
            image.exportThumbnailEntry = Entry(image.thumbnailRelativePath);
            if (zip.GetEntry(image.exportImageEntry) == null || zip.GetEntry(image.exportThumbnailEntry) == null)
                throw new InvalidDataException("Album photo is missing from the transfer file");
        }
    }

    internal static byte[] ReadImage(string archivePath, string entryName)
    {
        using (var zip = ZipFile.OpenRead(archivePath))
            return ReadEntry(zip, entryName, MaxImageBytes);
    }

    internal static void CopyReferences(CharacterData source, CharacterData target)
    {
        for (int i = 0; i < source.albumImages.Count; i++)
        {
            AlbumImageData from = source.albumImages[i];
            AlbumImageData to = target.albumImages[i];
            to.exportImageBytes = from.exportImageBytes;
            to.exportThumbnailBytes = from.exportThumbnailBytes;
            to.exportArchivePath = from.exportArchivePath;
            to.exportImageEntry = from.exportImageEntry;
            to.exportThumbnailEntry = from.exportThumbnailEntry;
        }
    }

    private static byte[] ReadEntry(ZipArchive zip, string entryName, int maxBytes)
    {
        ZipArchiveEntry entry = zip.GetEntry(entryName);
        if (entry == null || entry.Length < 1 || entry.Length > maxBytes)
            throw new InvalidDataException("Invalid or missing album media in transfer file");
        using (var input = entry.Open())
        using (var output = new MemoryStream((int)entry.Length))
        {
            input.CopyTo(output);
            if (output.Length != entry.Length) throw new InvalidDataException("Truncated album media");
            return output.ToArray();
        }
    }

    private static string Entry(string relativePath)
    {
        using (var sha = SHA256.Create())
            return "album/" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(relativePath)))
                .Replace("-", "").ToLowerInvariant();
    }

    internal static void Cleanup(AppSaveData data)
    {
        foreach (CharacterData character in data.characters)
            Cleanup(character);
    }

    internal static void Cleanup(CharacterData character)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumImageData image in character.albumImages)
            if (!string.IsNullOrEmpty(image.exportArchivePath)) paths.Add(image.exportArchivePath);
        string cache = Path.GetFullPath(Application.temporaryCachePath) + Path.DirectorySeparatorChar;
        foreach (string path in paths)
        {
            string full = Path.GetFullPath(path);
            if (full.StartsWith(cache, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(full).StartsWith("taruck-transfer-", StringComparison.Ordinal) && File.Exists(full))
                File.Delete(full);
        }
    }

    private static void EnsureSize(string path)
    {
        if (new FileInfo(path).Length > MaxTransferBytes)
            throw new InvalidDataException(TooLargeMessage);
    }
}
