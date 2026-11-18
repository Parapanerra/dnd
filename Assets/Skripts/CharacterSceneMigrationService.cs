// Upgrades old character-sheet data before the scene binds its controls.
internal sealed class CharacterSceneMigrationService
{
    internal void CopyLegacyFieldsIfEmpty(CharacterData character, CharacterSceneData sceneData)
    {
        if (character == null || sceneData == null ||
            sceneData.inputData.Count > 0 || sceneData.toggleData.Count > 0 ||
            sceneData.sliderData.Count > 0 || sceneData.dropdownData.Count > 0)
            return;

        sceneData.inputData.AddRange(character.inputData);
        sceneData.toggleData.AddRange(character.toggleData);
        sceneData.sliderData.AddRange(character.sliderData);
        sceneData.dropdownData.AddRange(character.dropdownData);
    }

    internal void SeparateLayOnHandsResource(CharacterData character, CharacterSceneData sceneData)
    {
        if (sceneData.HasInt("ResourceDropdownOrder_v3")) return;

        bool hasPool = int.TryParse(character.GetSharedString("CreationLayOnHandsMaximum", "0"), out int maximum) && maximum > 0;
        sceneData.SetInt("ResourceLayPool_v1", hasPool ? 1 : 0);
        if (!hasPool) return;

        const int legacyIndex = 101;
        const string key = "StableField_v1_dnd.cartapersonaj.inputfield.inputfieldspaspaw-10.03680bf3368e";
        bool hasLegacyValue = sceneData.inputData.Count > legacyIndex;
        string label = sceneData.GetString(key, hasLegacyValue ? sceneData.inputData[legacyIndex] : "");
        if (label != "Покладання рук" && label != "Lay on Hands" &&
            label != "Возложение рук" && label != "Наложение рук") return;

        Taruck.CharacterCreation.CreationResourceRecovery.Unregister(character, "rest.main.other");
        sceneData.SetString(key, "");
        if (hasLegacyValue) sceneData.inputData[legacyIndex] = "";
    }
}
