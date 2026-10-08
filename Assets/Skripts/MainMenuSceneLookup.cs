using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resolves legacy menu controls by name until scenes provide serialized references.
public static class MainMenuSceneLookup
{
    public static Button FindButtonInScene(string name) => FindInScene<Button>(name);
    public static Button FindFirstButtonInScene(params string[] names) => FindFirstInScene<Button>(names);
    public static Transform FindTransformInScene(string name) => FindInScene<Transform>(name);
    public static Dropdown FindDropdownInScene(string name) => FindInScene<Dropdown>(name);
    public static Dropdown FindFirstDropdownInScene(params string[] names) => FindFirstInScene<Dropdown>(names);
    public static TMP_Dropdown FindTmpDropdownInScene(string name) => FindInScene<TMP_Dropdown>(name);
    public static TMP_Dropdown FindFirstTmpDropdownInScene(params string[] names) => FindFirstInScene<TMP_Dropdown>(names);
    public static ScrollRect FindScrollRectInScene(string name) => FindInScene<ScrollRect>(name);

    public static Button FindButtonUnder(Transform root, string name)
    {
        if (root == null)
            return null;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (Matches(button.gameObject.name, name))
                return button;
        return null;
    }

    public static Button FindFirstDirectButtonInContent(Transform content)
    {
        if (content == null)
            return null;

        foreach (Transform child in content)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;
            button = child.GetComponentInChildren<Button>(true);
            if (button != null)
                return button;
        }

        return null;
    }

    public static Button FindFirstButtonUnder(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Button button = FindButtonUnder(root, name);
            if (button != null)
                return button;
        }

        return null;
    }

    public static bool Matches(string actualName, string expectedName)
    {
        return string.Equals(actualName.Trim(), expectedName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() &&
               !string.IsNullOrEmpty(gameObject.scene.name);
    }

    public static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static T FindInScene<T>(string name) where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
            if (IsSceneObject(component.gameObject) && Matches(component.gameObject.name, name))
                return component;
        return null;
    }

    private static T FindFirstInScene<T>(params string[] names) where T : Component
    {
        foreach (string name in names)
        {
            T component = FindInScene<T>(name);
            if (component != null)
                return component;
        }

        return null;
    }
}
