internal static class LocalizedTextSource
{
    internal static string Resolve(string sourceText, string visibleText, RuntimeLocalization localization)
    {
        string visibleSource = localization.GetSourceText(visibleText);
        if (localization.HasTranslationSource(visibleSource))
            return visibleSource;

        return string.IsNullOrEmpty(sourceText) ? visibleSource : sourceText;
    }
}
