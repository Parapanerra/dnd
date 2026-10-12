using UnityEngine;

public class PrefabSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs; // Array of prefab objects to switch between
    [SerializeField] private string identifier; // Идентификатор для этого переключателя
    private int currentIndex = 0;
    private GameObject currentInstance;
    private DndSaveManager saveManager;

    [SerializeField] private Transform prefabParent; // The parent transform where prefabs will be instantiated

    internal void Initialize(DndSaveManager manager)
    {
        saveManager = manager;
    }

    void Start()
    {
        if (saveManager == null)
        {
            Debug.LogError("PrefabSwitcher was not initialized by the character scene controller.");
            return;
        }
        if (prefabs == null || prefabs.Length == 0)
            return;

        currentIndex = Mathf.Clamp(saveManager.GetActiveSceneData().GetInt(GetSaveKey(), 0), 0, prefabs.Length - 1);
        SpawnPrefab();
    }

    public void ShowNextPrefab()
    {
        if (prefabs == null || prefabs.Length == 0)
            return;
        currentIndex = (currentIndex + 1) % prefabs.Length;
        SaveCurrentIndex();
        SpawnPrefab();
    }

    public void ShowPreviousPrefab()
    {
        if (prefabs == null || prefabs.Length == 0)
            return;
        currentIndex = (currentIndex - 1 + prefabs.Length) % prefabs.Length;
        SaveCurrentIndex();
        SpawnPrefab();
    }

    private void SpawnPrefab()
    {
        if (prefabs == null || currentIndex < 0 || currentIndex >= prefabs.Length || prefabs[currentIndex] == null)
            return;

        // Уничтожим текущий экземпляр, если он существует
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        // Создадим новый префаб
        Transform parent = prefabParent != null ? prefabParent : transform;
        currentInstance = Instantiate(prefabs[currentIndex], parent.position, parent.rotation, parent);
        foreach (HealthBar healthBar in currentInstance.GetComponentsInChildren<HealthBar>(true))
            healthBar.Initialize(saveManager);
    }

    private string GetSaveKey()
    {
        return "SelectedPrefabIndex_" + identifier;
    }

    private void SaveCurrentIndex()
    {
        saveManager.GetActiveSceneData().SetInt(GetSaveKey(), currentIndex);
        saveManager.RequestSaveData();
    }
}
