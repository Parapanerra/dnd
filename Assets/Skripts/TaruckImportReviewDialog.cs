using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TaruckImportReviewDialog : MonoBehaviour
{
    public static string ImportError(Exception exception, byte[] bytes, TaruckFileType expected)
    {
        if (exception is UnsupportedTaruckVersionException)
            return Text("Несумісна версія файлу. Оновіть застосунок.",
                "Unsupported file version. Update the app.");

        if (bytes != null && bytes.Length >= 11 &&
            System.Text.Encoding.ASCII.GetString(bytes, 0, 8) == "TARUCKPK")
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

    private static string FileDescription(TaruckFileType type)
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
    public static string Text(string ukrainian, string english)
    {
        return RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English ? english : ukrainian;
    }

    private static string KnownText(string source)
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

    [SerializeField] private Text titleText;
    [SerializeField] private Text detailsText;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform detailsContent;
    [SerializeField] private RectTransform detailsViewport;
    [SerializeField] private ScrollRect detailsScroll;
    [SerializeField] private Button primaryButton;
    [SerializeField] private Text primaryButtonText;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private Text secondaryButtonText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text cancelButtonText;
    [SerializeField] private float minimumPanelHeight = 240f;
    [SerializeField] private float maximumPanelHeight = 380f;
    [SerializeField] private float contentPaddingHeight = 190f;

    private static TaruckImportReviewDialog active;
    private Action primary;
    private Action secondary;
    private Action queued;
    private GameObject ownedEventSystem;

    public static void Show(string title, string details, string primaryLabel, Action primary,
        string secondaryLabel = null, Action secondary = null)
    {
        var prefab = Resources.Load<TaruckImportReviewDialog>("UI/TaruckImportReviewDialog");
        if (prefab == null)
            throw new InvalidOperationException("Missing Resources/UI/TaruckImportReviewDialog prefab");

        if (active != null)
            active.Close();

        var dialog = Instantiate(prefab);
        active = dialog;
        if (EventSystem.current == null)
        {
            dialog.ownedEventSystem = new GameObject("TaruckDialogEventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
        }
        dialog.Configure(KnownText(title), KnownText(details), KnownText(primaryLabel), primary,
            KnownText(secondaryLabel), secondary);
    }

    private void Configure(string title, string details, string primaryLabel, Action primaryAction,
        string secondaryLabel, Action secondaryAction)
    {
        titleText.text = title;
        detailsText.text = details;
        primary = primaryAction;
        secondary = secondaryAction;

        primaryButtonText.text = primaryLabel;
        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(() => queued = primary ?? (() => { }));

        secondaryButton.gameObject.SetActive(secondary != null);
        secondaryButtonText.text = secondaryLabel;
        secondaryButton.onClick.RemoveAllListeners();
        if (secondary != null)
            secondaryButton.onClick.AddListener(() => queued = secondary);

        bool canCancel = primaryLabel != Text("Закрити", "Close");
        cancelButton.gameObject.SetActive(canCancel);
        cancelButtonText.text = Text("Скасувати", "Cancel");
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(Close);

        Canvas.ForceUpdateCanvases();
        float desiredHeight = Mathf.Clamp(detailsText.preferredHeight + contentPaddingHeight,
            minimumPanelHeight, maximumPanelHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
        Canvas.ForceUpdateCanvases();
        float height = Mathf.Max(detailsViewport.rect.height, detailsText.preferredHeight + 24f);
        detailsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        detailsScroll.verticalNormalizedPosition = 1f;
    }

    private void Update()
    {
        if (queued == null) return;
        Action next = queued;
        queued = null;
        Close();
        next();
    }

    private void Close()
    {
        if (active == this) active = null;
        if (ownedEventSystem != null)
        {
            Destroy(ownedEventSystem);
            ownedEventSystem = null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
        if (ownedEventSystem != null) Destroy(ownedEventSystem);
    }
}
