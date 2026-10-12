using UnityEngine;
using UnityEngine.UI;

// Creates the legacy menu buttons. The list view owns layout and event bindings.
internal sealed class MainMenuCharacterButtonFactory
{
    private readonly MainMenuManager owner;
    private readonly DndSaveManager saveManager;
    private readonly MainMenuCharacterLabels labels;

    internal MainMenuCharacterButtonFactory(MainMenuManager owner, DndSaveManager saveManager, MainMenuCharacterLabels labels)
    {
        this.owner = owner;
        this.saveManager = saveManager;
        this.labels = labels;
    }

    internal GameObject Create(CharacterData character, GameObject template)
    {
        GameObject sourceButton = template != null
            ? template
            : owner.UseCharacterButtonPrefab && owner.CharacterButtonPrefab != null
                ? owner.CharacterButtonPrefab
                : null;

        GameObject buttonObject = sourceButton != null
            ? Object.Instantiate(sourceButton)
            : CreateFallbackButton();

        buttonObject.name = "CharacterButton_" +
            (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);

        if (buttonObject.GetComponent<Button>() == null)
            buttonObject.AddComponent<Button>();

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
            image = buttonObject.AddComponent<Image>();

        bool isActive = saveManager != null && saveManager.GetActiveCharacterId() == character.id;
        if (owner.ApplyDefaultCharacterButtonStyle)
        {
            image.color = isActive
                ? AppConfig.MainMenu.ActiveCharacterColor
                : AppConfig.MainMenu.InactiveCharacterColor;
        }

        Text nameText = buttonObject.GetComponentInChildren<Text>(true);
        if (nameText == null)
            nameText = CreateText(buttonObject.transform, "CharacterName", TextAnchor.MiddleLeft);

        if (nameText.GetComponent<LocalizedIgnore>() == null)
            nameText.gameObject.AddComponent<LocalizedIgnore>();
        nameText.text = labels.GetDisplayName(character, "Невідомий персонаж");
        if (owner.ApplyDefaultCharacterButtonStyle)
        {
            nameText.color = Color.white;
            nameText.fontSize = AppConfig.MainMenu.CharacterNameFontSize;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = AppConfig.MainMenu.CharacterNameMinimumFontSize;
            nameText.resizeTextMaxSize = AppConfig.MainMenu.CharacterNameFontSize;
        }

        EnsureDeleteButton(buttonObject.transform);
        return buttonObject;
    }

    private GameObject CreateFallbackButton()
    {
        GameObject buttonObject = new GameObject("CharacterButton", typeof(RectTransform), typeof(Image), typeof(Button));
        CreateText(buttonObject.transform, "CharacterName", TextAnchor.MiddleLeft);
        return buttonObject;
    }

    private Text CreateText(Transform parent, string name, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(
            AppConfig.MainMenu.CharacterTextLeftPadding,
            AppConfig.MainMenu.CharacterTextVerticalPadding);
        textRect.offsetMax = new Vector2(
            -AppConfig.MainMenu.CharacterTextRightPadding,
            -AppConfig.MainMenu.CharacterTextVerticalPadding);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.alignment = alignment;
        return text;
    }

    private void EnsureDeleteButton(Transform parent)
    {
        if (parent.Find("DeleteButton") != null)
            return;

        GameObject deleteObject = new GameObject("DeleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(parent, false);

        RectTransform deleteRect = deleteObject.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(1f, 0.5f);
        deleteRect.anchorMax = new Vector2(1f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = AppConfig.MainMenu.DeleteButtonPosition;
        deleteRect.sizeDelta = AppConfig.MainMenu.DeleteButtonSize;

        deleteObject.GetComponent<Image>().color = AppConfig.MainMenu.DeleteButtonColor;

        Text deleteText = CreateText(deleteObject.transform, "Text", TextAnchor.MiddleCenter);
        RectTransform textRect = deleteText.GetComponent<RectTransform>();
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        deleteText.text = "X";
        deleteText.color = Color.white;
        deleteText.fontSize = AppConfig.MainMenu.DeleteButtonFontSize;
    }
}
