using UnityEngine;
using UnityEngine.UI;

public sealed class RuntimeUiElementSpec
{
    public string Name { get; private set; }
    public Transform Parent { get; private set; }
    public Vector2 Anchor { get; private set; }
    public Vector2 Position { get; private set; }
    public Vector2 Size { get; private set; }

    private RuntimeUiElementSpec()
    {
    }

    public static RuntimeUiElementSpec Create(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        return new RuntimeUiElementSpec
        {
            Name = name,
            Parent = parent,
            Anchor = anchor,
            Position = position,
            Size = size
        };
    }
}

public sealed class RuntimeButtonSpec
{
    public RuntimeUiElementSpec Element { get; private set; }
    public string Label { get; private set; }

    private RuntimeButtonSpec()
    {
    }

    public static RuntimeButtonSpec Create(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string label)
    {
        return new RuntimeButtonSpec
        {
            Element = RuntimeUiElementSpec.Create(
                name,
                parent,
                AppConfig.MainMenu.CenterAnchor,
                position,
                size),
            Label = label
        };
    }
}

public static class RuntimeUiFactory
{
    public static GameObject CreateElement(RuntimeUiElementSpec spec)
    {
        GameObject element = new GameObject(spec.Name, typeof(RectTransform), typeof(CanvasRenderer));
        element.transform.SetParent(spec.Parent, false);

        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = spec.Anchor;
        rect.anchorMax = spec.Anchor;
        rect.pivot = AppConfig.MainMenu.CenterAnchor;
        rect.anchoredPosition = spec.Position;
        rect.sizeDelta = spec.Size;
        return element;
    }

    public static GameObject CreateButton(RuntimeButtonSpec spec)
    {
        GameObject buttonObject = CreateElement(spec.Element);
        Image image = buttonObject.AddComponent<Image>();
        image.color = AppConfig.MainMenu.DefaultButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = CreateElement(RuntimeUiElementSpec.Create(
            "Text",
            buttonObject.transform,
            AppConfig.MainMenu.CenterAnchor,
            Vector2.zero,
            spec.Element.Size));

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = spec.Label;
        text.color = AppConfig.MainMenu.DefaultButtonTextColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = AppConfig.MainMenu.DefaultButtonFontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = AppConfig.MainMenu.DefaultButtonMinimumFontSize;
        text.resizeTextMaxSize = AppConfig.MainMenu.DefaultButtonMaximumFontSize;
        text.raycastTarget = false;
        return buttonObject;
    }
}
