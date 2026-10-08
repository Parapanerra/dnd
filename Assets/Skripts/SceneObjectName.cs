using System;

// Keeps legacy scene-object name matching in one place.
public static class SceneObjectName
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
}
