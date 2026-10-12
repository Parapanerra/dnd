using System;

// Keeps legacy scene-object name matching in one place.
internal static class SceneObjectName
{
    public static string BaseName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }

    public static bool Matches(string actualName, string expectedName)
    {
        return BaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    internal static int ToggleNumber(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        return open >= 0 && close > open &&
               int.TryParse(name.Substring(open + 1, close - open - 1), out int number)
            ? number : 0;
    }
}
