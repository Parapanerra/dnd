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
        private static readonly string[] Steps = { "Правила і клас", "Походження", "Характеристики", "Навички й мови", "Спорядження", "Персонаж готовий" };
        public CharacterCreationDraft Draft => draft;
        public int CurrentStep => step;

        public void ConfigureVisuals(Sprite input, Sprite menuIcon, Sprite backIcon, Sprite nextIcon, Sprite completeIcon, Sprite createdIcon, Sprite panel,
            Sprite panelFrame = null, Sprite panelBackground = null, Sprite scrollTrack = null, Sprite scrollHandle = null, Sprite counter = null)
        {
            inputSprite = input; menuSprite = menuIcon;
            backSprite = backIcon; nextSprite = nextIcon; completeSprite = completeIcon; createdSprite = createdIcon; panelSprite = panel;
            panelFrameSprite = panelFrame; panelBackgroundSprite = panelBackground;
            scrollTrackSprite = scrollTrack; scrollHandleSprite = scrollHandle;
            counterSprite = counter;
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
            CreationWizardUi.Fill(scrollRoot, wide ? 235 : 14, 155, wide ? 330 : 14, 155);
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
            ui = new CreationWizardUi(font, buttonSprite, inputSprite);
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

        private void StyleVisuals()
        {
            if (panelFrameSprite != null && panelBackgroundSprite != null)
            {
                var shell = sidebar.parent.GetComponent<UnityEngine.UI.Image>();
                foreach (var panel in new[] { shell, sidebar.GetComponent<UnityEngine.UI.Image>(), summaryPanel.GetComponent<UnityEngine.UI.Image>() })
                {
                    panel.color = Color.clear;
                    StylePanelLayers(panel);
                }
                var outline = shell.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) outline.enabled = false;
            }
            else if (panelSprite != null)
            {
                var shell = sidebar.parent.GetComponent<UnityEngine.UI.Image>();
                foreach (var panel in new[] { shell, sidebar.GetComponent<UnityEngine.UI.Image>(), summaryPanel.GetComponent<UnityEngine.UI.Image>() })
                {
                    panel.sprite = panelSprite;
                    panel.type = UnityEngine.UI.Image.Type.Sliced;
                    panel.pixelsPerUnitMultiplier = 4;
                    panel.color = Color.white;
                }
                var outline = shell.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) outline.enabled = false;
            }
            StyleIcon(menu, menuSprite);
            StyleIcon(back, backSprite);
            StyleIcon(next, nextSprite);
        }

        private void StylePanelLayers(UnityEngine.UI.Image panel)
        {
            var background = panel.transform.Find("WizardBackground") as RectTransform;
            if (background == null) background = ui.Rect("WizardBackground", panel.transform);
            CreationWizardUi.Fill(background);
            background.SetAsFirstSibling();
            var bg = background.GetComponent<UnityEngine.UI.Image>();
            if (bg == null) bg = background.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = panelBackgroundSprite; bg.type = UnityEngine.UI.Image.Type.Simple; bg.color = Color.white; bg.raycastTarget = false;
            var frame = panel.transform.Find("WizardFrame") as RectTransform;
            if (frame == null) frame = ui.Rect("WizardFrame", panel.transform);
            CreationWizardUi.Fill(frame);
            frame.SetAsLastSibling();
            var border = frame.GetComponent<UnityEngine.UI.Image>();
            if (border == null) border = frame.gameObject.AddComponent<UnityEngine.UI.Image>();
            border.sprite = panelFrameSprite; border.type = UnityEngine.UI.Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = 4; border.color = Color.white; border.raycastTarget = false;
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
            step = Mathf.Clamp(page, 0, 5); Draw();
        }
        public void SetEdition(RulesEdition edition)
        {
            draft.edition = edition; CharacterCreationRules.RepairDependentChoices(draft); Draw(false);
        }
        private void Draw(bool resetScroll = true)
        {
            float previousScroll = scroll != null ? scroll.verticalNormalizedPosition : 1;
            ui.Clear(content); ui.Clear(stepList);
            heading.text = "Створення персонажа";
            subtitle.text = "ВОЇН • 1 РІВЕНЬ     /     ПРОБНИЙ МАЙСТЕР D&D 2014 · 2024";
            progress.text = (step + 1).ToString("00") + " / 06   •   " + Steps[step];
            feedback.text = ""; back.interactable = true;
            back.GetComponentInChildren<TextMeshProUGUI>(true).text = step == 0 ? "<  Головне меню" : "<  Назад";
            nextLabel.text = step == 5 ? "Створити персонажа" : "Далі  >";
            StyleIcon(next, step == 5 ? completeSprite : nextSprite);
            for (int i = 0; i < Steps.Length; i++)
            {
                int index = i;
                ui.Button(stepList, (i + 1).ToString("00") + "   " + Steps[i], () => ShowStep(index), i == step, 62);
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
        private void Choose(string a, string b, bool first, Action<bool> change, float height = 94, float multiplier = 4, Transform container = null)
        {
            Transform root = container == null ? content : container;
            Transform parent = compact ? root : ui.Row("Choices", root, height);
            var left = ui.Button(parent, a, () => { change(true); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, first, height);
            var right = ui.Button(parent, b, () => { change(false); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, !first, height);
            left.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
            right.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
        }
        private void Note(string text, float height = 68) => ui.Text(content, text, 21, new Color(.7f, .71f, .73f), height);
        private void RulesPage()
        {
            Note("Обери редакцію. Майстер застосує її бонуси походження, риси та класові ресурси.", 65);
            Choose("D&D 2014\nКласична п’ята редакція", "D&D 2024\nОновлені правила", draft.edition == RulesEdition.Rules2014,
                first => draft.edition = first ? RulesEdition.Rules2014 : RulesEdition.Rules2024, 112, 2);
            ui.Text(content, "Клас персонажа", 27, CreationWizardUi.Gold, 45);
            ui.Button(content, "ВОЇН\nЗброя, обладунки, бойовий стиль і Друге дихання", () => {}, true, 115);
            Note("Перший прототип: воїн 1 рівня. Підклас з’явиться на 3 рівні. Інші класи та рівні додаватимемо після перевірки майстра.", 100);
            Note(draft.edition == RulesEdition.Rules2024 ? "2024: бонуси характеристик дає передісторія. На 1 рівні є майстерність трьох видів зброї та 2 використання Другого дихання." : "2014: бонуси характеристик дає раса. На 1 рівні є бойовий стиль та 1 використання Другого дихання.", 115);
        }
        private void OriginPage()
        {
            ui.Text(content, "Раса / вид", 26, CreationWizardUi.Gold, 40);
            bool modern = draft.edition == RulesEdition.Rules2024;
            Choose(modern ? "ДВАРФ\nТемнозір 120 футів • міцність • чуття каменю" : "ПАГОРБОВИЙ ДВАРФ\n+2 Статура • +1 Мудрість • +1 ОЗ", modern ? "ЛЮДИНА\nДодаткова навичка • натхнення • Міцний" : "ЛЮДИНА\n+1 до кожної характеристики", draft.species == "dwarf", value => draft.species = value ? "dwarf" : "human", 125);
            if (modern && draft.species == "human") Note("Для першого тесту людині дається рекомендована тут риса Міцний (+2 ОЗ за рівень). Інші риси походження додамо окремо.", 90);
            ui.Text(content, "Передісторія", 26, CreationWizardUi.Gold, 40);
            Choose("СОЛДАТ\nАтлетика • Залякування" + (modern ? "\nЛютий нападник • Сил / Спр / Стат" : "\nВійськове звання"), "ЗЛОЧИНЕЦЬ\n" + (modern ? "Спритність рук • Скритність\nПильний • Спр / Стат / Інт" : "Обман • Скритність\nКримінальний зв’язок"), draft.background == "soldier", value => draft.background = value ? "soldier" : "criminal", 135);
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
                    draft.abilities = index == 1 ? new[] { 15, 13, 14, 8, 12, 10 } : index == 0 ? new[] { 15, 13, 14, 8, 12, 10 } : new[] { 10, 10, 10, 10, 10, 10 };
                    if (index == 2 && (draft.rolledScores == null || draft.rolledScores.Length != 6)) RollAbilities(false);
                    Draw(false);
                }, (int)draft.abilityMethod == i);
            }
            if (draft.abilityMethod == AbilityMethod.StandardArray)
                Note("Кнопки − / + міняють місцями числа стандартного набору 15, 14, 13, 12, 10, 8. Нові очки не додаються.", 70);
            else if (draft.abilityMethod == AbilityMethod.PointBuy)
                Counter("ЗАЛИШИЛОСЯ ОЧОК: " + (27 - draft.abilities.Sum(CharacterCreationRules.PointCost)) + " / 27");
            else
            {
                ui.Button(content, "Кинути 4к6 шість разів (найменший кубик відкидається)", () => { RollAbilities(true); Draw(false); });
                Counter("НЕРОЗПОДІЛЕНО: " + (6 - (draft.rolledAssignment == null ? 0 : draft.rolledAssignment.Count(x => x >= 0))) + " / 6");
                Note("Випало: " + string.Join(" • ", draft.rolledScores.Select(x => x.ToString())) + ". Кнопками − / + признач кожне число одній характеристиці.", 76);
            }
            int[] scores = CharacterCreationRules.Scores(draft);
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                var stat = ui.Row("Ability" + i, content, 62);
                var label = ui.Text(stat, CharacterCreationRules.AbilityNames[i], 22, null, 62);
                label.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 130 : 170;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                var minus = ui.Button(stat, "<b><color=#FF9A00>−</color></b>", () => ChangeAbility(index, -1));
                minus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 52;
                minus.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
                var value = ui.Text(stat, draft.abilityMethod == AbilityMethod.Manual && draft.rolledAssignment[i] < 0 ? "—" :
                    draft.abilities[i] + " > <color=#FF9A00>" + scores[i] + "</color> (" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i])) + ")", 23, null, 62);
                value.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 170;
                value.alignment = TextAlignmentOptions.Center;
                var plus = ui.Button(stat, "<b><color=#FF9A00>+</color></b>", () => ChangeAbility(index, 1));
                plus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 52;
                plus.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
            }
            if (draft.edition == RulesEdition.Rules2024)
            {
                ui.Text(content, "Бонуси передісторії", 25, CreationWizardUi.Gold, 44);
                Note("Обери, які характеристики отримають бонуси. Натисни +2 або +1, щоб змінити вибір; підсумкові числа вище оновляться.", 82);
                Choose("+2 та +1", "+1 до трьох", !draft.threeIncreases, value => draft.threeIncreases = !value, 62);
                if (draft.threeIncreases) Note("+1 до трьох характеристик: " + string.Join(", ", CharacterCreationRules.AllowedIncreases(draft).Select(x => CharacterCreationRules.AbilityNames[x])), 65);
                else
                {
                    Cycle("+2: " + CharacterCreationRules.AbilityNames[draft.increaseTwo], CharacterCreationRules.AllowedIncreases(draft), draft.increaseTwo, value => { draft.increaseTwo = value; CharacterCreationRules.RepairDependentChoices(draft); });
                    Cycle("+1: " + CharacterCreationRules.AbilityNames[draft.increaseOne], CharacterCreationRules.AllowedIncreases(draft).Where(x => x != draft.increaseTwo).ToArray(), draft.increaseOne, value => draft.increaseOne = value);
                }
            }
            else Note(draft.species == "human" ? "Бонус раси: +1 до всіх шести характеристик." : "Бонус раси: +2 Статура, +1 Мудрість.", 70);
        }
        private void ChangeAbility(int index, int delta)
        {
            if (draft.abilityMethod == AbilityMethod.StandardArray)
            {
                int[] order = { 8, 10, 12, 13, 14, 15 };
                int value = order[(Array.IndexOf(order, draft.abilities[index]) + delta + 6) % 6];
                int other = Array.IndexOf(draft.abilities, value);
                draft.abilities[other] = draft.abilities[index]; draft.abilities[index] = value;
            }
            else if (draft.abilityMethod == AbilityMethod.Manual)
            {
                int[] available = Enumerable.Range(0, 6).Where(x => !draft.rolledAssignment.Contains(x) || x == draft.rolledAssignment[index]).ToArray();
                if (available.Length == 1 && draft.rolledAssignment.All(x => x >= 0)) available = Enumerable.Range(0, 6).ToArray();
                if (available.Length == 0) return;
                int oldIndex = draft.rolledAssignment[index];
                int current = Array.IndexOf(available, oldIndex);
                int nextIndex = delta > 0 ? (current + 1) % available.Length : (current < 0 ? available.Length - 1 : (current - 1 + available.Length) % available.Length);
                int chosen = available[nextIndex];
                int occupant = Array.IndexOf(draft.rolledAssignment, chosen);
                if (occupant >= 0 && occupant != index)
                {
                    draft.rolledAssignment[occupant] = oldIndex;
                    draft.abilities[occupant] = oldIndex >= 0 ? draft.rolledScores[oldIndex] : 10;
                }
                draft.rolledAssignment[index] = chosen;
                draft.abilities[index] = draft.rolledScores[chosen];
            }
            else
            {
                int old = draft.abilities[index];
                draft.abilities[index] = Mathf.Clamp(old + delta, draft.abilityMethod == AbilityMethod.PointBuy ? 8 : 3, draft.abilityMethod == AbilityMethod.PointBuy ? 15 : 18);
                if (draft.abilityMethod == AbilityMethod.PointBuy && draft.abilities.Sum(CharacterCreationRules.PointCost) > 27) draft.abilities[index] = old;
            }
            Draw(false);
        }
        private void RollAbilities(bool replace)
        {
            if (!replace && draft.rolledScores != null && draft.rolledScores.Length == 6) return;
            draft.rolledScores = new int[6];
            draft.rolledAssignment = new[] { -1, -1, -1, -1, -1, -1 };
            draft.abilities = new[] { 10, 10, 10, 10, 10, 10 };
            for (int i = 0; i < 6; i++)
            {
                int[] dice = Enumerable.Range(0, 4).Select(_ => UnityEngine.Random.Range(1, 7)).OrderBy(x => x).ToArray();
                draft.rolledScores[i] = dice[1] + dice[2] + dice[3];
            }
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
            int[] bg = CharacterCreationRules.BackgroundSkills(draft);
            Note("Від передісторії: " + string.Join(", ", bg.Select(x => CharacterCreationRules.SkillNames[x])) + ". Обери ще 2 від воїна: " + draft.classSkills.Count + "/2.", 80);
            var options = CharacterCreationRules.FighterSkills.Where(x => !bg.Contains(x)).ToArray();
            for (int i = 0; i < options.Length; i += 2)
            {
                var row = ui.Row("SkillChoices", content, 65);
                foreach (int skill in options.Skip(i).Take(2))
                {
                    int selected = skill;
                    ui.Button(row, (draft.classSkills.Contains(skill) ? "• " : "") + CharacterCreationRules.SkillNames[skill], () => {
                        if (draft.classSkills.Contains(selected)) draft.classSkills.Remove(selected);
                        else if (draft.classSkills.Count < 2) draft.classSkills.Add(selected);
                        CharacterCreationRules.RepairDependentChoices(draft); Draw(false);
                    }, draft.classSkills.Contains(skill));
                }
            }
            if (draft.species == "human" && draft.edition == RulesEdition.Rules2024)
                Cycle("Навичка людини: " + CharacterCreationRules.SkillNames[draft.humanSkill], Enumerable.Range(0, 18).Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x)).ToArray(), draft.humanSkill, value => draft.humanSkill = value);
            ui.Text(content, "Мови й інструменти", 26, CreationWizardUi.Gold, 45);
            if (draft.edition == RulesEdition.Rules2014 && draft.species == "dwarf") Note("Мови: Загальна та Дварфська.", 45);
            else
            {
                string[] languages = { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" };
                CycleText("Мова: ", languages.Where(x => draft.edition == RulesEdition.Rules2014 || x != draft.secondLanguage).ToArray(), draft.language, value => draft.language = value);
                if (draft.edition == RulesEdition.Rules2024) CycleText("Друга мова: ", languages.Where(x => x != draft.language).ToArray(), draft.secondLanguage, value => draft.secondLanguage = value);
            }
            if (draft.background == "soldier" || draft.edition == RulesEdition.Rules2014) CycleText("Ігровий набір: ", new[] { "Гральні кості", "Карти", "Шахи" }, draft.gamingSet, value => draft.gamingSet = value);
            if (draft.species == "dwarf" && draft.edition == RulesEdition.Rules2014) CycleText("Ремесло дварфа: ", new[] { "Інструменти коваля", "Приладдя пивовара", "Інструменти каменяра" }, draft.artisanTool, value => draft.artisanTool = value);
        }
        private void Cycle(string label, int[] values, int value, Action<int> change)
        {
            ui.Button(content, HighlightChoice(label) + "   >", () => { change(values[(Array.IndexOf(values, value) + 1) % values.Length]); Draw(false); });
        }
        private void CycleText(string prefix, string[] values, string value, Action<string> change)
        {
            ui.Button(content, prefix + "<size=115%><color=#FF9A00>" + value + "</color></size>   >", () => { change(values[(Array.IndexOf(values, value) + 1) % values.Length]); Draw(false); });
        }
        private static string HighlightChoice(string label)
        {
            int colon = label.IndexOf(':');
            return colon < 0 ? label : label.Substring(0, colon + 1) + " <size=115%><color=#FF9A00>" + label.Substring(colon + 1).Trim() + "</color></size>";
        }
        private void EquipmentPage()
        {
            ui.Text(content, "Бойовий стиль", 25, CreationWizardUi.Gold, 40);
            string[] styles = { "Захист • +1 КЗ", "Дуелі • +2 шкоди", "Стрільба • +2 до атаки", "Велика зброя" };
            for (int i = 0; i < 4; i += 2)
            {
                var row = ui.Row("Styles", content, 65);
                for (int j = i; j < i + 2; j++) { int index = j; ui.Button(row, styles[j], () => { draft.fightingStyle = (FighterStyle)index; Draw(false); }, (int)draft.fightingStyle == j); }
            }
            ui.Text(content, "Набір спорядження — окремий вибір", 25, CreationWizardUi.Gold, 42);
            var kitFrame = ui.Panel("EquipmentKitFrame", content, Color.clear);
            CreationWizardUi.Height(kitFrame, compact ? 268 : 150);
            var frameLayout = kitFrame.GetComponent<UnityEngine.UI.LayoutElement>();
            frameLayout.preferredWidth = 1040; frameLayout.flexibleWidth = 1;
            var frameImage = kitFrame.GetComponent<UnityEngine.UI.Image>();
            if (panelFrameSprite != null) { frameImage.sprite = panelFrameSprite; frameImage.type = UnityEngine.UI.Image.Type.Sliced; frameImage.pixelsPerUnitMultiplier = 4; frameImage.color = Color.white; }
            var vertical = kitFrame.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 12, 12); vertical.spacing = 8;
            vertical.childControlWidth = true; vertical.childControlHeight = true;
            Choose(draft.edition == RulesEdition.Rules2024 ? "НАБІР A\nКольчуга • дворучний меч • ціп • дротики" : "БЛИЖНІЙ БІЙ\nКольчуга • меч і щит • 2 сокири", draft.edition == RulesEdition.Rules2024 ? "НАБІР B\nШкіра з клепками • мечі • довгий лук" : "ЛУЧНИК\nШкіра • довгий лук • меч і щит", draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            Note(CharacterCreationRules.Equipment(draft), 150);
            Note("КЗ враховує надягнений набір: у лучника щит лежить у спорядженні. Стиль діє лише за своїх умов. Перелік речей буде записаний у картку; окремі комірки інвентарю — наступний етап.", 110);
            if (draft.fightingStyle == FighterStyle.Archery && draft.equipment == 0 ||
                draft.fightingStyle == FighterStyle.GreatWeapon && draft.equipment == 1 ||
                draft.fightingStyle == FighterStyle.Dueling && draft.edition == RulesEdition.Rules2024 && draft.equipment == 0)
                Note("Увага: у вибраному наборі немає зброї, з якою цей бойовий стиль діятиме одразу. Стиль залишиться доступним для іншої зброї.", 88);
        }
        private void ReviewPage()
        {
            ui.Text(content, "Як звати твого персонажа?", 26, CreationWizardUi.Gold, 42);
            ui.Input(content, draft.characterName, "Введи ім’я…", value => { draft.characterName = value; UpdateSummary(); });
            CycleText("Світогляд: ", new[] { "Нейтральний", "Законно-добрий", "Нейтрально-добрий", "Хаотично-добрий", "Законно-нейтральний", "Хаотично-нейтральний", "Законно-злий", "Нейтрально-злий", "Хаотично-злий" }, draft.alignment, value => draft.alignment = value);
            var s = CharacterCreationRules.Calculate(draft);
            Note(s.speciesName + " • " + s.backgroundName + " • Воїн 1 • " + (draft.edition == RulesEdition.Rules2024 ? "2024" : "2014"), 50);
            ui.Text(content, "ОЗ " + s.hitPoints + "     КЗ " + s.armorClass + "     Ініціатива " + CharacterCreationRules.Signed(s.initiative) + "     Швидкість " + s.speed, 25, CreationWizardUi.Gold, 60);
            ui.Text(content, HighlightSummaryLabels(s.features), 21, null, 410);
            ui.Text(content, HighlightSummaryLabels(s.proficiencies), 21, null, 190);
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
            var s = CharacterCreationRules.Calculate(draft);
            summary.text = "<color=#E8AF58>ТВІЙ ПЕРСОНАЖ</color>\n\n" + (string.IsNullOrWhiteSpace(draft.characterName) ? "Новий герой" : draft.characterName) + "\nВоїн • рівень 1\nD&D " + (draft.edition == RulesEdition.Rules2024 ? "2024" : "2014") + "\n\n" + s.speciesName + "\n" + s.backgroundName + "\n\n<color=#E8AF58>" + s.hitPoints + " ОЗ     " + s.armorClass + " КЗ</color>\nІніціатива " + CharacterCreationRules.Signed(s.initiative) + "\nШвидкість " + s.speed + " футів\nМайстерність +2\n\n" + string.Join("\n", Enumerable.Range(0, 6).Select(i => CharacterCreationRules.AbilityNames[i] + "  " + s.scores[i] + " (" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i])) + ")"));
        }
        private void Next()
        {
            if (created) { SceneManager.LoadScene(AppConfig.Scenes.CharacterSheet); return; }
            var errors = CharacterCreationRules.Validate(draft, step == 5);
            if ((step == 2 && errors.Any(x => x.Contains("характерист") || x.Contains("очок") || x.Contains("набір") || x.Contains("Кубики"))) ||
                (step == 3 && errors.Any(x => x.Contains("навичк") || x.Contains("мов"))) || step == 5 && errors.Count > 0)
            { feedback.text = errors[0]; return; }
            if (step < 5) { ShowStep(step + 1); return; }
            next.interactable = false;
            if (new CharacterCreationService(bindings).TryCreate(draft, out string error, out createdId))
            {
                created = true; back.interactable = false; ui.Clear(stepList);
                ui.Clear(content); ui.Text(content, "Персонажа створено", 36, CreationWizardUi.Gold, 70);
                Note(draft.characterName + " вже у твоєму списку. Можна відкрити його звичайну картку.", 120);
                nextLabel.text = "Відкрити картку";
                StyleIcon(next, createdSprite);
            }
            else feedback.text = "Не вдалося зберегти: " + error;
            next.interactable = true;
        }
        private void ReturnToMenu()
        {
            if (!created && initialDraft != JsonUtility.ToJson(draft))
            {
                new TaruckDialogPresenter(() => new RuntimeManagerResolver().Localization().CurrentLanguage)
                    .ShowCompact("Повернутися до головного меню?", "Чернетка персонажа ще не збережена. Вийти з майстра?", "Вийти", () => SceneManager.LoadScene(AppConfig.Scenes.Menu));
                return;
            }
            SceneManager.LoadScene(AppConfig.Scenes.Menu);
        }
    }
}
