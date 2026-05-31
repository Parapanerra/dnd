using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryPageManager : MonoBehaviour
{
    private const string SceneName = "inventory";
    private const string PageButtonPrefix = "page";

    private readonly List<InventoryItemCell> cells = new List<InventoryItemCell>();
    private int currentPageIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForActiveScene();
    }

    private static void EnsureForActiveScene()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            return;

        if (FindAnyObjectByType<InventoryPageManager>() != null)
            return;

        GameObject managerObject = new GameObject("InventoryPageManager");
        managerObject.AddComponent<InventoryPageManager>();
    }

    private void Start()
    {
        DndSaveManager.EnsureExists();
        KeepMainPageActive();
        BindPageButtons();
        BindNavigationButtons();
        SwitchToPage(0);
    }

    private void BindPageButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        List<Button> pageButtons = new List<Button>();

        foreach (Button button in buttons)
            if (button != null && TryGetPageIndex(button.gameObject.name, out _))
                pageButtons.Add(button);

        pageButtons.Sort((left, right) => GetPageIndex(left.name).CompareTo(GetPageIndex(right.name)));

        foreach (Button button in pageButtons)
        {
            int pageIndex = GetPageIndex(button.gameObject.name);
            DisablePersistentOnClick(button);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SwitchToPage(pageIndex));
        }
    }

    private void BindNavigationButtons()
    {
        Button spellbookButton = FindButtonByName("spelbook", "spellbook");
        if (spellbookButton == null)
            return;

        DisablePersistentOnClick(spellbookButton);
        spellbookButton.onClick.RemoveAllListeners();
        spellbookButton.onClick.AddListener(() => load_scenes.LoadSceneByName("spelBook"));
    }

    private void SwitchToPage(int pageIndex)
    {
        SaveCurrentCells();
        currentPageIndex = Mathf.Max(0, pageIndex);
        KeepMainPageActive();

        string sceneDataName = GetSceneDataName(currentPageIndex);
        DndSaveManager.EnsureExists().SetActiveSceneDataName(sceneDataName);

        InitializeCells();
        UpdatePageTitle(currentPageIndex);
    }

    private void InitializeCells()
    {
        cells.Clear();
        Transform page = FindMainPage();
        if (page == null)
            return;

        List<Transform> cellTransforms = new List<Transform>();
        foreach (Transform child in page.GetComponentsInChildren<Transform>(true))
            if (child != page && NameMatches(child.name, "Cels"))
                cellTransforms.Add(child);

        cellTransforms.Sort((left, right) => GetHierarchyOrder(left).CompareTo(GetHierarchyOrder(right)));

        for (int i = 0; i < cellTransforms.Count; i++)
        {
            InventoryItemCell cell = cellTransforms[i].GetComponent<InventoryItemCell>();
            if (cell == null)
                cell = cellTransforms[i].gameObject.AddComponent<InventoryItemCell>();

            cell.Initialize(currentPageIndex, i);
            cells.Add(cell);
        }
    }

    private void SaveCurrentCells()
    {
        foreach (InventoryItemCell cell in cells)
            if (cell != null)
                cell.Save();
    }

    private void KeepMainPageActive()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
        {
            if (transform == null)
                continue;

            if (string.Equals(transform.name, "Page", StringComparison.Ordinal))
                transform.gameObject.SetActive(true);
            else if (transform.name.StartsWith("Page (", StringComparison.Ordinal))
                transform.gameObject.SetActive(false);
        }
    }

    private Transform FindMainPage()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
            if (transform != null && string.Equals(transform.name, "Page", StringComparison.Ordinal))
                return transform;

        return null;
    }

    private string GetSceneDataName(int pageIndex)
    {
        return pageIndex <= 0 ? SceneName : SceneName + " " + pageIndex;
    }

    private void UpdatePageTitle(int pageIndex)
    {
        string title = RuntimeLocalization.EnsureExists().Translate("Сторінка №") + (pageIndex + 1);

        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (Text text in texts)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;

        TMP_Text[] tmpTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        foreach (TMP_Text text in tmpTexts)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;
    }

    private bool IsPageTitle(Transform textTransform, string text)
    {
        if (textTransform == null || string.IsNullOrWhiteSpace(text))
            return false;

        if (textTransform.GetComponentInParent<Button>(true) != null)
            return false;

        return text.IndexOf("\u0421\u0442\u043E\u0440\u0456\u043D\u043A\u0430", StringComparison.OrdinalIgnoreCase) >= 0 ||
               textTransform.name.StartsWith("pageTitle", StringComparison.OrdinalIgnoreCase);
    }

    private void DisablePersistentOnClick(Button button)
    {
        if (button == null)
            return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private Button FindButtonByName(params string[] objectNames)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            foreach (string objectName in objectNames)
                if (NameMatches(button.gameObject.name, objectName))
                    return button;
        }

        return null;
    }

    private int GetHierarchyOrder(Transform transform)
    {
        int order = 0;
        int multiplier = 1;
        while (transform != null)
        {
            order += transform.GetSiblingIndex() * multiplier;
            multiplier *= 100;
            transform = transform.parent;
        }

        return order;
    }

    private static int GetPageIndex(string objectName)
    {
        TryGetPageIndex(objectName, out int pageIndex);
        return pageIndex;
    }

    private static bool TryGetPageIndex(string objectName, out int pageIndex)
    {
        pageIndex = 0;
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        objectName = objectName.Trim();
        if (string.Equals(objectName, PageButtonPrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!objectName.StartsWith(PageButtonPrefix + " (", StringComparison.OrdinalIgnoreCase) ||
            !objectName.EndsWith(")", StringComparison.Ordinal))
        {
            return false;
        }

        string numberText = objectName.Substring(PageButtonPrefix.Length + 2, objectName.Length - PageButtonPrefix.Length - 3);
        return int.TryParse(numberText, out pageIndex);
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
