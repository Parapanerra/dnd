using System;

internal sealed class CharacterSceneSaveService
{
    private readonly DndSaveManager manager;
    private readonly SceneSaveController fields;
    private readonly Action<CharacterData> saveIdentityAndSharedInputs;
    private readonly RestResourceRoleMapper restRoles = new RestResourceRoleMapper();

    public CharacterSceneSaveService(DndSaveManager manager, SceneSaveController fields,
        Action<CharacterData> saveIdentityAndSharedInputs)
    {
        this.manager = manager;
        this.fields = fields;
        this.saveIdentityAndSharedInputs = saveIdentityAndSharedInputs;
    }

    public CharacterSceneData Save(string characterId, string sceneName)
    {
        if (manager == null)
            return null;

        CharacterData character = manager.GetCharacter(characterId);
        CharacterSceneData scene = manager.GetSceneDataForCharacter(characterId, sceneName);
        if (character == null || scene == null)
            return null;

        fields.Save(scene, () => saveIdentityAndSharedInputs(character));
        restRoles.Save(scene);
        manager.RequestSaveData();
        return scene;
    }
}
