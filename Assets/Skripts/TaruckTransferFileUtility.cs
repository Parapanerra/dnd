using System;
using System.IO;

public static class TaruckTransferFileUtility
{
    public static string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
            return path;

        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = "." + extension;
        if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            return path;

        string[] oldExtensions = { ".taruck-save", ".taruck-character", ".taruck-item" };
        foreach (string oldExtension in oldExtensions)
            if (path.EndsWith(oldExtension, StringComparison.OrdinalIgnoreCase))
                return path.Substring(0, path.Length - oldExtension.Length) + extension;

        return path + extension;
    }

    public static string DefaultFileBrowserPath()
    {
        string[] candidates =
        {
            "/storage/emulated/0/Download",
            "/sdcard/Download",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        foreach (string candidate in candidates)
            if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
                return candidate;

        return null;
    }

    public static string SafeFileName(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = fallback;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
