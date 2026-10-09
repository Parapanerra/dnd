using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Renders the scene's existing character menu without owning save or navigation logic.
public class MainMenuCharacterListView : MonoBehaviour
{
    private MainMenuManager owner;
    private DndSaveManager saveManager;
    private Button userCharacterButtonTemplate;
    private Transform userMenuRoot;
    private Transform userMenuContent;
    private RectTransform lastUserMenuRowRect;
    private Vector3 addButtonWorldOffsetFromTemplate;
    private bool hasAddButtonWorldOffsetFromTemplate;
    private Coroutine addButtonPositionCoroutine;

    public void Initialize(MainMenuManager manager, DndSaveManager dataManager)
    {
        owner = manager;
        saveManager = dataManager;
    }

    public void EnsureEditableCharacterScrollView()
    {
        if (owner == null)
            return;

        Transform parent = owner.transform.parent != null ? owner.transform.parent : owner.transform;
        Transform existingScrollView = parent.Find("CharacterRowsScrollView");
        if (existingScrollView != null)
        {
            WireEditableCharacterScrollView(existingScrollView);
            return;
        }

        GameObject scrollView = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "CharacterRowsScrollView",
            parent,
            AppConfig.MainMenu.CenterAnchor,
            AppConfig.MainMenu.CharacterScrollPosition,
            AppConfig.MainMenu.CharacterScrollSize));
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = AppConfig.MainMenu.Transparent;
        scrollImage.raycastTarget = false;
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "Viewport",
            scrollView.transform,
            AppConfig.MainMenu.CenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterScrollSize));
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = AppConfig.MainMenu.Transparent;
        viewportImage.raycastTarget = false;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "RowsContent",
            viewport.transform,
            AppConfig.MainMenu.TopCenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterScrollSize));
        GameObject rowTemplate = CreateDefaultCharacterRowTemplate(content.transform);
        GameObject addButtonObject = RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "AddCharacterButton",
            scrollView.transform,
            AppConfig.MainMenu.AddButtonPosition,
            AppConfig.MainMenu.AddButtonSize,
            "Додати персонажа"));

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();

        owner.characterRowsContent = content.transform;
        owner.characterRowTemplate = rowTemplate;
        owner.addCharacterButton = addButtonObject.GetComponent<Button>();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(owner);
        UnityEditor.EditorUtility.SetDirty(scrollView);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
