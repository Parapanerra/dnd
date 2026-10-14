using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPageManager : MonoBehaviour
{
    private const string SceneName = AppConfig.Scenes.Inventory;

    private readonly List<InventoryItemCell> cells = new List<InventoryItemCell>();
    private int currentPageIndex;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;

    private void Start()
    {
        saveManager = new RuntimeManagerResolver().SaveManager();
        localization = new RuntimeManagerResolver().Localization();
        KeepMainPageActive();
        BindPageButtons();
        BindNavigationButtons();
        SwitchToPage(0);
    }

    private void BindPageButtons()
    {
        new ScenePageButtonBinder(SwitchToPage).Bind();
    }

    private void BindNavigationButtons()
    {
        new SceneNavigationButtonBinder(new SceneNavigator().LoadSceneByName)
            .BindFirst(AppConfig.Scenes.Spellbook, "spelbook", "spellbook");
    }

    private void SwitchToPage(int pageIndex)
    {
        SaveCurrentCells();
        currentPageIndex = Mathf.Max(0, pageIndex);
        KeepMainPageActive();

        string sceneDataName = GetSceneDataName(currentPageIndex);
        saveManager.SetActiveSceneDataName(sceneDataName);

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
        foreach (UiControlMarker marker in page.GetComponentsInChildren<UiControlMarker>(true))
            if (marker.Role == UiControlRole.InventoryCell)
                cellTransforms.Add(marker.transform);

        if (cellTransforms.Count == 0) // Legacy scene fallback.
        foreach (Transform child in page.GetComponentsInChildren<Transform>(true))
            if (child != page && SceneObjectName.Matches(child.name, "Cels"))
                cellTransforms.Add(child);

        cellTransforms.Sort(CompareHierarchyOrder);

        for (int i = 0; i < cellTransforms.Count; i++)
        {
            InventoryItemCell cell = cellTransforms[i].GetComponent<InventoryItemCell>();
            if (cell == null)
                cell = cellTransforms[i].gameObject.AddComponent<InventoryItemCell>();

            cell.Initialize(currentPageIndex, i, saveManager, localization);
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
        SceneRoleMarker page = new SceneRoleLookup().Find(SceneRole.MainPage);
        if (page != null)
        {
            page.gameObject.SetActive(true);
            foreach (UiControlMarker marker in FindObjectsByType<UiControlMarker>(FindObjectsInactive.Include))
                if (marker.gameObject.scene == gameObject.scene && marker.Role == UiControlRole.InactiveInventoryPage)
                    marker.gameObject.SetActive(false);
            return;
        }
        // Legacy scene fallback.
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
        SceneRoleMarker page = new SceneRoleLookup().Find(SceneRole.MainPage);
        if (page != null) return page.transform;
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
        string title = localization.Translate("\u0421\u0442\u043e\u0440\u0456\u043d\u043a\u0430 \u2116" + (pageIndex + 1));

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
        if (textTransform != null && textTransform.TryGetComponent(out UiControlMarker marker))
            return marker.Role == UiControlRole.PageTitle;
        if (textTransform == null || string.IsNullOrWhiteSpace(text))
            return false;

        if (textTransform.GetComponentInParent<Button>(true) != null)
            return false;

        return text.IndexOf("\u0421\u0442\u043E\u0440\u0456\u043D\u043A\u0430", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("Page #", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("\u0421\u0442\u0440\u0430\u043d\u0438\u0446\u0430 \u2116", StringComparison.OrdinalIgnoreCase) >= 0 ||
               textTransform.name.StartsWith("pageTitle", StringComparison.OrdinalIgnoreCase);
    }

    private int CompareHierarchyOrder(Transform left, Transform right)
    {
        List<int> leftPath = GetSiblingPath(left);
        List<int> rightPath = GetSiblingPath(right);

        for (int leftIndex = leftPath.Count - 1, rightIndex = rightPath.Count - 1;
             leftIndex >= 0 && rightIndex >= 0;
             leftIndex--, rightIndex--)
        {
            int difference = leftPath[leftIndex].CompareTo(rightPath[rightIndex]);
            if (difference != 0)
                return difference;
        }

        return leftPath.Count.CompareTo(rightPath.Count);
    }

    private List<int> GetSiblingPath(Transform transform)
    {
        List<int> path = new List<int>();
        for (; transform != null; transform = transform.parent)
            path.Add(transform.GetSiblingIndex());
        return path;
    }


}
