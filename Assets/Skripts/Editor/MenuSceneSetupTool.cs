using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneSetupTool
{
    private const string CharacterButtonTemplateName = "CharacterButtonTemplate";
    private const string CharacterButtonPrefabPath = "Assets/Prefab/kartaPerson (1).prefab";

    [MenuItem("Tools/DnD/Create Character Button Template")]
    public static void CreateCharacterButtonTemplate()
    {
        MainMenuManager menuManager = Object.FindAnyObjectByType<MainMenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("MainMenuManager not found in the open scene.");
            return;
        }

        EnsureCharacterButtonTemplate(menuManager, true);
    }

    [MenuItem("Tools/DnD/Create Clean Character Menu")]
    public static void CreateCleanCharacterMenu()
    {
        MainMenuManager menuManager = Object.FindAnyObjectByType<MainMenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("MainMenuManager not found in the open scene.");
            return;
        }

        menuManager.EnsureEditableCharacterScrollView();
        EditorUtility.SetDirty(menuManager);
        EditorSceneManager.MarkSceneDirty(menuManager.gameObject.scene);
        Debug.Log("Created clean editable CharacterRowsScrollView.");
    }

    [MenuItem("Tools/DnD/Repair Menu Character List")]
    public static void RepairMenuCharacterList()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/menu.unity", OpenSceneMode.Single);
        MainMenuManager menuManager = Object.FindAnyObjectByType<MainMenuManager>();

        if (menuManager == null)
        {
            Debug.LogError("MainMenuManager not found in Assets/Scenes/menu.unity");
            return;
        }

        RectTransform scrollRectTransform = menuManager.transform as RectTransform;
        if (scrollRectTransform != null)
        {
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, -120f);
            scrollRectTransform.sizeDelta = new Vector2(620f, 980f);
        }

        VerticalLayoutGroup wrongLayout = menuManager.GetComponent<VerticalLayoutGroup>();
        if (wrongLayout != null)
            Object.DestroyImmediate(wrongLayout);

        ContentSizeFitter wrongFitter = menuManager.GetComponent<ContentSizeFitter>();
        if (wrongFitter != null)
            Object.DestroyImmediate(wrongFitter);

        ScrollRect scrollRect = menuManager.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            if (scrollRect.viewport != null)
            {
                scrollRect.viewport.anchorMin = Vector2.zero;
                scrollRect.viewport.anchorMax = Vector2.one;
                scrollRect.viewport.offsetMin = Vector2.zero;
                scrollRect.viewport.offsetMax = Vector2.zero;
            }
        }

        RectTransform content = menuManager.characterListContent as RectTransform;
        if (content == null && scrollRect != null)
            content = scrollRect.content;

        if (content != null)
        {
            menuManager.characterListContent = content;

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 12);

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ConfigureContentLayoutForTemplateSize(content);

            if (scrollRect != null)
                scrollRect.content = content;
        }

        menuManager.openCharacterAfterCreate = false;
        menuManager.useCharacterButtonPrefab = true;
        menuManager.repairScrollViewAtRuntime = false;
        menuManager.applyDefaultCharacterListLayout = false;
        menuManager.applyDefaultCharacterButtonStyle = false;
        EnsureCharacterButtonTemplate(menuManager, false);

        EditorUtility.SetDirty(menuManager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Menu character list repaired and menu.unity saved.");
    }

    private static void EnsureCharacterButtonTemplate(MainMenuManager menuManager, bool logWhenExists)
    {
        if (menuManager.characterListContent == null)
            return;

        Transform existingTemplate = menuManager.characterListContent.Find(CharacterButtonTemplateName);
        if (existingTemplate != null)
        {
            menuManager.characterButtonTemplate = existingTemplate.gameObject;
            menuManager.useCharacterButtonPrefab = true;
            ConfigureContentLayoutForTemplateSize(menuManager.characterListContent);
            EditorUtility.SetDirty(menuManager);

            if (logWhenExists)
                Debug.Log("CharacterButtonTemplate already exists in Content.");

            return;
        }

        GameObject prefab = menuManager.characterButtonPrefab != null
            ? menuManager.characterButtonPrefab
            : AssetDatabase.LoadAssetAtPath<GameObject>(CharacterButtonPrefabPath);

        if (prefab == null)
        {
            Debug.LogError("Character button prefab not found: " + CharacterButtonPrefabPath);
            return;
        }

        GameObject template = PrefabUtility.InstantiatePrefab(prefab, menuManager.characterListContent) as GameObject;
        if (template == null)
            return;

        template.name = CharacterButtonTemplateName;
        template.SetActive(true);
        template.transform.SetAsFirstSibling();

        menuManager.characterButtonTemplate = template;
        menuManager.characterButtonPrefab = prefab;
        menuManager.useCharacterButtonPrefab = true;
        menuManager.repairScrollViewAtRuntime = false;
        menuManager.applyDefaultCharacterListLayout = false;
        menuManager.applyDefaultCharacterButtonStyle = false;
        ConfigureContentLayoutForTemplateSize(menuManager.characterListContent);

        EditorUtility.SetDirty(template);
        EditorUtility.SetDirty(menuManager);
        EditorSceneManager.MarkSceneDirty(menuManager.gameObject.scene);

        Debug.Log("Created CharacterButtonTemplate in Content. You can edit this object in Edit Mode.");
    }

    private static void ConfigureContentLayoutForTemplateSize(Transform content)
    {
        if (content == null)
            return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.enabled = false;
            EditorUtility.SetDirty(layout);
        }

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
            EditorUtility.SetDirty(fitter);
        }
    }
}
