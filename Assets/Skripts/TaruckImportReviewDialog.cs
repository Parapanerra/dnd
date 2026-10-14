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

    internal void ConfigureImage(string title, Texture2D image, TaruckDialogText messages)
    {
        Configure(title, "", messages.Text("Закрити", "Close"), null, null, null, messages);
        titleText.gameObject.SetActive(false);
        detailsText.gameObject.SetActive(false);
        const float photoHeight = 480f;
        float photoWidth = photoHeight * image.width / image.height;
        const float frameInset = 24f;
        const float footerHeight = 80f;
        const float frameGap = 0f;
        float photoPanelHeight = photoHeight + frameInset * 2f;
        float photoPanelWidth = photoWidth + frameInset * 2f;

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, photoPanelWidth);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, photoPanelHeight);
        panelRect.anchoredPosition = new Vector2(0f, (footerHeight + frameGap) * 0.5f);
        panelRect.localScale = Vector3.one;
        detailsViewport.anchoredPosition = Vector2.zero;
        detailsViewport.sizeDelta = new Vector2(-frameInset * 2f, -frameInset * 2f);
        Canvas.ForceUpdateCanvases();

        GameObject imageObject = new GameObject("EasterEggPhoto", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(detailsViewport, false);
        RectTransform imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        RawImage photo = imageObject.GetComponent<RawImage>();
        photo.texture = image;
        photo.raycastTarget = false;

        RectTransform actionsRect = (RectTransform)primaryButton.transform.parent;
        GameObject footerFrame = new GameObject("EasterEggButtonFrame", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        footerFrame.transform.SetParent(panelRect.parent, false);
        RectTransform footerRect = (RectTransform)footerFrame.transform;
        footerRect.anchorMin = footerRect.anchorMax = new Vector2(0.5f, 0.5f);
        footerRect.pivot = new Vector2(0.5f, 0.5f);
        footerRect.anchoredPosition = new Vector2(0f, -(photoPanelHeight + frameGap) * 0.5f);
        footerRect.sizeDelta = new Vector2(220f, footerHeight);
        footerRect.localScale = Vector3.one;
        Image panelImage = panelRect.GetComponent<Image>();
        Image footerImage = footerFrame.GetComponent<Image>();
        footerImage.sprite = panelImage.sprite;
        footerImage.type = panelImage.type;
        footerImage.color = panelImage.color;
        footerImage.raycastTarget = true;
        actionsRect.SetParent(footerRect, false);
        actionsRect.anchorMin = Vector2.zero;
        actionsRect.anchorMax = Vector2.one;
        actionsRect.pivot = new Vector2(0.5f, 0.5f);
        actionsRect.anchoredPosition = Vector2.zero;
        actionsRect.sizeDelta = new Vector2(-32f, -24f);
        detailsScroll.enabled = false;
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
