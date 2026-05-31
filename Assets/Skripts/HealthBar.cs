using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public Slider temporaryHealthSlider;
    public Text healthText;
    public Text temporaryHealthText;
    public InputField maxHealthInputField;
    public InputField damageInputField;
    public InputField healInputField;
    public Button damageButton;
    public Button healButton;
    public Button confirmMaxHealthButton;
    public Button resetButton;
    public int customIndex;

    public bool IsUsableForCalculator => isActiveAndEnabled && (healthSlider == null || healthSlider.gameObject.activeInHierarchy);

    private int currentHealth;
    private int maxHealth;
    private int currentTemporaryHealth;
    private int maxTemporaryHealth;
    private int damage;
    private int heal;
    private CharacterSceneData sceneData;

    private void Start()
    {
        DndSaveManager.EnsureExists();
        DndSaveManager.Instance.EnsureActiveCharacter();
        sceneData = DndSaveManager.Instance.GetActiveSceneData();

        if (maxHealthInputField != null) maxHealthInputField.contentType = InputField.ContentType.IntegerNumber;
        if (damageInputField != null) damageInputField.contentType = InputField.ContentType.IntegerNumber;
        if (healInputField != null) healInputField.contentType = InputField.ContentType.IntegerNumber;

        if (maxHealthInputField != null) maxHealthInputField.onEndEdit.AddListener(UpdateMaxHealth);
        if (damageInputField != null) damageInputField.onEndEdit.AddListener(UpdateDamage);
        if (healInputField != null) healInputField.onEndEdit.AddListener(UpdateHeal);

        if (damageButton != null) damageButton.onClick.AddListener(TakeDamage);
        if (healButton != null) healButton.onClick.AddListener(Heal);
        if (confirmMaxHealthButton != null) confirmMaxHealthButton.onClick.AddListener(ConfirmMaxHealth);
        if (resetButton != null) resetButton.onClick.AddListener(ResetHealth);

        RefreshHealthFromData();
    }

    private void UpdateMaxHealth(string value)
    {
        if (int.TryParse(value, out int parsedMaxHealth))
            SetMaxHealth(parsedMaxHealth);
    }

    private void UpdateDamage(string value)
    {
        if (int.TryParse(value, out int parsedDamage))
            damage = Mathf.Max(0, parsedDamage);
    }

    private void UpdateHeal(string value)
    {
        if (int.TryParse(value, out int parsedHeal))
            heal = Mathf.Max(0, parsedHeal);
    }

    public void TakeDamage()
    {
        ApplyDamage(damage);
    }

    public void Heal()
    {
        ApplyHeal(heal);
    }

    public void ConfirmMaxHealth()
    {
        if (maxHealthInputField != null && int.TryParse(maxHealthInputField.text, out int parsedMaxHealth))
        {
            maxHealth = Mathf.Max(0, parsedMaxHealth);
            currentHealth = maxHealth;
            SaveSceneData();
            UpdateHealthUI();
        }
    }

    public void ResetHealth()
    {
        currentHealth = 0;
        maxHealth = 0;
        currentTemporaryHealth = 0;
        maxTemporaryHealth = 0;
        SaveSceneData();
        UpdateHealthUI();
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(0, value);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        SaveSceneData();
        UpdateHealthUI();
    }

    public void SetMaxHealthAndFill(int value)
    {
        maxHealth = Mathf.Max(0, value);
        currentHealth = maxHealth;
        SaveSceneData();
        UpdateHealthUI();
    }

    public int ApplyDamage(int value)
    {
        int remainingDamage = Mathf.Max(0, value);
        int temporaryDamage = Mathf.Min(currentTemporaryHealth, remainingDamage);
        currentTemporaryHealth -= temporaryDamage;
        remainingDamage -= temporaryDamage;

        int beforeHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - remainingDamage, 0, maxHealth);
        SaveSceneData();
        UpdateHealthUI();
        return temporaryDamage + beforeHealth - currentHealth;
    }

    public int ApplyHeal(int value)
    {
        int before = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0, value), 0, maxHealth);
        SaveSceneData();
        UpdateHealthUI();
        return currentHealth - before;
    }

    public int RestoreToMaxHealth()
    {
        int before = currentHealth;
        currentHealth = maxHealth;
        ClearTemporaryHealth(false);
        SaveSceneData();
        UpdateHealthUI();
        return currentHealth - before;
    }

    public void SetTemporaryHealth(int value)
    {
        maxTemporaryHealth = Mathf.Max(0, value);
        currentTemporaryHealth = maxTemporaryHealth;
        SaveSceneData();
        UpdateHealthUI();
    }

    public void ClearTemporaryHealth()
    {
        ClearTemporaryHealth(true);
    }

    private void ClearTemporaryHealth(bool save)
    {
        currentTemporaryHealth = 0;
        maxTemporaryHealth = 0;
        if (save)
        {
            SaveSceneData();
            UpdateHealthUI();
        }
    }

    public void RefreshHealthFromData()
    {
        if (sceneData == null)
            sceneData = DndSaveManager.Instance.GetActiveSceneData();

        if (sceneData == null)
            return;

        string maxKey = GetSaveKey("maxHealth");
        string currentKey = GetSaveKey("currentHealth");
        string temporaryMaxKey = GetSaveKey("maxTemporaryHealth");
        string temporaryCurrentKey = GetSaveKey("currentTemporaryHealth");
        if (sceneData.HasInt(maxKey) || sceneData.HasInt(currentKey))
        {
            maxHealth = sceneData.GetInt(maxKey, 0);
            currentHealth = sceneData.GetInt(currentKey, maxHealth);
        }
        else
        {
            maxHealth = 0;
            currentHealth = 0;
        }

        maxTemporaryHealth = sceneData.GetInt(temporaryMaxKey, 0);
        currentTemporaryHealth = Mathf.Clamp(sceneData.GetInt(temporaryCurrentKey, maxTemporaryHealth), 0, maxTemporaryHealth);
        UpdateHealthUI();
    }

    private void SaveSceneData()
    {
        if (sceneData == null)
            sceneData = DndSaveManager.Instance.GetActiveSceneData();

        if (sceneData == null)
            return;

        sceneData.SetInt(GetSaveKey("maxHealth"), maxHealth);
        sceneData.SetInt(GetSaveKey("currentHealth"), currentHealth);
        sceneData.SetInt(GetSaveKey("maxTemporaryHealth"), maxTemporaryHealth);
        sceneData.SetInt(GetSaveKey("currentTemporaryHealth"), currentTemporaryHealth);
        DndSaveManager.Instance.SaveData();
    }

    private string GetSaveKey(string fieldName)
    {
        return "HealthBar_" + customIndex + "_" + fieldName;
    }

    private void UpdateHealthUI()
    {
        if (maxHealthInputField != null && maxHealthInputField.text != maxHealth.ToString())
            maxHealthInputField.SetTextWithoutNotify(maxHealth.ToString());

        if (healthSlider != null)
        {
            UpdateSliderPercent(healthSlider, currentHealth, maxHealth);
        }

        if (temporaryHealthSlider == null)
            temporaryHealthSlider = FindSliderByName("folslive");

        if (temporaryHealthSlider != null)
        {
            UpdateSliderPercent(temporaryHealthSlider, currentTemporaryHealth, maxTemporaryHealth);
        }

        if (temporaryHealthText == null)
            temporaryHealthText = FindTemporaryHealthText();

        if (temporaryHealthText != null)
            temporaryHealthText.text = $"{currentTemporaryHealth} / {maxTemporaryHealth}";

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }

    private void UpdateSliderPercent(Slider slider, int current, int max)
    {
        if (slider == null)
            return;

        bool visible = current > 0 && max > 0;
        float percent = visible ? Mathf.Clamp01((float)current / max) : 0f;

        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(percent);

        SetSliderHandleVisible(slider, visible);
        SetSliderFillVisible(slider, visible);
    }

    private void SetSliderHandleVisible(Slider slider, bool visible)
    {
        if (slider == null || slider.handleRect == null)
            return;

        Graphic[] graphics = slider.handleRect.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            if (graphic != null)
                graphic.enabled = visible;
    }

    private void SetSliderFillVisible(Slider slider, bool visible)
    {
        if (slider == null || slider.fillRect == null)
            return;

        Graphic[] graphics = slider.fillRect.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            if (graphic != null)
                graphic.enabled = visible;
    }

    private Slider FindSliderByName(string objectName)
    {
        Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include);
        foreach (Slider slider in sliders)
        {
            if (slider != null && string.Equals(slider.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return slider;
        }

        foreach (Slider slider in sliders)
        {
            if (slider != null && HasParentNamed(slider.transform, objectName))
                return slider;
        }

        Transform namedObject = FindTransformByName(objectName);
        if (namedObject != null)
        {
            Slider childSlider = namedObject.GetComponentInChildren<Slider>(true);
            if (childSlider != null)
                return childSlider;
        }

        return null;
    }

    private Text FindTemporaryHealthText()
    {
        if (temporaryHealthSlider != null)
        {
            Text text = temporaryHealthSlider.GetComponentInChildren<Text>(true);
            if (text != null)
                return text;
        }

        Transform namedObject = FindTransformByName("folslive");
        return namedObject != null ? namedObject.GetComponentInChildren<Text>(true) : null;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
        {
            if (candidate != null && string.Equals(candidate.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    private bool HasParentNamed(Transform transform, string objectName)
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.parent;
        }

        return false;
    }
}
