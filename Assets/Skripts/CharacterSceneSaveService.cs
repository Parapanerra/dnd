using System;

public static class CharacterSceneSaveService
{
    public static CharacterSceneData Save(
        DndSaveManager manager,
        string characterId,
        string sceneName,
        SceneSaveController fields,
        Action<CharacterData> saveIdentityAndSharedInputs)
    {
        if (manager == null)
            return null;

        CharacterData character = manager.GetCharacter(characterId);
        CharacterSceneData scene = manager.GetSceneDataForCharacter(characterId, sceneName);
        if (character == null || scene == null)
            return null;

        fields.Save(scene, () => saveIdentityAndSharedInputs(character));
        SceneRoleLookup.SaveRestResourcePaths(scene);
        manager.RequestSaveData();
        return scene;
    }
}