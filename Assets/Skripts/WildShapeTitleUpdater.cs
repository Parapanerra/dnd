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

        Apply();
        BindSpellbookButton();
        StartCoroutine(ApplyAfterLoad());
    }

    private IEnumerator ApplyAfterLoad()
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            Apply();
            BindSpellbookButton();
        }
    }

    public static void Apply()
    {
        int number = GetWildShapeNumber();
        string title = GetWildShapeTitle(number);

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }

        foreach (TextMesh text in Resources.FindObjectsOfTypeAll<TextMesh>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }
    }

    private static string GetWildShapeTitle(int number)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
        if (language == AppLanguage.English)
            return "Wild Shape #" + number;

        if (language == AppLanguage.Russian)
            return "\u0414\u0438\u043a\u0430\u044f \u0444\u043e\u0440\u043c\u0430 \u2116" + number;

        return "\u0414\u0438\u043a\u0430 \u0444\u043e\u0440\u043c\u0430 \u2116" + number;
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
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        return trimmed.StartsWith("\u0414\u0438\u043a\u0430 \u0444\u043e\u0440\u043c\u0430 \u2116") ||
               trimmed.StartsWith("\u0414\u0438\u043a\u0430\u044f \u0444\u043e\u0440\u043c\u0430 \u2116") ||
               trimmed.StartsWith("Wild Shape #") ||
               trimmed.StartsWith("Р”РёРєР° С„РѕСЂРјР° в„–");
    }

    private static bool TryGetWildShapeButtonText(string value, out string translated)
    {
        translated = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (TryReadNumberAfterPrefix(trimmed, "\u0424\u043e\u0440\u043c\u0430 ", out int number) ||
            TryReadNumberAfterPrefix(trimmed, "Form ", out number))
        {
            translated = RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English
                ? "Form " + number
                : "\u0424\u043e\u0440\u043c\u0430 " + number;
            return true;
        }

        return false;
    }

    private static bool TryReadNumberAfterPrefix(string value, string prefix, out int number)
    {
        number = 0;
        if (!value.StartsWith(prefix))
            return false;

        return int.TryParse(value.Substring(prefix.Length), out number) && number > 0;
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
