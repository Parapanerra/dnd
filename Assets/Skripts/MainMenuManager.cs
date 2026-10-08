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
    public Transform characterListContent;
    public GameObject characterButtonPrefab;
    public Button createNewCharacterButton;

    [Header("Scenes")]
    public string characterSheetSceneName = AppConfig.Scenes.CharacterSheet;
    public string inventorySceneName = AppConfig.Scenes.Inventory;
    public string spellbookSceneName = AppConfig.Scenes.Spellbook;

    [Header("Layout")]
    public float characterRowSpacing = AppConfig.MainMenu.CharacterRowSpacing;

    [HideInInspector] public Transform characterRowsContent;
    [HideInInspector] public GameObject characterRowTemplate;
    [HideInInspector] public GameObject characterButtonTemplate;
    [HideInInspector] public Button addCharacterButton;
    [HideInInspector] public bool openCharacterAfterCreate;
    [HideInInspector] public bool useCharacterButtonPrefab = true;
    [HideInInspector] public bool repairScrollViewAtRuntime;
    [HideInInspector] public bool applyDefaultCharacterListLayout;
    [HideInInspector] public bool applyDefaultCharacterButtonStyle;
    [HideInInspector] public float characterButtonSpacing = AppConfig.MainMenu.CharacterButtonSpacing;

    [HideInInspector] public Button importButton;
    [HideInInspector] public Button exportButton;

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
        DndSaveManager.EnsureExists();
        RuntimeLocalization.EnsureExists();
        characterListView = GetComponent<MainMenuCharacterListView>();
        if (characterListView == null)
            characterListView = gameObject.AddComponent<MainMenuCharacterListView>();
        characterListView.Initialize(this);
        characterListView.Wire();
        transferPanel = GetComponent<MainMenuTransferPanel>();
        if (transferPanel == null)
            transferPanel = gameObject.AddComponent<MainMenuTransferPanel>();
        transferPanel.Initialize(this);
        transferPanel.Wire();
        transferPanel.HideSavePanel();

        if (repairScrollViewAtRuntime)
            characterListView.EnsureScrollViewIsVisible();
        if (applyDefaultCharacterListLayout)
            characterListView.EnsureCharacterListLayout();
        languageSelector = GetComponent<MainMenuLanguageSelector>();
        if (languageSelector == null)
            languageSelector = gameObject.AddComponent<MainMenuLanguageSelector>();
        languageSelector.Initialize(this);
        languageSelector.EnsureLanguageDropdown();
        characterListView.CacheCharacterButtonTemplate();
        characterListView.DisableAutomaticContentLayout();

        RefreshCharacterList();
        RuntimeLocalization.EnsureExists().ApplyToScene();
        languageSelector.SyncLanguageDropdownValue();

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

        string storageError = DndSaveManager.Instance != null ? DndSaveManager.Instance.SaveError : null;
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
            characterListView.Initialize(this);
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

        CharacterData newChar = DndSaveManager.Instance.CreateNewCharacter();
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
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;
        
        SceneManager.LoadScene(characterSheetSceneName);
    }

    internal void OnInventorySelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(inventorySceneName);
    }

    internal void OnSpellbookSelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(spellbookSceneName);
    }

}
