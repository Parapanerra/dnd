using System;
using System.IO;
using System.Text;

internal sealed class TaruckDialogText
{
    private readonly Func<AppLanguage> currentLanguage;

    internal TaruckDialogText(Func<AppLanguage> currentLanguage)
    {
        this.currentLanguage = currentLanguage ?? throw new ArgumentNullException(nameof(currentLanguage));
    }

    internal string ImportError(Exception exception, byte[] bytes, TaruckFileType expected)
    {
        if (exception is UnsupportedTaruckVersionException)
            return Text("Несумісна версія файлу. Оновіть застосунок.",
                "Unsupported file version. Update the app.");

        if (bytes != null && bytes.Length >= 11 && TaruckBinaryCodec.HasMagic(bytes))
        {
            TaruckFileType actual = (TaruckFileType)bytes[10];
            if (actual != expected && actual >= TaruckFileType.LocalData && actual <= TaruckFileType.ItemExport)
                return Text("Ви вибрали " + FileDescription(actual) + ". Тут потрібен " +
                        FileDescription(expected) + ".",
                    "You selected " + FileDescription(actual) + ". This screen needs " +
                        FileDescription(expected) + ".");
        }

        if (expected == TaruckFileType.FullSaveExport && exception is InvalidDataException &&
            (exception.Message == "Not a legacy full save" || exception.Message == "Invalid legacy full save"))
            return Text("Це не файл з усіма персонажами. Спробуйте імпорт одного персонажа.",
                "This is not a file with all characters. Try importing one character.");

        if (exception is InvalidDataException || exception is DecoderFallbackException || exception is FormatException)
            return Text("Файл пошкоджений або формат невідомий.",
                "The file is damaged or its format is unknown.");

        return Text("Не вдалося відкрити файл. Перевірте доступ.",
            "Could not open the file. Check access.");
    }

    private string FileDescription(TaruckFileType type)
    {
        switch (type)
        {
            case TaruckFileType.LocalData: return Text("файл даних застосунку (.taruck-data)", "an app data file (.taruck-data)");
            case TaruckFileType.FullSaveExport: return Text("файл з усіма персонажами (.tall)", "a file with all characters (.tall)");
            case TaruckFileType.CharacterExport: return Text("файл одного персонажа (.tchar)", "a file for one character (.tchar)");
            case TaruckFileType.ItemExport: return Text("файл предмета (.titem)", "an item file (.titem)");
            default: return Text("інший файл", "another file");
        }
    }

    internal string Text(string ukrainian, string english)
    {
        return currentLanguage() == AppLanguage.English ? english : ukrainian;
    }

    internal string Text(string ukrainian, string english, string russian)
    {
        switch (currentLanguage())
        {
            case AppLanguage.English: return english;
            case AppLanguage.Russian: return russian;
            default: return ukrainian;
        }
    }

    internal string KnownText(string source)
    {
        switch (source)
        {
            case "Стан сховища": return Text(source, "Storage status");
            case "Експорт": return Text(source, "Export");
            case "Колекцію персонажів збережено.": return Text(source, "Character collection saved.");
            case "Персонажа збережено.": return Text(source, "Character saved.");
            case "Предмет збережено.": return Text(source, "Item saved.");
            case "Предмет завантажено.": return Text(source, "Item imported.");
            case "Помилка експорту": return Text(source, "Export error");
            case "Помилка імпорту": return Text(source, "Import error");
            case "Перевірка імпорту": return Text(source, "Review import");
            case "Перевірка персонажа": return Text(source, "Review character");
            case "Перевірка предмета": return Text(source, "Review item");
            case "Імпорт завершено": return Text(source, "Import complete");
            case "Замінити все": return Text(source, "Replace all");
            case "Замінити": return Text(source, "Replace");
            case "Додати": return Text(source, "Add");
            case "Закрити": return Text(source, "Close");
            default: return source;
        }
    }

}
