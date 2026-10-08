using System;

// Prepares a fully validated replacement or merge without mutating the active save.
public static class CharacterCollectionImportService
{
    public static AppSaveData Prepare(AppSaveData current, AppSaveData imported, bool merge, string appVersion)
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
