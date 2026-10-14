using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class TaruckDialogPresenter
{
    private readonly TaruckDialogText messages;

    internal TaruckDialogPresenter(Func<AppLanguage> currentLanguage)
    {
        messages = new TaruckDialogText(currentLanguage);
    }

    internal string Text(string ukrainian, string english)
    {
        return messages.Text(ukrainian, english);
    }

    internal string ImportError(Exception exception, byte[] bytes, TaruckFileType expected)
    {
        return messages.ImportError(exception, bytes, expected);
    }

    internal void Show(string title, string details, string primaryLabel, Action primary,
        string secondaryLabel = null, Action secondary = null)
    {
        TaruckImportReviewDialog dialog = OpenDialog();
        dialog.Configure(messages.KnownText(title), messages.KnownText(details),
            messages.KnownText(primaryLabel), primary, messages.KnownText(secondaryLabel),
            secondary, messages);
    }

    internal void ShowImage(string title, Texture2D image)
    {
        TaruckImportReviewDialog dialog = OpenDialog();
        dialog.ConfigureImage(title, image, messages);
    }

    private TaruckImportReviewDialog OpenDialog()
    {
        TaruckImportReviewDialog prefab = Resources.Load<TaruckImportReviewDialog>("UI/TaruckImportReviewDialog");
        if (prefab == null)
            throw new InvalidOperationException("Missing Resources/UI/TaruckImportReviewDialog prefab");

        TaruckImportReviewDialog current = UnityEngine.Object.FindAnyObjectByType<TaruckImportReviewDialog>();
        if (current != null)
            current.Close();

        TaruckImportReviewDialog dialog = UnityEngine.Object.Instantiate(prefab);
        if (EventSystem.current == null)
            dialog.AttachEventSystem(new GameObject("TaruckDialogEventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule)));

        return dialog;
    }
}
