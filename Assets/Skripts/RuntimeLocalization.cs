using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum AppLanguage
{
    Ukrainian,
    English,
    Russian
}

public class LocalizedIgnore : MonoBehaviour
{
}

public partial class RuntimeLocalization : MonoBehaviour
{
    public static RuntimeLocalization Instance { get; private set; }

    private const string LanguagePrefsKey = "DndAppLanguage";
    private readonly Dictionary<string, Translation> translations = new Dictionary<string, Translation>();
    private readonly Dictionary<string, string> sourceByTranslatedText = new Dictionary<string, string>();

    public AppLanguage CurrentLanguage { get; private set; }
    private Coroutine syncUnityLocaleCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildTranslations();
        CurrentLanguage = NormalizeLanguage(PlayerPrefs.GetInt(LanguagePrefsKey, (int)AppLanguage.Ukrainian));
        SyncUnityLocalizationPackage(CurrentLanguage);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static RuntimeLocalization EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject localizationObject = new GameObject("RuntimeLocalization");
        return localizationObject.AddComponent<RuntimeLocalization>();
    }

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = NormalizeLanguage((int)language);
        PlayerPrefs.SetInt(LanguagePrefsKey, (int)CurrentLanguage);
        PlayerPrefs.Save();
        SyncUnityLocalizationPackage(CurrentLanguage);
        ApplyToScene();
    }

    private AppLanguage NormalizeLanguage(int value)
    {
        if (value < 0 || value > 2)
            return AppLanguage.Ukrainian;

        return (AppLanguage)value;
    }

    private void SyncUnityLocalizationPackage(AppLanguage language)
    {
        if (!isActiveAndEnabled)
            return;

        if (syncUnityLocaleCoroutine != null)
            StopCoroutine(syncUnityLocaleCoroutine);

        syncUnityLocaleCoroutine = StartCoroutine(SyncUnityLocalizationPackageRoutine(language));
    }

    private IEnumerator SyncUnityLocalizationPackageRoutine(AppLanguage language)
    {
        yield return LocalizationSettings.InitializationOperation;

        string targetCode = GetUnityLocaleCode(language);
        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale != null &&
                (locale.Identifier.Code == targetCode || locale.Identifier.Code.StartsWith(targetCode + "-")))
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        syncUnityLocaleCoroutine = null;
    }

    private string GetUnityLocaleCode(AppLanguage language)
    {
        if (language == AppLanguage.English)
            return "en";

        if (language == AppLanguage.Russian)
            return "ru";

        return "uk";
    }

    public string Translate(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        string visibleKey = Normalize(source);
        if (sourceByTranslatedText.TryGetValue(visibleKey, out string canonicalSource))
            source = canonicalSource;

        if (CurrentLanguage == AppLanguage.Ukrainian)
            return source;

        string key = Normalize(source);
        if (!translations.TryGetValue(key, out Translation translation))
            return source;

        return CurrentLanguage == AppLanguage.English ? translation.English : translation.Russian;
    }

    public bool HasTranslationSource(string source)
    {
        if (string.IsNullOrEmpty(source))
            return false;

        return translations.ContainsKey(Normalize(source));
    }

    public void ApplyToScene()
    {
        foreach (ManualLocalizedText text in Resources.FindObjectsOfTypeAll<ManualLocalizedText>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            text.Apply();
        }

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedText localizedText = text.GetComponent<LocalizedText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedText>();

            localizedText.Apply();
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedTmpText localizedText = text.GetComponent<LocalizedTmpText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTmpText>();

            localizedText.Apply();
        }

        foreach (TextMesh text in Resources.FindObjectsOfTypeAll<TextMesh>())
        {
            if (!IsSceneObject(text != null ? text.gameObject : null))
                continue;

            if (ShouldSkip(text))
                continue;

            LocalizedTextMesh localizedText = text.GetComponent<LocalizedTextMesh>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTextMesh>();

            localizedText.Apply();
        }

        foreach (Dropdown dropdown in Resources.FindObjectsOfTypeAll<Dropdown>())
        {
            if (!IsSceneObject(dropdown != null ? dropdown.gameObject : null))
                continue;

            ApplyDropdownOptions(dropdown);
        }

        foreach (TMP_Dropdown dropdown in Resources.FindObjectsOfTypeAll<TMP_Dropdown>())
        {
            if (!IsSceneObject(dropdown != null ? dropdown.gameObject : null))
                continue;

            ApplyDropdownOptions(dropdown);
        }

        foreach (CalculatorManager calculator in Resources.FindObjectsOfTypeAll<CalculatorManager>())
        {
            if (!IsSceneObject(calculator != null ? calculator.gameObject : null))
                continue;

            calculator.RefreshLocalization();
        }

        foreach (InventoryItemCell inventoryCell in Resources.FindObjectsOfTypeAll<InventoryItemCell>())
        {
            if (!IsSceneObject(inventoryCell != null ? inventoryCell.gameObject : null))
                continue;

            inventoryCell.RefreshLocalization();
        }
    }

    private void ApplyDropdownOptions(Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null)
            return;

        bool changed = false;
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string translated = Translate(dropdown.options[i].text);
            if (translated == dropdown.options[i].text)
                continue;

            dropdown.options[i].text = translated;
            changed = true;
        }

        if (changed)
            dropdown.RefreshShownValue();
    }

    private void ApplyDropdownOptions(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null)
            return;

        bool changed = false;
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string translated = Translate(dropdown.options[i].text);
            if (translated == dropdown.options[i].text)
                continue;

            dropdown.options[i].text = translated;
            changed = true;
        }

        if (changed)
            dropdown.RefreshShownValue();
    }

    private bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyAfterSceneLoaded());
    }

    private IEnumerator ApplyAfterSceneLoaded()
    {
        yield return null;
        ApplyToScene();
        yield return null;
        ApplyToScene();
        yield return new WaitForSecondsRealtime(0.1f);
        ApplyToScene();
    }

    private bool ShouldSkip(Text text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        if (IsEditableInputText(text) && !HasTranslationForVisibleText(text.text))
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool ShouldSkip(TMP_Text text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        if (IsEditableInputText(text) && !HasTranslationForVisibleText(text.text))
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsEditableInputText(Text text)
    {
        InputField inputField = text != null ? text.GetComponentInParent<InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

    private bool IsEditableInputText(TMP_Text text)
    {
        TMP_InputField inputField = text != null ? text.GetComponentInParent<TMP_InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

    private bool HasTranslationForVisibleText(string value)
    {
        string source = GetSourceText(value);
        return HasTranslationSource(source);
    }

    private bool ShouldSkip(TextMesh text)
    {
        if (text == null)
            return true;

        if (text.GetComponentInParent<LocalizedIgnore>(true) != null)
            return true;

        Transform current = text.transform;
        while (current != null)
        {
            if (current.name == "CharacterButton" ||
                current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" ||
                current.name == "LanguageButtons")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    public string GetSourceText(string value)
    {
        string key = Normalize(value);
        if (sourceByTranslatedText.TryGetValue(key, out string source))
            return source;

        return value;
    }

    private string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Replace('\u00A0', ' ')
            .Replace('\u200B', ' ')
            .Trim();

        while (value.Contains("  "))
            value = value.Replace("  ", " ");

        return value.ToLowerInvariant();
    }

    private void Add(string ukrainian, string english, string russian)
    {
        string sourceKey = Normalize(ukrainian);
        translations[sourceKey] = new Translation(english, russian);
        sourceByTranslatedText[Normalize(english)] = ukrainian;
        sourceByTranslatedText[Normalize(russian)] = ukrainian;
        sourceByTranslatedText[sourceKey] = ukrainian;
    }

    private void BuildTranslations()
    {
        Add("Ресет", "Reset", "Сброс");
        Add("Ресет HP", "Reset HP", "Сброс HP");
        Add("Підтвердить", "Confirm", "Подтвердить");
        Add("Підтвердити", "Confirm", "Подтвердить");
        Add("Сторінка №", "Page #", "Страница №");
        Add("Сторінка №1", "Page #1", "Страница №1");
        Add("Сторінка №2", "Page #2", "Страница №2");
        Add("Сторінка №3", "Page #3", "Страница №3");
        Add("Сторінка №4", "Page #4", "Страница №4");
        Add("Сторінка №5", "Page #5", "Страница №5");
        Add("Введіть опис...", "Enter description...", "Введите описание...");
        Add("Файл збережених\nперсонажів", "Saved\ncharacters file", "Файл сохраненных\nперсонажей");
        Add("Файл збережених даних", "Saved data file", "Файл сохраненных данных");
        Add("Файл збережених данних", "Saved data file", "Файл сохраненных данных");
        Add("Файл одного персонажа", "Single character file", "Файл одного персонажа");
        Add("Файл всіх персонажів", "All characters file", "Файл всех персонажей");
        Add("Додати нового персонажа", "Add new character", "Добавить нового персонажа");
        Add("Додать нового персонажа", "Add new character", "Добавить нового персонажа");
        Add("Додати персонажа", "Add character", "Добавить персонажа");
        Add("Новий персонаж", "New character", "Новый персонаж");
        Add("Невідомий персонаж", "Unknown character", "Неизвестный персонаж");
        Add("Немає персонажів", "No characters", "Нет персонажей");
        Add("Оберіть персонажа", "Choose character", "Выберите персонажа");
        Add("Автори", "Authors", "Авторы");
        Add("Про додаток", "About the app", "О приложении");
        Add("Інформація про додаток", "Application information", "Информация о приложении");
        Add("Інформація про\nдодаток", "Application\ninformation", "Информация о\nприложении");
        Add("Записник ідей\nдля персонажа", "Character idea\nnotebook", "Записник идей\nдля персонажа");
        Add("Button", "Button", "Кнопка");

        Add("ПередІсторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Передісторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Нотатки  №1", "Notes #1", "Заметки №1");
        Add("Нотатки  №2", "Notes #2", "Заметки №2");
        Add("Волося", "Hair", "Волосы");
        Add("Зріст", "Height", "Рост");
        Add("Шкіра", "Skin", "Кожа");
        Add("Очі", "Eyes", "Глаза");
        Add("Вік", "Age", "Возраст");
        Add("Вага", "Weight", "Вес");
        Add("Ім'я персонажа", "Character Name", "Имя персонажа");
        Add("Особисті Цілі", "Personal Goals", "Личные цели");
        Add("Зовнішність персонажа", "Character Appearance", "Внешность персонажа");
        Add("Слабкості", "Flaws", "Слабости");
        Add("Додаткові риси", "Additional Traits", "Дополнительные черты");
        Add("Ідеали", "Ideals", "Идеалы");
        Add("Союзники та Організації", "Allies and Organizations", "Союзники и организации");
        Add("Уподобання", "Likes", "Предпочтения");
        Add("Риси характеру", "Personality Traits", "Черты характера");
        Add("Завдання", "Quest", "Задание");

        Add("Зброя", "Weapons", "Оружие");
        Add("Броня", "Armor", "Броня");
        Add("Сумки", "Bags", "Сумки");
        Add("Магія", "Magic", "Магия");
        Add("Інше", "Other", "Другое");
        Add("Скарби", "Treasure", "Сокровища");
        Add("Своя картинка", "Custom image", "Своя картинка");
        Add("Набори", "Kits", "Наборы");
        Add("Різне", "Other", "Разное");
        Add("Прикраси", "Jewelry", "Украшения");
        Add("Введіть назву...", "Enter name...", "Введите название...");
        Add("Навички - Інфузії", "Skills - Infusions", "Навыки - Инфузии");
        Add("Калькулятор", "Calculator", "Калькулятор");
        Add("Предісторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("ПредІсторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Передісторія Персонажа", "Character Backstory", "Предыстория персонажа");
        Add("Завантажити фото персонажа", "Upload character photo", "Загрузить фото персонажа");
        Add("Завантажити фото\nперсонажа", "Upload character\nphoto", "Загрузить фото\nперсонажа");
        Add("Довгий відпочинок", "Long rest", "Долгий отдых");
        Add("Довгій відпочинок", "Long rest", "Долгий отдых");
        Add("Короткій відпочинок", "Short rest", "Короткий отдых");
        Add("Короткий відпочинок", "Short rest", "Короткий отдых");
        Add("Псевдожиття", "Temp HP", "Врем. HP");
        Add("Шкода", "Damage", "Урон");
        Add("Зцілення", "Healing", "Исцеление");
        Add("Мах ХП", "Max HP", "Макс. ХП");
        Add("Макс ХП", "Max HP", "Макс. ХП");
        Add("Max ХП", "Max HP", "Макс. ХП");
        Add("Випити", "Drink", "Выпить");
        Add("Оберіть зілля", "Choose potion", "Выберите зелье");
        Add("Сила", "Strength", "Сила");
        Add("Спритність", "Dexterity", "Ловкость");
        Add("Тілобудова", "Constitution", "Телосложение");
        Add("Інтелект", "Intelligence", "Интеллект");
        Add("Мудрість", "Wisdom", "Мудрость");
        Add("Харизма", "Charisma", "Харизма");
        Add("Модифікатори", "Modifiers", "Модификаторы");
        Add("Усього", "Total", "Всего");
        Add("Значення", "Value", "Значение");
        Add("Мод", "Mod", "Мод");
        Add("Натхнення", "Inspiration", "Вдохновение");
        Add("Бонус Майстерності", "Proficiency Bonus", "Бонус мастерства");
        Add("Кидки Рятунку", "Saving Throws", "Спасброски");
        Add("Рятунок від смерті", "Death Saves", "Спасброски от смерти");
        Add("НАВИЧКИ", "Skills", "Навыки");
        Add("Уміння та Особлисовсті", "Features & Traits", "Умения и особенности");
        Add("Уміння та Особливості", "Features & Traits", "Умения и особенности");
        Add("Атаки та заклинання", "Attacks & Spellcasting", "Атаки и заклинания");
        Add("Спорядження", "Equipment", "Снаряжение");
        Add("Інші володіння та мови", "Other Proficiencies & Languages", "Прочие владения и языки");
        Add("Пасивна Мудрість", "Passive Wisdom", "Пассивная мудрость");
        Add("Виснаження", "Exhaustion", "Истощение");
        Add("Швидкість", "Speed", "Скорость");
        Add("Ініціатива", "Initiative", "Инициатива");
        Add("Клас", "Class", "Класс");
        Add("Підклас", "Subclass", "Подкласс");
        Add("Рівень", "Level", "Уровень");
        Add("м.Підклас", "m.Subclass", "м.Подкласс");
        Add("м.Рівень", "m.Level", "м.Уровень");
        Add("Раса", "Race", "Раса");
        Add("Підраса", "Subrace", "Подраса");
        Add("Мультиклас", "Multiclass", "Мультикласс");
        Add("Передісторія", "Background", "Предыстория");
        Add("Світогляд", "Alignment", "Мировоззрение");
        Add("Досвід", "Experience", "Опыт");
        Add("Кістка хіт", "Hit Die", "Кость хитов");
        Add("Мод Магії", "Spellcasting Mod", "Мод. магии");
        Add("СП Кідок Магії", "Spell Save DC", "Сл. спасброска магии");
        Add("Ресурси класа / раси", "Class / Race Resources", "Ресурсы класса / расы");
        Add("Ресурси класу / раси", "Class / Race Resources", "Ресурсы класса / расы");
        Add("Зілля", "Potions", "Зелья");
        Add("Акробатика (Спр)", "Acrobatics (Dex)", "Акробатика (Лов)");
        Add("Аналіз (Інт)", "Investigation (Int)", "Анализ (Инт)");
        Add("Аналіз поведінки (Мдр)", "Insight (Wis)", "Проницательность (Мдр)");
        Add("Артистичність (Хар)", "Performance (Cha)", "Выступление (Хар)");
        Add("Атлетика (Сил)", "Athletics (Str)", "Атлетика (Сил)");
        Add("Виживання (Мдр)", "Survival (Wis)", "Выживание (Мдр)");
        Add("Догляд тварин (Мдр)", "Animal Handling (Wis)", "Уход за животными (Мдр)");
        Add("Залякування (Хар)", "Intimidation (Cha)", "Запугивание (Хар)");
        Add("Історія (Інт)", "History (Int)", "История (Инт)");
        Add("Магія (Інт)", "Arcana (Int)", "Магия (Инт)");
        Add("Медицина (Мдр)", "Medicine (Wis)", "Медицина (Мдр)");
        Add("Обман (Хар)", "Deception (Cha)", "Обман (Хар)");
        Add("Переконливість (Хар)", "Persuasion (Cha)", "Убеждение (Хар)");
        Add("Природа (Інт)", "Nature (Int)", "Природа (Инт)");
        Add("Релігія (інт)", "Religion (Int)", "Религия (Инт)");
        Add("Скритність (Спр)", "Stealth (Dex)", "Скрытность (Лов)");
        Add("Спритність рук (Спр)", "Sleight of Hand (Dex)", "Ловкость рук (Лов)");
        Add("Уважність (Мдр)", "Perception (Wis)", "Внимательность (Мдр)");
        Add("Прихована атака", "Sneak Attack", "Скрытая атака");
        Add("Дихання дракона", "Dragon Breath", "Дыхание дракона");
        Add("Кістка Гематокрафта", "Hemocraft Die", "Кость гемокрафта");
        Add("Лють варвара", "Barbarian Rage", "Ярость варвара");
        Add("Божественний канал", "Channel Divinity", "Божественный канал");
        Add("Божествений канал", "Channel Divinity", "Божественный канал");
        Add("Очкі ци", "Ki Points", "Очки ци");
        Add("Очки ци", "Ki Points", "Очки ци");
        Add("Метамагія", "Metamagic", "Метамагия");
        Add("Мета магія", "Metamagic", "Метамагия");
        Add("Прокляття крові", "Blood Curse", "Проклятие крови");
        Add("Багряні обряди", "Crimson Rites", "Багряные обряды");
        Add("Політ", "Flight", "Полет");
        Add("Сховати все", "Hide all", "Скрыть все");
        Add("Дикі форми", "Wild Shapes", "Дикие формы");
        Add("Дика форма", "Wild Shape", "Дикая форма");
        Add("Інша навичка", "Custom feature", "Своя особенность");
        Add("Оберіть Картинку навички", "Choose feature image", "Выберите картинку навыка");
        Add("Оберіть картинку навички", "Choose feature image", "Выберите картинку навыка");
        Add("Оберіть свою картинку", "Choose your image", "Выберите свою картинку");
        Add("Обрати свою картинку", "Choose your image", "Выбрать свою картинку");
        Add("мм", "CP", "мм");
        Add("см", "SP", "см");
        Add("ем", "EP", "эм");
        Add("зм", "GP", "зм");
        Add("пм", "PP", "пм");
        Add("ММ", "CP", "ММ");
        Add("СМ", "SP", "СМ");
        Add("ЕМ", "EP", "ЭМ");
        Add("ЗМ", "GP", "ЗМ");
        Add("ПМ", "PP", "ПМ");
        Add("Нотатки № 1", "Notes #1", "Заметки №1");
        Add("Нотатки № 2", "Notes #2", "Заметки №2");
        Add("Нотатки № 3", "Notes #3", "Заметки №3");
        Add("Нотатки № 4", "Notes #4", "Заметки №4");
        Add("Нотатки  № 3", "Notes #3", "Заметки №3");
        LoadTranslationsFromResource();
        BuildGeneratedTranslations();
    }

    partial void BuildGeneratedTranslations();

    private void LoadTranslationsFromResource()
    {
        TextAsset asset = Resources.Load<TextAsset>("Localization");
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return;

        string[] lines = asset.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split('\t');
            if (columns.Length < 3)
                continue;

            string ukrainian = DecodeResourceText(columns[0]);
            string english = DecodeResourceText(columns[1]);
            string russian = DecodeResourceText(columns[2]);

            if (!string.IsNullOrWhiteSpace(ukrainian) &&
                !string.IsNullOrWhiteSpace(english) &&
                !string.IsNullOrWhiteSpace(russian))
            {
                Add(ukrainian, english, russian);
            }
        }
    }

    private string DecodeResourceText(string value)
    {
        return value.Replace("\\n", "\n").Trim();
    }

    private struct Translation
    {
        public readonly string English;
        public readonly string Russian;

        public Translation(string english, string russian)
        {
            English = english;
            Russian = russian;
        }
    }
}

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private Text text;

    private void Awake()
    {
        text = GetComponent<Text>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<Text>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}

public class LocalizedTmpText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}

public class LocalizedTextMesh : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TextMesh text;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (text == null)
            text = GetComponent<TextMesh>();

        if (text == null)
            return;

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        CaptureSourceFromVisibleTextIfPossible(localization);
        CaptureSourceIfNeeded();
        text.text = localization.Translate(sourceText);
    }

    private void CaptureSourceIfNeeded()
    {
        if (text == null)
            return;

        if (string.IsNullOrEmpty(sourceText))
            sourceText = RuntimeLocalization.EnsureExists().GetSourceText(text.text);
    }

    private void CaptureSourceFromVisibleTextIfPossible(RuntimeLocalization localization)
    {
        if (text == null || localization == null)
            return;

        string visibleSource = localization.GetSourceText(text.text);
        if (localization.HasTranslationSource(visibleSource))
            sourceText = visibleSource;
    }
}
