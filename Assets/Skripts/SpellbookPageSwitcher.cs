using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SpellbookPageSwitcher : MonoBehaviour
{
    private const string SceneName = "spelBook";
    private const string PageButtonPrefix = "page";

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
        if (SceneManager.GetActiveScene().name != SceneName)
            return;

        if (FindAnyObjectByType<SpellbookPageSwitcher>() != null)
            return;

        GameObject switcherObject = new GameObject("SpellbookPageSwitcher");
        switcherObject.AddComponent<SpellbookPageSwitcher>();
    }

    private void Start()
    {
        KeepMainPagePanelActive();
        EnsurePersistentDropdowns();
        BindPageButtons();
        BindNavigationButtons();
        SwitchToPage(0);
    }

    private void BindPageButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        List<Button> pageButtons = new List<Button>();

        foreach (Button button in buttons)
        {
            if (button == null || !TryGetPageIndex(button.gameObject.name, out _))
                continue;

            pageButtons.Add(button);
        }

        pageButtons.Sort((a, b) =>
            GetPageIndex(a.gameObject.name).CompareTo(GetPageIndex(b.gameObject.name)));

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
        BindNavigationButton("dukaforma", "petsesn");
        BindNavigationButton("inventar", "inventory");
        BindNavigationButton("inventory", "inventory");
        BindNavigationButton("ifopropersonaj", "informForPerson");
    }

    private void BindNavigationButton(string buttonName, string sceneName)
    {
        Button button = FindButtonByName(buttonName);
        if (button == null)
            return;

        DisablePersistentOnClick(button);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => load_scenes.LoadSceneByName(sceneName));
    }

    private void DisablePersistentOnClick(Button button)
    {
        if (button == null)
            return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private Button FindButtonByName(string objectName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
            if (button != null && NameMatches(button.gameObject.name, objectName))
                return button;

        return null;
    }

    private void SwitchToPage(int pageIndex)
    {
        KeepMainPagePanelActive();
        string sceneDataName = pageIndex <= 0 ? SceneName : SceneName + " " + pageIndex;

        CharacterSheetManagerScene1 sheetManager = FindAnyObjectByType<CharacterSheetManagerScene1>();
        if (sheetManager != null)
        {
            sheetManager.SwitchSceneData(sceneDataName);
            UpdatePageTitle(pageIndex);
            ReloadPersistentDropdowns();
            return;
        }

        CharacterSceneAutoSave autoSave = FindAnyObjectByType<CharacterSceneAutoSave>();
        if (autoSave != null)
        {
            autoSave.SwitchSceneData(sceneDataName);
            UpdatePageTitle(pageIndex);
            ReloadPersistentDropdowns();
            return;
        }

        DndSaveManager.EnsureExists().SetActiveSceneDataName(sceneDataName);
        UpdatePageTitle(pageIndex);
        ReloadPersistentDropdowns();
    }

    private void EnsurePersistentDropdowns()
    {
        Dropdown[] dropdowns = FindObjectsByType<Dropdown>(FindObjectsInactive.Include);
        foreach (Dropdown dropdown in dropdowns)
            if (dropdown != null && dropdown.GetComponent<PersistentDropdownValue>() == null)
                dropdown.gameObject.AddComponent<PersistentDropdownValue>();

        TMP_Dropdown[] tmpDropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include);
        foreach (TMP_Dropdown dropdown in tmpDropdowns)
            if (dropdown != null && dropdown.GetComponent<PersistentDropdownValue>() == null)
                dropdown.gameObject.AddComponent<PersistentDropdownValue>();
    }

    private void ReloadPersistentDropdowns()
    {
        PersistentDropdownValue[] dropdowns = FindObjectsByType<PersistentDropdownValue>(FindObjectsInactive.Include);
        foreach (PersistentDropdownValue dropdown in dropdowns)
            if (dropdown != null)
                dropdown.Reload();
    }

    private void UpdatePageTitle(int pageIndex)
    {
        const string pageTitlePrefix = "\u0421\u0442\u043E\u0440\u0456\u043D\u043A\u0430 \u2116";
        string title = pageTitlePrefix + (pageIndex + 1);

        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (Text text in texts)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;

        TMP_Text[] tmpTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        foreach (TMP_Text text in tmpTexts)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;

        TextMesh[] textMeshes = FindObjectsByType<TextMesh>(FindObjectsInactive.Include);
        foreach (TextMesh text in textMeshes)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;
    }

    private bool IsPageTitle(Transform transform, string text)
    {
        if (transform == null)
            return false;

        if (transform.GetComponentInParent<Button>(true) != null)
            return false;

        Transform image = transform.parent;
        Transform page = image != null ? image.parent : null;
        if (image == null || page == null)
            return false;

        return string.Equals(page.name, "Page", StringComparison.Ordinal) &&
               string.Equals(image.name, "Image", StringComparison.Ordinal) &&
               transform.name.StartsWith("Text", StringComparison.OrdinalIgnoreCase);
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

    private void KeepMainPagePanelActive()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
            if (transform != null && string.Equals(transform.name, "Page", StringComparison.Ordinal))
                transform.gameObject.SetActive(true);
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
}
