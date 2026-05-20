using UnityEngine;

public class PrefabSwitcher : MonoBehaviour
{
    public GameObject[] prefabs; // Array of prefab objects to switch between
    public string identifier; // Идентификатор для этого переключателя
    private int currentIndex = 0;
    private GameObject currentInstance;

    public Transform prefabParent; // The parent transform where prefabs will be instantiated

    void Start()
    {
        DndSaveManager.EnsureExists();
        currentIndex = DndSaveManager.Instance.GetActiveSceneData().GetInt(GetSaveKey(), 0);
        SpawnPrefab();
    }

    public void ShowNextPrefab()
    {
        currentIndex = (currentIndex + 1) % prefabs.Length;
        SaveCurrentIndex();
        SpawnPrefab();
    }

    public void ShowPreviousPrefab()
    {
        currentIndex = (currentIndex - 1 + prefabs.Length) % prefabs.Length;
        SaveCurrentIndex();
        SpawnPrefab();
    }

    private void SpawnPrefab()
    {
        // Уничтожим текущий экземпляр, если он существует
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        // Создадим новый префаб
        currentInstance = Instantiate(prefabs[currentIndex], prefabParent.position, prefabParent.rotation, prefabParent);
    }

    private string GetSaveKey()
    {
        return "SelectedPrefabIndex_" + identifier;
    }

    private void SaveCurrentIndex()
    {
        DndSaveManager.Instance.GetActiveSceneData().SetInt(GetSaveKey(), currentIndex);
        DndSaveManager.Instance.SaveData();
    }
}
