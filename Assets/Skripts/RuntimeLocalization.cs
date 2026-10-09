using System.Collections;
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
    private bool initialized;

    private TranslationCatalog catalog;
    private SceneLocalizationApplier sceneApplier;

    public AppLanguage CurrentLanguage { get; private set; }
    private Coroutine syncUnityLocaleCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private void Awake()
    {
        RuntimeLocalization existing = FindExisting();
        if (existing != null && existing != this && existing.initialized)
        {
            Destroy(gameObject);
            return;
        }

        initialized = true;
        DontDestroyOnLoad(gameObject);
        catalog = new TranslationCatalog();
        CurrentLanguage = GetInitialLanguage();
        SyncUnityLocalizationPackage(CurrentLanguage);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (initialized)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static RuntimeLocalization EnsureExists()
    {
        RuntimeLocalization existing = FindExisting();
        if (existing != null)
            return existing;

        GameObject localizationObject = new GameObject("RuntimeLocalization");
        return localizationObject.AddComponent<RuntimeLocalization>();
    }

    private static RuntimeLocalization FindExisting()
    {
        RuntimeLocalization[] candidates = FindObjectsByType<RuntimeLocalization>(FindObjectsInactive.Include);
        foreach (RuntimeLocalization candidate in candidates)
            if (candidate != null && candidate.initialized)
                return candidate;
        return candidates.Length > 0 ? candidates[0] : null;
    }

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = NormalizeLanguage((int)language);
        PlayerPrefs.SetInt(AppConfig.Localization.LanguagePrefsKey, (int)CurrentLanguage);
        PlayerPrefs.Save();
        SyncUnityLocalizationPackage(CurrentLanguage);
        ApplyToScene();
    }

    private AppLanguage NormalizeLanguage(int value)
    {
        if (value < AppConfig.Localization.MinimumLanguageIndex ||
            value > AppConfig.Localization.MaximumLanguageIndex)
            return AppLanguage.Ukrainian;

        return (AppLanguage)value;
    }

    private AppLanguage GetInitialLanguage()
    {
        if (PlayerPrefs.HasKey(AppConfig.Localization.LanguagePrefsKey))
            return NormalizeLanguage(PlayerPrefs.GetInt(
                AppConfig.Localization.LanguagePrefsKey,
                (int)AppLanguage.Ukrainian));

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Ukrainian:
                return AppLanguage.Ukrainian;
            case SystemLanguage.Russian:
            case SystemLanguage.Belarusian:
                return AppLanguage.Russian;
            default:
                return AppLanguage.English;
        }
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
        return catalog.Translate(source, CurrentLanguage);
    }

    public bool HasTranslationSource(string source)
    {
        return catalog.HasTranslationSource(source);
    }

    public void ApplyToScene()
    {
        if (sceneApplier == null)
            sceneApplier = new SceneLocalizationApplier(this);
        sceneApplier.Apply();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene();
        StartCoroutine(ApplyAfterSceneLoaded());
    }

    private IEnumerator ApplyAfterSceneLoaded()
    {
        yield return null;
        ApplyToScene();
        yield return null;
        ApplyToScene();
        yield return new WaitForSecondsRealtime(AppConfig.Localization.SceneRefreshDelaySeconds);
        ApplyToScene();
    }

    public string GetSourceText(string value)
    {
        return catalog.GetSourceText(value);
    }

}

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private Text text;
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply(RuntimeLocalization service = null)
    {
        if (service != null)
            localization = service;
        if (localization == null)
            localization = RuntimeLocalization.EnsureExists();
        if (text == null)
            text = GetComponent<Text>();

        if (text == null)
            return;

        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = localization.GetSourceText(text.text);
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
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply(RuntimeLocalization service = null)
    {
        if (service != null)
            localization = service;
        if (localization == null)
            localization = RuntimeLocalization.EnsureExists();
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
            return;

        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = localization.GetSourceText(text.text);
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
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply(RuntimeLocalization service = null)
    {
        if (service != null)
            localization = service;
        if (localization == null)
            localization = RuntimeLocalization.EnsureExists();
        if (text == null)
            text = GetComponent<TextMesh>();

        if (text == null)
            return;

        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = localization.GetSourceText(text.text);
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
