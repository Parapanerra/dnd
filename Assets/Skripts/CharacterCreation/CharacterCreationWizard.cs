using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taruck.CharacterCreation
{
    public sealed class CharacterCreationWizard : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite inputSprite, menuSprite, backSprite, nextSprite, completeSprite, createdSprite, panelSprite;
        [SerializeField] private Sprite panelFrameSprite, panelBackgroundSprite, scrollTrackSprite, scrollHandleSprite, counterSprite;
        [SerializeField] private Sprite dropdownTemplateSprite;
        [SerializeField] private CreationSheetBindings bindings;
        [SerializeField] private RectTransform content, stepList, sidebar, summaryPanel, scrollRoot;
        [SerializeField] private TextMeshProUGUI heading, subtitle, summary, feedback, progress, nextLabel;
        [SerializeField] private UnityEngine.UI.Button next, back, menu;
        [SerializeField] private UnityEngine.UI.ScrollRect scroll;
        private CharacterCreationDraft draft = new CharacterCreationDraft();
        private CreationWizardUi ui;
        private int step;
        private bool created;
        private float lastAspect = -1;
        private bool compact;
        private Rect lastSafeArea;
        private string createdId;
        private string initialDraft;
        private TextMeshProUGUI nameError;
        private RuntimeLocalization localization;
        private AppLanguage shownLanguage;
        private static readonly string[] Steps = { "Правила і клас", "Походження", "Характеристики", "Навички й мови", "Спорядження", "Персонаж готовий" };
        public CharacterCreationDraft Draft => draft;
        public int CurrentStep => step;

        public void ConfigureVisuals(Sprite input, Sprite menuIcon, Sprite backIcon, Sprite nextIcon, Sprite completeIcon, Sprite createdIcon, Sprite panel,
            Sprite panelFrame = null, Sprite panelBackground = null, Sprite scrollTrack = null, Sprite scrollHandle = null, Sprite counter = null, Sprite dropdownTemplate = null)
        {
            inputSprite = input; menuSprite = menuIcon;
            backSprite = backIcon; nextSprite = nextIcon; completeSprite = completeIcon; createdSprite = createdIcon; panelSprite = panel;
            panelFrameSprite = panelFrame; panelBackgroundSprite = panelBackground;
            scrollTrackSprite = scrollTrack; scrollHandleSprite = scrollHandle;
            counterSprite = counter;
            dropdownTemplateSprite = dropdownTemplate;
            if (content != null) Rebuild();
        }

        public void Configure(TMP_FontAsset f, Sprite sprite, CreationSheetBindings map, RectTransform body,
            RectTransform steps, RectTransform side, RectTransform previewPanel, RectTransform view,
            TextMeshProUGUI title, TextMeshProUGUI description, TextMeshProUGUI preview,
            TextMeshProUGUI error, TextMeshProUGUI page, UnityEngine.UI.Button forward,
            UnityEngine.UI.Button previous, UnityEngine.UI.ScrollRect scroller)
        {
            font = f; buttonSprite = sprite; bindings = map; content = body; stepList = steps;
            sidebar = side; summaryPanel = previewPanel; scrollRoot = view; heading = title;
            subtitle = description; summary = preview; feedback = error; progress = page;
            next = forward; back = previous; nextLabel = forward.GetComponentInChildren<TextMeshProUGUI>(); scroll = scroller;
        }
        private void Awake() { Rebuild(); }
        private void Update()
        {
            if (localization != null && shownLanguage != localization.CurrentLanguage)
            {
                shownLanguage = localization.CurrentLanguage;
                if (ui != null && !created) Draw(false);
            }
            float aspect = (float)Screen.width / Math.Max(1, Screen.height);
            if (Math.Abs(aspect - lastAspect) < .005f && lastSafeArea == Screen.safeArea) return;
            lastAspect = aspect; lastSafeArea = Screen.safeArea; Layout(aspect);
        }
        private void Layout(float aspect)
        {
            bool wide = Screen.width >= 960 && aspect >= 1.3f;
            bool changed = compact == wide;
            compact = !wide;
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.referenceResolution = wide ? new Vector2(1600, 1000) : new Vector2(600, 900);
            var shell = (RectTransform)sidebar.parent;
            Rect safe = Screen.safeArea;
            shell.anchorMin = new Vector2(safe.xMin / Math.Max(1, Screen.width) + .02f, safe.yMin / Math.Max(1, Screen.height) + .02f);
            shell.anchorMax = new Vector2(safe.xMax / Math.Max(1, Screen.width) - .02f, safe.yMax / Math.Max(1, Screen.height) - .02f);
            shell.offsetMin = shell.offsetMax = Vector2.zero;
            if (wide)
            {
                Canvas.ForceUpdateCanvases();
                float availableWidth = ((RectTransform)transform).rect.width;
                float inset = Mathf.Max(0, (availableWidth * .96f - 1460f) * .5f);
                shell.offsetMin = new Vector2(inset, 0);
                shell.offsetMax = new Vector2(-inset, 0);
            }
            sidebar.gameObject.SetActive(wide); summaryPanel.gameObject.SetActive(wide);
            content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = !wide;
            CreationWizardUi.Fill(scrollRoot, wide ? 235 : 14, 155, wide ? 330 : 14, 200);
            SetNavigationRect((RectTransform)back.transform, false, wide);
            SetNavigationRect((RectTransform)next.transform, true, wide);
            if (menu != null)
            {
                var rect = (RectTransform)menu.transform;
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-24, -16);
                rect.sizeDelta = new Vector2(wide ? 70 : 68, wide ? 70 : 68);
                heading.rectTransform.offsetMax = new Vector2(wide ? -112 : -110, heading.rectTransform.offsetMax.y);
            }
            heading.fontSize = wide ? 38 : 29;
            subtitle.fontSize = wide ? 19 : 16;
            if (changed && ui != null) Draw(false);
        }
        private static void SetNavigationRect(RectTransform rect, bool forward, bool wide)
        {
            if (wide)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(forward ? 1 : 0, 0);
                rect.pivot = new Vector2(forward ? 1 : 0, 0);
                rect.anchoredPosition = new Vector2(forward ? -26 : 26, 10);
                rect.sizeDelta = new Vector2(78, 78);
            }
            else
            {
                rect.pivot = new Vector2(forward ? 1 : 0, 0);
                rect.anchorMin = rect.anchorMax = new Vector2(forward ? 1 : 0, 0);
                rect.anchoredPosition = new Vector2(forward ? -18 : 18, 10);
                rect.sizeDelta = new Vector2(76, 76);
            }
        }
        public void Rebuild()
        {
            if (content == null) return;
            localization = new RuntimeManagerResolver().Localization();
            shownLanguage = localization.CurrentLanguage;
            if (scroll != null) scroll.scrollSensitivity = 90;
            ui = new CreationWizardUi(font, buttonSprite, inputSprite, completeSprite, dropdownTemplateSprite, scrollTrackSprite, scrollHandleSprite, L);
            if (menu == null)
            {
                menu = ui.Button(heading.transform.parent, "Меню", ReturnToMenu, false, 68);
                menu.name = "MainMenuButton";
            }
            else
            {
                menu.onClick.RemoveAllListeners();
                menu.onClick.AddListener(ReturnToMenu);
            }
            StyleVisuals();
            EnsureScrollbar();
            Layout((float)Screen.width / Math.Max(1, Screen.height));
            CharacterCreationRules.RepairDependentChoices(draft);
            if (initialDraft == null) initialDraft = JsonUtility.ToJson(draft);
            next.onClick.RemoveAllListeners(); back.onClick.RemoveAllListeners();
            next.onClick.AddListener(Next);
            back.onClick.AddListener(() => { if (step == 0 || created) ReturnToMenu(); else ShowStep(step - 1); });
            Draw();
        }

        private string L(string source) => localization == null ? source : localization.Translate(source);
        private string LF(string source, params object[] values) => string.Format(L(source), values);

        private void StyleVisuals()
        {
            StyleIcon(menu, menuSprite);
            StyleIcon(back, backSprite);
            StyleIcon(next, nextSprite);
        }

        private void EnsureScrollbar()
        {
            if (scrollTrackSprite == null || scrollHandleSprite == null || scroll == null) return;
            var viewport = scroll.viewport;
            viewport.offsetMax = new Vector2(-30, viewport.offsetMax.y);
            var track = scrollRoot.Find("WizardScrollbar") as RectTransform;
            if (track == null) track = ui.Rect("WizardScrollbar", scrollRoot);
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(-25, 0); track.offsetMax = Vector2.zero;
            var trackImage = track.GetComponent<UnityEngine.UI.Image>();
            if (trackImage == null) trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            trackImage.sprite = scrollTrackSprite; trackImage.type = UnityEngine.UI.Image.Type.Sliced;
            trackImage.pixelsPerUnitMultiplier = 4;
            var sliding = track.Find("Sliding Area") as RectTransform;
            if (sliding == null) sliding = ui.Rect("Sliding Area", track);
            CreationWizardUi.Fill(sliding, 2, 5, 2, 5);
            var handle = sliding.Find("Handle") as RectTransform;
            if (handle == null) handle = ui.Rect("Handle", sliding);
            handle.sizeDelta = Vector2.zero;
            var handleImage = handle.GetComponent<UnityEngine.UI.Image>();
            if (handleImage == null) handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            handleImage.sprite = scrollHandleSprite; handleImage.type = UnityEngine.UI.Image.Type.Sliced;
            handleImage.pixelsPerUnitMultiplier = 1;
            var scrollbar = track.GetComponent<UnityEngine.UI.Scrollbar>();
            if (scrollbar == null) scrollbar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            scrollbar.targetGraphic = handleImage; scrollbar.handleRect = handle;
            scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 4;
        }

        private void StyleIcon(UnityEngine.UI.Button button, Sprite sprite)
        {
            if (button == null || sprite == null) return;
            var oldIcon = button.transform.Find("NavigationIcon");
            if (oldIcon != null)
            {
                oldIcon.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(oldIcon.gameObject); else DestroyImmediate(oldIcon.gameObject);
            }
            var image = button.GetComponent<UnityEngine.UI.Image>();
            bool framed = button == next && sprite == completeSprite && inputSprite != null;
            image.sprite = framed ? inputSprite : sprite;
            image.type = framed ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.pixelsPerUnitMultiplier = framed ? 4 : 1;
            image.preserveAspect = !framed;
            image.color = Color.white;
            if (framed)
            {
                var icon = ui.Rect("NavigationIcon", button.transform);
                CreationWizardUi.Fill(icon, 18, 18, 18, 18);
                var iconImage = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
                iconImage.sprite = sprite; iconImage.preserveAspect = true; iconImage.raycastTarget = false;
            }
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.gameObject.SetActive(false);
        }
        public void ShowStep(int page)
        {
            if (created) return;
            if (page > step)
            {
                for (int previous = 0; previous < page; previous++)
                {
                    string missing = StepError(previous);
                    if (missing == null) continue;
                    step = previous;
                    Draw();
                    feedback.text = L(missing);
                    return;
                }
            }
            step = Mathf.Clamp(page, 0, 5); Draw();
        }

        private string StepError(int page)
        {
            if (page == 0) return (int)draft.edition < 0 ? "Обери редакцію D&D." : null;
            if (page == 1)
            {
                if (string.IsNullOrEmpty(draft.species)) return "Обери расу або вид.";
                return string.IsNullOrEmpty(draft.background) ? "Обери передісторію." : null;
            }
            if (page == 2)
            {
                if ((int)draft.abilityMethod < 0) return "Обери спосіб визначення характеристик.";
                if (draft.abilities == null || draft.abilities.Length != 6 || draft.abilities.Any(x => x <= 0)) return "Розподіли всі шість значень характеристик.";
                if (draft.abilityMethod == AbilityMethod.StandardArray && !draft.abilities.OrderBy(x => x).SequenceEqual(new[] { 8, 10, 12, 13, 14, 15 })) return "Признач кожне число стандартного набору один раз.";
                if (draft.abilityMethod == AbilityMethod.Manual && (draft.rolledCount != 6 || draft.rolledAssignment.Any(x => x < 0))) return "Зроби шість кидків і розподіли всі результати.";
                if (draft.edition == RulesEdition.Rules2024)
                {
                    if (draft.bonusMode < 0) return "Обери спосіб розподілу бонусів передісторії.";
                    if (!draft.threeIncreases && (draft.increaseTwo < 0 || draft.increaseOne < 0)) return "Обери характеристики для бонусів +2 та +1.";
                }
                return null;
            }
            if (page == 3)
            {
                if (draft.classSkills.Count != 2) return "Обери дві навички воїна.";
                if (draft.edition == RulesEdition.Rules2024 && draft.species == "human" && draft.humanSkill < 0) return "Обери додаткову навичку людини.";
                if (string.IsNullOrEmpty(draft.language)) return "Обери мову.";
                if (draft.edition == RulesEdition.Rules2024 && string.IsNullOrEmpty(draft.secondLanguage)) return "Обери другу мову.";
                if ((draft.background == "soldier" || draft.edition == RulesEdition.Rules2014) && string.IsNullOrEmpty(draft.gamingSet)) return "Обери ігровий набір.";
                if (draft.edition == RulesEdition.Rules2014 && draft.species == "dwarf" && string.IsNullOrEmpty(draft.artisanTool)) return "Обери ремесло дварфа.";
                return null;
            }
            if (page == 4)
            {
                if ((int)draft.fightingStyle < 0) return "Обери бойовий стиль.";
                if (draft.equipment < 0) return "Обери класовий набір спорядження.";
                if (draft.edition == RulesEdition.Rules2024 && draft.backgroundEquipmentChoice < 0) return "Обери спорядження передісторії або 50 зм.";
            }
            return null;
        }
        public void SetEdition(RulesEdition edition)
        {
            draft.edition = edition; CharacterCreationRules.RepairDependentChoices(draft); Draw(false);
        }
        private void Draw(bool resetScroll = true)
        {
            float previousScroll = scroll != null ? scroll.verticalNormalizedPosition : 1;
            ui.Clear(content); ui.Clear(stepList);
            heading.text = L("Створення персонажа");
            subtitle.text = L("ВОЇН • 1 РІВЕНЬ     /     ПРОБНИЙ МАЙСТЕР D&D 2014 · 2024");
            progress.text = (step + 1).ToString("00") + " / 06   •   " + L(Steps[step]);
            feedback.text = ""; nameError = null; back.interactable = true;
            back.GetComponentInChildren<TextMeshProUGUI>(true).text = step == 0 ? L("<  Головне меню") : L("<  Назад");
            nextLabel.text = step == 5 ? L("Створити персонажа") : L("Далі  >");
            StyleIcon(next, step == 5 ? completeSprite : nextSprite);
            for (int i = 0; i < Steps.Length; i++)
            {
                int index = i;
                ui.Button(stepList, (i + 1).ToString("00") + "   " + L(Steps[i]), () => ShowStep(index), i == step, 62);
            }
            ui.Text(content, Steps[step], 32, CreationWizardUi.Gold, 50);
            switch (step)
            {
                case 0: RulesPage(); break;
                case 1: OriginPage(); break;
                case 2: AbilitiesPage(); break;
                case 3: SkillsPage(); break;
                case 4: EquipmentPage(); break;
                case 5: ReviewPage(); break;
            }
            UpdateSummary();
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = resetScroll ? 1 : previousScroll;
        }
        private void Choose(string a, string b, bool? first, Action<bool> change, float height = 94, float multiplier = 4, Transform container = null)
        {
            Transform root = container == null ? content : container;
            Transform parent = compact ? root : ui.Row("Choices", root, height);
            var left = ui.Button(parent, a, () => { change(true); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, first == true, height, true);
            var right = ui.Button(parent, b, () => { change(false); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, first == false, height, true);
            left.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
            right.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
        }
        private void Note(string text, float height = 68) => ui.Text(content, L(text), 21, new Color(.7f, .71f, .73f), height);
        private void RulesPage()
        {
            Note("Обери редакцію. Майстер застосує її бонуси походження, риси та класові ресурси.", 65);
            Choose("D&D 2014\nКласична п’ята редакція", "D&D 2024\nОновлені правила", (int)draft.edition < 0 ? null : draft.edition == RulesEdition.Rules2014,
                first => draft.edition = first ? RulesEdition.Rules2014 : RulesEdition.Rules2024, 112, 2);
            ui.Text(content, "Клас персонажа", 27, CreationWizardUi.Gold, 45);
            ui.Button(content, "ВОЇН\nЗброя, обладунки, бойовий стиль і Друге дихання", () => {}, true, 115);
            Note("Перший прототип: воїн 1 рівня. Підклас з’явиться на 3 рівні. Інші класи та рівні додаватимемо після перевірки майстра.", 100);
            Note(draft.edition == RulesEdition.Rules2024 ? "2024: бонуси характеристик дає передісторія. На 1 рівні є майстерність трьох видів зброї та 2 використання Другого дихання." : "2014: бонуси характеристик дає раса. На 1 рівні є бойовий стиль та 1 використання Другого дихання.", 115);
        }
        private void OriginPage()
        {
            if ((int)draft.edition < 0) { Note("Спочатку обери редакцію правил на першій сторінці."); return; }
            ui.Text(content, "Раса / вид", 26, CreationWizardUi.Gold, 40);
            bool modern = draft.edition == RulesEdition.Rules2024;
            Choose(modern ? "ДВАРФ\nТемнозір 120 футів • міцність • чуття каменю" : "ПАГОРБОВИЙ ДВАРФ\n+2 Статура • +1 Мудрість • +1 ОЗ", modern ? "ЛЮДИНА\nДодаткова навичка • натхнення • Міцний" : "ЛЮДИНА\n+1 до кожної характеристики", string.IsNullOrEmpty(draft.species) ? null : draft.species == "dwarf", value => draft.species = value ? "dwarf" : "human", 125);
            if (modern && draft.species == "human") Note("Для першого тесту людині дається рекомендована тут риса Міцний (+2 ОЗ за рівень). Інші риси походження додамо окремо.", 90);
            ui.Text(content, "Передісторія", 26, CreationWizardUi.Gold, 40);
            Choose(modern ? "СОЛДАТ\nАтлетика • Залякування\nЛютий нападник • Сил / Спр / Стат" : "СОЛДАТ\nАтлетика • Залякування\nВійськове звання", modern ? "ЗЛОЧИНЕЦЬ\nСпритність рук • Скритність\nПильний • Спр / Стат / Інт" : "ЗЛОЧИНЕЦЬ\nОбман • Скритність\nКримінальний зв’язок", string.IsNullOrEmpty(draft.background) ? null : draft.background == "soldier", value => draft.background = value ? "soldier" : "criminal", 135);
            Note("Отримані навички не можна повторно вибрати від класу. Зміна походження прибере несумісні варіанти.", 80);
        }
        private void AbilitiesPage()
        {
            Transform row = compact ? content : ui.Row("Method", content, 70);
            string[] names = { "Стандартний набір", "27 очок", "Кубики" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                ui.Button(row, names[i], () => {
                    if ((int)draft.abilityMethod == index) return;
                    draft.abilityMethod = (AbilityMethod)index;
                    draft.abilities = index == 1 ? new[] { 8, 8, 8, 8, 8, 8 } : new int[6];
                    if (index == 2) PrepareRolls();
                    Draw(false);
                }, (int)draft.abilityMethod == i, showCheck: true);
            }
            if ((int)draft.abilityMethod < 0) { Note("Обери спосіб визначення характеристик."); return; }
            if (draft.abilityMethod == AbilityMethod.StandardArray)
            {
                var remaining = new[] { 15, 14, 13, 12, 10, 8 }.Where(x => !draft.abilities.Contains(x)).ToArray();
                Counter(remaining.Length == 0 ? "УСІ ЧИСЛА РОЗПОДІЛЕНО" : LF("ДОСТУПНІ: {0}", string.Join(" • ", remaining)));
                Note("Обери число для кожної характеристики. Щоб переставити використане число, спочатку звільни його в попередньому списку.", 68);
            }
            else if (draft.abilityMethod == AbilityMethod.PointBuy)
            {
                Counter(LF("ЗАЛИШИЛОСЯ ОЧОК: {0} / 27", 27 - draft.abilities.Sum(CharacterCreationRules.PointCost)));
                Note("Купуєш базове значення від 8 до 15. Бонус походження додається після купівлі, тому підсумок може бути 16 або 17.", 70);
            }
            else
            {
                var free = Enumerable.Range(0, draft.rolledCount).Where(x => !draft.rolledAssignment.Contains(x))
                    .Select(x => draft.rolledScores[x].ToString()).ToArray();
                Counter(draft.rolledCount == 0 ? "ДОСТУПНІ: —" : free.Length == 0 ? "УСІ КИДКИ РОЗПОДІЛЕНО" : LF("ДОСТУПНІ: {0}", string.Join(" • ", free)));
                var rollAction = ui.Row("RollAction", content, 52);
                rollAction.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                bool allRolled = draft.rolledCount >= 6;
                var action = ui.Button(rollAction, allRolled ? "Скинути кидки" : LF("Кинути 4к6 • кинуто {0}/6", draft.rolledCount),
                    () => { if (allRolled) PrepareRolls(); else RollNext(); Draw(false); }, height: 52);
                action.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 260;
                action.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
            }
            Transform abilityHost = content;
            if (draft.abilityMethod == AbilityMethod.Manual)
            {
                if (compact) DrawRollResults(content);
                else
                {
                    var group = ui.Row("AbilitiesAndRolls", content, 432);
                    group.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                    var abilities = ui.Column("AbilityRows", group);
                    CreationWizardUi.Height(abilities, 432);
                    abilities.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
                    abilityHost = abilities;
                    var results = ui.Column("RollResults", group, 3);
                    CreationWizardUi.Height(results, 432);
                    var resultLayout = results.GetComponent<UnityEngine.UI.LayoutElement>();
                    resultLayout.preferredWidth = 250; resultLayout.flexibleWidth = 0;
                    DrawRollResults(results);
                }
            }
            int[] scores = CharacterCreationRules.Scores(draft);
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                var stat = ui.Row("Ability" + i, abilityHost, 62);
                stat.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                var label = ui.Text(stat, CharacterCreationRules.AbilityNames[i], 22, new Color(1f, .604f, 0f), 62);
                label.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 130 : 170;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                if (draft.abilityMethod != AbilityMethod.PointBuy)
                {
                    bool standard = draft.abilityMethod == AbilityMethod.StandardArray;
                    int current = standard ? draft.abilities[i] : draft.rolledAssignment[i];
                    int[] available = standard
                        ? new[] { 15, 14, 13, 12, 10, 8 }.Where(x => x == current || !draft.abilities.Contains(x)).ToArray()
                        : Enumerable.Range(0, draft.rolledCount).Where(x => x == current || !draft.rolledAssignment.Contains(x)).ToArray();
                    var labels = new[] { "—" }.Concat(available.Select(x => standard ? x.ToString() : draft.rolledScores[x].ToString())).ToArray();
                    int[] values = available;
                    var dropdown = ui.Dropdown(stat, CharacterCreationRules.AbilityNames[i], labels, Array.IndexOf(values, current) + 1,
                        choice => { if (standard) draft.abilities[index] = choice == 0 ? 0 : values[choice - 1];
                                    else { draft.rolledAssignment[index] = choice == 0 ? -1 : values[choice - 1]; draft.abilities[index] = choice == 0 ? 0 : draft.rolledScores[values[choice - 1]]; }
                                    Draw(false); }, compact ? 90 : 105, 42);
                }
                else
                {
                    var minus = ui.Button(stat, "−", () => ChangeAbility(index, -1), height: 62);
                    minus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 65 : 95;
                    minus.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
                    StyleAdjustButton(minus);
                }
                var value = ui.Text(stat, AbilityDisplay(i, scores[i]), 23, null, 62);
                value.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = draft.abilityMethod == AbilityMethod.PointBuy ? (compact ? 155 : 200) : 170;
                value.alignment = TextAlignmentOptions.Center;
                if (draft.abilityMethod == AbilityMethod.PointBuy)
                {
                    var plus = ui.Button(stat, "+", () => ChangeAbility(index, 1), height: 62);
                    plus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 65 : 95;
                    plus.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
                    StyleAdjustButton(plus);
                }
            }
            if (draft.abilityMethod == AbilityMethod.StandardArray)
            {
                ui.Text(content, "Розподіл чисел", 22, CreationWizardUi.Gold, 34);
                var resetRow = ui.Row("ResetDistributionRow", content, 48);
                resetRow.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                var reset = ui.Button(resetRow, "Скинути розподіл", () => { draft.abilities = new int[6]; Draw(false); }, height: 48);
                var resetLayout = reset.GetComponent<UnityEngine.UI.LayoutElement>();
                resetLayout.preferredWidth = 190;
                resetLayout.flexibleWidth = 0;
            }
            if (draft.edition == RulesEdition.Rules2024)
            {
                ui.Text(content, "Бонуси передісторії", 25, CreationWizardUi.Gold, 44);
                Note("Бонуси можна дати лише трьом характеристикам передісторії.", 55);
                Choose("+2 та +1", "+1 до кожної з трьох", draft.bonusMode < 0 ? null : !draft.threeIncreases, value => { draft.bonusMode = value ? 0 : 1; draft.threeIncreases = !value; }, 62);
                if (draft.threeIncreases) Note(LF("+1 до кожної: {0}", string.Join(", ", CharacterCreationRules.AllowedIncreases(draft).Select(x => L(CharacterCreationRules.AbilityNames[x])))), 65);
                else
                {
                    DropdownInt("+2 до характеристики", CharacterCreationRules.AllowedIncreases(draft), draft.increaseTwo,
                        value => { draft.increaseTwo = value; CharacterCreationRules.RepairDependentChoices(draft); }, x => CharacterCreationRules.AbilityNames[x]);
                    DropdownInt("+1 до характеристики", CharacterCreationRules.AllowedIncreases(draft).Where(x => x != draft.increaseTwo).ToArray(), draft.increaseOne,
                        value => draft.increaseOne = value, x => CharacterCreationRules.AbilityNames[x]);
                }
            }
            else Note(draft.species == "human" ? "Бонус раси: +1 до всіх шести характеристик." : "Бонус раси: +2 Статура, +1 Мудрість.", 70);
        }
        private void DrawRollResults(Transform parent)
        {
            for (int i = 0; i < draft.rolledCount; i++)
            {
                int[] dice = draft.rolledDice.Skip(i * 4).Take(4).ToArray();
                int dropped = Array.IndexOf(dice, dice.Min());
                string shown = string.Join(" + ", dice.Select((die, index) =>
                    index == dropped ? "<color=#B9A99A>(-" + die + ")</color>" : "<color=#FF9A00>" + die + "</color>"));
                ui.Text(parent, LF("Кидок {0}: {1} = <b><color=#FF9A00>{2}</color></b>", i + 1, shown, draft.rolledScores[i]), 18, null, 31);
            }
        }
        private string AbilityDisplay(int index, int finalScore)
        {
            int baseScore = draft.abilities[index];
            if (baseScore <= 0) return "—";
            string baseText = "<color=#D5D0C5>" + baseScore + "</color>";
            string value = baseScore == finalScore ? baseText : baseText + "→<color=#FF9A00>" + finalScore + "</color>";
            return value + " (" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(finalScore)) + ")";
        }
        private static void StyleAdjustButton(UnityEngine.UI.Button button)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "<b><color=#FF9A00>" + text.text + "</color></b>";
            text.fontSize = 50;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            CreationWizardUi.Fill(text.rectTransform, 2, 0, 2, 0);
        }
        private void ChangeAbility(int index, int delta)
        {
            if (draft.abilityMethod == AbilityMethod.PointBuy)
            {
                int old = draft.abilities[index];
                draft.abilities[index] = Mathf.Clamp(old + delta, draft.abilityMethod == AbilityMethod.PointBuy ? 8 : 3, draft.abilityMethod == AbilityMethod.PointBuy ? 15 : 18);
                if (draft.abilityMethod == AbilityMethod.PointBuy && draft.abilities.Sum(CharacterCreationRules.PointCost) > 27) draft.abilities[index] = old;
            }
            Draw(false);
        }
        private void PrepareRolls()
        {
            draft.rolledScores = new int[6];
            draft.rolledDice = new int[24];
            draft.rolledCount = 0;
            draft.rolledAssignment = new[] { -1, -1, -1, -1, -1, -1 };
            draft.abilities = new int[6];
        }
        private void RollNext()
        {
            if (draft.rolledScores == null || draft.rolledScores.Length != 6 || draft.rolledDice == null || draft.rolledDice.Length != 24) PrepareRolls();
            if (draft.rolledCount >= 6) return;
            int[] dice = Enumerable.Range(0, 4).Select(_ => UnityEngine.Random.Range(1, 7)).ToArray();
            Array.Copy(dice, 0, draft.rolledDice, draft.rolledCount * 4, 4);
            draft.rolledScores[draft.rolledCount] = dice.Sum() - dice.Min();
            draft.rolledCount++;
        }
        private void Counter(string label)
        {
            var row = ui.Panel("AbilityCounter", content, Color.white);
            CreationWizardUi.Height(row, 65);
            var layout = row.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredWidth = 390; layout.flexibleWidth = 1;
            var image = row.GetComponent<UnityEngine.UI.Image>();
            if (counterSprite != null) { image.sprite = counterSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 3; }
            else image.color = new Color(.12f, .09f, .13f, .9f);
            var text = ui.Text(row, label, 24, new Color(1f, .604f, 0f));
            CreationWizardUi.Fill(text.rectTransform, 14, 5, 14, 5);
            text.alignment = TextAlignmentOptions.Center; text.fontStyle = FontStyles.Bold;
        }
        private void SkillsPage()
        {
            if ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.species) || string.IsNullOrEmpty(draft.background))
            { Note("Спочатку обери редакцію, расу й передісторію."); return; }
            int[] bg = CharacterCreationRules.BackgroundSkills(draft);
            Note(LF("Від передісторії: {0}. Обери ще 2 від воїна: <b><color=#FF9A00>{1}/2</color></b>. {2}",
                string.Join(", ", bg.Select(x => L(CharacterCreationRules.SkillNames[x]))), draft.classSkills.Count,
                draft.classSkills.Count == 2 ? L("Щоб замінити навичку, спочатку зніми один вибір.") : ""), 88);
            var options = CharacterCreationRules.FighterSkills.Where(x => !bg.Contains(x)).ToArray();
            for (int i = 0; i < options.Length; i += 2)
            {
                var row = ui.Row("SkillChoices", content, 65);
                foreach (int skill in options.Skip(i).Take(2))
                {
                    int selected = skill;
                    var button = ui.Button(row, CharacterCreationRules.SkillNames[skill], () => {
                        if (draft.classSkills.Contains(selected)) draft.classSkills.Remove(selected);
                        else if (draft.classSkills.Count < 2) draft.classSkills.Add(selected);
                        CharacterCreationRules.RepairDependentChoices(draft); Draw(false);
                    }, draft.classSkills.Contains(skill), showCheck: true);
                    if (!draft.classSkills.Contains(skill) && draft.classSkills.Count == 2) button.interactable = false;
                }
            }
            if (draft.species == "human" && draft.edition == RulesEdition.Rules2024)
            {
                ui.Text(content, "Додаткова навичка людини", 25, CreationWizardUi.Gold, 44);
                DropdownInt("Навичка людини", Enumerable.Range(0, 18).Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x)).ToArray(),
                    draft.humanSkill, value => draft.humanSkill = value, x => CharacterCreationRules.SkillNames[x], 300);
            }
            ui.Text(content, "Мови й інструменти", 26, CreationWizardUi.Gold, 45);
            if (draft.edition == RulesEdition.Rules2014 && draft.species == "dwarf") Note("Мови: Загальна та Дварфська.", 45);
            else
            {
                string[] languages = { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" };
                DropdownText("Мова", languages.Where(x => draft.edition == RulesEdition.Rules2014 || x != draft.secondLanguage).ToArray(), draft.language, value => draft.language = value);
                if (draft.edition == RulesEdition.Rules2024) DropdownText("Друга мова", languages.Where(x => x != draft.language).ToArray(), draft.secondLanguage, value => draft.secondLanguage = value);
            }
            if (draft.background == "soldier" || draft.edition == RulesEdition.Rules2014) DropdownText("Ігровий набір", new[] { "Гральні кості", "Карти", "Шахи" }, draft.gamingSet, value => draft.gamingSet = value);
            if (draft.species == "dwarf" && draft.edition == RulesEdition.Rules2014) DropdownText("Ремесло дварфа", new[] { "Інструменти коваля", "Приладдя пивовара", "Інструменти каменяра" }, draft.artisanTool, value => draft.artisanTool = value);
        }
        private void DropdownInt(string label, int[] values, int value, Action<int> change, Func<int, string> display, float width = 160)
        {
            ui.Text(content, label, 22, CreationWizardUi.Gold, 36);
            var choices = new[] { "—" }.Concat(values.Select(display)).ToArray();
            ui.Dropdown(content, label, choices, Array.IndexOf(values, value) + 1,
                choice => { change(choice == 0 ? -1 : values[choice - 1]); Draw(false); }, width, 42);
        }
        private void DropdownText(string label, string[] values, string value, Action<string> change)
        {
            ui.Text(content, label, 22, CreationWizardUi.Gold, 36);
            var choices = new[] { "—" }.Concat(values).ToArray();
            ui.Dropdown(content, label, choices, Array.IndexOf(values, value) + 1,
                choice => { change(choice == 0 ? "" : values[choice - 1]); Draw(false); });
        }
        private static string HighlightChoice(string label)
        {
            int colon = label.IndexOf(':');
            return colon < 0 ? label : label.Substring(0, colon + 1) + " <size=115%><color=#FF9A00>" + label.Substring(colon + 1).Trim() + "</color></size>";
        }
        private void EquipmentPage()
        {
            if ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.background))
            { Note("Спочатку обери редакцію та передісторію."); return; }
            ui.Text(content, "Бойовий стиль", 25, CreationWizardUi.Gold, 40);
            var styleFrame = FramedChoiceGroup("FightingStyleFrame", compact ? 330 : 176);
            string[] styles = { "Захист • +1 КО", "Дуелі • +2 шкоди", "Стрільба • +2 до атаки", "Велика зброя" };
            for (int i = 0; i < 4; i += 2)
            {
                var row = ui.Row("Styles", styleFrame, 65);
                for (int j = i; j < i + 2; j++) { int index = j; ui.Button(row, styles[j], () => { draft.fightingStyle = (FighterStyle)index; Draw(false); }, (int)draft.fightingStyle == j, showCheck: true); }
            }
            ui.Text(content, "Набір спорядження", 25, CreationWizardUi.Gold, 42);
            var kitFrame = FramedChoiceGroup("EquipmentKitFrame", compact ? 268 : 150);
            Choose(draft.edition == RulesEdition.Rules2024 ? "НАБІР A\nКольчуга • дворучний меч • ціп • дротики" : "БЛИЖНІЙ БІЙ\nКольчуга • меч і щит • 2 сокири", draft.edition == RulesEdition.Rules2024 ? "НАБІР B\nШкіра з клепками • мечі • довгий лук" : "ЛУЧНИК\nШкіра • довгий лук • меч і щит", draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            if (draft.edition == RulesEdition.Rules2024)
            {
                ui.Text(content, "Спорядження передісторії", 25, CreationWizardUi.Gold, 42);
                string pack = draft.background == "soldier" ? "СПОРЯДЖЕННЯ\nСпис • короткий лук • 20 стріл • ігровий набір • 14 зм" : "СПОРЯДЖЕННЯ\n2 кинджали • злодійські інструменти • ломик • 16 зм";
                Choose(pack, "ЗОЛОТО\n50 зм замість спорядження передісторії", draft.backgroundEquipmentChoice < 0 ? null : draft.backgroundEquipmentChoice == 0,
                    value => draft.backgroundEquipmentChoice = value ? 0 : 1, 96);
            }
            Note(draft.equipment < 0 ? "Обери набір спорядження." : CharacterCreationPresentation.Equipment(draft, L), 150);
            Note("КО враховує надягнений набір: у лучника щит лежить у спорядженні. Стиль діє лише за своїх умов. Перелік речей буде записаний у картку; окремі комірки інвентарю — наступний етап.", 110);
            if (draft.fightingStyle == FighterStyle.Archery && draft.equipment == 0 ||
                draft.fightingStyle == FighterStyle.GreatWeapon && draft.equipment == 1 ||
                draft.fightingStyle == FighterStyle.Dueling && draft.edition == RulesEdition.Rules2024 && draft.equipment == 0)
                Note("Увага: у вибраному наборі немає зброї, з якою цей бойовий стиль діятиме одразу. Стиль залишиться доступним для іншої зброї.", 88);
        }
        private RectTransform FramedChoiceGroup(string name, float height)
        {
            var frame = ui.Panel(name, content, Color.clear);
            CreationWizardUi.Height(frame, height);
            var sizing = frame.GetComponent<UnityEngine.UI.LayoutElement>();
            sizing.preferredWidth = 1040; sizing.flexibleWidth = 1;
            var image = frame.GetComponent<UnityEngine.UI.Image>();
            if (panelFrameSprite != null) { image.sprite = panelFrameSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 4; image.color = Color.white; }
            var vertical = frame.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 12, 12); vertical.spacing = 8;
            vertical.childControlWidth = true; vertical.childControlHeight = true;
            return frame;
        }
        private void ReviewPage()
        {
            ui.Text(content, "Як звати твого персонажа?", 26, CreationWizardUi.Gold, 42);
            ui.Input(content, draft.characterName, "Введи ім’я…", value => { draft.characterName = value; if (nameError != null && !string.IsNullOrWhiteSpace(value)) nameError.text = ""; UpdateSummary(); });
            nameError = ui.Text(content, "", 18, new Color(1f, .5f, .38f), 28);
            DropdownText("Світогляд", new[] { "Нейтральний", "Законно-добрий", "Нейтрально-добрий", "Хаотично-добрий", "Законно-нейтральний", "Хаотично-нейтральний", "Законно-злий", "Нейтрально-злий", "Хаотично-злий" }, draft.alignment, value => draft.alignment = value);
            if ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.species) || string.IsNullOrEmpty(draft.background) || draft.abilities.Any(x => x <= 0) || draft.equipment < 0 || (int)draft.fightingStyle < 0)
            { Note("Спершу заверши вибір на попередніх сторінках, щоб побачити підсумок."); return; }
            var s = CharacterCreationRules.Calculate(draft);
            Note(L(s.speciesName) + " • " + L(s.backgroundName) + "\n" + LF("Воїн • рівень 1 • D&D {0}", draft.edition == RulesEdition.Rules2024 ? "2024" : "2014"), 66);
            ui.Text(content, LF("ОЗ {0}     КО {1}     Ініціатива {2}     Швидкість {3}", s.hitPoints, s.armorClass, CharacterCreationRules.Signed(s.initiative), s.speed), 25, CreationWizardUi.Gold, 60);
            ui.Text(content, HighlightSummaryLabels(CharacterCreationPresentation.FeatureLines(s.features, L)), 21, null, 345);
            ui.Text(content, HighlightSummaryLabels(CharacterCreationPresentation.Proficiencies(draft, s, L)), 21, null, 190);
            Note("Натисни «Створити персонажа», щоб додати незалежного персонажа до твого списку. До цього моменту всі зміни існують лише в цій чернетці.", 100);
        }
        private static string HighlightSummaryLabels(string text)
        {
            return string.Join("\n", text.Split('\n').Select(line => {
                int colon = line.IndexOf(':');
                return colon < 0 ? line : "<b><color=#FF9A00>" + line.Substring(0, colon + 1) + "</color></b>" + line.Substring(colon + 1);
            }));
        }
        private void UpdateSummary()
        {
            if ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.species) || string.IsNullOrEmpty(draft.background) || draft.abilities == null || draft.abilities.Any(x => x <= 0) || draft.equipment < 0 || (int)draft.fightingStyle < 0)
            {
                summary.text = "<color=#E8AF58>" + L("ТВІЙ ПЕРСОНАЖ") + "</color>\n\n" + L("Розподіли всі шість значень характеристик, щоб побачити підсумок.");
                return;
            }
            var s = CharacterCreationRules.Calculate(draft);
            summary.text = "<color=#E8AF58>" + L("ТВІЙ ПЕРСОНАЖ") + "</color>\n\n<color=#FF9A00>" + (string.IsNullOrWhiteSpace(draft.characterName) ? L("Новий герой") : draft.characterName) + "</color>\n" + L("Воїн • рівень 1") + "\nD&D " + (draft.edition == RulesEdition.Rules2024 ? "2024" : "2014") + "\n\n" + L(s.speciesName) + "\n" + L(s.backgroundName) + "\n\n<color=#E8AF58>" + s.hitPoints + " " + L("ОЗ") + "     " + s.armorClass + " " + L("КО") + "</color>\n" + L("Ініціатива") + " " + CharacterCreationRules.Signed(s.initiative) + "\n" + L("Швидкість") + " " + s.speed + " " + L("футів") + "\n" + L("Майстерність +2") + "\n\n" + string.Join("\n", Enumerable.Range(0, 6).Select(i => "<color=#FF9A00>" + L(CharacterCreationRules.AbilityNames[i]) + "</color>  " + s.scores[i] + " (<color=#FF9A00>" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i])) + "</color>)"));
        }
        private void Next()
        {
            if (created) { SceneManager.LoadScene(AppConfig.Scenes.CharacterSheet); return; }
            var errors = CharacterCreationRules.Validate(draft, step == 5);
            string pageError = StepError(step);
            if (step == 5) pageError = errors.FirstOrDefault();
            if (pageError != null)
            {
                if (step == 5 && pageError.Contains("ім’я") && nameError != null) nameError.text = L(pageError);
                else feedback.text = L(pageError);
                return;
            }
            if (step < 5) { ShowStep(step + 1); return; }
            next.interactable = false;
            if (new CharacterCreationService(bindings).TryCreate(draft, out string error, out createdId))
            {
                created = true; back.interactable = false; ui.Clear(stepList);
                feedback.text = "";
                ui.Clear(content); ui.Text(content, "Персонажа створено", 36, CreationWizardUi.Gold, 70);
                Note(LF("{0} вже у твоєму списку. Можна відкрити його звичайну картку.", draft.characterName), 120);
                nextLabel.text = L("Відкрити картку");
                StyleIcon(next, createdSprite);
            }
            else feedback.text = LF("Не вдалося зберегти: {0}", L(error));
            next.interactable = true;
        }
        private void ReturnToMenu()
        {
            if (!created && initialDraft != JsonUtility.ToJson(draft))
            {
                new TaruckDialogPresenter(() => new RuntimeManagerResolver().Localization().CurrentLanguage)
                    .ShowCompact(L("Повернутися до головного меню?"), L("Чернетка персонажа ще не збережена. Вийти з майстра?"), L("Вийти"), () => SceneManager.LoadScene(AppConfig.Scenes.Menu));
                return;
            }
            SceneManager.LoadScene(AppConfig.Scenes.Menu);
        }
    }
}
