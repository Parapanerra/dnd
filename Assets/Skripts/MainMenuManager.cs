using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using SimpleFileBrowser;
using TMPro;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Transform characterListContent;
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Button createNewCharacterButton;

    [Header("Scenes")]
    [SerializeField] private string characterSheetSceneName = AppConfig.Scenes.CharacterSheet;
    [SerializeField] private string inventorySceneName = AppConfig.Scenes.Inventory;
    [SerializeField] private string spellbookSceneName = AppConfig.Scenes.Spellbook;

    [Header("Layout")]
    [SerializeField] private float characterRowSpacing = AppConfig.MainMenu.CharacterRowSpacing;

    [SerializeField, HideInInspector] private bool openCharacterAfterCreate;
    [SerializeField, HideInInspector] private bool useCharacterButtonPrefab = true;
    [SerializeField, HideInInspector] private bool repairScrollViewAtRuntime;
    [SerializeField, HideInInspector] private bool applyDefaultCharacterListLayout;
    [SerializeField, HideInInspector] private bool applyDefaultCharacterButtonStyle;
    [SerializeField, HideInInspector] private float characterButtonSpacing = AppConfig.MainMenu.CharacterButtonSpacing;

    public Transform CharacterListContent => characterListContent;
    public GameObject CharacterButtonPrefab => characterButtonPrefab;
    internal float CharacterRowSpacing => characterRowSpacing;
    internal bool UseCharacterButtonPrefab => useCharacterButtonPrefab;
    internal bool ApplyDefaultCharacterListLayout => applyDefaultCharacterListLayout;
    internal bool ApplyDefaultCharacterButtonStyle => applyDefaultCharacterButtonStyle;
    internal float CharacterButtonSpacing => characterButtonSpacing;

#if UNITY_EDITOR
    public void SetCharacterListContent(Transform content)
    {
        characterListContent = content;
    }

    public void SetCharacterButtonPrefab(GameObject prefab)
    {
        characterButtonPrefab = prefab;
    }

    public void PrepareRepairedCharacterMenu()
    {
        openCharacterAfterCreate = false;
        useCharacterButtonPrefab = true;
        DisableRuntimeMenuRepair();
    }

    public void UseCharacterButtonTemplate(bool disableRuntimeRepair)
    {
        useCharacterButtonPrefab = true;
        if (disableRuntimeRepair)
            DisableRuntimeMenuRepair();
    }

    private void DisableRuntimeMenuRepair()
    {
        repairScrollViewAtRuntime = false;
        applyDefaultCharacterListLayout = false;
        applyDefaultCharacterButtonStyle = false;
    }

    public void SetCharacterButtonTemplate(GameObject template)
    {
        if (characterListView == null)
        {
            characterListView = GetComponent<MainMenuCharacterListView>();
            if (characterListView == null)
                characterListView = gameObject.AddComponent<MainMenuCharacterListView>();
            characterListView.Initialize(this, saveManager);
        }
        characterListView.SetCharacterButtonTemplate(template);
    }
#endif

    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private MainMenuLanguageSelector languageSelector;
    private MainMenuTransferPanel transferPanel;
    private MainMenuCharacterListView characterListView;
    private float lastCharacterCreateTime = -AppConfig.MainMenu.CharacterCreateDebounceSeconds;

    private void OnEnable()
    {
        if (languageSelector != null)
            languageSelector.ScheduleSync();
    }

    private void Start()
    {
        NormalizeSceneNames();
        saveManager = DndSaveManager.EnsureExists();
        localization = RuntimeLocalization.EnsureExists();
        characterListView = GetComponent<MainMenuCharacterListView>();
        if (characterListView == null)
            characterListView = gameObject.AddComponent<MainMenuCharacterListView>();
        characterListView.Initialize(this, saveManager);
        characterListView.Wire();
        transferPanel = GetComponent<MainMenuTransferPanel>();
        if (transferPanel == null)
            transferPanel = gameObject.AddComponent<MainMenuTransferPanel>();
        transferPanel.Initialize(this, saveManager);
        transferPanel.Wire();
        transferPanel.HideSavePanel();

        if (repairScrollViewAtRuntime)
            characterListView.EnsureScrollViewIsVisible();
        if (applyDefaultCharacterListLayout)
            characterListView.EnsureCharacterListLayout();
        languageSelector = GetComponent<MainMenuLanguageSelector>();
        if (languageSelector == null)
            languageSelector = gameObject.AddComponent<MainMenuLanguageSelector>();
        languageSelector.Initialize(this, localization);
        languageSelector.EnsureLanguageDropdown();
        characterListView.CacheCharacterButtonTemplate();
        characterListView.DisableAutomaticContentLayout();

        RefreshCharacterList();
        localization.ApplyToScene();
        languageSelector.SyncLanguageDropdownValue();

        Button addCharacterButton = characterListView.AddCharacterButton;
        if (addCharacterButton != null)
        {
            addCharacterButton.onClick.RemoveAllListeners();
            addCharacterButton.onClick.AddListener(OnCreateNewCharacterClicked);
        }

        if (createNewCharacterButton != null)
        {
            createNewCharacterButton.onClick.RemoveAllListeners();
            createNewCharacterButton.onClick.AddListener(OnCreateNewCharacterClicked);
        }

        transferPanel.BindButtons();

        string storageError = saveManager != null ? saveManager.SaveError : null;
        if (!string.IsNullOrEmpty(storageError))
            TaruckImportReviewDialog.Show("Стан сховища", storageError, "Закрити", () => { });
    }

    private void NormalizeSceneNames()
    {
        if (string.IsNullOrWhiteSpace(characterSheetSceneName) || characterSheetSceneName == "CharacterSheetScene")
            characterSheetSceneName = AppConfig.Scenes.CharacterSheet;

        if (string.IsNullOrWhiteSpace(inventorySceneName))
            inventorySceneName = AppConfig.Scenes.Inventory;

        if (string.IsNullOrWhiteSpace(spellbookSceneName))
            spellbookSceneName = AppConfig.Scenes.Spellbook;
    }

    public void EnsureEditableCharacterScrollView()
    {
        if (characterListView == null)
        {
            characterListView = GetComponent<MainMenuCharacterListView>();
            if (characterListView == null)
                characterListView = gameObject.AddComponent<MainMenuCharacterListView>();
            characterListView.Initialize(this, saveManager);
        }

        characterListView.EnsureEditableCharacterScrollView();
    }

    public void RefreshCharacterList()
    {
        if (transferPanel != null)
            transferPanel.RefreshOneCharacterDropdown();
        if (characterListView != null)
            characterListView.Refresh();
    }

    private void OnCreateNewCharacterClicked()
    {
        if (Time.unscaledTime - lastCharacterCreateTime <
            AppConfig.MainMenu.CharacterCreateDebounceSeconds)
            return;

        lastCharacterCreateTime = Time.unscaledTime;

        CharacterData newChar = saveManager.CreateNewCharacter();
        RefreshCharacterList();

        if (openCharacterAfterCreate)
            OnCharacterSelected(newChar.id);
    }

    internal void OpenImportedCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        OnCharacterSelected(characterId);
    }

    internal void OnCharacterSelected(string characterId)
    {
        OpenCharacterScene(characterId, characterSheetSceneName);
    }

    internal void OnInventorySelected(string characterId)
    {
        OpenCharacterScene(characterId, inventorySceneName);
    }

    internal void OnSpellbookSelected(string characterId)
    {
        OpenCharacterScene(characterId, spellbookSceneName);
    }

    private void OpenCharacterScene(string characterId, string sceneName)
    {
        if (!saveManager.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(sceneName);
    }

}
