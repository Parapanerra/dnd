using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resolves legacy menu controls by name until scenes provide serialized references.
internal sealed class MainMenuSceneLookup
{
    internal Button FindButtonInScene(string name) => FindInScene<Button>(name);
    internal Button FindFirstButtonInScene(params string[] names) => FindFirstInScene<Button>(names);
    internal Transform FindTransformInScene(string name) => FindInScene<Transform>(name);
    internal Dropdown FindDropdownInScene(string name) => FindInScene<Dropdown>(name);
    internal Dropdown FindFirstDropdownInScene(params string[] names) => FindFirstInScene<Dropdown>(names);
    internal TMP_Dropdown FindTmpDropdownInScene(string name) => FindInScene<TMP_Dropdown>(name);
    internal TMP_Dropdown FindFirstTmpDropdownInScene(params string[] names) => FindFirstInScene<TMP_Dropdown>(names);
    internal ScrollRect FindScrollRectInScene(string name) => FindInScene<ScrollRect>(name);

    internal Button FindButtonUnder(Transform root, string name)
    {
        if (root == null)
            return null;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (Matches(button.gameObject.name, name))
                return button;
        return null;
    }

    internal Button FindFirstDirectButtonInContent(Transform content)
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

    internal Button FindFirstButtonUnder(Transform root, params string[] names)
    {
        if (root == null)
            return null;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (string name in names)
            foreach (Button button in buttons)
                if (Matches(button.gameObject.name, name))
                    return button;

        return null;
    }

    internal bool Matches(string actualName, string expectedName)
    {
        return string.Equals(actualName.Trim(), expectedName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() &&
               gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    }

    private T FindInScene<T>(string name) where T : Component
    {
        foreach (T component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if (IsSceneObject(component.gameObject) && Matches(component.gameObject.name, name))
                return component;
        return null;
    }

    private T FindFirstInScene<T>(params string[] names) where T : Component
    {
        T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        foreach (string name in names)
            foreach (T component in components)
                if (IsSceneObject(component.gameObject) && Matches(component.gameObject.name, name))
                    return component;

        return null;
    }
}
