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

        string maxKey = GetSaveKey("maxHealth");
        string currentKey = GetSaveKey("currentHealth");
        int maxHealth = 0;
        int currentHealth = 0;
        if (data.HasInt(maxKey) || data.HasInt(currentKey))
        {
            maxHealth = data.GetInt(maxKey, 0);
            currentHealth = data.GetInt(currentKey, maxHealth);
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

        data.SetInt(GetSaveKey("maxHealth"), values.MaxHealth);
        data.SetInt(GetSaveKey("currentHealth"), values.CurrentHealth);
        data.SetInt(GetSaveKey("maxTemporaryHealth"), values.MaxTemporaryHealth);
        data.SetInt(GetSaveKey("currentTemporaryHealth"), values.CurrentTemporaryHealth);
        saveManager?.RequestSaveData();
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