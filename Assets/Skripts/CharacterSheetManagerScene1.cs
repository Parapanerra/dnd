using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Unity entry point for character scenes; the session owns their saved data.
public class CharacterSheetManagerScene1 : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button deleteCharacterButton;
    [SerializeField] private Button resetButton;

    private readonly CharacterSceneUiService uiService = new CharacterSceneUiService();
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private CharacterPortraitManager portraitManager;
    private CharacterSceneDataSession session;
    private CharacterNameEasterEgg characterNameEasterEgg;
    private bool isMainSheetScene;

    private void Awake()
    {
        saveManager = new RuntimeManagerResolver().SaveManager();
        BindSceneComponents();
    }

    private void Start()
    {
        isMainSheetScene = SceneManager.GetActiveScene().name == AppConfig.Scenes.CharacterSheet;
        localization = new RuntimeManagerResolver().Localization();
        session = new CharacterSceneDataSession(saveManager, isMainSheetScene);
        if (!session.Initialize())
        {
            Debug.LogError("Could not find or create active character.");
            return;
        }

        new SceneInputActivationInstaller().Install();
        portraitManager = uiService.EnsurePortraitManager(gameObject, saveManager);
        session.Load();
        uiService.RefreshAfterLoad();
        new ToggleSequenceSceneInstaller(saveManager).Install();
        localization.ApplyToScene();
        session.Subscribe();
        if (isMainSheetScene)
        {
            characterNameEasterEgg = new CharacterNameEasterEgg(localization);
            characterNameEasterEgg.Bind(session.CharacterNameInput, session.CharacterNameTmpInput);
        }
        BindNavigationButtons();
        BindResetButtons();
    }

    internal void SaveCharacterData()
    {
        session?.Save();
    }

    internal void SwitchSceneData(string newSceneName)
    {
        if (session == null || !session.Switch(newSceneName))
            return;

        uiService.RefreshAfterLoad();
        localization.ApplyToScene();
    }

    internal void ResetSceneData()
    {
        if (session == null || !session.ResetData())
            return;

        portraitManager?.ClearPortraitForActiveCharacter();
        uiService.ResetSceneWidgets();
        session.Save();
        uiService.RefreshAfterReset();
    }

    private void BindSceneComponents()
    {
        foreach (HealthBar healthBar in FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (healthBar != null && healthBar.gameObject.scene == gameObject.scene)
                healthBar.Initialize(saveManager);

        foreach (PrefabSwitcher switcher in FindObjectsByType<PrefabSwitcher>(FindObjectsInactive.Include))
            if (switcher != null && switcher.gameObject.scene == gameObject.scene)
                switcher.Initialize(saveManager);

        foreach (StatManager statManager in FindObjectsByType<StatManager>(FindObjectsInactive.Include))
            if (statManager != null && statManager.gameObject.scene == gameObject.scene)
                statManager.Initialize(saveManager);

        foreach (InventorySubmenuController scrollPanel in FindObjectsByType<InventorySubmenuController>(FindObjectsInactive.Include))
            if (scrollPanel != null && scrollPanel.gameObject.scene == gameObject.scene)
                scrollPanel.Initialize(saveManager);
    }

    private void BindNavigationButtons()
    {
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(AppConfig.Scenes.Menu));

        if (deleteCharacterButton != null)
        {
            deleteCharacterButton.onClick.AddListener(() =>
            {
                saveManager.DeleteCharacter(session.CharacterId);
                SceneManager.LoadScene(AppConfig.Scenes.Menu);
            });
        }
    }

    private void BindResetButtons()
    {
        List<Button> resetButtons = new List<Button>(session.ResetButtons);
        resetButtons.RemoveAll(button => !uiService.IsResetButton(button));
        if (resetButton != null && !resetButtons.Contains(resetButton))
            resetButtons.Add(resetButton);

        foreach (Button button in resetButtons)
            if (button != null)
            {
                uiService.DisablePersistentOnClick(button);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ResetSceneData);
            }
    }

    private void OnEnable()
    {
        session?.NoteChange();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            session?.FlushOnExit();
        else
            session?.NoteChange();
    }

    private void OnDisable()
    {
        session?.FlushOnExit();
    }

    private void OnApplicationQuit()
    {
        session?.FlushOnExit();
    }
}
