using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Taruck.CharacterCreation
{
    internal sealed class LiveCreationResourceRecovery
    {
        internal void Apply(CharacterData character, bool longRest)
        {
            if (character == null) return;
            var controls = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Where(scene => scene.isLoaded)
                .SelectMany(scene => scene.GetRootGameObjects())
                .SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)).ToArray();
            var toggles = controls.Select(field => (key: (field.gameObject.scene.name, field.Id), control: field.GetComponent<Toggle>()))
                .Where(binding => binding.control != null).ToLookup(binding => binding.key, binding => binding.control);
            var inputs = controls.Select(field => (key: (field.gameObject.scene.name, field.Id), control: field.GetComponent<InputField>()))
                .Where(binding => binding.control != null).ToLookup(binding => binding.key, binding => binding.control);

            CreationResourceRecovery.Apply(character, longRest,
                field => field.collection == "inputData"
                    ? inputs.Contains((field.scene, field.stableId)) : toggles.Contains((field.scene, field.stableId)),
                field => toggles[(field.scene, field.stableId)].Any(toggle => toggle.isOn),
                (field, value) =>
                {
                    if (field.collection == "inputData")
                        foreach (var input in inputs[(field.scene, field.stableId)]) input.text = value.ToString();
                    else
                        foreach (var toggle in toggles[(field.scene, field.stableId)]) toggle.isOn = value != 0;
                });
        }
    }
}
