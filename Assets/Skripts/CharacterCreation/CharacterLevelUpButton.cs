using Taruck.CharacterCreation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class CharacterLevelUpButton : MonoBehaviour
{
    [SerializeField] private CharacterSheetManagerScene1 characterSheet;

    private void Start()
    {
        var button = GetComponent<Button>();
        var manager = new RuntimeManagerResolver().SaveManager();
        button.interactable = characterSheet != null && CharacterLevelUp.CanStart(manager.GetActiveCharacter());
        button.onClick.AddListener(() =>
        {
            var character = manager.GetActiveCharacter();
            if (characterSheet == null || !CharacterLevelUp.CanStart(character)) return;
            characterSheet.SaveCharacterData();
            manager.FlushPendingSave();
            manager.SetPendingLevelUpCharacter(character.id);
            SceneManager.LoadScene(AppConfig.Scenes.CharacterCreation);
        });
    }
}
