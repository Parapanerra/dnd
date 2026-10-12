using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Keeps the Unity Localization package in sync with the app language.
internal sealed class UnityLocaleSynchronizer
{
    private readonly MonoBehaviour owner;
    private Coroutine currentSync;

    internal UnityLocaleSynchronizer(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    internal void Sync(AppLanguage language)
    {
        if (owner == null || !owner.isActiveAndEnabled)
            return;

        if (currentSync != null)
            owner.StopCoroutine(currentSync);

        currentSync = owner.StartCoroutine(SyncRoutine(language));
    }

    private IEnumerator SyncRoutine(AppLanguage language)
    {
        yield return LocalizationSettings.InitializationOperation;

        string targetCode = language == AppLanguage.English ? "en" :
            language == AppLanguage.Russian ? "ru" : "uk";
        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale != null &&
                (locale.Identifier.Code == targetCode || locale.Identifier.Code.StartsWith(targetCode + "-")))
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        currentSync = null;
    }
}
