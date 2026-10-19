using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Unity boundary for the character album. Files and metadata have separate owners.
public sealed class AlbumSceneController : MonoBehaviour
{
    private const int ImagesPerPage = 40;
    private const int GridColumns = 4;
    private const float ProgressFillWidth = 405f;
    private static readonly Color AlbumTextColor = new Color32(255, 154, 0, 255);
    [SerializeField] private Button addImageButton;
    [SerializeField] private Button closeViewerButton;
    [SerializeField] private Button previousImageButton;
    [SerializeField] private Button nextImageButton;
    [SerializeField] private Sprite deleteButtonSprite;
    [SerializeField] private Sprite gridLineSprite;
    [SerializeField] private RectTransform gridContent;
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private Text imageCount;
    [SerializeField] private GameObject importProgress;
    [SerializeField] private Image importProgressFill;
    [SerializeField] private Text importProgressText;
    [SerializeField] private GameObject imageViewer;
    [SerializeField] private RectTransform viewerFrame;
    [SerializeField] private RawImage fullImage;

    private readonly List<Texture2D> visibleTextures = new List<Texture2D>();
    private DndSaveManager saveManager;
    private CharacterData character;
    private AlbumMediaStore mediaStore;
    private AlbumImageImportService imageImport;
    private RuntimeLocalization localization;
    private AppLanguage displayedLanguage;
    private Texture2D viewerTexture;
    private int currentPage;
    private int selectedIndex = -1;
    private Coroutine importRoutine;
    private Coroutine failedMessageRoutine;
    private int importProcessed;
    private int importTotal;

    private void Start()
    {
        saveManager = new RuntimeManagerResolver().SaveManager();
        character = saveManager.EnsureActiveCharacter();
        mediaStore = new AlbumMediaStore(Application.persistentDataPath);
        localization = new RuntimeManagerResolver().Localization();
        if (character == null || addImageButton == null || gridContent == null ||
            imageViewer == null || viewerFrame == null || fullImage == null ||
            importProgress == null || importProgressFill == null || importProgressText == null)
        {
            Debug.LogError("Album scene is missing required references.");
            enabled = false;
            return;
        }
        imageImport = new AlbumImageImportService(character, mediaStore,
            saveManager.SaveData, () => saveManager.SaveError, ImagesPerPage);
        addImageButton.onClick.AddListener(PickImage);
        closeViewerButton.onClick.AddListener(CloseViewer);
        previousImageButton.onClick.AddListener(() => OpenImage(selectedIndex - 1));
        nextImageButton.onClick.AddListener(() => OpenImage(selectedIndex + 1));
        new ScenePageButtonBinder(SwitchPage).Bind();
        new SceneNavigationButtonBinder(new SceneNavigator().LoadSceneByName)
            .BindFirst(AppConfig.Scenes.Inventory, "inventar");
        imageViewer.SetActive(false);
        importProgress.SetActive(false);
        ConfigureGrid();
        ApplyLanguage();
        Refresh();
    }

    private void Update()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            Input.GetKeyDown(KeyCode.V))
            PasteImage();
