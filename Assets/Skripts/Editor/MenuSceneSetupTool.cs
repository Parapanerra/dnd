using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneSetupTool
{
    private const string CharacterButtonTemplateName = "CharacterButtonTemplate";
    private const string CharacterButtonPrefabPath = "Assets/Prefab/kartaPerson (1).prefab";
    private const string CleanMenuRootName = "CharacterMenuRoot";
    private const string RowsContentName = "RowsContent";
    private const string RowTemplateName = "CharacterRowTemplate";

    [MenuItem("Tools/DnD/Create Character Button Template")]
    public static void CreateCharacterButtonTemplate()
    {
        MainMenuManager menuManager = Object.FindObjectOfType<MainMenuManager>();
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
        MainMenuManager menuManager = Object.FindObjectOfType<MainMenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("MainMenuManager not found in the open scene.");
            return;
        }

        menuManager.EnsureEditableCharacterScrollView();
        EditorUtility.SetDirty(menuManager);
        EditorSceneManager.MarkSceneDirty(menuManager.gameObject.scene);
        Debug.Log("Created clean editable CharacterRowsScrollView.");
        return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in the open scene.");
            return;
        }

        Transform existingRoot = canvas.transform.Find(CleanMenuRootName);
        GameObject root = existingRoot != null
            ? existingRoot.gameObject
            : CreateUiObject(CleanMenuRootName, canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), new Vector2(560f, 650f));

        Transform rowsContent = root.transform.Find(RowsContentName);
        if (rowsContent == null)
            rowsContent = CreateUiObject(RowsContentName, root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(560f, 500f)).transform;

        Transform rowTemplate = rowsContent.Find(RowTemplateName);
        if (rowTemplate == null)
            rowTemplate = CreateCharacterRowTemplate(rowsContent).transform;

        Button addButton = GetOrCreateButton(root.transform, "AddCharacterButton", new Vector2(0f, -292f), new Vector2(310f, 58f), "Додати персонажа");

        menuManager.characterRowsContent = rowsContent;
        menuManager.characterRowTemplate = rowTemplate.gameObject;
        menuManager.addCharacterButton = addButton;
        menuManager.characterSheetSceneName = "cartaPersonaj";
        menuManager.inventorySceneName = "inventory";
        menuManager.spellbookSceneName = "spelBook";
        menuManager.openCharacterAfterCreate = false;
        menuManager.repairScrollViewAtRuntime = false;
        menuManager.applyDefaultCharacterListLayout = false;
        menuManager.applyDefaultCharacterButtonStyle = false;
        menuManager.characterRowSpacing = 12f;

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(menuManager);
        EditorSceneManager.MarkSceneDirty(menuManager.gameObject.scene);

        Debug.Log("Created clean editable character menu. Edit CharacterMenuRoot/RowsContent/CharacterRowTemplate visuals in Edit Mode.");
    }

    [MenuItem("Tools/DnD/Repair Menu Character List")]
    public static void RepairMenuCharacterList()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/menu.unity", OpenSceneMode.Single);
        MainMenuManager menuManager = Object.FindObjectOfType<MainMenuManager>();

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

    private static GameObject CreateCharacterRowTemplate(Transform parent)
    {
        GameObject row = CreateUiObject(RowTemplateName, parent, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(560f, 78f));

        GetOrCreateButton(row.transform, "InventoryButton", new Vector2(-235f, 0f), new Vector2(66f, 66f), "I");
        GetOrCreateButton(row.transform, "CharacterButton", new Vector2(-25f, 0f), new Vector2(330f, 66f), "Персонаж №1");
        GetOrCreateButton(row.transform, "SpellsButton", new Vector2(185f, 0f), new Vector2(66f, 66f), "S");
        GetOrCreateButton(row.transform, "DeleteButton", new Vector2(265f, 0f), new Vector2(66f, 66f), "X");

        return row;
    }

    private static Button GetOrCreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
                return existingButton;
        }

        GameObject buttonObject = CreateUiObject(name, parent, new Vector2(0.5f, 0.5f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.09f, 0.07f, 0.9f);
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = CreateUiObject("Text", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.color = new Color(0.95f, 0.55f, 0.05f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 28;
        text.raycastTarget = false;

        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return gameObject;
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
