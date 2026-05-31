using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalculatorManager : MonoBehaviour
{
    private const string PotionSaveKeyPrefix = "PotionCount_";
    private const string ToggleKeyPrefix = "Toggle_";
    private const string RestResourceKeyPrefix = "RestResource_";

    public List<Button> buttons;
    public Text equationText;
    public Text resultText;

    private readonly string[] potionFormulas = { "2d4+2", "4d4+4", "8d4+8", "10d4+20" };
    private readonly int[] potionCounts = new int[4];
    private string currentEquation = "";
    private string hpModeLabel = "";
    private Color hpTextColor = Color.white;
    private bool hasHpTextColor;
    private bool isOperatorClicked;
    private bool isLastInputDice;
    private Dropdown potionDropdown;
    private Button potionPlusButton;
    private Button potionMinusButton;
    private Button potionUseButton;
    private HpCalculatorMode hpMode = HpCalculatorMode.None;

    private enum HpCalculatorMode
    {
        None,
        MaxHp,
        TemporaryHp,
        Damage,
        Heal
    }

    private void Start()
    {
        RuntimeLocalization.EnsureExists();
        EnsureDisplayTexts();
        AssignButtonFunctions();
        AssignHpButtonFunctions();
        AssignPotionControls();
    }

    public void RefreshLocalization()
    {
        if (hpMode != HpCalculatorMode.None)
        {
            hpModeLabel = GetHpModeLabel(hpMode);
            RefreshEquationText();
        }

        RefreshPotionDropdownOptions();
    }

    private void EnsureDisplayTexts()
    {
        if (equationText != null && resultText != null)
            return;

        Transform searchRoot = FindCalculatorRoot();
        List<Text> displayTexts = new List<Text>();
        foreach (Text text in searchRoot.GetComponentsInChildren<Text>(true))
        {
            if (text == null || IsInsideInteractiveControl(text.transform) || IsStaticCalculatorLabel(text.text))
                continue;

            displayTexts.Add(text);
        }

        displayTexts.Sort(CompareDisplayTextCandidates);

        if (equationText == null && displayTexts.Count > 0)
            equationText = displayTexts[0];

        if (resultText == null)
            resultText = displayTexts.Count > 1 ? displayTexts[1] : equationText;
    }

    private int CompareDisplayTextCandidates(Text left, Text right)
    {
        bool leftEmpty = left == null || string.IsNullOrWhiteSpace(left.text);
        bool rightEmpty = right == null || string.IsNullOrWhiteSpace(right.text);
        if (leftEmpty != rightEmpty)
            return leftEmpty ? -1 : 1;

        return right.rectTransform.position.y.CompareTo(left.rectTransform.position.y);
    }

    private void AssignButtonFunctions()
    {
        EnsureCalculatorButtons();

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string label = GetButtonLabel(button);
            if (string.IsNullOrEmpty(label))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClick(label));
        }
    }

    private void EnsureCalculatorButtons()
    {
        if (buttons == null)
            buttons = new List<Button>();

        buttons.Clear();
        Transform searchRoot = FindCalculatorRoot();
        foreach (Button button in searchRoot.GetComponentsInChildren<Button>(true))
        {
            if (button == null || IsSpecialCalculatorButton(button))
                continue;

            string label = NormalizeLabel(GetButtonLabel(button));
            if (IsNumberLabel(label) || IsOperator(label) || IsDiceLabel(label) || label == "C" || label == "CE" || label == "=")
                buttons.Add(button);
        }
    }

    private Transform FindCalculatorRoot()
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, "kalPanel", StringComparison.OrdinalIgnoreCase))
                return current;

            current = current.parent;
        }

        return transform.parent != null ? transform.parent : transform;
    }

    private string GetButtonLabel(Button button)
    {
        if (button == null)
            return "";

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
            return labelText.text.Trim();

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        return tmpText != null ? tmpText.text.Trim() : "";
    }

    private bool IsInsideInteractiveControl(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.GetComponent<Button>() != null ||
                current.GetComponent<Dropdown>() != null ||
                current.GetComponent<TMP_Dropdown>() != null ||
                current.GetComponent<InputField>() != null ||
                current.GetComponent<TMP_InputField>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsStaticCalculatorLabel(string text)
    {
        string label = NormalizeLabel(text).ToLowerInvariant();
        return string.IsNullOrEmpty(label) == false &&
               (label.Contains("калькулятор") ||
                label.Contains("calculator") ||
                label.Contains("меню") ||
                label.Contains("menu") ||
                label.Contains("шкода") ||
                label.Contains("урон") ||
                label.Contains("damage") ||
                label.Contains("зцілення") ||
                label.Contains("heal") ||
                label.Contains("маххп") ||
                label.Contains("maxhp") ||
                label.Contains("відпочинок") ||
                label.Contains("rest") ||
                label.Contains("випити") ||
                label.Contains("зілля") ||
                label.Contains("псевдожиття"));
    }

    private bool IsSpecialCalculatorButton(Button button)
    {
        string buttonName = NormalizeLabel(button.gameObject.name).ToLowerInvariant();
        return IsHpButtonName(buttonName) ||
               buttonName == "potionplus" ||
               buttonName == "potionminus" ||
               buttonName == "potionuse";
    }

    private void AssignHpButtonFunctions()
    {
        Transform searchRoot = FindCalculatorRoot();
        Button[] calculatorButtons = searchRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in calculatorButtons)
        {
            if (button == null)
                continue;

            string buttonName = NormalizeLabel(button.gameObject.name);
            if (!IsHpButtonName(buttonName))
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnHpButtonClick(buttonName, button));
        }
    }

    private void AssignPotionControls()
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
        {
            if (item == null)
                continue;

            string objectName = item.gameObject.name;
            if (string.Equals(objectName, "potionDropdown", StringComparison.OrdinalIgnoreCase))
                potionDropdown = item.GetComponent<Dropdown>();
            else if (string.Equals(objectName, "potionPlus", StringComparison.OrdinalIgnoreCase))
                potionPlusButton = item.GetComponent<Button>();
            else if (string.Equals(objectName, "potionMinus", StringComparison.OrdinalIgnoreCase))
                potionMinusButton = item.GetComponent<Button>();
            else if (string.Equals(objectName, "potionUse", StringComparison.OrdinalIgnoreCase))
                potionUseButton = item.GetComponent<Button>();
        }

        if (potionDropdown == null)
            return;

        LoadPotionCounts();
        RefreshPotionDropdownOptions();

        if (potionPlusButton != null)
        {
            potionPlusButton.onClick.RemoveAllListeners();
            potionPlusButton.onClick.AddListener(() => ChangeSelectedPotionCount(1));
        }

        if (potionMinusButton != null)
        {
            potionMinusButton.onClick.RemoveAllListeners();
            potionMinusButton.onClick.AddListener(() => ChangeSelectedPotionCount(-1));
        }

        if (potionUseButton != null)
        {
            potionUseButton.onClick.RemoveAllListeners();
            potionUseButton.onClick.AddListener(UseSelectedPotion);
        }
    }

    private void LoadPotionCounts()
    {
        CharacterSceneData sceneData = GetPotionSceneData(false);
        for (int i = 0; i < potionCounts.Length; i++)
            potionCounts[i] = Mathf.Max(0, sceneData != null ? sceneData.GetInt(PotionSaveKeyPrefix + i, 0) : 0);
    }

    private void SavePotionCounts()
    {
        CharacterSceneData sceneData = GetPotionSceneData(true);
        if (sceneData == null || DndSaveManager.Instance == null)
            return;

        for (int i = 0; i < potionCounts.Length; i++)
            sceneData.SetInt(PotionSaveKeyPrefix + i, Mathf.Max(0, potionCounts[i]));

        DndSaveManager.Instance.SaveData();
    }

    private CharacterSceneData GetPotionSceneData(bool createIfMissing)
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        if (saveManager == null)
            return null;

        CharacterData character = saveManager.EnsureActiveCharacter();
        return character != null ? character.GetSceneData(saveManager.GetActiveSceneDataName(), createIfMissing) : null;
    }

    private void ChangeSelectedPotionCount(int delta)
    {
        int index = GetSelectedPotionIndex();
        if (index < 0)
        {
            ShowHpResult(GetCalculatorText("choosePotion"));
            return;
        }

        potionCounts[index] = Mathf.Max(0, potionCounts[index] + delta);
        SavePotionCounts();
        RefreshPotionDropdownOptions();
    }

    private void UseSelectedPotion()
    {
        CaptureHpTextColorFromButton(potionUseButton);

        int index = GetSelectedPotionIndex();
        if (index < 0)
        {
            ShowHpResult(GetCalculatorText("choosePotion"));
            return;
        }

        if (potionCounts[index] <= 0)
        {
            ShowHpResult(GetCalculatorText("noPotion"));
            return;
        }

        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult(GetCalculatorText("hpBarNotFound"));
            return;
        }

        string rolledExpression = ProcessDiceNotation(potionFormulas[index]);
        if (!TryEvaluateExpression(rolledExpression, out double rollResult))
        {
            ShowHpResult(GetCalculatorText("potionError"));
            return;
        }

        int roll = Mathf.Max(0, Mathf.RoundToInt((float)rollResult));
        int healed = healthBar != null ? healthBar.ApplyHeal(roll) : healthBar1.ApplyHeal(roll);
        potionCounts[index]--;
        SavePotionCounts();
        RefreshPotionDropdownOptions();
        ShowHpResult(GetHealedText(healed) + " (" + potionFormulas[index] + "=" + roll + ")");
        ResetHpInputState();
    }

    private int GetSelectedPotionIndex()
    {
        if (potionDropdown == null)
            return -1;

        if (potionDropdown.value <= 0)
            return -1;

        return Mathf.Clamp(potionDropdown.value - 1, 0, potionCounts.Length - 1);
    }

    private void RefreshPotionDropdownOptions()
    {
        if (potionDropdown == null)
            return;

        int selectedDropdownValue = Mathf.Clamp(potionDropdown.value, 0, potionCounts.Length);
        potionDropdown.options.Clear();
        potionDropdown.options.Add(new Dropdown.OptionData(GetCalculatorText("choosePotion")));
        for (int i = 0; i < potionCounts.Length; i++)
            potionDropdown.options.Add(new Dropdown.OptionData(GetPotionName(i) + " x" + potionCounts[i]));

        potionDropdown.SetValueWithoutNotify(selectedDropdownValue);
        potionDropdown.RefreshShownValue();
    }

    private string GetPotionName(int index)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
        if (language == AppLanguage.English)
        {
            switch (index)
            {
                case 0:
                    return "Potion of Healing";
                case 1:
                    return "Potion of Greater Healing";
                case 2:
                    return "Potion of Superior Healing";
                case 3:
                    return "Potion of Supreme Healing";
            }
        }

        if (language == AppLanguage.Russian)
        {
            switch (index)
            {
                case 0:
                    return "Зелье лечения";
                case 1:
                    return "Большое зелье лечения";
                case 2:
                    return "Улучшенное зелье лечения";
                case 3:
                    return "Высшее зелье лечения";
            }
        }

        switch (index)
        {
            case 0:
                return "Зілля лікування";
            case 1:
                return "Велике зілля лікування";
            case 2:
                return "Покращене зілля лікування";
            case 3:
                return "Найвище зілля лікування";
            default:
                return "Зілля";
        }
    }

    private string GetCalculatorText(string key)
    {
        AppLanguage language = RuntimeLocalization.EnsureExists().CurrentLanguage;
        bool english = language == AppLanguage.English;
        bool russian = language == AppLanguage.Russian;

        switch (key)
        {
            case "choosePotion":
                return english ? "Choose potion" : russian ? "Выберите зелье" : "Оберіть зілля";
            case "noPotion":
                return english ? "No potion" : russian ? "Нет зелья" : "Немає зілля";
            case "potionError":
                return english ? "Potion error" : russian ? "Ошибка зелья" : "Помилка зілля";
            case "hpBarNotFound":
                return english ? "HP bar not found" : russian ? "HP бар не найден" : "HP бар не знайдено";
            case "tempHp":
                return english ? "Temp HP" : russian ? "Врем. HP" : "Тимч. HP";
            case "damageDone":
                return english ? "Damage taken" : russian ? "Получено урона" : "Отримано урону";
            case "healed":
                return english ? "Healed" : russian ? "Исцелено" : "Зцілено";
            case "longRest":
                return english ? "Long rest" : russian ? "Долгий отдых" : "Довгий відпочинок";
            case "shortRest":
                return english ? "Short rest" : russian ? "Короткий отдых" : "Короткий відпочинок";
            case "hitDiceNotFound":
                return english ? "Hit dice not found" : russian ? "Кости хитов не найдены" : "Кістки хітів не знайдено";
        }

        return key;
    }

    private string GetHpModeLabel(HpCalculatorMode mode)
    {
        switch (mode)
        {
            case HpCalculatorMode.MaxHp:
                return "Max HP:";
            case HpCalculatorMode.TemporaryHp:
                return GetCalculatorText("tempHp") + ":";
            case HpCalculatorMode.Damage:
                return RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English ? "Damage:" :
                    RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.Russian ? "Урон:" : "Урон:";
            case HpCalculatorMode.Heal:
                return RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.English ? "Healing:" :
                    RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.Russian ? "Лечение:" : "Зцілення:";
            default:
                return "";
        }
    }

    private string GetHealedText(int value)
    {
        return GetCalculatorText("healed") + "  " + value + " HP";
    }

    private string GetDamageText(int value)
    {
        return GetCalculatorText("damageDone") + "  " + value;
    }

    private void OnHpButtonClick(string buttonName, Button button)
    {
        CaptureHpTextColorFromButton(button);

        OnButtonClick(buttonName);
    }

    private void CaptureHpTextColorFromButton(Button button)
    {
        Text buttonText = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (buttonText != null)
        {
            hpTextColor = buttonText.color;
            hasHpTextColor = true;
            ApplyHpTextColor();
        }
    }

    private void OnButtonClick(string rawLabel)
    {
        string label = NormalizeLabel(rawLabel);
        if (string.IsNullOrEmpty(label))
            return;

        if (HandleHpModeButton(label))
            return;

        if (label == "C")
        {
            ResetCalculator();
            return;
        }

        if (label == "CE")
        {
            ClearEntry();
            return;
        }

        if (label == "=")
        {
            CalculateResult();
            return;
        }

        if (IsOperator(label))
        {
            AddOperator(label[0]);
            return;
        }

        if (IsDiceLabel(label))
        {
            AddDice(label);
            return;
        }

        if (IsNumberLabel(label))
        {
            AddNumber(label);
        }
    }

    private string NormalizeLabel(string label)
    {
        if (label == "×" || label == "x" || label == "X")
            return "*";

        if (label == "÷")
            return "/";

        return label.Replace(" ", "");
    }

    private bool HandleHpModeButton(string label)
    {
        string normalized = label.ToLowerInvariant();
        if (normalized == "maxhp")
        {
            SetHpMode(HpCalculatorMode.MaxHp);
            return true;
        }

        if (normalized == "folslive")
        {
            SetHpMode(HpCalculatorMode.TemporaryHp);
            return true;
        }

        if (normalized == "damage")
        {
            SetHpMode(HpCalculatorMode.Damage);
            return true;
        }

        if (normalized == "heal")
        {
            SetHpMode(HpCalculatorMode.Heal);
            return true;
        }

        if (normalized == "shortrest")
        {
            ApplyShortRest();
            return true;
        }

        if (normalized == "longrest")
        {
            ApplyLongRest();
            return true;
        }

        return false;
    }

    private bool IsHpButtonName(string label)
    {
        string normalized = label.ToLowerInvariant();
        return normalized == "maxhp" ||
               normalized == "damage" ||
               normalized == "heal" ||
               normalized == "folslive" ||
               normalized == "shortrest" ||
               normalized == "longrest";
    }

    private void SetHpMode(HpCalculatorMode mode)
    {
        hpMode = mode;
        hpModeLabel = GetHpModeLabel(mode);
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        if (equationText != null)
            equationText.text = hpModeLabel;
        if (resultText != null)
            resultText.text = "";
        ApplyHpTextColor();
    }

    private void AddNumber(string label)
    {
        if (HasDisplayedResult())
            ResetCalculator();

        currentEquation += label;
        isOperatorClicked = false;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void AddOperator(char operatorChar)
    {
        if (currentEquation.Length == 0)
        {
            if (operatorChar == '-')
            {
                currentEquation = "-";
                RefreshEquationText();
            }

            return;
        }

        if (IsLastCharOperator())
        {
            currentEquation = currentEquation.Substring(0, currentEquation.Length - 1) + operatorChar;
        }
        else
        {
            currentEquation += operatorChar;
        }

        isOperatorClicked = true;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void AddDice(string label)
    {
        if (HasDisplayedResult())
            ResetCalculator();

        if (isLastInputDice)
            return;

        currentEquation += label.ToLowerInvariant();
        isOperatorClicked = false;
        isLastInputDice = true;
        RefreshEquationText();
    }

    private void CalculateResult()
    {
        if (string.IsNullOrWhiteSpace(currentEquation))
            return;

        string expression = TrimTrailingOperators(currentEquation);
        if (string.IsNullOrWhiteSpace(expression))
            return;

        expression = ProcessDiceNotation(expression);
        if (!TryEvaluateExpression(expression, out double result))
        {
            if (resultText != null)
                resultText.text = "=0";
            return;
        }

        if (hpMode != HpCalculatorMode.None)
        {
            ApplyHpMode(Mathf.RoundToInt((float)result));
            return;
        }

        string formattedResult = FormatNumber(result);
        if (resultText != null)
            resultText.text = "=" + formattedResult;
        currentEquation = formattedResult;
        isOperatorClicked = false;
        isLastInputDice = false;
        RefreshEquationText();
    }

    private void ApplyHpMode(int value)
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;
        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult(GetCalculatorText("hpBarNotFound"));
            hpMode = HpCalculatorMode.None;
            currentEquation = "";
            return;
        }

        value = Mathf.Max(0, value);
        if (hpMode == HpCalculatorMode.MaxHp)
        {
            if (healthBar != null)
                healthBar.SetMaxHealthAndFill(value);
            else
                healthBar1.SetMaxHealthAndFill(value);

            ShowHpResult("Max HP =  " + value);
        }
        else if (hpMode == HpCalculatorMode.TemporaryHp)
        {
            if (healthBar != null)
                healthBar.SetTemporaryHealth(value);
            else
                healthBar1.SetTemporaryHealth(value);

            ShowHpResult(GetCalculatorText("tempHp") + " =  " + value);
        }
        else if (hpMode == HpCalculatorMode.Damage)
        {
            int applied = healthBar != null ? healthBar.ApplyDamage(value) : healthBar1.ApplyDamage(value);
            ShowHpResult(GetDamageText(applied));
        }
        else if (hpMode == HpCalculatorMode.Heal)
        {
            int applied = healthBar != null ? healthBar.ApplyHeal(value) : healthBar1.ApplyHeal(value);
            ShowHpResult(GetHealedText(applied));
        }

        hpMode = HpCalculatorMode.None;
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private void ApplyLongRest()
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;

        int healed = 0;
        if (healthBar != null)
            healed = healthBar.RestoreToMaxHealth();
        else if (healthBar1 != null)
            healed = healthBar1.RestoreToMaxHealth();

        ApplyRestResources(true);
        ClearSpellSlots();
        ReduceExhaustionByOne();
        ClearDeathSaves();
        SaveSceneAfterRest();
        ApplyGlobalRestToSaveData(true);
        ShowHpResult(healthBar != null || healthBar1 != null ? GetHealedText(healed) : GetCalculatorText("longRest"));
        ResetHpInputState();
    }

    private void ApplyShortRest()
    {
        HealthBar healthBar = FindActiveHealthBar();
        HealthBar1 healthBar1 = healthBar == null ? FindActiveHealthBar1() : null;

        if (healthBar != null)
            healthBar.ClearTemporaryHealth();
        else if (healthBar1 != null)
            healthBar1.ClearTemporaryHealth();

        ApplyRestResources(false);
        SaveSceneAfterRest();
        ApplyGlobalRestToSaveData(false);

        if (healthBar == null && healthBar1 == null)
        {
            ShowHpResult(GetCalculatorText("shortRest"));
            ResetHpInputState();
            return;
        }

        if (!TryGetHitDice(out int diceCount, out int diceSides))
        {
            ShowHpResult(GetCalculatorText("hitDiceNotFound"));
            ResetHpInputState();
            return;
        }

        int diceToRoll = Mathf.CeilToInt(diceCount / 2f);
        int roll = 0;
        for (int i = 0; i < diceToRoll; i++)
            roll += UnityEngine.Random.Range(1, diceSides + 1);

        int healed = healthBar != null ? healthBar.ApplyHeal(roll) : healthBar1.ApplyHeal(roll);
        ShowHpResult(GetHealedText(healed) + " (" + diceToRoll + "d" + diceSides + "=" + roll + ")");
        ResetHpInputState();
    }

    private void ApplyRestResources(bool isLongRest)
    {
        Transform resourceRoot = FindSceneTransformByName("resursClas");
        if (resourceRoot == null)
            return;

        ClearPanelsByMarkers(resourceRoot, "WildShape", "ChannelDivinity", "KiPoints", "DragonBreath");

        Transform bloodPanel = FindPanelByMarker(resourceRoot, "BloodCurse");
        if (bloodPanel == null)
            bloodPanel = FindDirectChild(resourceRoot, "Panel (5)");

        if (bloodPanel != null)
        {
            ClearPanelToggles(bloodPanel, 0, 7);
            if (isLongRest)
                ClearPanelToggles(bloodPanel, 8, 11);
        }

        if (isLongRest)
            ClearPanelsByMarkers(resourceRoot, "Rage", "SorceryPoints", "Flight");
    }

    private void ClearPanelsByMarkers(Transform resourceRoot, params string[] markerNames)
    {
        HashSet<Transform> clearedPanels = new HashSet<Transform>();
        foreach (string markerName in markerNames)
        {
            Transform panel = FindPanelByMarker(resourceRoot, markerName);
            if (panel != null && clearedPanels.Add(panel))
                ClearPanelToggles(panel);
        }
    }

    private Transform FindPanelByMarker(Transform resourceRoot, string markerName)
    {
        if (resourceRoot == null)
            return null;

        foreach (Transform child in resourceRoot.GetComponentsInChildren<Transform>(true))
            if (child != resourceRoot && NameMatches(child.name, markerName))
                return child.parent;

        return null;
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
            if (child != null && child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;

        return null;
    }

    private void ReduceExhaustionByOne()
    {
        Transform exhaustionRoot = FindSceneTransformByName("vtoma");
        if (exhaustionRoot == null)
            return;

        List<Toggle> toggles = GetPanelToggles(exhaustionRoot, 0, 5);
        if (toggles.Count == 0)
            return;

        toggles.Sort((left, right) => GetToggleNumber(left.name).CompareTo(GetToggleNumber(right.name)));

        int checkedCount = 0;
        foreach (Toggle toggle in toggles)
            if (toggle != null && toggle.isOn)
                checkedCount++;

        if (checkedCount <= 0)
            return;

        Toggle lastCheckedToggle = toggles[Mathf.Clamp(checkedCount - 1, 0, toggles.Count - 1)];
        if (lastCheckedToggle != null)
            lastCheckedToggle.isOn = false;
    }

    private void ClearDeathSaves()
    {
        Transform deathRoot = FindSceneTransformByName("deadChekBox");
        if (deathRoot == null)
            deathRoot = FindSceneTransformByName("deadCheckBox");

        if (deathRoot == null)
            return;

        ClearPanelToggles(deathRoot);
    }

    private void ClearSpellSlots()
    {
        Transform spellSlotsRoot = FindSceneTransformByName("spelChek");
        if (spellSlotsRoot == null)
            return;

        ClearPanelToggles(spellSlotsRoot);
    }

    private void ClearPanelToggles(Transform panel, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        foreach (Toggle toggle in GetPanelToggles(panel, minToggleNumber, maxToggleNumber))
            if (toggle != null)
                toggle.isOn = false;
    }

    private List<Toggle> GetPanelToggles(Transform panel, int minToggleNumber, int maxToggleNumber)
    {
        List<Toggle> toggles = new List<Toggle>();
        if (panel == null)
            return toggles;

        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle == null || !NameMatches(toggle.name, "Toggle"))
                continue;

            int toggleNumber = GetToggleNumber(toggle.name);
            if (toggleNumber < minToggleNumber || toggleNumber > maxToggleNumber)
                continue;

            if (IsInsideDropdown(toggle.transform, panel))
                continue;

            toggles.Add(toggle);
        }

        return toggles;
    }

    private Transform FindSceneTransformByName(string objectName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
            if (item != null && item.gameObject.scene.IsValid() && NameMatches(item.name, objectName))
                return item;

        return null;
    }

    private bool IsInsideDropdown(Transform transform, Transform stopAt)
    {
        Transform current = transform;
        while (current != null && current != stopAt)
        {
            if (current.GetComponent<Dropdown>() != null || current.GetComponent<TMP_Dropdown>() != null)
                return true;

            current = current.parent;
        }

        return false;
    }

    private int GetToggleNumber(string name)
    {
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open >= 0 && close > open && int.TryParse(name.Substring(open + 1, close - open - 1), out int number))
            return number;

        return 0;
    }

    private bool NameMatches(string actualName, string expectedName)
    {
        return GetBaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetBaseName(string name)
    {
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }

    private void SaveSceneAfterRest()
    {
        CharacterSheetManagerScene1 sheetManager = UnityEngine.Object.FindAnyObjectByType<CharacterSheetManagerScene1>();
        if (sheetManager != null)
        {
            sheetManager.SaveCharacterData();
            return;
        }

        CharacterSceneAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<CharacterSceneAutoSave>();
        if (autoSave != null)
            autoSave.SaveSceneData();
    }

    private void ApplyGlobalRestToSaveData(bool isLongRest)
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
        if (character == null || character.sceneStates == null)
            return;

        foreach (CharacterSceneData sceneData in character.sceneStates)
        {
            if (sceneData == null)
                continue;

            ClearSavedPanelsByMarkers(sceneData, "WildShape", "ChannelDivinity", "KiPoints", "DragonBreath");

            string bloodPanelPath = GetRestPanelPath(sceneData, "BloodCurse");
            ClearSavedPanelToggles(sceneData, bloodPanelPath, 0, 7);
            if (isLongRest)
                ClearSavedPanelToggles(sceneData, bloodPanelPath, 8, 11);

            if (!isLongRest)
                continue;

            RestoreSavedHealthBars(sceneData);
            ClearSavedPanelsByMarkers(sceneData, "Rage", "SorceryPoints", "Flight");
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "SpellSlots"));
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, "DeathSaves"));
            ReduceSavedExhaustionByOne(sceneData, GetRestPanelPath(sceneData, "Exhaustion"));
        }

        saveManager.SaveData();
    }

    private void RestoreSavedHealthBars(CharacterSceneData sceneData)
    {
        if (sceneData == null || sceneData.intData == null)
            return;

        foreach (IntSaveEntry entry in sceneData.intData)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || !entry.key.StartsWith("HealthBar_", StringComparison.Ordinal))
                continue;

            if (!entry.key.EndsWith("_maxHealth", StringComparison.Ordinal))
                continue;

            string prefix = entry.key.Substring(0, entry.key.Length - "maxHealth".Length);
            sceneData.SetInt(prefix + "currentHealth", Mathf.Max(0, entry.value));
            sceneData.SetInt(prefix + "maxTemporaryHealth", 0);
            sceneData.SetInt(prefix + "currentTemporaryHealth", 0);
        }
    }

    private void ClearSavedPanelsByMarkers(CharacterSceneData sceneData, params string[] markerNames)
    {
        foreach (string markerName in markerNames)
            ClearSavedPanelToggles(sceneData, GetRestPanelPath(sceneData, markerName));
    }

    private string GetRestPanelPath(CharacterSceneData sceneData, string markerName)
    {
        return sceneData != null ? sceneData.GetString(RestResourceKeyPrefix + markerName, "") : "";
    }

    private void ClearSavedPanelToggles(CharacterSceneData sceneData, string panelPath, int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        if (sceneData == null || string.IsNullOrEmpty(panelPath) || sceneData.intData == null)
            return;

        string prefix = ToggleKeyPrefix + panelPath + "/";
        foreach (IntSaveEntry entry in sceneData.intData)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || !entry.key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            int toggleNumber = GetToggleNumber(entry.key);
            if (toggleNumber < minToggleNumber || toggleNumber > maxToggleNumber)
                continue;

            entry.value = 0;
        }
    }

    private void ReduceSavedExhaustionByOne(CharacterSceneData sceneData, string panelPath)
    {
        if (sceneData == null || string.IsNullOrEmpty(panelPath) || sceneData.intData == null)
            return;

        string prefix = ToggleKeyPrefix + panelPath + "/";
        List<IntSaveEntry> entries = new List<IntSaveEntry>();
        foreach (IntSaveEntry entry in sceneData.intData)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || !entry.key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            int toggleNumber = GetToggleNumber(entry.key);
            if (toggleNumber >= 0 && toggleNumber <= 5)
                entries.Add(entry);
        }

        entries.Sort((left, right) => GetToggleNumber(left.key).CompareTo(GetToggleNumber(right.key)));

        int checkedCount = 0;
        foreach (IntSaveEntry entry in entries)
            if (entry.value != 0)
                checkedCount++;

        if (checkedCount <= 0)
            return;

        entries[Mathf.Clamp(checkedCount - 1, 0, entries.Count - 1)].value = 0;
    }

    private void ResetHpInputState()
    {
        hpMode = HpCalculatorMode.None;
        hpModeLabel = "";
        currentEquation = "";
        isOperatorClicked = false;
        isLastInputDice = false;
    }

    private bool HasDisplayedResult()
    {
        return resultText != null && resultText != equationText && !string.IsNullOrEmpty(resultText.text);
    }

    private void ResetCalculator()
    {
        currentEquation = "";
        hpModeLabel = "";
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = "";
        isOperatorClicked = false;
        isLastInputDice = false;
        hpMode = HpCalculatorMode.None;
    }

    private void ClearEntry()
    {
        if (currentEquation.Length == 0)
            return;

        currentEquation = currentEquation.Substring(0, currentEquation.Length - 1);
        RecalculateInputFlags();
        RefreshEquationText();
    }

    private string ProcessDiceNotation(string equation)
    {
        return Regex.Replace(equation, @"(\d*)[dD](\d+)", match =>
        {
            int diceCount = 1;
            if (!string.IsNullOrEmpty(match.Groups[1].Value))
                int.TryParse(match.Groups[1].Value, out diceCount);

            if (!int.TryParse(match.Groups[2].Value, out int diceSides))
                return "0";

            diceCount = Mathf.Clamp(diceCount, 1, 100);
            diceSides = Mathf.Clamp(diceSides, 1, 1000);

            int total = 0;
            for (int i = 0; i < diceCount; i++)
                total += UnityEngine.Random.Range(1, diceSides + 1);

            return total.ToString(CultureInfo.InvariantCulture);
        });
    }

    private bool TryEvaluateExpression(string expression, out double result)
    {
        result = 0;
        expression = expression.Replace(" ", "");

        try
        {
            ExpressionParser parser = new ExpressionParser(expression);
            result = parser.ParseExpression();
            return parser.IsAtEnd && !double.IsNaN(result) && !double.IsInfinity(result);
        }
        catch
        {
            result = 0;
            return false;
        }
    }

    private HealthBar FindActiveHealthBar()
    {
        HealthBar[] bars = UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Exclude);
        foreach (HealthBar bar in bars)
            if (bar != null && bar.IsUsableForCalculator)
                return bar;

        return bars.Length > 0 ? bars[0] : null;
    }

    private HealthBar1 FindActiveHealthBar1()
    {
        HealthBar1[] bars = UnityEngine.Object.FindObjectsByType<HealthBar1>(FindObjectsInactive.Exclude);
        foreach (HealthBar1 bar in bars)
            if (bar != null && bar.IsUsableForCalculator)
                return bar;

        return bars.Length > 0 ? bars[0] : null;
    }

    private bool TryGetHitDice(out int diceCount, out int diceSides)
    {
        diceCount = 0;
        diceSides = 0;

        InputField allDiceField = FindInputFieldByName("alldise", "alldaise");
        InputField diceValueField = FindInputFieldByName("daicevalueperson");
        if (allDiceField == null || diceValueField == null)
            return false;

        if (!int.TryParse(ExtractFirstNumber(allDiceField.text), out diceCount))
            return false;

        if (!int.TryParse(ExtractFirstNumber(diceValueField.text), out diceSides))
            return false;

        diceCount = Mathf.Clamp(diceCount, 0, 100);
        diceSides = Mathf.Clamp(diceSides, 1, 1000);
        return diceCount > 0;
    }

    private InputField FindInputFieldByName(params string[] objectNames)
    {
        InputField[] fields = UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include);
        foreach (InputField field in fields)
        {
            if (field == null || !field.gameObject.activeInHierarchy)
                continue;

            foreach (string objectName in objectNames)
            {
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
            }
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;

            foreach (string objectName in objectNames)
            {
                if (string.Equals(field.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return field;
            }
        }

        foreach (InputField field in fields)
        {
            if (field == null)
                continue;

            foreach (string objectName in objectNames)
            {
                if (field.gameObject.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return field;
            }
        }

        return null;
    }

    private void ApplyHpTextColor()
    {
        if (!hasHpTextColor)
            return;

        if (equationText != null)
            equationText.color = hpTextColor;

        if (resultText != null)
            resultText.color = hpTextColor;
    }

    private void ShowHpResult(string message)
    {
        if (equationText != null)
            equationText.text = "";
        if (resultText != null)
            resultText.text = message;
        ApplyHpTextColor();
    }

    private string ExtractFirstNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        Match match = Regex.Match(value, @"\d+");
        return match.Success ? match.Value : "";
    }

    private bool IsNumberLabel(string label)
    {
        return double.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private bool IsDiceLabel(string label)
    {
        return Regex.IsMatch(label, @"^[dD]\d+$");
    }

    private bool IsOperator(string label)
    {
        return label.Length == 1 && "+-*/".Contains(label);
    }

    private bool IsLastCharOperator()
    {
        return currentEquation.Length > 0 && "+-*/".Contains(currentEquation[currentEquation.Length - 1].ToString());
    }

    private string TrimTrailingOperators(string expression)
    {
        while (expression.Length > 0 && "+-*/".Contains(expression[expression.Length - 1].ToString()))
            expression = expression.Substring(0, expression.Length - 1);

        return expression;
    }

    private void RecalculateInputFlags()
    {
        isOperatorClicked = IsLastCharOperator();
        isLastInputDice = Regex.IsMatch(currentEquation, @"[dD]\d+$");
    }

    private void RefreshEquationText()
    {
        if (equationText == null)
            return;

        equationText.text = hpMode != HpCalculatorMode.None ? hpModeLabel + currentEquation : currentEquation;
    }

    private string FormatNumber(double value)
    {
        if (Math.Abs(value % 1) < 0.000001)
            return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private class ExpressionParser
    {
        private readonly string expression;
        private int index;

        public bool IsAtEnd
        {
            get
            {
                SkipWhitespace();
                return index >= expression.Length;
            }
        }

        public ExpressionParser(string expression)
        {
            this.expression = expression;
        }

        public double ParseExpression()
        {
            double value = ParseTerm();

            while (true)
            {
                SkipWhitespace();
                if (Match('+'))
                    value += ParseTerm();
                else if (Match('-'))
                    value -= ParseTerm();
                else
                    return value;
            }
        }

        private double ParseTerm()
        {
            double value = ParseFactor();

            while (true)
            {
                SkipWhitespace();
                if (Match('*'))
                    value *= ParseFactor();
                else if (Match('/'))
                    value /= ParseFactor();
                else
                    return value;
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();

            if (Match('+'))
                return ParseFactor();

            if (Match('-'))
                return -ParseFactor();

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            int start = index;

            while (index < expression.Length &&
                   (char.IsDigit(expression[index]) || expression[index] == '.'))
            {
                index++;
            }

            if (start == index)
                throw new FormatException("Expected number.");

            string number = expression.Substring(start, index - start);
            return double.Parse(number, CultureInfo.InvariantCulture);
        }

        private bool Match(char symbol)
        {
            if (index >= expression.Length || expression[index] != symbol)
                return false;

            index++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (index < expression.Length && char.IsWhiteSpace(expression[index]))
                index++;
        }
    }
}
