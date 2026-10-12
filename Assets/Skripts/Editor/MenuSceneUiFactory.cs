using UnityEngine;
using UnityEngine.UI;

internal sealed class MenuSceneUiElementSpec
{
    internal string Name { get; private set; }
    internal Transform Parent { get; private set; }
    internal Vector2 Anchor { get; private set; }
    internal Vector2 Position { get; private set; }
    internal Vector2 Size { get; private set; }

    private MenuSceneUiElementSpec()
    {
    }

    internal static MenuSceneUiElementSpec Create(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        return new MenuSceneUiElementSpec
        {
            Name = name,
            Parent = parent,
            Anchor = anchor,
            Position = position,
            Size = size
        };
    }
}

internal sealed class MenuSceneButtonSpec
{
    internal MenuSceneUiElementSpec Element { get; private set; }
    internal string Label { get; private set; }

    private MenuSceneButtonSpec()
    {
    }

    internal static MenuSceneButtonSpec Create(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string label)
    {
        return new MenuSceneButtonSpec
        {
            Element = MenuSceneUiElementSpec.Create(
                name,
                parent,
                MenuSceneLayout.CenterAnchor,
                position,
                size),
            Label = label
        };
    }
}

internal static class MenuSceneUiFactory
{
    private const int ButtonFontSize = 24;
    private const int ButtonMinimumFontSize = 10;
    private const int ButtonMaximumFontSize = 28;
    private static readonly Color ButtonColor = new Color(0.12f, 0.09f, 0.07f, 0.9f);
    private static readonly Color ButtonTextColor = new Color(0.95f, 0.55f, 0.05f, 1f);

    internal static GameObject CreateElement(MenuSceneUiElementSpec spec)
    {
        GameObject element = new GameObject(spec.Name, typeof(RectTransform), typeof(CanvasRenderer));
        element.transform.SetParent(spec.Parent, false);

        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = spec.Anchor;
        rect.anchorMax = spec.Anchor;
        rect.pivot = MenuSceneLayout.CenterAnchor;
        rect.anchoredPosition = spec.Position;
        rect.sizeDelta = spec.Size;
        return element;
    }

    internal static GameObject CreateButton(MenuSceneButtonSpec spec)
    {
        GameObject buttonObject = CreateElement(spec.Element);
        Image image = buttonObject.AddComponent<Image>();
        image.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = CreateElement(MenuSceneUiElementSpec.Create(
            "Text",
            buttonObject.transform,
            MenuSceneLayout.CenterAnchor,
            Vector2.zero,
            spec.Element.Size));

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = spec.Label;
        text.color = ButtonTextColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = ButtonFontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = ButtonMinimumFontSize;
        text.resizeTextMaxSize = ButtonMaximumFontSize;
        text.raycastTarget = false;
        return buttonObject;
    }
}
