using System;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldIncrementer : MonoBehaviour
{
    [Serializable]
    public class FieldConfig
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button incrementButton;
        [SerializeField] private Button decrementButton;
        [SerializeField] private CharacterSheetManagerScene1 characterSheetManager;

        public void Bind()
        {
            if (incrementButton != null)
            {
                incrementButton.onClick.RemoveListener(Increment);
                incrementButton.onClick.AddListener(Increment);
            }
            if (decrementButton != null)
            {
                decrementButton.onClick.RemoveListener(Decrement);
                decrementButton.onClick.AddListener(Decrement);
            }
        }

        public void Unbind()
        {
            if (incrementButton != null)
                incrementButton.onClick.RemoveListener(Increment);
            if (decrementButton != null)
                decrementButton.onClick.RemoveListener(Decrement);
        }

        private void Increment()
        {
            ChangeValue(1);
        }

        private void Decrement()
        {
            ChangeValue(-1);
        }

        private void ChangeValue(int delta)
        {
            if (inputField == null || !int.TryParse(inputField.text, out int value))
                return;

            inputField.text = (value + delta).ToString();
            if (characterSheetManager != null)
                characterSheetManager.SaveCharacterData();
        }
    }

    [SerializeField] private FieldConfig[] fieldConfigs;

    private void Start()
    {
        if (fieldConfigs == null)
            return;

        foreach (FieldConfig config in fieldConfigs)
            config?.Bind();
    }

    private void OnDestroy()
    {
        if (fieldConfigs == null)
            return;

        foreach (FieldConfig config in fieldConfigs)
            config?.Unbind();
    }
}