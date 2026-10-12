using System;

// Formats names for menu rows and the transfer dropdown without changing saved names.
internal sealed class MainMenuCharacterLabels
{
    private readonly RuntimeLocalization localization;

    internal MainMenuCharacterLabels(RuntimeLocalization localization)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    internal string GetDisplayName(CharacterData character, string emptyFallback = "Новий персонаж")
    {
        string rawName = character != null ? character.characterName : "";
        if (TryGetDefaultCharacterNumber(rawName, out int number))
        {
            string localizedBase = Localize("Новий персонаж");
            return number > 0 ? localizedBase + " " + number : localizedBase;
        }

        return string.IsNullOrWhiteSpace(rawName) ? Localize(emptyFallback) : rawName;
    }

    internal string Localize(string source)
    {
        return localization.Translate(source);
    }

    private static bool TryGetDefaultCharacterNumber(string name, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(name))
            return true;

        string normalized = name.Trim();
        string[] bases = { "Новий персонаж", "New character", "Новый персонаж" };
        foreach (string baseName in bases)
        {
            if (string.Equals(normalized, baseName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!normalized.StartsWith(baseName + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = normalized.Substring(baseName.Length).Trim();
            if (int.TryParse(suffix, out number))
                return true;
        }

        return false;
    }
}
