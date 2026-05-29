using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPortraitManager : MonoBehaviour
{
    private const string PortraitDataKey = "CharacterPortraitJpgBase64";
    private const string PortraitImageName = "photopersonaja";
    private const string SelectButtonName = "Buttonphotopersoj";
    private const int PortraitWidth = 300;
    private const int PortraitHeight = 400;
    private const int JpgQuality = 80;

    private Button selectPortraitButton;
    private Image portraitImage;
    private Sprite loadedSprite;
    private Texture2D loadedTexture;
    private Sprite defaultSprite;
    private Color defaultColor = Color.white;
    private bool defaultPreserveAspect;

    private void Start()
    {
        portraitImage = FindImageByName(PortraitImageName);
        selectPortraitButton = FindButtonByName(SelectButtonName);

        if (portraitImage != null)
        {
            defaultSprite = portraitImage.sprite;
            defaultColor = portraitImage.color;
            defaultPreserveAspect = portraitImage.preserveAspect;
            ConfigureImageForFullFill();
        }

        if (selectPortraitButton != null)
        {
            selectPortraitButton.onClick.RemoveAllListeners();
            selectPortraitButton.onClick.AddListener(SelectPortrait);
        }

        LoadSavedPortrait();
    }

    private void SelectPortrait()
    {
        NativeGallery.GetImageFromGallery(
            path =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                LoadPortraitFromPath(path);
            },
            "Select portrait",
            "image/*"
        );
    }

    private void LoadPortraitFromPath(string path)
    {
        if (portraitImage == null)
            return;

        try
        {
            Texture2D texture = NativeGallery.LoadImageAtPath(path, 1024, false, false);
            if (texture == null)
                return;

            Texture2D resizedTexture = ResizeToPortrait(texture);
            Destroy(texture);

            SavePortrait(resizedTexture);
            ApplyTexture(resizedTexture);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not load character portrait: " + exception.Message);
        }
    }

    private void LoadSavedPortrait()
    {
        string base64 = GetSavedPortrait();
        if (string.IsNullOrWhiteSpace(base64))
        {
            ClearPortraitImage();
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
                ApplyTexture(texture);
            else
                Destroy(texture);
        }
        catch
        {
            ClearPortraitImage();
        }
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (portraitImage == null || texture == null)
            return;

        if (loadedSprite != null)
            Destroy(loadedSprite);
        if (loadedTexture != null)
            Destroy(loadedTexture);

        loadedTexture = texture;
        loadedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        portraitImage.sprite = loadedSprite;
        portraitImage.color = Color.white;
        ConfigureImageForFullFill();
    }

    public static void ClearPortraitForActiveCharacter()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character != null)
        {
            character.DeleteSharedString(PortraitDataKey);
            saveManager.SaveData();
        }

        CharacterPortraitManager manager = FindFirstObjectByType<CharacterPortraitManager>();
        if (manager != null)
            manager.ClearPortraitImage();
    }

    private void SavePortrait(Texture2D texture)
    {
        if (texture == null)
            return;

        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character == null)
            return;

        byte[] jpgBytes = texture.EncodeToJPG(JpgQuality);
        character.SetSharedString(PortraitDataKey, Convert.ToBase64String(jpgBytes));
        saveManager.SaveData();
    }

    private string GetSavedPortrait()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
        return character != null ? character.GetSharedString(PortraitDataKey, "") : "";
    }

    private Texture2D ResizeToPortrait(Texture2D source)
    {
        Texture2D result = new Texture2D(PortraitWidth, PortraitHeight, TextureFormat.RGB24, false);
        Color[] pixels = new Color[PortraitWidth * PortraitHeight];

        for (int y = 0; y < PortraitHeight; y++)
        {
            float sourceY = PortraitHeight == 1 ? 0f : (float)y / (PortraitHeight - 1);
            for (int x = 0; x < PortraitWidth; x++)
            {
                float sourceX = PortraitWidth == 1 ? 0f : (float)x / (PortraitWidth - 1);
                pixels[y * PortraitWidth + x] = source.GetPixelBilinear(sourceX, sourceY);
            }
        }

        result.SetPixels(pixels);
        result.Apply(false, false);
        return result;
    }

    private void ClearPortraitImage()
    {
        if (portraitImage == null)
            return;

        if (loadedSprite != null)
        {
            Destroy(loadedSprite);
            loadedSprite = null;
        }

        if (loadedTexture != null)
        {
            Destroy(loadedTexture);
            loadedTexture = null;
        }

        portraitImage.sprite = defaultSprite;
        portraitImage.color = defaultColor;
        portraitImage.preserveAspect = defaultPreserveAspect;
    }

    private void ConfigureImageForFullFill()
    {
        if (portraitImage == null)
            return;

        portraitImage.type = Image.Type.Simple;
        portraitImage.preserveAspect = false;
        portraitImage.SetAllDirty();
    }

    private Button FindButtonByName(string objectName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
            if (button != null && NameMatches(button.gameObject.name, objectName))
                return button;

        return null;
    }

    private Image FindImageByName(string objectName)
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image image in images)
            if (image != null && NameMatches(image.gameObject.name, objectName))
                return image;

        return null;
    }

    private bool NameMatches(string actualName, string expectedName)
    {
        return GetBaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetBaseName(string name)
    {
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }
}
