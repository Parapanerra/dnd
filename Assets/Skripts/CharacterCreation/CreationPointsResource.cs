using UnityEngine;
using UnityEngine.UI;

namespace Taruck.CharacterCreation
{
    // A generated hit-point pool uses the existing resource panel, without one toggle per HP.
    public sealed class CreationPointsResource : MonoBehaviour
    {
        [SerializeField] private GameObject pointsPanel;
        [SerializeField] private InputField points;
        [SerializeField] private Text maximum;
        [SerializeField] private GameObject[] usageControls;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private InputField amount;
        [SerializeField] private Button spend;
        private DndSaveManager save;
        private int limit;
        private string shownCharacter;
        private int shownLimit = -1;
        private int lastRemaining = -1;
        private RuntimeLocalization localization;

        private void Awake()
        {
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
            amount.onEndEdit.AddListener(OnAmountEdited);
            spend.onClick.AddListener(Spend);
        }
        private void LateUpdate()
        {
            if (save == null) save = new RuntimeManagerResolver().FindSaveManager();
            var character = save != null && save.IsInitialized ? save.EnsureActiveCharacter() : null;
            int.TryParse(character?.GetSharedString("CreationLayOnHandsMaximum", "0"), out limit);
            bool changedCharacter = shownCharacter != character?.id;
            if (changedCharacter || shownLimit != limit)
            {
                shownCharacter = character?.id; shownLimit = limit;
                pointsPanel.SetActive(limit > 0);
                foreach (var control in usageControls) if (control != null) control.SetActive(limit <= 0);
                if (changedCharacter) { amountSlider.SetValueWithoutNotify(0); amount.SetTextWithoutNotify("0"); }
            }
            int remaining = Remaining();
            if (remaining != lastRemaining || changedCharacter)
            {
                lastRemaining = remaining;
                amountSlider.minValue = 0; amountSlider.maxValue = remaining;
                amountSlider.wholeNumbers = true;
                OnAmountEdited(amount.text);
            }
            if (localization == null) localization = new RuntimeManagerResolver().FindLocalization();
            string template = localization != null ? localization.Translate("Залишилось: {0} / {1} ОЗ") : "Залишилось: {0} / {1} ОЗ";
            maximum.text = string.Format(template, remaining, limit);
            spend.interactable = limit > 0 && remaining > 0 && amountSlider.value > 0;
        }

        private int Remaining()
        {
            int.TryParse(points.text, out int remaining);
            return Mathf.Clamp(remaining, 0, limit);
        }
        private void OnSliderChanged(float value) => amount.SetTextWithoutNotify(Mathf.RoundToInt(value).ToString());
        private void OnAmountEdited(string value)
        {
            int.TryParse(value, out int number);
            int selected = Mathf.Clamp(number, 0, Remaining());
            amount.SetTextWithoutNotify(selected.ToString());
            amountSlider.SetValueWithoutNotify(selected);
        }
        private void Spend()
        {
            var character = save != null ? save.EnsureActiveCharacter() : null;
            if (character == null || character.id != shownCharacter || limit <= 0) return;
            OnAmountEdited(amount.text);
            int used = Mathf.RoundToInt(amountSlider.value);
            if (used <= 0) return;
            string remaining = (Remaining() - used).ToString();
            points.text = remaining;
            var field = points.GetComponent<PersistentFieldId>();
            var data = character.GetSceneData(points.gameObject.scene.name);
            data.SetString("StableField_v1_" + field.Id, remaining);
            if (field.LegacyIndex >= 0 && field.LegacyIndex < data.inputData.Count) data.inputData[field.LegacyIndex] = remaining;
            save.RequestSaveData();
            amountSlider.SetValueWithoutNotify(0); amount.SetTextWithoutNotify("0");
        }
    }
}
