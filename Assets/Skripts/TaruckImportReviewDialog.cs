using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TaruckImportReviewDialog : MonoBehaviour
{
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

    private Action primary;
    private Action secondary;
    private Action queued;
    private GameObject ownedEventSystem;

    internal void AttachEventSystem(GameObject eventSystem)
    {
        ownedEventSystem = eventSystem;
    }

    internal void Configure(string title, string details, string primaryLabel, Action primaryAction,
        string secondaryLabel, Action secondaryAction, TaruckDialogText messages)
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

        bool canCancel = primaryLabel != messages.Text("Закрити", "Close");
        cancelButton.gameObject.SetActive(canCancel);
        cancelButtonText.text = messages.Text("Скасувати", "Cancel");
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

    internal void Close()
    {
        if (ownedEventSystem != null)
        {
            Destroy(ownedEventSystem);
            ownedEventSystem = null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ownedEventSystem != null) Destroy(ownedEventSystem);
    }
}