#endif
    }

    private void WireEditableCharacterScrollView(Transform scrollView)
    {
        Transform viewport = scrollView.Find("Viewport");
        Transform content = viewport != null ? viewport.Find("RowsContent") : scrollView.Find("RowsContent");
        Transform rowTemplate = content != null ? content.Find("CharacterRowTemplate") : null;
        Transform addButton = scrollView.Find("AddCharacterButton");

        if (content != null)
            owner.characterRowsContent = content;
        if (rowTemplate != null)
            owner.characterRowTemplate = rowTemplate.gameObject;
        if (addButton != null)
            owner.addCharacterButton = addButton.GetComponent<Button>();
    }

    private GameObject CreateDefaultCharacterRowTemplate(Transform parent)
    {
        GameObject row = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "CharacterRowTemplate",
            parent,
            AppConfig.MainMenu.TopCenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterRowSize));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "InventoryButton", row.transform, AppConfig.MainMenu.InventoryButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "I"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "CharacterButton", row.transform, AppConfig.MainMenu.CharacterButtonPosition, AppConfig.MainMenu.CharacterNameButtonSize, "Персонаж №1"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "SpellsButton", row.transform, AppConfig.MainMenu.SpellsButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "S"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "DeleteButton", row.transform, AppConfig.MainMenu.DeleteRowButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "X"));
        return row;
    }

    public void Wire()
    {
        ScrollRect menuScroll = MainMenuSceneLookup.FindScrollRectInScene("menukart");
        if (menuScroll == null || menuScroll.content == null)
        {
            Button addButton = MainMenuSceneLookup.FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                owner.addCharacterButton = addButton;

            return;
        }

        ConfigureUserMenuScroll(menuScroll);

        userMenuRoot = menuScroll.transform;
        userMenuContent = menuScroll.content;

        userCharacterButtonTemplate = MainMenuSceneLookup.FindButtonUnder(userMenuContent, "kartaPerson (1)");
        if (userCharacterButtonTemplate == null)
            userCharacterButtonTemplate = MainMenuSceneLookup.FindFirstDirectButtonInContent(userMenuContent);

        Button contentAddButton = MainMenuSceneLookup.FindButtonUnder(userMenuContent, "newpersonajbaton");
        if (contentAddButton != null)
            owner.addCharacterButton = contentAddButton;
        else
        {
            Button addButton = MainMenuSceneLookup.FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                owner.addCharacterButton = addButton;
        }

        if (userCharacterButtonTemplate == null)
            Debug.LogWarning("MainMenuManager: cannot find character button template 'kartaPerson (1)' under menukart.");
        if (owner.addCharacterButton == null)
            Debug.LogWarning("MainMenuManager: cannot find add button 'newpersonajbaton'.");
    }

    private void ConfigureUserMenuScroll(ScrollRect menuScroll)
    {
        menuScroll.horizontal = false;
        menuScroll.vertical = true;
        menuScroll.movementType = ScrollRect.MovementType.Clamped;
        menuScroll.inertia = true;
        menuScroll.decelerationRate = 0.08f;
        menuScroll.scrollSensitivity = AppConfig.MainMenu.MenuScrollSensitivity;
        menuScroll.horizontalNormalizedPosition = 0f;

        RectTransform contentRect = menuScroll.content;
        if (contentRect != null)
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);

        DisableAutomaticLayout(menuScroll.content);
    }

    private bool RefreshUserCreatedMenu()
    {
        Wire();
        if (userMenuContent == null || userCharacterButtonTemplate == null)
            return false;

        KeepAddButtonVisibleOutsideTemplate();
        SetTemplateActive(false);

        List<GameObject> rowsToDestroy = new List<GameObject>();
        Transform searchRoot = userMenuRoot != null ? userMenuRoot : userMenuContent;
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == searchRoot || child == userMenuContent)
                continue;

            if (IsTemplateOrTemplateChild(child))
                continue;

            if (child.name.StartsWith("CharacterMenuClone_"))
                rowsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject row in rowsToDestroy)
            Destroy(row);

        lastUserMenuRowRect = null;
        int index = 0;
        foreach (CharacterData character in saveManager.GetCharactersSnapshot())
        {
            CreateUserMenuCharacterRow(character, index);
            index++;
        }

        PositionAddButtonAfterRows();
        ResizeUserMenuContent(index);
        ScheduleAddButtonPositionRefresh();
        return true;
    }

    private void CreateUserMenuCharacterRow(CharacterData character, int index)
    {
        Button characterButton = CloneUserMenuButton(userCharacterButtonTemplate, "CharacterMenuClone_Row_" + index, index);
        if (characterButton == null)
            return;

        Button inventoryButton = MainMenuSceneLookup.FindButtonUnder(characterButton.transform, "spellbook");
        Button spellsButton = MainMenuSceneLookup.FindFirstButtonUnder(characterButton.transform, "invetory", "inventory");
        Button deleteButton = MainMenuSceneLookup.FindButtonUnder(characterButton.transform, "DeleteButton");

        Text nameText = FindCharacterNameText(characterButton);
        if (nameText != null)
            IgnoreLocalizationForDynamicText(nameText.gameObject);

        if (nameText != null)
            nameText.text = MainMenuCharacterLabels.GetDisplayName(character);

        string characterId = character.id;
        MainMenuSceneLookup.BindButton(characterButton, () => owner.OnCharacterSelected(characterId));
        MainMenuSceneLookup.BindButton(inventoryButton, () => owner.OnInventorySelected(characterId));
        MainMenuSceneLookup.BindButton(spellsButton, () => owner.OnSpellbookSelected(characterId));
        MainMenuSceneLookup.BindButton(deleteButton, () =>
        {
            saveManager.DeleteCharacter(characterId);
            owner.RefreshCharacterList();
        });

    }

    private void IgnoreLocalizationForDynamicText(GameObject textObject)
    {
        if (textObject != null && textObject.GetComponent<LocalizedIgnore>() == null)
            textObject.AddComponent<LocalizedIgnore>();
    }

    private Text FindCharacterNameText(Button rowButton)
    {
        if (rowButton == null)
            return null;

        Transform legacyText = rowButton.transform.Find("Text (Legacy)");
        if (legacyText != null && legacyText.TryGetComponent(out Text legacyTextComponent))
            return legacyTextComponent;

        Transform text = rowButton.transform.Find("Text");
        if (text != null && text.TryGetComponent(out Text textComponent))
            return textComponent;

        Text[] texts = rowButton.GetComponentsInChildren<Text>(true);
        foreach (Text candidate in texts)
        {
            Button ownerButton = FindOwningButton(candidate.transform);
            if (ownerButton == rowButton)
                return candidate;
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private Button FindOwningButton(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            Button button = current.GetComponent<Button>();
            if (button != null)
                return button;

            current = current.parent;
        }

        return null;
    }

    private Button CloneUserMenuButton(Button template, string cloneName, int index)
    {
        if (template == null)
            return null;

        GameObject clone = Instantiate(template.gameObject, template.transform.parent, false);
        clone.name = cloneName;
        clone.SetActive(true);
        RemoveAddButtonsFromClone(clone.transform);

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
        {
            cloneRect.anchorMin = templateRect.anchorMin;
            cloneRect.anchorMax = templateRect.anchorMax;
            cloneRect.pivot = templateRect.pivot;
            cloneRect.sizeDelta = templateRect.sizeDelta;
            cloneRect.localScale = templateRect.localScale;
            cloneRect.localRotation = templateRect.localRotation;

            float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
            cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + owner.characterRowSpacing));
            lastUserMenuRowRect = cloneRect;
        }

        return clone.GetComponent<Button>();
    }

    private void SetTemplateActive(bool active)
    {
        if (userCharacterButtonTemplate != null)
            userCharacterButtonTemplate.gameObject.SetActive(active);
    }

    private bool IsTemplateOrTemplateChild(Transform child)
    {
        return IsSameOrChild(child, userCharacterButtonTemplate);
    }

    private bool IsSameOrChild(Transform child, Button button)
    {
        return button != null && (child == button.transform || child.IsChildOf(button.transform));
    }

    private void RemoveAddButtonsFromClone(Transform cloneRoot)
    {
        Button[] buttons = cloneRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            if (button != null && MainMenuSceneLookup.Matches(button.gameObject.name, "newpersonajbaton"))
                Destroy(button.gameObject);
    }

    private void KeepAddButtonVisibleOutsideTemplate()
    {
        if (owner.addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null)
            return;

        Transform addTransform = owner.addCharacterButton.transform;
        RectTransform addRect = owner.addCharacterButton.GetComponent<RectTransform>();
        RectTransform templateRect = userCharacterButtonTemplate.GetComponent<RectTransform>();

        if (!hasAddButtonWorldOffsetFromTemplate && addRect != null && templateRect != null)
        {
            addButtonWorldOffsetFromTemplate = addRect.position - templateRect.position;
            hasAddButtonWorldOffsetFromTemplate = true;
        }

        if (addTransform.IsChildOf(userCharacterButtonTemplate.transform))
            addTransform.SetParent(userMenuContent, true);
    }

    private void PositionAddButtonAfterRows()
    {
        if (owner.addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null || !hasAddButtonWorldOffsetFromTemplate)
            return;

        RectTransform addRect = owner.addCharacterButton.GetComponent<RectTransform>();
        RectTransform templateRect = userCharacterButtonTemplate.GetComponent<RectTransform>();
        if (addRect == null || templateRect == null || owner.addCharacterButton.transform.parent != userMenuContent)
            return;

        RectTransform targetRow = lastUserMenuRowRect != null ? lastUserMenuRowRect : templateRect;
        addRect.position = targetRow.position + addButtonWorldOffsetFromTemplate;
    }

    private void ScheduleAddButtonPositionRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (addButtonPositionCoroutine != null)
            StopCoroutine(addButtonPositionCoroutine);

        addButtonPositionCoroutine = StartCoroutine(RefreshAddButtonPositionAtEndOfFrame());
    }

    private IEnumerator RefreshAddButtonPositionAtEndOfFrame()
    {
        yield return null;
        PositionAddButtonAfterRows();
        ResizeUserMenuContent(saveManager != null ? saveManager.GetCharacterCount() : 0);
        addButtonPositionCoroutine = null;
    }

    private void ResizeUserMenuContent(int rowCount)
    {
        RectTransform contentRect = userMenuContent as RectTransform;
        RectTransform templateRect = userCharacterButtonTemplate != null ? userCharacterButtonTemplate.GetComponent<RectTransform>() : null;
        if (contentRect == null || templateRect == null || rowCount <= 0)
            return;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        float addHeight = 0f;
        float addBottom = 0f;
        if (owner.addCharacterButton != null && owner.addCharacterButton.transform.parent == userMenuContent && owner.addCharacterButton.TryGetComponent(out RectTransform addRect))
        {
            addHeight = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, addRect.rect.height);
            addBottom = Mathf.Abs(addRect.anchoredPosition.y) + addHeight +
                AppConfig.MainMenu.ContentBottomPadding;
        }

        float bottom = Mathf.Abs(templateRect.anchoredPosition.y) + rowCount * height +
            Mathf.Max(0, rowCount) * owner.characterRowSpacing + addHeight +
            AppConfig.MainMenu.ContentBottomPadding;
        bottom = Mathf.Max(bottom, addBottom);
        if (contentRect.sizeDelta.y < bottom)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, bottom);
    }

    public void Refresh()
    {
        if (RefreshUserCreatedMenu())
            return;
        if (owner.characterRowsContent != null && owner.characterRowTemplate != null)
        {
            RefreshCharacterRows();
            return;
        }
        RefreshLegacyButtons();
    }

    private void RefreshLegacyButtons()
    {
        if (saveManager == null || owner.characterListContent == null)
            return;

        if (owner.applyDefaultCharacterListLayout)
            EnsureCharacterListLayout();
        CacheCharacterButtonTemplate();
        DisableAutomaticContentLayout();
        if (owner.characterButtonTemplate != null)
            owner.characterButtonTemplate.SetActive(false);

        RectTransform templateRect = owner.characterButtonTemplate != null
            ? owner.characterButtonTemplate.GetComponent<RectTransform>()
            : null;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in owner.characterListContent)
        {
            if (owner.characterButtonTemplate != null && child.gameObject == owner.characterButtonTemplate)
                continue;

            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
            Destroy(child);

        int buttonIndex = 0;
        RectTransform layoutSourceRect = templateRect;
        foreach (CharacterData character in saveManager.GetCharactersSnapshot())
        {
            GameObject btnObj = CreateCharacterButtonObject(character);
            btnObj.transform.SetParent(owner.characterListContent, false);
            btnObj.SetActive(true);
            btnObj.transform.localScale = Vector3.one;

            RectTransform rectTransform = btnObj.GetComponent<RectTransform>();
            if (layoutSourceRect == null)
                layoutSourceRect = rectTransform;

            ApplyTemplateRectToClone(layoutSourceRect, rectTransform, buttonIndex);
            if (owner.applyDefaultCharacterListLayout && rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(0f, AppConfig.MainMenu.CharacterButtonHeight);
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => owner.OnCharacterSelected(character.id));
            }

            Transform deleteBtnTransform = btnObj.transform.Find("DeleteButton");
            if (deleteBtnTransform != null)
            {
                Button deleteBtn = deleteBtnTransform.GetComponent<Button>();
                if (deleteBtn != null)
                {
                    string characterId = character.id;
                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(() =>
                    {
                        saveManager.DeleteCharacter(characterId);
                        owner.RefreshCharacterList();
                    });
                }
            }

            buttonIndex++;
        }

        ResizeContentForManualLayout(layoutSourceRect, buttonIndex);
    }

    private void RefreshCharacterRows()
    {
        if (saveManager == null)
            return;

        owner.characterRowTemplate.SetActive(false);

        List<GameObject> rowsToDestroy = new List<GameObject>();
        foreach (Transform child in owner.characterRowsContent)
        {
            if (child.gameObject == owner.characterRowTemplate)
                continue;

            rowsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject row in rowsToDestroy)
            Destroy(row);

        RectTransform templateRect = owner.characterRowTemplate.GetComponent<RectTransform>();
        int rowIndex = 0;
        foreach (CharacterData character in saveManager.GetCharactersSnapshot())
        {
            GameObject row = Instantiate(owner.characterRowTemplate, owner.characterRowsContent, false);
            row.name = "CharacterRow_" + (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);
            row.SetActive(true);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            ApplyRowTemplateRect(templateRect, rowRect, rowIndex);

            BindCharacterRow(row.transform, character);
            rowIndex++;
        }

        ResizeRowsContent(templateRect, rowIndex);
    }

    private void ApplyRowTemplateRect(RectTransform templateRect, RectTransform rowRect, int index)
    {
        if (templateRect == null || rowRect == null)
            return;

        rowRect.anchorMin = templateRect.anchorMin;
        rowRect.anchorMax = templateRect.anchorMax;
        rowRect.pivot = templateRect.pivot;
        rowRect.sizeDelta = templateRect.sizeDelta;
        rowRect.localRotation = templateRect.localRotation;
        rowRect.localScale = templateRect.localScale;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        rowRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + owner.characterRowSpacing));
    }

    private void ResizeRowsContent(RectTransform templateRect, int rowCount)
    {
        RectTransform contentRect = owner.characterRowsContent as RectTransform;
        if (contentRect == null || templateRect == null || rowCount <= 0)
            return;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        float topOffset = Mathf.Abs(templateRect.anchoredPosition.y);
        float requiredHeight = topOffset + rowCount * height +
            Mathf.Max(0, rowCount - 1) * owner.characterRowSpacing +
            AppConfig.MainMenu.ContentBottomPadding;
        if (contentRect.sizeDelta.y < requiredHeight)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    private void BindCharacterRow(Transform row, CharacterData character)
    {
        Button characterButton = FindButton(row, "CharacterButton");
        Button inventoryButton = FindButton(row, "InventoryButton");
        Button deleteButton = FindButton(row, "DeleteButton");
        Button spellsButton = FindButton(row, "SpellsButton");

        Text nameText = characterButton != null
            ? characterButton.GetComponentInChildren<Text>(true)
            : row.GetComponentInChildren<Text>(true);
        if (nameText != null)
            nameText.text = MainMenuCharacterLabels.GetDisplayName(character);

        string characterId = character.id;
        MainMenuSceneLookup.BindButton(characterButton, () => owner.OnCharacterSelected(characterId));
        MainMenuSceneLookup.BindButton(inventoryButton, () => owner.OnInventorySelected(characterId));
        MainMenuSceneLookup.BindButton(spellsButton, () => owner.OnSpellbookSelected(characterId));
        MainMenuSceneLookup.BindButton(deleteButton, () =>
        {
            saveManager.DeleteCharacter(characterId);
            owner.RefreshCharacterList();
        });
    }

    private Button FindButton(Transform root, string name)
    {
        Transform found = root.Find(name);
        return found != null ? found.GetComponent<Button>() : null;
    }

    public void CacheCharacterButtonTemplate()
    {
        if (owner.characterButtonTemplate != null || owner.characterListContent == null)
            return;

        Transform template = owner.characterListContent.Find("CharacterButtonTemplate");
        if (template != null)
            owner.characterButtonTemplate = template.gameObject;
    }

    public void DisableAutomaticContentLayout()
    {
        if (owner.characterListContent == null)
            return;

        DisableAutomaticLayout(owner.characterListContent);
    }

    internal void DisableAutomaticLayout(Transform content)
    {
        if (content == null)
            return;

        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;
    }

    private void ApplyTemplateRectToClone(RectTransform templateRect, RectTransform cloneRect, int index)
    {
        if (templateRect == null || cloneRect == null)
            return;

        cloneRect.anchorMin = templateRect.anchorMin;
        cloneRect.anchorMax = templateRect.anchorMax;
        cloneRect.pivot = templateRect.pivot;
        cloneRect.sizeDelta = templateRect.sizeDelta;
        cloneRect.localRotation = templateRect.localRotation;
        cloneRect.localScale = templateRect.localScale;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + owner.characterButtonSpacing));
    }

    private void ResizeContentForManualLayout(RectTransform templateRect, int buttonCount)
    {
        RectTransform contentRect = owner.characterListContent as RectTransform;
        if (contentRect == null || templateRect == null || buttonCount <= 0)
            return;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        float topOffset = Mathf.Abs(templateRect.anchoredPosition.y);
        float requiredHeight = topOffset + buttonCount * height +
            Mathf.Max(0, buttonCount - 1) * owner.characterButtonSpacing +
            AppConfig.MainMenu.ContentBottomPadding;
        if (contentRect.sizeDelta.y < requiredHeight)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    public void EnsureCharacterListLayout()
    {
        if (owner.characterListContent == null)
            return;

        RectTransform contentRect = owner.characterListContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
        }

        VerticalLayoutGroup layoutGroup = owner.characterListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = owner.characterListContent.gameObject.AddComponent<VerticalLayoutGroup>();

        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = AppConfig.MainMenu.DefaultLayoutPadding;
        layoutGroup.padding = new RectOffset(
            AppConfig.MainMenu.DefaultLayoutPadding,
            AppConfig.MainMenu.DefaultLayoutPadding,
            AppConfig.MainMenu.DefaultLayoutTopPadding,
            AppConfig.MainMenu.DefaultLayoutPadding);

        ContentSizeFitter fitter = owner.characterListContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = owner.characterListContent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void EnsureScrollViewIsVisible()
    {
        ScrollRect scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
            return;

        VerticalLayoutGroup wrongLayoutGroup = GetComponent<VerticalLayoutGroup>();
        if (wrongLayoutGroup != null)
            wrongLayoutGroup.enabled = false;

        ContentSizeFitter wrongFitter = GetComponent<ContentSizeFitter>();
        if (wrongFitter != null)
            wrongFitter.enabled = false;

        RectTransform scrollRectTransform = transform as RectTransform;
        if (scrollRectTransform != null)
        {
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);

            Vector2 size = scrollRectTransform.sizeDelta;
            if (size.x < AppConfig.MainMenu.MinimumVisibleScrollSize)
                size.x = AppConfig.MainMenu.DefaultVisibleScrollWidth;
            if (size.y < AppConfig.MainMenu.MinimumVisibleScrollSize)
                size.y = AppConfig.MainMenu.DefaultVisibleScrollHeight;

            scrollRectTransform.sizeDelta = size;
        }

        if (scrollRect.viewport != null)
        {
            scrollRect.viewport.anchorMin = Vector2.zero;
            scrollRect.viewport.anchorMax = Vector2.one;
            scrollRect.viewport.offsetMin = Vector2.zero;
            scrollRect.viewport.offsetMax = Vector2.zero;
        }

        if (scrollRect.content == null && owner.characterListContent is RectTransform contentRect)
            scrollRect.content = contentRect;
    }

    private GameObject CreateCharacterButtonObject(CharacterData character)
    {
        GameObject sourceButton = owner.characterButtonTemplate != null
            ? owner.characterButtonTemplate
            : owner.useCharacterButtonPrefab && owner.characterButtonPrefab != null
                ? owner.characterButtonPrefab
                : null;

        GameObject btnObj = sourceButton != null
            ? Instantiate(sourceButton)
            : CreateFallbackCharacterButton();

        btnObj.name = "CharacterButton_" + (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);

        if (btnObj.GetComponent<Button>() == null)
            btnObj.AddComponent<Button>();

        Image image = btnObj.GetComponent<Image>();
        if (image == null)
            image = btnObj.AddComponent<Image>();

        bool isActive = saveManager != null &&
                        saveManager.GetActiveCharacterId() == character.id;
        if (owner.applyDefaultCharacterButtonStyle)
        {
            image.color = isActive
                ? AppConfig.MainMenu.ActiveCharacterColor
                : AppConfig.MainMenu.InactiveCharacterColor;
        }

        Text btnText = btnObj.GetComponentInChildren<Text>(true);
        if (btnText == null)
            btnText = CreateButtonText(btnObj.transform, "CharacterName", TextAnchor.MiddleLeft);

        btnText.text = MainMenuCharacterLabels.GetDisplayName(character, "Невідомий персонаж");
        if (owner.applyDefaultCharacterButtonStyle)
        {
            btnText.color = Color.white;
            btnText.fontSize = AppConfig.MainMenu.CharacterNameFontSize;
            btnText.resizeTextForBestFit = true;
            btnText.resizeTextMinSize = AppConfig.MainMenu.CharacterNameMinimumFontSize;
            btnText.resizeTextMaxSize = AppConfig.MainMenu.CharacterNameFontSize;
        }

        EnsureDeleteButton(btnObj.transform);
        return btnObj;
    }

    private GameObject CreateFallbackCharacterButton()
    {
        GameObject buttonObject = new GameObject("CharacterButton", typeof(RectTransform), typeof(Image), typeof(Button));
        CreateButtonText(buttonObject.transform, "CharacterName", TextAnchor.MiddleLeft);
        return buttonObject;
    }

    public Text CreateButtonText(Transform parent, string name, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(
            AppConfig.MainMenu.CharacterTextLeftPadding,
            AppConfig.MainMenu.CharacterTextVerticalPadding);
        textRect.offsetMax = new Vector2(
            -AppConfig.MainMenu.CharacterTextRightPadding,
            -AppConfig.MainMenu.CharacterTextVerticalPadding);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        text.alignment = alignment;
        return text;
    }

    public void EnsureDeleteButton(Transform parent)
    {
        Transform existingDeleteButton = parent.Find("DeleteButton");
        if (existingDeleteButton != null)
            return;

        GameObject deleteObject = new GameObject("DeleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(parent, false);

        RectTransform deleteRect = deleteObject.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(1f, 0.5f);
        deleteRect.anchorMax = new Vector2(1f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = AppConfig.MainMenu.DeleteButtonPosition;
        deleteRect.sizeDelta = AppConfig.MainMenu.DeleteButtonSize;

        Image deleteImage = deleteObject.GetComponent<Image>();
        deleteImage.color = AppConfig.MainMenu.DeleteButtonColor;

        Text deleteText = CreateButtonText(deleteObject.transform, "Text", TextAnchor.MiddleCenter);
        RectTransform textRect = deleteText.GetComponent<RectTransform>();
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        deleteText.text = "X";
        deleteText.color = Color.white;
        deleteText.fontSize = AppConfig.MainMenu.DeleteButtonFontSize;
    }
}
