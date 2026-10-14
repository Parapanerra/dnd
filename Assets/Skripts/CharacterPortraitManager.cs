using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPortraitManager : MonoBehaviour
{
    private const string PortraitDataKey = "CharacterPortraitJpgBase64";
    private const string PortraitImageName = "photopersonaja";
    private const string SelectButtonName = "Buttonphotopersoj";
    private readonly GalleryImagePicker galleryPicker = new GalleryImagePicker();
    private readonly RuntimeImageView imageView = new RuntimeImageView();
    private DndSaveManager saveManager;
    [SerializeField] private Button selectPortraitButton;
    [SerializeField] private Image portraitImage;

    internal void Initialize(DndSaveManager dataManager)
    {
        saveManager = dataManager;
    }

    private void Start()
    {
        SceneRoleLookup roles = new SceneRoleLookup();
        if (portraitImage == null)
        {
            SceneRoleMarker marker = roles.Find(SceneRole.PortraitImage);
            portraitImage = marker != null ? marker.GetComponent<Image>() : FindImageByName(PortraitImageName);
        }
        if (selectPortraitButton == null)
        {
            SceneRoleMarker marker = roles.Find(SceneRole.PortraitButton);
            selectPortraitButton = marker != null ? marker.GetComponent<Button>() : FindButtonByName(SelectButtonName);
        }

        if (portraitImage != null)
        {
            imageView.Bind(portraitImage);
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
        if (portraitImage == null)
            return;

        galleryPicker.Pick("Select portrait", AppConfig.Images.PortraitWidth,
            AppConfig.Images.PortraitHeight, ApplySelectedPortrait,
            exception => Debug.LogError("Could not load character portrait: " + exception.Message));
    }

    private void ApplySelectedPortrait(Texture2D texture)
    {
        try
        {
            SavePortrait(texture);
            imageView.Show(texture);
        }
        catch (Exception exception)
        {
            if (imageView.Texture != texture)
                Destroy(texture);
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
            Texture2D texture = new Texture2D(
                AppConfig.Images.TextureBootstrapSize,
                AppConfig.Images.TextureBootstrapSize,
                TextureFormat.RGBA32,
                false);
            if (texture.LoadImage(bytes))
                imageView.Show(texture);
            else
                Destroy(texture);
        }
        catch
        {
            ClearPortraitImage();
        }
    }

    internal void ClearPortraitForActiveCharacter()
    {
        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character != null)
        {
            character.DeleteSharedString(PortraitDataKey);
            saveManager.RequestSaveData();
        }

        ClearPortraitImage();
    }

    private void SavePortrait(Texture2D texture)
    {
        if (texture == null)
            return;

        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character == null)
            return;

        byte[] jpgBytes = texture.EncodeToJPG(AppConfig.Images.PortraitJpgQuality);
        character.SetSharedString(PortraitDataKey, Convert.ToBase64String(jpgBytes));
        saveManager.SaveData();
    }

    private string GetSavedPortrait()
    {
        CharacterData character = saveManager.EnsureActiveCharacter();
        return character != null ? character.GetSharedString(PortraitDataKey, "") : "";
    }

    private void ClearPortraitImage()
    {
        if (portraitImage == null)
            return;

        imageView.Restore();
    }

    private void OnDestroy()
    {
        imageView.Dispose();
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
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
            if (button != null && SceneObjectName.Matches(button.gameObject.name, objectName))
                return button;

        return null;
    }

    private Image FindImageByName(string objectName)
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
        foreach (Image image in images)
            if (image != null && SceneObjectName.Matches(image.gameObject.name, objectName))
                return image;

        return null;
    }

}
