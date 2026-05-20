using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum AppLanguage
{
    Ukrainian,
    English,
    Russian
}

public class LocalizedIgnore : MonoBehaviour
{
}

public partial class RuntimeLocalization : MonoBehaviour
{
    public static RuntimeLocalization Instance { get; private set; }

    private const string LanguagePrefsKey = "DndAppLanguage";
    private readonly Dictionary<string, Translation> translations = new Dictionary<string, Translation>();
    private readonly Dictionary<string, string> sourceByTranslatedText = new Dictionary<string, string>();

    public AppLanguage CurrentLanguage { get; private set; }
    private Coroutine syncUnityLocaleCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildTranslations();
        CurrentLanguage = NormalizeLanguage(PlayerPrefs.GetInt(LanguagePrefsKey, (int)AppLanguage.Ukrainian));
        SyncUnityLocalizationPackage(CurrentLanguage);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static RuntimeLocalization EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject localizationObject = new GameObject("RuntimeLocalization");
        return localizationObject.AddComponent<RuntimeLocalization>();
    }

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = NormalizeLanguage((int)language);
        PlayerPrefs.SetInt(LanguagePrefsKey, (int)CurrentLanguage);
        PlayerPrefs.Save();
        SyncUnityLocalizationPackage(CurrentLanguage);
        ApplyToScene();
    }

    private AppLanguage NormalizeLanguage(int value)
    {
        if (value < 0 || value > 2)
            return AppLanguage.Ukrainian;

        return (AppLanguage)value;
    }

    private void SyncUnityLocalizationPackage(AppLanguage language)
    {
        if (!isActiveAndEnabled)
            return;

        if (syncUnityLocaleCoroutine != null)
            StopCoroutine(syncUnityLocaleCoroutine);

        syncUnityLocaleCoroutine = StartCoroutine(SyncUnityLocalizationPackageRoutine(language));
    }

    private IEnumerator SyncUnityLocalizationPackageRoutine(AppLanguage language)
    {
        yield return LocalizationSettings.InitializationOperation;

        string targetCode = GetUnityLocaleCode(language);
        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale != null &&
                (locale.Identifier.Code == targetCode || locale.Identifier.Code.StartsWith(targetCode + "-")))
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        syncUnityLocaleCoroutine = null;
    }

    private string GetUnityLocaleCode(AppLanguage language)
    {
        if (language == AppLanguage.English)
            return "en";

        if (language == AppLanguage.Russian)
            return "ru";

        return "uk";
    }

    public string Translate(string source)
    {
        if (string.IsNullOrEmpty(source) || CurrentLanguage == AppLanguage.Ukrainian)
            return source;

        string key = Normalize(source);
        if (!translations.TryGetValue(key, out Translation translation))
            return source;

        return CurrentLanguage == AppLanguage.English ? translation.English : translation.Russian;
    }

    public bool HasTranslationSource(string source)
    {
        if (string.IsNullOrEmpty(source))
            return false;

        return translations.ContainsKey(Normalize(source));
    }

    public void ApplyToScene()
    {
        foreach (ManualLocalizedText text in Resources.FindObjectsOfTypeAll<ManualLocalizedText>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            text.Apply();
        }

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedText localizedText = text.GetComponent<LocalizedText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedText>();

            localizedText.Apply();
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedTmpText localizedText = text.GetComponent<LocalizedTmpText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTmpText>();

            localizedText.Apply();
        }

        foreach (TextMesh text in Resources.FindObjectsOfTypeAll<TextMesh>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedTextMesh localizedText = text.GetComponent<LocalizedTextMesh>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTextMesh>();

            localizedText.Apply();
        }
    }

    private bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyAfterSceneLoaded());
    }

    private IEnumerator ApplyAfterSceneLoaded()
    {
        yield return null;
        ApplyToScene();
        yield return null;
        ApplyToScene();
        yield return new WaitForSecondsRealtime(0.1f);
        ApplyToScene();
    }

    private bool ShouldSkip(Text text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        if (IsEditableInputText(text) && !HasTranslationForVisibleText(text.text))
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool ShouldSkip(TMP_Text text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        if (IsEditableInputText(text) && !HasTranslationForVisibleText(text.text))
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsEditableInputText(Text text)
    {
        InputField inputField = text != null ? text.GetComponentInParent<InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

    private bool IsEditableInputText(TMP_Text text)
    {
        TMP_InputField inputField = text != null ? text.GetComponentInParent<TMP_InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

    private bool HasTranslationForVisibleText(string value)
    {
        string source = GetSourceText(value);
        return HasTranslationSource(source);
    }

    private bool ShouldSkip(TextMesh text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    public string GetSourceText(string value)
    {
        string key = Normalize(value);
        if (sourceByTranslatedText.TryGetValue(key, out string source))
            return source;

        return value;
    }

    private string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Replace('\u00A0', ' ')
            .Replace('\u200B', ' ')
            .Trim();

        while (value.Contains("  "))
            value = value.Replace("  ", " ");

        return value;
    }

    private void Add(string ukrainian, string english, string russian)
    {
        string source = Normalize(ukrainian);
        translations[source] = new Translation(english, russian);
        sourceByTranslatedText[Normalize(english)] = source;
        sourceByTranslatedText[Normalize(russian)] = source;
        sourceByTranslatedText[source] = source;
    }

    private void BuildTranslations()
    {
        Add("Ресет", "Reset", "Сброс");
        Add("Ресет HP", "Reset HP", "Сброс HP");
        Add("Підтвердить", "Confirm", "Подтвердить");
        Add("Підтвердити", "Confirm", "Подтвердить");
        Add("Файл збережених\nперсонажів", "Saved\ncharacters file", "Файл сохраненных\nперсонажей");
        Add("Файл збережених даних", "Saved data file", "Файл сохраненных данных");
        Add("Файл збережених данних", "Saved data file", "Файл сохраненных данных");
        Add("Додати нового персонажа", "Add new character", "Добавить нового персонажа");
        Add("Додать нового персонажа", "Add new character", "Добавить нового персонажа");
        Add("Автори", "Authors", "Авторы");
        Add("Про додаток", "About the app", "О приложении");
        Add("Інформація про додаток", "Application information", "Информация о приложении");
        Add("Інформація про\nдодаток", "Application\ninformation", "Информация о\nприложении");
        Add("Записник ідей\nдля персонажа", "Character idea\nnotebook", "Записник идей\nдля персонажа");
        Add("Button", "Button", "Кнопка");

        Add("ПередІсторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Передісторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Нотатки  №1", "Notes #1", "Заметки №1");
        Add("Нотатки  №2", "Notes #2", "Заметки №2");
        Add("Волося", "Hair", "Волосы");
        Add("Зріст", "Height", "Рост");
        Add("Шкіра", "Skin", "Кожа");
        Add("Очі", "Eyes", "Глаза");
        Add("Вік", "Age", "Возраст");
        Add("Вага", "Weight", "Вес");
        Add("Ім'я персонажа", "Character Name", "Имя персонажа");
        Add("Особисті Цілі", "Personal Goals", "Личные цели");
        Add("Зовнішність персонажа", "Character Appearance", "Внешность персонажа");
        Add("Слабкості", "Flaws", "Слабости");
        Add("Додаткові риси", "Additional Traits", "Дополнительные черты");
        Add("Ідеали", "Ideals", "Идеалы");
        Add("Союзники та Організації", "Allies and Organizations", "Союзники и организации");
        Add("Уподобання", "Likes", "Предпочтения");
        Add("Риси характеру", "Personality Traits", "Черты характера");
        Add("Завдання", "Quest", "Задание");

        Add("Зброя", "Weapons", "Оружие");
        Add("Броня", "Armor", "Броня");
        Add("Магія", "Magic", "Магия");
        Add("Набори", "Kits", "Наборы");
        Add("Різне", "Other", "Разное");
        Add("Прикраси", "Jewelry", "Украшения");
        Add("Введіть назву...", "Enter name...", "Введите название...");
        Add("Навички - Інфузії", "Skills - Infusions", "Навыки - Инфузии");
        LoadTranslationsFromResource();
        BuildGeneratedTranslations();
    }

    partial void BuildGeneratedTranslations();

    private void LoadTranslationsFromResource()
    {
        TextAsset asset = Resources.Load<TextAsset>("Localization");
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return;

        string[] lines = asset.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split('\t');
            if (columns.Length < 3)
                continue;

            string ukrainian = DecodeResourceText(columns[0]);
            string english = DecodeResourceText(columns[1]);
            string russian = DecodeResourceText(columns[2]);

            if (!string.IsNullOrWhiteSpace(ukrainian) &&
                !string.IsNullOrWhiteSpace(english) &&
                !string.IsNullOrWhiteSpace(russian))
            {
                Add(ukrainian, english, russian);
            }
        }
    }

    private string DecodeResourceText(string value)
    {
        return value.Replace("\\n", "\n").Trim();
    }

    private struct Translation
    {
        public readonly string English;
        public readonly string Russian;

        public Translation(string english, string russian)
        {
            English = english;
            Russian = russian;
        }
    }
}

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private Text text;

    private void Awake()
    {
        text = GetComponent<Text>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<Text>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}

public class LocalizedTmpText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}

public class LocalizedTextMesh : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TextMesh text;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<TextMesh>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}
