using UnityEngine;

internal readonly struct HealthBarValues
{
    internal readonly int MaxHealth;
    internal readonly int CurrentHealth;
    internal readonly int MaxTemporaryHealth;
    internal readonly int CurrentTemporaryHealth;

    internal HealthBarValues(int maxHealth, int currentHealth,
        int maxTemporaryHealth, int currentTemporaryHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = currentHealth;
        MaxTemporaryHealth = maxTemporaryHealth;
        CurrentTemporaryHealth = currentTemporaryHealth;
    }
}

// Owns the legacy HealthBar_<index>_* keys and the save request.
internal sealed class HealthBarPersistence
{
    private readonly DndSaveManager saveManager;
    private readonly int customIndex;
    private CharacterSceneData sceneData;

    internal HealthBarPersistence(DndSaveManager saveManager, int customIndex)
    {
        this.saveManager = saveManager;
        this.customIndex = customIndex;
    }

    internal void BindScene(CharacterSceneData data)
    {
        sceneData = data;
    }

    internal bool TryLoad(out HealthBarValues values)
    {
        values = default;
        CharacterSceneData data = ResolveScene();
        if (data == null)
            return false;

        CharacterSceneData healthData = HealthScene(data, out string healthPrefix);
        string maxKey = healthPrefix + "maxHealth";
        string currentKey = healthPrefix + "currentHealth";
        int maxHealth = 0;
        int currentHealth = 0;
        if (healthData.HasInt(maxKey) || healthData.HasInt(currentKey))
        {
            maxHealth = healthData.GetInt(maxKey, 0);
            currentHealth = healthData.GetInt(currentKey, maxHealth);
        }

        int maxTemporaryHealth = data.GetInt(GetSaveKey("maxTemporaryHealth"), 0);
        int currentTemporaryHealth = Mathf.Clamp(
            data.GetInt(GetSaveKey("currentTemporaryHealth"), maxTemporaryHealth),
            0, maxTemporaryHealth);
        values = new HealthBarValues(maxHealth, currentHealth,
            maxTemporaryHealth, currentTemporaryHealth);
        return true;
    }

    internal void Save(HealthBarValues values)
    {
        CharacterSceneData data = ResolveScene();
        if (data == null)
            return;

        CharacterSceneData healthData = HealthScene(data, out string healthPrefix);
        healthData.SetInt(healthPrefix + "maxHealth", values.MaxHealth);
        healthData.SetInt(healthPrefix + "currentHealth", values.CurrentHealth);
        data.SetInt(GetSaveKey("maxTemporaryHealth"), values.MaxTemporaryHealth);
        data.SetInt(GetSaveKey("currentTemporaryHealth"), values.CurrentTemporaryHealth);
        saveManager?.RequestSaveData();
    }

    // XPHB Wild Shape retains the character's HP; only its temporary HP belong to the form.
    // Other forms and ordinary characters keep their existing independent health storage.
    private CharacterSceneData HealthScene(CharacterSceneData form, out string prefix)
    {
        prefix = "HealthBar_" + customIndex + "_";
        if (form.GetInt("DruidSharedHealth_v1", 0) != 1 || saveManager == null) return form;
        CharacterData owner = saveManager.GetActiveCharacter();
        string sharedPrefix = form.GetString("DruidSharedHealthPrefix_v1", "");
        if (owner == null || string.IsNullOrEmpty(sharedPrefix)) return form;
        prefix = sharedPrefix;
        return owner.GetSceneData("cartaPersonaj");
    }

    private CharacterSceneData ResolveScene()
    {
        if (sceneData == null && saveManager != null)
            sceneData = saveManager.GetActiveSceneData();
        return sceneData;
    }

    private string GetSaveKey(string fieldName)
    {
        return "HealthBar_" + customIndex + "_" + fieldName;
    }
}