#endif
        if (localization != null && displayedLanguage != localization.CurrentLanguage)
        {
            ApplyLanguage();
            Refresh();
        }
    }

    private void ConfigureGrid()
    {
        GridLayoutGroup grid = gridContent.GetComponent<GridLayoutGroup>();
        if (grid == null) return;
        Canvas.ForceUpdateCanvases();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridColumns;
        float usable = gridContent.rect.width - grid.padding.left - grid.padding.right -
            grid.spacing.x * (GridColumns - 1);
        float width = Mathf.Max(1f, Mathf.Floor(usable / GridColumns));
        grid.cellSize = new Vector2(width, Mathf.Round(width * 1.23f));
    }

    private void ApplyLanguage()
    {
        displayedLanguage = localization.CurrentLanguage;
        Text title = transform.Find("AlbumTitle")?.GetComponent<Text>();
        if (title != null) title.text = localization.Translate("Альбом персонажа");
        Text addLabel = addImageButton.GetComponentInChildren<Text>(true);
        if (addLabel != null) addLabel.text = localization.Translate("+ Додати зображення");
        Text hint = emptyHint.GetComponent<Text>();
        if (hint != null) hint.text = localization.Translate("Немає зображень. Додайте перше.");
        Text closeLabel = closeViewerButton.GetComponentInChildren<Text>(true);
        if (closeLabel != null) closeLabel.text = localization.Translate("Закрити");
        foreach (Text text in GetComponentsInChildren<Text>(true))
            text.color = AlbumTextColor;
        if (importProgress.activeSelf) UpdateImportProgress();
    }

    private void SwitchPage(int index)
    {
        if (failedMessageRoutine != null)
        {
            StopCoroutine(failedMessageRoutine);
            failedMessageRoutine = null;
        }
        currentPage = Mathf.Clamp(index, 0, 4);
        CloseViewer();
        Refresh();
    }

    private void PickImage()
    {
        if (importRoutine != null) return;
        int selectedPage = currentPage;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try { OnSelectedPaths(WindowsNativeImagePicker.PickMany(localization.Translate("Виберіть зображення")), selectedPage); }
        catch (Exception error) { ShowError(error); }
#else
        string title = localization.Translate("Виберіть зображення");
        if (NativeGallery.CanSelectMultipleFilesFromGallery())
            NativeGallery.GetImagesFromGallery(paths => OnSelectedPaths(paths, selectedPage), title, "image/*");
        else
            NativeGallery.GetImageFromGallery(path => OnSelectedPaths(new[] { path }, selectedPage), title, "image/*");
#endif
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private void PasteImage()
    {
        if (importRoutine != null) return;
        string temporaryPath = null;
        try
        {
            if (!WindowsClipboardImage.TryRead(out byte[] bytes, out string[] paths))
            {
                imageCount.text = localization.Translate("У буфері немає зображення.");
                return;
            }
            if (paths != null)
            {
                OnSelectedPaths(paths, currentPage);
                return;
            }
            temporaryPath = Path.Combine(Application.temporaryCachePath,
                "taruck-clipboard-" + Guid.NewGuid().ToString("N") + ".png");
            File.WriteAllBytes(temporaryPath, bytes);
            OnSelectedPaths(new[] { temporaryPath }, currentPage, temporaryPath);
            temporaryPath = null; // The import coroutine owns the temporary file now.
        }
        catch (Exception error) { ShowError(error); }
        finally
        {
            if (temporaryPath != null && File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
#endif

    private void OnSelectedPaths(string[] paths, int selectedPage, string temporaryPath = null)
    {
        if (paths == null || paths.Length == 0 || importRoutine != null) return;
        string[] selected = Array.FindAll(paths, path => !string.IsNullOrEmpty(path));
        if (selected.Length == 0) return;
        if (failedMessageRoutine != null)
        {
            StopCoroutine(failedMessageRoutine);
            failedMessageRoutine = null;
        }
        importRoutine = StartCoroutine(ImportImages(selected, selectedPage, temporaryPath));
    }

    private IEnumerator ImportImages(string[] paths, int selectedPage, string temporaryPath)
    {
        long beforeBytes = TaruckAlbumArchive.LocalAlbumBytes(saveManager.GetCollectionForExport(), mediaStore);
        importProcessed = 0;
        importTotal = paths.Length;
        importProgressFill.type = Image.Type.Sliced;
        importProgress.SetActive(true);
        importProgress.transform.SetAsLastSibling();
        UpdateImportProgress();
        yield return null;

        int failed = 0;
        try
        {
            foreach (string path in paths)
            {
                try { imageImport.Add(path, selectedPage); }
                catch (Exception error)
                {
                    failed++;
                    Debug.LogError("Album: " + error.Message);
                }
                importProcessed++;
                UpdateImportProgress();
                yield return null;
            }
        }
        finally
        {
            if (temporaryPath != null && File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        Refresh();
        importProgress.SetActive(false);
        importRoutine = null;
        long afterBytes = TaruckAlbumArchive.LocalAlbumBytes(saveManager.GetCollectionForExport(), mediaStore);
        if (afterBytes > TaruckAlbumArchive.AlbumTransferBudgetBytes && afterBytes > beforeBytes)
        {
            var dialog = new TaruckDialogPresenter(() => localization.CurrentLanguage);
            dialog.ShowCompact(
                dialog.Text("Ліміт перенесення фото", "Photo transfer limit"),
                dialog.Text("Фото можна додавати й далі. У файл перенесення потрапить лише частина фото в межах 250 МБ; решта залишиться локально на цьому пристрої.",
                    "You can keep adding photos. Only photos within the 250 MB budget will be included in the transfer file; the rest will stay on this device."),
                dialog.Text("Закрити", "Close"), () => { });
        }
        if (failed > 0)
            failedMessageRoutine = StartCoroutine(ShowFailedCountTemporarily(failed, paths.Length));
    }

    private IEnumerator ShowFailedCountTemporarily(int failed, int total)
    {
        imageCount.text = string.Format(localization.Translate("Не додано: {0}/{1}"), failed, total);
        yield return new WaitForSecondsRealtime(3f);
        UpdateImageCount(PageImages().Count);
        failedMessageRoutine = null;
    }

    private void UpdateImportProgress()
    {
        float progress = importTotal > 0 ? (float)importProcessed / importTotal : 0f;
        importProgressFill.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, ProgressFillWidth * progress);
        importProgressFill.enabled = importProcessed > 0;
        importProgressText.text = string.Format(localization.Translate("Завантажено: {0}/{1}"),
            importProcessed, importTotal);
    }

    private void Refresh()
    {
        foreach (Transform child in gridContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        foreach (Texture2D texture in visibleTextures) DestroyRuntime(texture);
        visibleTextures.Clear();
        List<AlbumImageData> images = PageImages();
        for (int index = 0; index < images.Count; index++)
        {
            AlbumImageData image = images[index];
            try
            {
                Texture2D texture = Decode(mediaStore.Read(image.thumbnailRelativePath));
                visibleTextures.Add(texture);
                AddTile(image, texture, index);
            }
            catch (Exception error) { Debug.LogError("Album thumbnail: " + error.Message); }
        }
        AddGridLines(images.Count);
        emptyHint.SetActive(images.Count == 0);
        UpdateImageCount(images.Count);
    }

    private void UpdateImageCount(int count)
    {
        imageCount.text = string.Format(localization.Translate("Зображень: {0}/{1} · сторінка {2} / 5"),
            count, Mathf.Max(ImagesPerPage, count), currentPage + 1);
    }

    private List<AlbumImageData> PageImages() => character.albumImages
        .Where(x => x.pageIndex == currentPage).OrderBy(x => x.sortOrder).ToList();

    private void AddTile(AlbumImageData image, Texture2D texture, int index)
    {
        var tile = new GameObject("AlbumImage_" + image.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        tile.transform.SetParent(gridContent, false);
        tile.GetComponent<Image>().color = new Color(.035f, .03f, .045f, 1f);
        tile.GetComponent<Button>().onClick.AddListener(() => OpenImage(index));
        var preview = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        preview.transform.SetParent(tile.transform, false);
        var rect = (RectTransform)preview.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var raw = preview.GetComponent<RawImage>(); raw.texture = texture; raw.raycastTarget = false;
        var aspect = preview.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = (float)texture.width / texture.height;
        AddDeleteButton(tile.transform, image.id);
    }

    private void AddGridLines(int imageCount)
    {
        if (gridLineSprite == null) return;
        var grid = gridContent.GetComponent<GridLayoutGroup>();
        var frame = gridContent.parent.parent.Find("AlbumGridFrame") as RectTransform;
        if (frame == null) return;
        foreach (Transform child in frame)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (imageCount == 0) return;
        int rows = Mathf.CeilToInt((float)imageCount / GridColumns);
        float lineWidth = gridLineSprite.rect.width / 4f;
        const float edge = 14f;
        float frameWidth = frame.rect.width;
        float frameHeight = frame.rect.height;
        float lineHeight = frameHeight - edge * 2f;
        for (int column = 1; column < GridColumns; column++)
            AddGridLine(frame, "VerticalGridLine", new Vector2(grid.padding.left +
                grid.cellSize.x * column + grid.spacing.x * (column - .5f), -edge),
                new Vector2(lineWidth, lineHeight), false);
        AddGridLine(gridContent, "TopGridLine", new Vector2(frameWidth * .5f, -grid.padding.top),
            new Vector2(lineWidth, frameWidth - edge * 2f), true);
        for (int row = 1; row < rows; row++)
            AddGridLine(gridContent, "HorizontalGridLine", new Vector2(frameWidth * .5f,
                -grid.padding.top - grid.cellSize.y * row - grid.spacing.y * (row - .5f)),
                new Vector2(lineWidth, frameWidth - edge * 2f), true);
        AddGridLine(gridContent, "BottomGridLine", new Vector2(frameWidth * .5f,
            -grid.padding.top - grid.cellSize.y * rows - grid.spacing.y * (rows - 1)),
            new Vector2(lineWidth, frameWidth - edge * 2f), true);
    }

    private void AddGridLine(Transform parent, string name, Vector2 position, Vector2 size, bool horizontal)
    {
        var line = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(LayoutElement));
        line.transform.SetParent(parent, false);
        if (horizontal) line.transform.SetAsFirstSibling();
        var rect = (RectTransform)line.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = horizontal ? new Vector2(.5f, .5f) : new Vector2(.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        if (horizontal) rect.localRotation = Quaternion.Euler(0f, 0f, 90f);
        line.GetComponent<LayoutElement>().ignoreLayout = true;
        var graphic = line.GetComponent<Image>();
        graphic.sprite = gridLineSprite;
        graphic.type = Image.Type.Sliced;
        graphic.pixelsPerUnitMultiplier = 4f;
        graphic.raycastTarget = false;
    }

    private void AddDeleteButton(Transform parent, string imageId)
    {
        var delete = new GameObject("DeleteImageButton", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        delete.transform.SetParent(parent, false);
        var rect = (RectTransform)delete.transform;
        rect.anchorMin = Vector2.one; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one;
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(52, 52);
        Image buttonImage = delete.GetComponent<Image>();
        buttonImage.sprite = deleteButtonSprite;
        buttonImage.color = Color.white;
        buttonImage.preserveAspect = true;
        delete.GetComponent<Button>().onClick.AddListener(() => ConfirmDelete(imageId));
    }

    private void ConfirmDelete(string imageId)
    {
        new TaruckDialogPresenter(() => localization.CurrentLanguage).ShowCompact(
            localization.Translate("Видалити зображення?"),
            localization.Translate("Зображення буде видалено з альбому цього персонажа."),
            localization.Translate("Видалити"), () => DeleteImage(imageId));
    }

    private void DeleteImage(string imageId)
    {
        AlbumImageData image = character.albumImages.Find(x => x.id == imageId);
        if (image == null) return;
        var before = new List<AlbumImageData>(character.albumImages);
        int[] oldOrder = before.Select(x => x.sortOrder).ToArray();
        character.albumImages.Remove(image);
        List<AlbumImageData> remaining = PageImages();
        for (int i = 0; i < remaining.Count; i++) remaining[i].sortOrder = i;
        saveManager.SaveData();
        if (!string.IsNullOrEmpty(saveManager.SaveError))
        {
            character.albumImages.Clear();
            character.albumImages.AddRange(before);
            for (int i = 0; i < before.Count; i++) before[i].sortOrder = oldOrder[i];
            ShowError(new IOException(saveManager.SaveError));
            return;
        }
        try { mediaStore.Delete(image); }
        catch (IOException error) { Debug.LogWarning("Album cleanup: " + error.Message); }
        CloseViewer();
        Refresh();
    }

    internal void ResetAlbum()
    {
        if (character == null || mediaStore == null) return;
        if (!saveManager.CanWrite)
        {
            ShowError(new IOException(saveManager.SaveError ?? "Local storage is unavailable"));
            return;
        }
        try
        {
            foreach (Exception error in new AlbumResetService().Reset(character, mediaStore,
                         saveManager.SaveData, () => saveManager.SaveError))
                Debug.LogWarning("Album cleanup: " + error.Message);
        }
        catch (Exception error)
        {
            ShowError(error);
            return;
        }
        CloseViewer();
        Refresh();
    }

    private void OpenImage(int index)
    {
        List<AlbumImageData> images = PageImages();
        if (index < 0 || index >= images.Count) return;
        try
        {
            Texture2D next = Decode(mediaStore.Read(images[index].relativeFilePath));
            DestroyRuntime(viewerTexture);
            viewerTexture = next;
            fullImage.texture = next;
            LayoutViewer(next);
            selectedIndex = index;
            imageViewer.SetActive(true);
        }
        catch (Exception error) { ShowError(error); }
    }

    private void LayoutViewer(Texture2D image)
    {
        const float frameInset = 48f;
        const float frameBorder = 28f;
        RectTransform viewer = (RectTransform)imageViewer.transform;
        float maximumWidth = Mathf.Max(1f, viewer.rect.width + 24f - frameInset * 2f);
        float maximumHeight = Mathf.Max(1f, viewer.rect.height - 180f - frameInset * 2f);
        float scale = Mathf.Min(maximumWidth / image.width, maximumHeight / image.height);
        Vector2 imageSize = new Vector2(image.width * scale, image.height * scale);
        Vector2 frameSize = imageSize + Vector2.one * (frameInset * 2f);
        viewerFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameSize.x);
        viewerFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frameSize.y);
        Vector2 innerSize = frameSize - Vector2.one * (frameBorder * 2f);
        float fillScale = Mathf.Max(innerSize.x / imageSize.x, innerSize.y / imageSize.y);
        Vector2 filledImageSize = imageSize * fillScale;
        if (filledImageSize.x > frameSize.x - 16f || filledImageSize.y > frameSize.y - 16f)
            filledImageSize = imageSize * Mathf.Min(innerSize.x / imageSize.x, innerSize.y / imageSize.y);
        RectTransform imageRect = (RectTransform)fullImage.transform;
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, filledImageSize.x);
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, filledImageSize.y);
        imageRect.anchoredPosition = Vector2.zero;
    }

    private void CloseViewer()
    {
        imageViewer.SetActive(false);
        fullImage.texture = null;
        DestroyRuntime(viewerTexture);
        viewerTexture = null;
        selectedIndex = -1;
    }

    private static Texture2D Decode(byte[] bytes)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes)) return texture;
        DestroyRuntime(texture);
        throw new InvalidDataException("Image cannot be decoded");
    }

    private void ShowError(Exception error)
    {
        Debug.LogError("Album: " + error.Message);
        imageCount.text = localization.Translate("Не вдалося обробити зображення.");
    }

    private static void DestroyRuntime(UnityEngine.Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }

    private void OnDestroy()
    {
        foreach (Texture2D texture in visibleTextures) DestroyRuntime(texture);
        DestroyRuntime(viewerTexture);
    }
}
