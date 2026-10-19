using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Owns preparation and durable replacement of an imported collection.
internal sealed class CharacterCollectionImportService
{
    private readonly string filePath;
    private readonly string appVersion;
    private readonly TaruckLocalRepository repository;

    internal CharacterCollectionImportService(TaruckLocalRepository repository, string filePath, string appVersion)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        this.appVersion = appVersion ?? throw new ArgumentNullException(nameof(appVersion));
    }

    internal AppSaveData Import(AppSaveData current, AppSaveData imported, bool merge)
    {
        AppSaveData candidate = Prepare(current, imported, merge);
        var store = new AlbumMediaStore(Application.persistentDataPath);
        var created = new List<AlbumImageData>();
        try
        {
            foreach (CharacterData character in candidate.characters)
                RestoreAlbumMedia(character, store, created);
            if (File.Exists(filePath))
                File.Copy(filePath, filePath + ".preimport.bak", true);
            repository.Save(filePath, candidate, appVersion);
            return candidate;
        }
        catch
        {
            foreach (AlbumImageData image in created)
                store.Delete(image);
            throw;
        }
    }

    internal AppSaveData Prepare(AppSaveData current, AppSaveData imported, bool merge)
    {
        TaruckDataValidator.Validate(imported);

        AppSaveData candidate = TaruckBinaryCodec.DecodeFullSave(
            TaruckBinaryCodec.EncodeFullSave(merge ? current : imported, appVersion));
        if (!merge)
            for (int i = 0; i < imported.characters.Count; i++)
                TaruckAlbumArchive.CopyReferences(imported.characters[i], candidate.characters[i]);
        if (merge)
        {
            foreach (CharacterData source in imported.characters)
            {
                CharacterData copy = TaruckBinaryCodec.DecodeCharacterExport(
                    TaruckBinaryCodec.EncodeCharacterExport(source, appVersion));
                TaruckAlbumArchive.CopyReferences(source, copy);
                if (candidate.characters.Exists(item => item.id == copy.id))
                    copy.id = Guid.NewGuid().ToString();
                candidate.characters.Add(copy);
            }

            if (string.IsNullOrEmpty(candidate.lastActiveCharacterId) && candidate.characters.Count > 0)
                candidate.lastActiveCharacterId = candidate.characters[0].id;
        }

        TaruckDataValidator.Validate(candidate);
        return candidate;
    }

    private static void RestoreAlbumMedia(CharacterData character, AlbumMediaStore store, List<AlbumImageData> created)
    {
        for (int i = character.albumImages.Count - 1; i >= 0; i--)
        {
            AlbumImageData old = character.albumImages[i];
            if (old.exportArchivePath != null)
            {
                old.exportImageBytes = TaruckAlbumArchive.ReadImage(old.exportArchivePath, old.exportImageEntry);
                old.exportThumbnailBytes = TaruckAlbumArchive.ReadImage(old.exportArchivePath, old.exportThumbnailEntry);
            }
            if (old.exportImageBytes == null || old.exportThumbnailBytes == null)
            {
                // Older exports contained only paths from the other device.
                if (!System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,
                    old.relativeFilePath.Replace('/', System.IO.Path.DirectorySeparatorChar))))
                    character.albumImages.RemoveAt(i);
                continue;
            }
            AlbumImageData restored = store.Add(character.id, old.exportImageBytes,
                old.exportThumbnailBytes, old.width, old.height);
            created.Add(restored);
            restored.title = old.title;
            restored.description = old.description;
            restored.createdAtUtcTicks = old.createdAtUtcTicks;
            restored.sortOrder = old.sortOrder;
            restored.pageIndex = old.pageIndex;
            character.albumImages[i] = restored;
        }
    }
}
