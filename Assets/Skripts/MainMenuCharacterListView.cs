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
    private MainMenuCharacterLabels characterLabels;
    private MainMenuCharacterButtonFactory buttonFactory;
    private readonly MainMenuSceneLookup sceneLookup = new MainMenuSceneLookup();
    [SerializeField, HideInInspector] private Transform characterRowsContent;
    [SerializeField, HideInInspector] private GameObject characterRowTemplate;
    [SerializeField, HideInInspector] private GameObject characterButtonTemplate;
    [SerializeField, HideInInspector] private Button addCharacterButton;
    internal Button AddCharacterButton => addCharacterButton;

    private Button userCharacterButtonTemplate;
    private Transform userMenuRoot;
    private Transform userMenuContent;
    private RectTransform lastUserMenuRowRect;
    private Vector3 addButtonWorldOffsetFromTemplate;
    private bool hasAddButtonWorldOffsetFromTemplate;
    private Coroutine addButtonPositionCoroutine;

    internal void Initialize(MainMenuManager manager, DndSaveManager dataManager, MainMenuCharacterLabels labels)
    {
        owner = manager;
        saveManager = dataManager;
        characterLabels = labels;
        buttonFactory = new MainMenuCharacterButtonFactory(manager, dataManager, labels);
    }

    internal void SetCharacterButtonTemplate(GameObject template)
    {
        characterButtonTemplate = template;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    public void SetEditableCharacterScrollViewReferences(Transform content, GameObject rowTemplate, Button addButton)
    {
        if (content != null)
            characterRowsContent = content;
        if (rowTemplate != null)
            characterRowTemplate = rowTemplate;
        if (addButton != null)
            addCharacterButton = addButton;
    }
#endif

    internal void Wire()
    {
        ScrollRect menuScroll = sceneLookup.FindScrollRectInScene("menukart");
        if (menuScroll == null || menuScroll.content == null)
        {
            Button addButton = sceneLookup.FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                addCharacterButton = addButton;

            return;
        }

        ConfigureUserMenuScroll(menuScroll);

        userMenuRoot = menuScroll.transform;
        userMenuContent = menuScroll.content;

        userCharacterButtonTemplate = sceneLookup.FindButtonUnder(userMenuContent, "kartaPerson (1)");
        if (userCharacterButtonTemplate == null)
            userCharacterButtonTemplate = sceneLookup.FindFirstDirectButtonInContent(userMenuContent);

        Button contentAddButton = sceneLookup.FindButtonUnder(userMenuContent, "newpersonajbaton");
        if (contentAddButton != null)
            addCharacterButton = contentAddButton;
        else
        {
            Button addButton = sceneLookup.FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                addCharacterButton = addButton;
        }

        if (userCharacterButtonTemplate == null)
            Debug.LogWarning("MainMenuManager: cannot find character button template 'kartaPerson (1)' under menukart.");
        if (addCharacterButton == null)
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

        Button inventoryButton = sceneLookup.FindButtonUnder(characterButton.transform, "spellbook");
        Button spellsButton = sceneLookup.FindFirstButtonUnder(characterButton.transform, "invetory", "inventory");
        Button deleteButton = sceneLookup.FindButtonUnder(characterButton.transform, "DeleteButton");

        Text nameText = FindCharacterNameText(characterButton);
        if (nameText != null)
            IgnoreLocalizationForDynamicText(nameText.gameObject);

        if (nameText != null)
            nameText.text = characterLabels.GetDisplayName(character);

        BindCharacterActions(character, characterButton, inventoryButton, spellsButton, deleteButton);
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
        ApplyTemplateRectToClone(templateRect, cloneRect, index, owner.CharacterRowSpacing);
        if (templateRect != null && cloneRect != null)
            lastUserMenuRowRect = cloneRect;

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
            if (button != null && sceneLookup.Matches(button.gameObject.name, "newpersonajbaton"))
                Destroy(button.gameObject);
    }

    private void KeepAddButtonVisibleOutsideTemplate()
    {
        if (addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null)
            return;

        Transform addTransform = addCharacterButton.transform;
        RectTransform addRect = addCharacterButton.GetComponent<RectTransform>();
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
        if (addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null || !hasAddButtonWorldOffsetFromTemplate)
            return;

        RectTransform addRect = addCharacterButton.GetComponent<RectTransform>();
        RectTransform templateRect = userCharacterButtonTemplate.GetComponent<RectTransform>();
        if (addRect == null || templateRect == null || addCharacterButton.transform.parent != userMenuContent)
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
        if (addCharacterButton != null && addCharacterButton.transform.parent == userMenuContent && addCharacterButton.TryGetComponent(out RectTransform addRect))
        {
            addHeight = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, addRect.rect.height);
            addBottom = Mathf.Abs(addRect.anchoredPosition.y) + addHeight +
                AppConfig.MainMenu.ContentBottomPadding;
        }

        float bottom = Mathf.Abs(templateRect.anchoredPosition.y) + rowCount * height +
            Mathf.Max(0, rowCount) * owner.CharacterRowSpacing + addHeight +
            AppConfig.MainMenu.ContentBottomPadding;
        bottom = Mathf.Max(bottom, addBottom);
        if (contentRect.sizeDelta.y < bottom)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, bottom);
    }

    internal void Refresh()
    {
        if (RefreshUserCreatedMenu())
            return;
        if (characterRowsContent != null && characterRowTemplate != null)
        {
            RefreshCharacterRows();
            return;
        }
        RefreshLegacyButtons();
    }

    private void RefreshLegacyButtons()
    {
        if (saveManager == null || owner.CharacterListContent == null)
            return;

        if (owner.ApplyDefaultCharacterListLayout)
            EnsureCharacterListLayout();
        CacheCharacterButtonTemplate();
        DisableAutomaticContentLayout();
        if (characterButtonTemplate != null)
            characterButtonTemplate.SetActive(false);

        RectTransform templateRect = characterButtonTemplate != null
            ? characterButtonTemplate.GetComponent<RectTransform>()
            : null;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in owner.CharacterListContent)
        {
            if (characterButtonTemplate != null && child.gameObject == characterButtonTemplate)
                continue;

            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
            Destroy(child);

        int buttonIndex = 0;
        RectTransform layoutSourceRect = templateRect;
        foreach (CharacterData character in saveManager.GetCharactersSnapshot())
        {
            GameObject btnObj = buttonFactory.Create(character, characterButtonTemplate);
            btnObj.transform.SetParent(owner.CharacterListContent, false);
            btnObj.SetActive(true);
            btnObj.transform.localScale = Vector3.one;

            RectTransform rectTransform = btnObj.GetComponent<RectTransform>();
            if (layoutSourceRect == null)
                layoutSourceRect = rectTransform;

            ApplyTemplateRectToClone(layoutSourceRect, rectTransform, buttonIndex, owner.CharacterButtonSpacing);
            if (owner.ApplyDefaultCharacterListLayout && rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(0f, AppConfig.MainMenu.CharacterButtonHeight);
            }

            Button btn = btnObj.GetComponent<Button>();
            Transform deleteBtnTransform = btnObj.transform.Find("DeleteButton");
            Button deleteBtn = deleteBtnTransform != null ? deleteBtnTransform.GetComponent<Button>() : null;
            BindCharacterActions(character, btn, null, null, deleteBtn);

            buttonIndex++;
        }

        ResizeContentForManualLayout(owner.CharacterListContent as RectTransform, layoutSourceRect, buttonIndex, owner.CharacterButtonSpacing);
    }

    private void RefreshCharacterRows()
    {
        if (saveManager == null)
            return;

        characterRowTemplate.SetActive(false);

        List<GameObject> rowsToDestroy = new List<GameObject>();
        foreach (Transform child in characterRowsContent)
        {
            if (child.gameObject == characterRowTemplate)
                continue;

            rowsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject row in rowsToDestroy)
            Destroy(row);

        RectTransform templateRect = characterRowTemplate.GetComponent<RectTransform>();
        int rowIndex = 0;
        foreach (CharacterData character in saveManager.GetCharactersSnapshot())
        {
            GameObject row = Instantiate(characterRowTemplate, characterRowsContent, false);
            row.name = "CharacterRow_" + (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);
            row.SetActive(true);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            ApplyTemplateRectToClone(templateRect, rowRect, rowIndex, owner.CharacterRowSpacing);

            BindCharacterRow(row.transform, character);
            rowIndex++;
        }

        ResizeContentForManualLayout(characterRowsContent as RectTransform, templateRect, rowIndex, owner.CharacterRowSpacing);
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
        {
            IgnoreLocalizationForDynamicText(nameText.gameObject);
            nameText.text = characterLabels.GetDisplayName(character);
        }

        BindCharacterActions(character, characterButton, inventoryButton, spellsButton, deleteButton);
    }

    private void BindCharacterActions(CharacterData character, Button characterButton, Button inventoryButton,
        Button spellsButton, Button deleteButton)
    {
        string characterId = character.id;
        BindButton(characterButton, () => owner.OnCharacterSelected(characterId));
        BindButton(inventoryButton, () => owner.OnInventorySelected(characterId));
        BindButton(spellsButton, () => owner.OnSpellbookSelected(characterId));
        BindButton(deleteButton, () =>
        {
            CharacterData selected = saveManager.GetCharacter(characterId);
            if (selected == null)
                return;

            var dialog = new TaruckDialogPresenter(() => new RuntimeManagerResolver().Localization().CurrentLanguage);
            string characterName = characterLabels.GetDisplayName(selected);
            dialog.ShowCompact(
                dialog.Text("Видалити персонажа?", "Delete character?", "Удалить персонажа?"),
                dialog.Text("Персонажа «", "Character “", "Персонаж «") + characterName +
                dialog.Text("» буде видалено назавжди.", "” will be permanently deleted.", "» будет удалён навсегда."),
                dialog.Text("Видалити", "Delete", "Удалить"),
                () =>
                {
                    saveManager.DeleteCharacter(characterId);
                    owner.RefreshCharacterList();
                });
        });
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private Button FindButton(Transform root, string name)
    {
        Transform found = root.Find(name);
        return found != null ? found.GetComponent<Button>() : null;
    }

    internal void CacheCharacterButtonTemplate()
    {
        if (characterButtonTemplate != null || owner.CharacterListContent == null)
            return;

        Transform template = owner.CharacterListContent.Find("CharacterButtonTemplate");
        if (template != null)
            characterButtonTemplate = template.gameObject;
    }

    internal void DisableAutomaticContentLayout()
    {
        if (owner.CharacterListContent == null)
            return;

        DisableAutomaticLayout(owner.CharacterListContent);
    }

    private void DisableAutomaticLayout(Transform content)
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

    private static void ApplyTemplateRectToClone(RectTransform templateRect, RectTransform cloneRect, int index, float spacing)
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
        cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + spacing));
    }

    private static void ResizeContentForManualLayout(RectTransform contentRect, RectTransform templateRect, int buttonCount, float spacing)
    {
        if (contentRect == null || templateRect == null || buttonCount <= 0)
            return;

        float height = Mathf.Max(AppConfig.MainMenu.MinimumUsableRectSize, templateRect.rect.height);
        float topOffset = Mathf.Abs(templateRect.anchoredPosition.y);
        float requiredHeight = topOffset + buttonCount * height +
            Mathf.Max(0, buttonCount - 1) * spacing +
            AppConfig.MainMenu.ContentBottomPadding;
        if (contentRect.sizeDelta.y < requiredHeight)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    internal void EnsureCharacterListLayout()
    {
        if (owner.CharacterListContent == null)
            return;

        RectTransform contentRect = owner.CharacterListContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
        }

        VerticalLayoutGroup layoutGroup = owner.CharacterListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = owner.CharacterListContent.gameObject.AddComponent<VerticalLayoutGroup>();

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

        ContentSizeFitter fitter = owner.CharacterListContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = owner.CharacterListContent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    internal void EnsureScrollViewIsVisible()
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

        if (scrollRect.content == null && owner.CharacterListContent is RectTransform contentRect)
            scrollRect.content = contentRect;
    }
}
