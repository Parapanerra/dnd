using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum AppLanguage
{
    Ukrainian,
    English,
    Russian
}

public class RuntimeLocalization : MonoBehaviour
{
    private bool initialized;
    internal bool IsInitialized => initialized;

    private TranslationCatalog catalog;
    private SceneLocalizationApplier sceneApplier;
    private UnityLocaleSynchronizer localeSynchronizer;

    internal AppLanguage CurrentLanguage { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        new RuntimeManagerResolver().Localization();
    }

    private void Awake()
    {
        RuntimeLocalization existing = new RuntimeManagerResolver().FindLocalization();
        if (existing != null && existing != this && existing.initialized)
        {
            Destroy(gameObject);
            return;
        }

        initialized = true;
        DontDestroyOnLoad(gameObject);
        catalog = new TranslationCatalog();
        localeSynchronizer = new UnityLocaleSynchronizer(this);
        CurrentLanguage = GetInitialLanguage();
        localeSynchronizer.Sync(CurrentLanguage);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (initialized)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    internal void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = NormalizeLanguage((int)language);
        PlayerPrefs.SetInt(AppConfig.Localization.LanguagePrefsKey, (int)CurrentLanguage);
        PlayerPrefs.Save();
        localeSynchronizer.Sync(CurrentLanguage);
        ApplyToScene();
    }

    private AppLanguage NormalizeLanguage(int value)
    {
        switch ((AppLanguage)value)
        {
            case AppLanguage.Ukrainian:
            case AppLanguage.English:
            case AppLanguage.Russian:
                return (AppLanguage)value;
            default:
                return AppLanguage.Ukrainian;
        }
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

    internal string Translate(string source)
    {
        return catalog.Translate(source, CurrentLanguage);
    }

    internal bool HasTranslationSource(string source)
    {
        return catalog.HasTranslationSource(source);
    }

    internal void ApplyToScene()
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

    internal string GetSourceText(string value)
    {
        return catalog.GetSourceText(value);
    }

}
