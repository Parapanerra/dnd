using System;

public static class SaveMigrationService
{
    public static AppSaveData FromLegacy(AppSaveData data)
    {
        data = SaveDataNormalizer.Normalize(data);
        if (data.characters.Count > 0 &&
            !data.characters.Exists(character => character.id == data.lastActiveCharacterId))
            data.lastActiveCharacterId = data.characters[0].id;
        if (data.characters.Count == 0) data.lastActiveCharacterId = "";
        TaruckDataValidator.Validate(data);
        return data;
    }
}
