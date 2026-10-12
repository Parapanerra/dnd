using System;
using System.IO;

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
        if (File.Exists(filePath))
            File.Copy(filePath, filePath + ".preimport.bak", true);
        repository.Save(filePath, candidate, appVersion);
        return candidate;
    }

    internal AppSaveData Prepare(AppSaveData current, AppSaveData imported, bool merge)
    {
        TaruckDataValidator.Validate(imported);

        AppSaveData candidate = TaruckBinaryCodec.DecodeFullSave(
            TaruckBinaryCodec.EncodeFullSave(merge ? current : imported, appVersion));
        if (merge)
        {
            foreach (CharacterData source in imported.characters)
            {
                CharacterData copy = TaruckBinaryCodec.DecodeCharacterExport(
                    TaruckBinaryCodec.EncodeCharacterExport(source, appVersion));
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
}
