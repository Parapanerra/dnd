using System;

internal sealed class SaveMigrationService
{
    private readonly SaveDataNormalizer normalizer = new SaveDataNormalizer();

    internal AppSaveData FromLegacy(AppSaveData data)
    {
        data = normalizer.Normalize(data);
        if (data.characters.Count > 0 &&
            !data.characters.Exists(character => character.id == data.lastActiveCharacterId))
            data.lastActiveCharacterId = data.characters[0].id;
        if (data.characters.Count == 0) data.lastActiveCharacterId = "";
        TaruckDataValidator.Validate(data);
        return data;
    }
}
