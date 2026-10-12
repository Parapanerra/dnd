using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Replaces legacy Inspector navigation for named scene buttons.
internal sealed class SceneNavigationButtonBinder
{
    private readonly Action<string> navigate;
    private readonly Button[] buttons;

    internal SceneNavigationButtonBinder(Action<string> navigate, Button[] buttons = null)
    {
        this.navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        this.buttons = buttons ?? UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
    }

    internal bool BindFirst(string sceneName, params string[] buttonNames)
    {
        foreach (Button button in buttons)
        {
            if (button == null || !MatchesAnyName(button.name, buttonNames))
                continue;

            Bind(button, sceneName);
            return true;
        }

        return false;
    }

    internal int BindAll(string sceneName, Func<Button, bool> filter, params string[] buttonNames)
    {
        int count = 0;
        foreach (Button button in buttons)
        {
            if (button == null || (filter != null && !filter(button)) ||
                !MatchesAnyName(button.name, buttonNames))
                continue;

            Bind(button, sceneName);
            count++;
        }

        return count;
    }

    private void Bind(Button button, string sceneName)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => navigate(sceneName));
    }

    private static bool MatchesAnyName(string actualName, string[] expectedNames)
    {
        foreach (string expectedName in expectedNames)
            if (SceneObjectName.Matches(actualName, expectedName))
                return true;
        return false;
    }
}
