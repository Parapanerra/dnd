using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider temporaryHealthSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Text temporaryHealthText;
    [SerializeField] private InputField maxHealthInputField;
    [SerializeField] private InputField damageInputField;
    [SerializeField] private InputField healInputField;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button confirmMaxHealthButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private int customIndex;

    public bool IsUsableForCalculator => isActiveAndEnabled && (healthSlider == null || healthSlider.gameObject.activeInHierarchy);

    private int currentHealth;
    private int maxHealth;
    private int currentTemporaryHealth;
    private int maxTemporaryHealth;
    private int damage;
    private int heal;
    private DndSaveManager saveManager;
    private HealthBarPersistence persistence;
    private Coroutine deferredUiRefresh;
    private HealthBarView view;

    protected virtual void Awake()
    {
        view = new HealthBarView(maxHealthInputField, healthSlider, temporaryHealthSlider,
            healthText, temporaryHealthText);
    }

    internal void Initialize(DndSaveManager manager)
    {
        saveManager = manager;
        persistence = new HealthBarPersistence(manager, customIndex);
    }

    protected virtual void OnEnable()
    {
        QueueDeferredUiRefresh();
    }

    protected virtual void Start()
    {
        if (saveManager == null)
        {
            Debug.LogError("HealthBar was not initialized by the character scene controller.");
            return;
        }
        saveManager.EnsureActiveCharacter();
        persistence.BindScene(saveManager.GetActiveSceneData());

        if (maxHealthInputField != null) maxHealthInputField.contentType = InputField.ContentType.IntegerNumber;
        if (damageInputField != null) damageInputField.contentType = InputField.ContentType.IntegerNumber;
        if (healInputField != null) healInputField.contentType = InputField.ContentType.IntegerNumber;

        if (maxHealthInputField != null) maxHealthInputField.onEndEdit.AddListener(UpdateMaxHealth);
        if (damageInputField != null) damageInputField.onEndEdit.AddListener(UpdateDamage);
        if (healInputField != null) healInputField.onEndEdit.AddListener(UpdateHeal);

        if (damageButton != null) damageButton.onClick.AddListener(TakeDamage);
        if (healButton != null) healButton.onClick.AddListener(Heal);
        if (confirmMaxHealthButton != null) confirmMaxHealthButton.onClick.AddListener(ConfirmMaxHealth);
        if (resetButton != null) resetButton.onClick.AddListener(ConfirmResetHealth);

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

    private void ConfirmResetHealth()
    {
        RuntimeLocalization localization = new RuntimeManagerResolver().Localization();
        new TaruckDialogPresenter(() => localization.CurrentLanguage).ShowCompact(
            localization.Translate("Скинути HP?"),
            localization.Translate("Поточне, максимальне й тимчасове HP буде скинуто."),
            localization.Translate("Скинути"), ResetHealth);
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

    public void SetHealthToZero()
    {
        RefreshHealthFromData();
        currentHealth = 0;
        ClearTemporaryHealth(false);
        SaveSceneData();
        UpdateHealthUI();
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
        if (persistence == null || !persistence.TryLoad(out HealthBarValues values))
            return;
        maxHealth = values.MaxHealth;
        currentHealth = values.CurrentHealth;
        maxTemporaryHealth = values.MaxTemporaryHealth;
        currentTemporaryHealth = values.CurrentTemporaryHealth;
        UpdateHealthUI();
        QueueDeferredUiRefresh();
    }

    private void SaveSceneData()
    {
        persistence?.Save(new HealthBarValues(maxHealth, currentHealth,
            maxTemporaryHealth, currentTemporaryHealth));
    }

    private void UpdateHealthUI()
    {
        view?.Render(currentHealth, maxHealth, currentTemporaryHealth, maxTemporaryHealth);
    }

    private void QueueDeferredUiRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (deferredUiRefresh != null)
            StopCoroutine(deferredUiRefresh);

        deferredUiRefresh = StartCoroutine(RefreshUiAtEndOfFrame());
    }

    private IEnumerator RefreshUiAtEndOfFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        UpdateHealthUI();
        deferredUiRefresh = null;
    }
}
