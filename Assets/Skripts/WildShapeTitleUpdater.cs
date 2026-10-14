using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WildShapeTitleUpdater : MonoBehaviour
{
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<WildShapeTitleUpdater>() != null)
            return;

        GameObject updaterObject = new GameObject("WildShapeTitleUpdater");
        updaterObject.AddComponent<WildShapeTitleUpdater>();
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
        if (load_scenes.GetVisualSceneName(scene.name) != AppConfig.Scenes.WildShape)
            return;

        Apply();
        BindSpellbookButton();
    }

    public void Apply()
    {
        if (saveManager == null)
            saveManager = new RuntimeManagerResolver().SaveManager();
        if (localization == null)
            localization = new RuntimeManagerResolver().Localization();

        int number = GetWildShapeNumber();
        string title = GetWildShapeTitle(number);

        foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }

        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }

        foreach (TextMesh text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (IsWildShapeTitle(text.text))
                text.text = title;
            else if (TryGetWildShapeButtonText(text.text, out string translatedButton))
                text.text = translatedButton;
        }
    }

    private string GetWildShapeTitle(int number)
    {
        AppLanguage language = localization.CurrentLanguage;
        if (language == AppLanguage.English)
            return "Wild Shape #" + number;

        if (language == AppLanguage.Russian)
            return "\u0414\u0438\u043a\u0430\u044f \u0444\u043e\u0440\u043c\u0430 \u2116" + number;

        return "\u0414\u0438\u043a\u0430 \u0444\u043e\u0440\u043c\u0430 \u2116" + number;
    }

    private int GetWildShapeNumber()
    {
        string sceneDataName = saveManager != null
            ? saveManager.GetActiveSceneDataName()
            : SceneManager.GetActiveScene().name;

        if (string.IsNullOrWhiteSpace(sceneDataName) || sceneDataName == AppConfig.Scenes.WildShape)
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

    private bool TryGetWildShapeButtonText(string value, out string translated)
    {
        translated = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (TryReadNumberAfterPrefix(trimmed, "\u0424\u043e\u0440\u043c\u0430 ", out int number) ||
            TryReadNumberAfterPrefix(trimmed, "Form ", out number))
        {
            translated = localization.CurrentLanguage == AppLanguage.English
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

    private void BindSpellbookButton()
    {
        new SceneNavigationButtonBinder(
            new SceneNavigator().LoadSceneByName, UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            .BindAll(AppConfig.Scenes.Spellbook, button => IsSceneObject(button.gameObject),
                "spelbook", "spellbook");
    }
}
