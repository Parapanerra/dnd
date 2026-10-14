using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(100)]
public class SpellbookPageSwitcher : MonoBehaviour
{
    private const string SceneName = AppConfig.Scenes.Spellbook;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;

    private void Start()
    {
        saveManager = new RuntimeManagerResolver().SaveManager();
        localization = new RuntimeManagerResolver().Localization();
        KeepMainPagePanelActive();
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
        SceneNavigationButtonBinder binder = new SceneNavigationButtonBinder(new SceneNavigator().LoadSceneByName);
        binder.BindFirst(AppConfig.Scenes.WildShape, "dukaforma");
        binder.BindFirst(AppConfig.Scenes.Inventory, "inventar");
        binder.BindFirst(AppConfig.Scenes.Inventory, "inventory");
        binder.BindFirst(AppConfig.Scenes.CharacterInfo, "ifopropersonaj");
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
            return;
        }


        saveManager.SetActiveSceneDataName(sceneDataName);
        UpdatePageTitle(pageIndex);
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

        TextMesh[] textMeshes = FindObjectsByType<TextMesh>(FindObjectsInactive.Include);
        foreach (TextMesh text in textMeshes)
            if (IsPageTitle(text != null ? text.transform : null, text != null ? text.text : ""))
                text.text = title;
    }

    private bool IsPageTitle(Transform transform, string text)
    {
        if (transform != null && transform.TryGetComponent(out UiControlMarker marker))
            return marker.Role == UiControlRole.PageTitle;
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

    private void KeepMainPagePanelActive()
    {
        SceneRoleMarker page = new SceneRoleLookup().Find(SceneRole.MainPage);
        if (page != null)
        {
            page.gameObject.SetActive(true);
            return;
        }
        // Legacy scene fallback.
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
            if (transform != null && string.Equals(transform.name, "Page", StringComparison.Ordinal))
                transform.gameObject.SetActive(true);
    }

}
