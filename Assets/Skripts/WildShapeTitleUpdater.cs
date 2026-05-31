using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WildShapeTitleUpdater : MonoBehaviour
{
    private static WildShapeTitleUpdater instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject updaterObject = new GameObject("WildShapeTitleUpdater");
        instance = updaterObject.AddComponent<WildShapeTitleUpdater>();
        DontDestroyOnLoad(updaterObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Contains("petsesn"))
            return;

        StartCoroutine(ApplyAfterLoad());
    }

    private IEnumerator ApplyAfterLoad()
    {
        yield return null;
        Apply();
        BindSpellbookButton();
        yield return new WaitForSecondsRealtime(0.2f);
        Apply();
        BindSpellbookButton();
    }

    public static void Apply()
    {
        int number = GetWildShapeNumber();
        string title = "Дика форма №" + number;

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (IsSceneObject(text != null ? text.gameObject : null) && IsWildShapeTitle(text.text))
                text.text = title;
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (IsSceneObject(text != null ? text.gameObject : null) && IsWildShapeTitle(text.text))
                text.text = title;
        }

        foreach (TextMesh text in Resources.FindObjectsOfTypeAll<TextMesh>())
        {
            if (IsSceneObject(text != null ? text.gameObject : null) && IsWildShapeTitle(text.text))
                text.text = title;
        }
    }

    private static int GetWildShapeNumber()
    {
        string sceneDataName = DndSaveManager.Instance != null
            ? DndSaveManager.Instance.GetActiveSceneDataName()
            : SceneManager.GetActiveScene().name;

        if (string.IsNullOrWhiteSpace(sceneDataName) || sceneDataName == "petsesn")
            return 1;

        string prefix = "petsesn ";
        if (!sceneDataName.StartsWith(prefix))
            return 1;

        return int.TryParse(sceneDataName.Substring(prefix.Length), out int index) ? index + 1 : 1;
    }

    private static bool IsWildShapeTitle(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith("Дика форма №");
    }

    private static bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }

    private static void BindSpellbookButton()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button == null || !IsSceneObject(button.gameObject) || !IsSpellbookButton(button.gameObject.name))
                continue;

            DisablePersistentOnClick(button);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => load_scenes.LoadSceneByName("spelBook"));
        }
    }

    private static bool IsSpellbookButton(string objectName)
    {
        string baseName = GetBaseName(objectName);
        return baseName == "spelbook" || baseName == "spellbook";
    }

    private static void DisablePersistentOnClick(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
    }

    private static string GetBaseName(string name)
    {
        int suffixStart = name.LastIndexOf(" (", System.StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }
}
