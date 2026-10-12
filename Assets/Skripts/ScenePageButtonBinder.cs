using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Binds the shared page-button naming convention; scene-specific page state stays with the owner.
internal sealed class ScenePageButtonBinder
{
    private const string PageButtonPrefix = "page";
    private readonly Action<int> switchToPage;

    internal ScenePageButtonBinder(Action<int> switchToPage)
    {
        this.switchToPage = switchToPage ?? throw new ArgumentNullException(nameof(switchToPage));
    }

    internal void Bind()
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        List<Button> pageButtons = new List<Button>();

        foreach (Button button in buttons)
            if (button != null && TryGetPageIndex(button.gameObject.name, out _))
                pageButtons.Add(button);

        pageButtons.Sort((left, right) =>
            GetPageIndex(left.gameObject.name).CompareTo(GetPageIndex(right.gameObject.name)));

        foreach (Button button in pageButtons)
        {
            int pageIndex = GetPageIndex(button.gameObject.name);
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => switchToPage(pageIndex));
        }
    }

    private int GetPageIndex(string objectName)
    {
        TryGetPageIndex(objectName, out int pageIndex);
        return pageIndex;
    }

    internal bool TryGetPageIndex(string objectName, out int pageIndex)
    {
        pageIndex = 0;
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        objectName = objectName.Trim();
        if (string.Equals(objectName, PageButtonPrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!objectName.StartsWith(PageButtonPrefix + " (", StringComparison.OrdinalIgnoreCase) ||
            !objectName.EndsWith(")", StringComparison.Ordinal))
            return false;

        string numberText = objectName.Substring(
            PageButtonPrefix.Length + 2, objectName.Length - PageButtonPrefix.Length - 3);
        return int.TryParse(numberText, out pageIndex);
    }
}
