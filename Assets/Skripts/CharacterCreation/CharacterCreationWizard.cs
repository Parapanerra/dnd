using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard : MonoBehaviour
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
        private bool showIndividualHitPoints;
        private RectTransform scrollTarget;
        private bool revealChoice;
        private Rect lastSafeArea;
        private string createdId;
        private string initialDraft;
        private TextMeshProUGUI nameError;
        private RuntimeLocalization localization;
        private AppLanguage shownLanguage;
        private static readonly string[] Steps = { "Правила і клас", "Походження", "Характеристики", "Навички й мови", "Спорядження", "Персонаж готовий" };

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
        private void Awake() { LoadLevelUp(); Rebuild(); }
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
            sidebar.gameObject.SetActive(wide && !IsLevelUp); summaryPanel.gameObject.SetActive(wide && !IsLevelUp);
            content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = !wide;
            CreationWizardUi.Fill(scrollRoot, wide && !IsLevelUp ? 235 : 14, 155, wide && !IsLevelUp ? 330 : 14, 200);
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
            menu.gameObject.SetActive(!IsLevelUp);
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
        private void ShowStep(int page)
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

        private string StepError(int page) => CharacterCreationValidation.StepError(draft, page, IsLevelUp, L);

        private void Draw(bool resetScroll = true)
        {
            if (drawingSecondaryTrack)
            {
                redrawSecondaryTrack = true;
                return;
            }
            CharacterCreationChoices.RepairCatalogChoices(draft);
            float previousOffset = content.anchoredPosition.y;
            scroll.StopMovement();
            scrollTarget = null;
            ui.Clear(content); ui.Clear(stepList);
            if (IsLevelUp)
            {
                DrawLevelUp();
                RestoreScroll(resetScroll ? 0 : previousOffset);
                return;
            }
            heading.text = L("Створення персонажа");
            var selectedClass = CharacterClassCatalog.Find(draft.characterClass);
            subtitle.text = (selectedClass == null ? "—" : L(selectedClass.name).ToUpperInvariant()) + " • " + (draft.level > 0 ? draft.level.ToString() : "—") + " " + L("РІВЕНЬ") + "     /     D&D 2014 · 2024";
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
            RestoreScroll(resetScroll ? 0 : previousOffset, revealChoice ? scrollTarget : null);
            revealChoice = false;
        }
        private void RestoreScroll(float offset, RectTransform target = null)
        {
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var viewport = scroll.viewport;
            float overflow = Mathf.Max(0, content.rect.height - viewport.rect.height);
            if (target != null)
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);
                float top = content.rect.yMax - bounds.max.y;
                float bottom = content.rect.yMax - bounds.min.y;
                const float padding = 12;
                offset = Mathf.Clamp(offset, bottom + padding - viewport.rect.height, top - padding);
            }
            scroll.verticalNormalizedPosition = overflow > 0 ? 1 - Mathf.Clamp01(offset / overflow) : 1;
        }
        private void Choose(string a, string b, bool? first, Action<bool> change, float height = 94, float multiplier = 4, Transform container = null)
        {
            Transform root = container == null ? content : container;
            Transform parent = compact ? root : ui.Row("Choices", root, height);
            if (IsLevelUp && !compact) LimitLevelUpButtonRow(parent);
            var left = ui.Button(parent, a, () => { change(true); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, first == true, height, true);
            var right = ui.Button(parent, b, () => { change(false); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, first == false, height, true);
            left.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
            right.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = multiplier;
        }
        // TMP provides its wrapped preferred height to the parent layout; fixed estimates overlap on narrow screens.
        private void Note(string text, float height = 68)
        {
            if (!drawingNewClassChoices) ui.Text(content, L(text), 21, new Color(.7f, .71f, .73f), 0);
        }
        private void RulesPage()
        {
            {
                Note("Автоматично заповнені дані можуть містити неточності або пропуски. Звіряй їх із правилами обраної редакції.");
                Note("Обери редакцію. Майстер застосує її бонуси походження, риси та класові ресурси.", 65);
                Choose("D&D 2014\nКласична п’ята редакція", "D&D 2024\nОновлені правила", (int)draft.edition < 0 ? null : draft.edition == RulesEdition.Rules2014,
                    first => CharacterCreationChoices.SetEdition(draft, first ? RulesEdition.Rules2014 : RulesEdition.Rules2024), 112, 2);
                var classes = CharacterClassCatalog.SelectableFor(draft.edition).ToArray();
                DropdownKey("Клас персонажа", classes.Select(item => item.id).ToArray(),
                    classes.Select(item => L(item.name)).ToArray(), draft.characterClass, value => CharacterCreationChoices.SetClass(draft, value), 300);
                if (!string.IsNullOrEmpty(draft.characterClass))
                    DropdownInt(draft.secondaryClass?.IsSelected == true ? "Рівень основного класу" : "Рівень персонажа",
                        Enumerable.Range(1, CharacterCreationRules.MaxSupportedLevel(draft) -
                            (draft.secondaryClass?.IsSelected == true ? draft.secondaryClass.level : 0)).ToArray(), draft.level,
                        value => { draft.level = value; revealChoice = value > 1; }, value => value.ToString(), 160);
            }
            DrawHitPointChoices();
            DrawMulticlassCreation();
            DrawClassProgressionChoices();
        }

        private void DrawClassProgressionChoices()
        {
            if (draft.characterClass == "bloodhunter" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterBloodHunterRules.Orders, CharacterBloodHunterRules.Orders.Select(id => L(CharacterBloodHunterRules.Text("order." + id))).ToArray(), draft.subclass, value => draft.subclass = value, 300);
            else if (draft.characterClass == "artificer" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterArtificerRules.Options(draft), CharacterArtificerRules.Options(draft).Select(id => L(CharacterArtificerRules.Name(id))).ToArray(), draft.subclass, value => draft.subclass = value, 300);
            else if (draft.characterClass == "monk" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterMonkSubclassRules.Options(draft), CharacterMonkSubclassRules.Options(draft).Select(id => L(CharacterMonkSubclassRules.Names[Array.IndexOf(CharacterMonkSubclassRules.Modern, id)])).ToArray(), draft.subclass, value => draft.subclass = value, 260);
            else if (draft.characterClass == "barbarian" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterBarbarianRules.Options(draft), CharacterBarbarianRules.Options(draft).Select(id => L(CharacterBarbarianRules.Name(id))).ToArray(), draft.subclass, value => { draft.subclass = value; draft.barbarianSpirit = draft.barbarianAspect = draft.barbarianAttunement = ""; }, 300);
            else if (draft.characterClass == "fighter" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterFighterRules.Options(draft), CharacterFighterRules.Options(draft).Select(id => L(CharacterFighterRules.Names[Array.IndexOf(CharacterFighterRules.Modern, id)])).ToArray(), draft.subclass, value => { draft.subclass = value; draft.preparedSpells.Clear(); draft.cantrips.Clear(); draft.fighterManeuvers.Clear(); draft.fighterSkill = -1; draft.fighterTool = ""; draft.fighterSecondStyle = -1; }, 300);
            else if (draft.characterClass == "ranger" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterRangerRules.Options(draft), CharacterRangerRules.Options(draft).Select(id => L(CharacterRangerRules.Names[Array.IndexOf(CharacterRangerRules.Modern, id)])).ToArray(), draft.subclass, value => { draft.subclass = value; CharacterCreationRules.RepairDependentChoices(draft); }, 300);
            else if (draft.characterClass == "paladin" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterPaladinRules.Options(draft), CharacterPaladinRules.Options(draft).Select(id => L(CharacterPaladinRules.Names[Array.IndexOf(CharacterPaladinRules.Modern, id)])).ToArray(), draft.subclass, value => { draft.subclass = value; draft.preparedSpells.Clear(); }, 300);
            else if (draft.characterClass == "rogue" && draft.level >= 3)
                DropdownKey("Підклас персонажа", CharacterRogueRules.Options(draft), CharacterRogueRules.Options(draft).Select(id => L(CharacterRogueRules.Name(id))).ToArray(), draft.subclass, value => { draft.subclass = value; draft.preparedSpells.Clear(); draft.cantrips.Clear(); }, 300);
            else if (!string.IsNullOrEmpty(CharacterCreationRules.RequiredSubclass(draft)))
            {
                ui.Text(content, "Підклас персонажа", 24, CreationWizardUi.Gold, 42);
                bool barbarian = draft.characterClass == "barbarian";
                string subclass = CharacterCreationRules.RequiredSubclass(draft);
                ui.Button(content, draft.characterClass == "monk" ? "ВІДКРИТА ДОЛОНЯ\nПрийоми беззбройного бою" : barbarian ? "БЕРСЕРК\nШаленство" : draft.characterClass == "rogue" ? "ЗЛОДІЙ\nСпритні руки та робота на висоті" : "ЧЕМПІОН\nПокращений критичний удар",
                    () => { draft.subclass = subclass; Draw(false); }, draft.subclass == subclass, 78, showCheck: true);
            }
            if (CharacterBarbarianRules.Is(draft, "totem"))
            {
                DropdownKey("Тотемний дух — рівень 3", CharacterBarbarianRules.Animals, CharacterBarbarianRules.AnimalNames.Select(L).ToArray(), draft.barbarianSpirit, value => draft.barbarianSpirit = value, 230);
                if (draft.level >= 6) DropdownKey("Аспект звіра — рівень 6", CharacterBarbarianRules.Animals, CharacterBarbarianRules.AnimalNames.Select(L).ToArray(), draft.barbarianAspect, value => draft.barbarianAspect = value, 230);
                if (draft.level >= 14) DropdownKey("Тотемне єднання — рівень 14", CharacterBarbarianRules.Animals, CharacterBarbarianRules.AnimalNames.Select(L).ToArray(), draft.barbarianAttunement, value => draft.barbarianAttunement = value, 230);
            }
            if (CharacterBarbarianRules.Is(draft, "wildheart") && draft.level >= 6)
                DropdownKey("Аспект дикої природи", CharacterBarbarianRules.Aspects, CharacterBarbarianRules.AspectNames.Select(L).ToArray(), draft.barbarianAspect, value => draft.barbarianAspect = value, 230);
            if (CharacterMonkSubclassRules.ClassicElements(draft))
            {
                if (!IsLevelUp) ui.Text(content, "Стихійні дисципліни", 25, CreationWizardUi.Gold, 42);
                draft.monkDisciplines ??= new System.Collections.Generic.List<string>();
                int required = CharacterMonkSubclassRules.DisciplineCount(draft);
                while (draft.monkDisciplines.Count < required) draft.monkDisciplines.Add("");
                if (draft.monkDisciplines.Count > required) draft.monkDisciplines.RemoveRange(required, draft.monkDisciplines.Count - required);
                for (int i = 0; i < required; i++)
                {
                    int slot = i;
                    string[] options = CharacterMonkSubclassRules.DisciplineIds.Where((id, index) => CharacterMonkSubclassRules.DisciplineLevels[index] <= draft.level && (id == draft.monkDisciplines[slot] || !draft.monkDisciplines.Contains(id))).ToArray();
                    DropdownKey(LF("Дисципліна {0}", i + 1), options, options.Select(id => L(CharacterMonkSubclassRules.DisciplineNames[Array.IndexOf(CharacterMonkSubclassRules.DisciplineIds, id)])).ToArray(), draft.monkDisciplines[i], value => draft.monkDisciplines[slot] = value, 300);
                }
            }
            if (draft.characterClass == "bard")
            {
                Note(LF(CharacterBardText.Get("ui.progress"), draft.level, CharacterBardRules.Die(draft), CharacterBardRules.SpellCount(draft)+CharacterBardRules.CoreSecrets(draft), CharacterBardRules.Cantrips(draft)), 86);
                if(draft.level>=3) DropdownKey("Підклас персонажа",CharacterBardRules.Options(draft),CharacterBardRules.Options(draft).Select(id=>L(CharacterBardRules.Names[Array.IndexOf(CharacterBardRules.Modern,id)])).ToArray(),draft.subclass,value=>draft.subclass=value,300);
                return;
            }
            if (draft.characterClass == "druid")
            {
                Note(CharacterDruidRules.Text("progress"), 86);
                if (draft.edition == RulesEdition.Rules2024)
                    DropdownKey("Первісний шлях", new[] { "magician", "warden" }, new[] { L("Маг — додатковий заговір"), L("Вартовий — зброя й обладунки") }, draft.primalOrder, value => draft.primalOrder = value, 320);
                if(draft.level>=CharacterDruidRules.SubclassLevel(draft)) DropdownKey(CharacterDruidRules.Text("ui.circle"),CharacterDruidRules.Options(draft),CharacterDruidRules.Options(draft).Select(id=>L(CharacterDruidRules.Text("circle."+id))).ToArray(),draft.subclass,value=>draft.subclass=value,280);
                if(CharacterDruidRules.Is(draft,"land")&&draft.level>=3) DropdownKey(CharacterDruidRules.Text("ui.land"),CharacterDruidRules.Lands(draft),CharacterDruidRules.Lands(draft).Select(id=>L(CharacterDruidRules.Text("land."+id))).ToArray(),draft.druidLand,value=>draft.druidLand=value,240);
                if(CharacterDruidRules.Revised(draft)&&draft.level>=7) DropdownKey(CharacterDruidRules.Text("ui.fury"),new[]{"potent","primal"},new[]{L(CharacterDruidRules.Text("ui.potent")),L(CharacterDruidRules.Text("ui.primal"))},draft.druidFury,value=>draft.druidFury=value,260);
                if(CharacterDruidRules.KnownForms(draft)>0){
                    Note(CharacterDruidRules.Text("ui.forms"),75);
                    for(int i=0;i<draft.druidKnownForms.Count;i++){int slot=i;var forms=CharacterDruidFormChoices.Options(draft).Where(f=>f.id==draft.druidKnownForms[slot]||!draft.druidKnownForms.Contains(f.id)).ToArray();DropdownKey(string.Format(L(CharacterDruidRules.Text("ui.form")),i+1),forms.Select(f=>f.id).ToArray(),forms.Select(f=>L(f.name)).ToArray(),draft.druidKnownForms[slot],value=>draft.druidKnownForms[slot]=value,250);}
                }
                return;
            }
            if (draft.characterClass == "cleric")
            {
                Note(CharacterClericRules.Text("progress"),86);
                if(draft.level>=CharacterClericRules.SubclassLevel(draft))DropdownKey("Божественний домен",CharacterClericRules.Options(draft),CharacterClericRules.Options(draft).Select(id=>L(CharacterClericRules.DomainNames[Array.IndexOf(CharacterClericRules.Domains,id)])).ToArray(),draft.subclass,value=>draft.subclass=value,280);
                if(draft.edition==RulesEdition.Rules2024) DropdownKey("Божественний порядок", new[] { "protector", "thaumaturge" }, new[] { L("Захисник — зброя й обладунки"), L("Чудотворець — додатковий заговір") }, draft.divineOrder, value => draft.divineOrder = value, 320);
                if(CharacterClericRules.Revised(draft)&&draft.level>=7)DropdownKey(CharacterClericRules.Text("ui.strike"),new[]{"divine","potent"},new[]{L(CharacterClericRules.Text("ui.strike.weapon")),L(CharacterClericRules.Text("ui.strike.potent"))},draft.clericStrike,value=>draft.clericStrike=value,280);
                return;
            }
            if (draft.characterClass == "wizard")
            {
                Note(CharacterWizardText.Get("progress"), 86);
                if (draft.level >= CharacterWizardRules.SchoolLevel(draft))
                {
                    string[] schools = CharacterWizardRules.Schools(draft);
                    DropdownKey(CharacterWizardText.Get("school"), schools, schools.Select(id => L(CharacterWizardText.Get(id))).ToArray(),
                        draft.subclass, value => { draft.subclass = value; draft.wizardSavant.Clear(); }, 300);
                }
                return;
            }
            if (CharacterSorcererRules.Active(draft))
            {
                Note(CharacterSorcererText.Get("progress"));
                if (draft.level >= CharacterSorcererRules.SubclassLevel(draft))
                    DropdownKey("Походження чародія", CharacterSorcererRules.Options(draft), CharacterSorcererRules.Options(draft).Select(id => L(CharacterSorcererRules.Names[Array.IndexOf(CharacterSorcererRules.Modern, id)])).ToArray(), draft.subclass, value => draft.subclass = value, 300);
                if (CharacterSorcererRules.Draconic(draft)) DropdownInt("Драконячий предок", Enumerable.Range(0, CharacterSorcererRules.DragonNames.Length).ToArray(), draft.dragonAncestor, value => draft.dragonAncestor = value, x => CharacterSorcererRules.DragonNames[x], 280);
                if (CharacterSorcererRules.Is(draft, "draconic") && CharacterSorcererRules.Revised(draft) && draft.level >= 6)
                    DropdownInt("Стихійна спорідненість", Enumerable.Range(0, 5).ToArray(), draft.sorcererAffinity, value => draft.sorcererAffinity = value, x => CharacterSorcererRules.Elements[x], 250);
                return;
            }
            if (draft.characterClass == "bloodhunter")
            {
                Note(CharacterBloodHunterRules.Text("ui.progress"), 86);
                DropdownInt(CharacterBloodHunterRules.Text("ui.ability"), new[] { 3, 4 }, draft.bloodAbility, value => draft.bloodAbility = value, value => CharacterCreationRules.AbilityNames[value], 240);
                for(int i=0;i<draft.bloodCurses.Count;i++)
                {
                    int slot=i; var options=CharacterBloodHunterRules.CurseOptions(draft,slot);
                    DropdownKey(LF(CharacterBloodHunterRules.Text("ui.curse"),i+1),options,options.Select(id=>L(CharacterBloodHunterRules.CurseNames[Array.IndexOf(CharacterBloodHunterRules.CurseIds,id)])).ToArray(),draft.bloodCurses[slot],value=>draft.bloodCurses[slot]=value,320);
                }
                for(int i=0;i<draft.bloodRites.Count;i++)
                {
                    int slot=i;var options=CharacterBloodHunterRules.RiteOptions(draft,slot);
                    DropdownKey(LF(CharacterBloodHunterRules.Text("ui.rite"),i+1),options,options.Select(id=>L(CharacterBloodHunterRules.Text("rite."+id))).ToArray(),draft.bloodRites[slot],value=>draft.bloodRites[slot]=value,270);
                }
                if(CharacterBloodHunterRules.Profane(draft))
                    DropdownKey(CharacterBloodHunterRules.Text("ui.patron"),CharacterBloodHunterRules.Patrons,CharacterBloodHunterRules.Patrons.Select(id=>L(CharacterBloodHunterRules.Text("patron."+id))).ToArray(),draft.bloodPatron,value=>draft.bloodPatron=value,300);
                for(int i=0;i<draft.bloodFormulas.Count;i++)
                {
                    int slot=i;var options=CharacterBloodHunterRules.FormulaOptions(draft,slot);
                    DropdownKey(LF(CharacterBloodHunterRules.Text("ui.formula"),i+1),options,options.Select(id=>L(CharacterBloodHunterRules.FormulaName(id))).ToArray(),draft.bloodFormulas[slot],value=>draft.bloodFormulas[slot]=value,280);
                    if(!string.IsNullOrEmpty(draft.bloodFormulas[slot]))Note(CharacterBloodHunterRules.FormulaDescription(draft.bloodFormulas[slot]),64);
                }
                return;
            }
            if (draft.characterClass == "artificer")
            {
                Note(CharacterArtificerText.Get("progress"), 86);
                if (CharacterArtificerRules.Is(draft, "armorer"))
                    DropdownKey(CharacterArtificerText.Get("ui.model"), CharacterArtificerRules.Models(draft), CharacterArtificerRules.Models(draft).Select(id => L(CharacterArtificerText.Get("name." + id))).ToArray(), draft.artificerModel, value => draft.artificerModel = value, 280);
                return;
            }
            if(CharacterWarlockRules.Active(draft))
            {
                if(draft.level>=(CharacterWarlockRules.Modern(draft)?3:1)){
                    string[] options=CharacterWarlockRules.PatronOptions(draft);
                    DropdownKey("Покровитель чаклуна",options,options.Select(id=>L(CharacterWarlockRules.PatronNames[Array.IndexOf(CharacterWarlockRules.Patrons,id)])).ToArray(),draft.subclass,value=>draft.subclass=value,300);
                }
                if(!CharacterWarlockRules.Modern(draft)&&draft.level>=3)DropdownKey(L(CharacterWarlockRules.Text("ui.pact")),new[]{"blade","chain","tome"},new[]{L("Договір клинка"),L("Договір ланцюга"),L("Договір книги")},draft.warlockPact,value=>draft.warlockPact=value,300);
                return;
            }
            if (!string.IsNullOrEmpty(draft.characterClass))
                Note(draft.characterClass == "ranger" ? CharacterRangerText.Get("progress") : draft.characterClass == "paladin" ? CharacterPaladinText.Get("progress") : draft.characterClass == "monk" ? "Монах 1–20: обери рівень, підклас і підвищення характеристик. Ци, удари, швидкість та здібності відповідають редакції." : draft.characterClass == "rogue" ? "Плут 1–20: обери підклас, експертизи й підвищення характеристик. Прихована атака та ресурси зростають із рівнем." : draft.characterClass == "barbarian" ? "Варвар 1–20: обери рівень і шлях. Лють, майстерність зброї та здібності відповідають редакції." : "На 2 рівні воїн отримує Сплеск дії; за правилами 2024 — також Тактичний розум. На 3 рівні обери підклас.", 86);
            if (draft.characterClass == "fighter") Note(draft.edition == RulesEdition.Rules2024 ? "2024: бонуси характеристик дає передісторія. На 1 рівні є майстерність трьох видів зброї та 2 використання Другого дихання." : "2014: бонуси характеристик дає раса. На 1 рівні є бойовий стиль та 1 використання Другого дихання.", 115);
            if (draft.characterClass == "ranger" && draft.edition == RulesEdition.Rules2014)
            {
                DropdownKey("Обраний ворог", CharacterCreationRules.RangerEnemies, CharacterCreationRules.RangerEnemies.Select(L).ToArray(), draft.rangerEnemy, value => draft.rangerEnemy = value, 320);
                if (draft.rangerEnemy == "Два види гуманоїдів")
                    ui.Input(content, draft.rangerHumanoids, L("Два види гуманоїдів через кому"), value => draft.rangerHumanoids = value);
                DropdownKey("Обрана місцевість", CharacterCreationRules.RangerTerrains, CharacterCreationRules.RangerTerrains.Select(L).ToArray(), draft.rangerTerrain, value => draft.rangerTerrain = value, 260);
                for (int i = 1; i < CharacterRangerRules.EnemyCount(draft); i++) {
                    int slot = i - 1;
                    string[] enemies = CharacterCreationRules.RangerEnemies.Where(x => x == CharacterCreationRules.RangerEnemies.Last() || x == draft.rangerExtraEnemies[slot] || !CharacterRangerRules.Enemies(draft).Contains(x)).ToArray();
                    DropdownKey(LF("Обраний ворог {0}", i + 1), enemies, enemies.Select(L).ToArray(), draft.rangerExtraEnemies[slot], value => draft.rangerExtraEnemies[slot] = value, 300);
                    if (draft.rangerExtraEnemies[slot] == CharacterCreationRules.RangerEnemies.Last())
                        ui.Input(content, draft.rangerExtraHumanoids[slot], L("Два види гуманоїдів через кому"), value => draft.rangerExtraHumanoids[slot] = value);
                }
                for (int i = 1; i < CharacterRangerRules.TerrainCount(draft); i++) {
                    int slot = i - 1;
                    string[] terrains = CharacterCreationRules.RangerTerrains.Where(x => x == draft.rangerExtraTerrains[slot] || !CharacterRangerRules.Terrains(draft).Contains(x)).ToArray();
                    DropdownKey(LF("Обрана місцевість {0}", i + 1), terrains, terrains.Select(L).ToArray(), draft.rangerExtraTerrains[slot], value => draft.rangerExtraTerrains[slot] = value, 260);
                }
            }
            RangerHunterChoices();
        }
        private void RangerHunterChoices()
        {
            if (!CharacterRangerRules.Is(draft, "hunter")) return;
            void Pick(string label, string[] ids, string current, Action<string> assign, string prefix) => DropdownKey(L(label), ids,
                ids.Select(id => L(CharacterRangerText.Get(prefix + id + (id == "multi" ? CharacterRangerRules.Revised(draft) ? "24" : "14" : "")).Split(':')[0])).ToArray(), current, assign, 300);
            Pick("Здобич мисливця", CharacterRangerRules.Prey(draft), draft.rangerHunterPrey, value => draft.rangerHunterPrey = value, "prey.");
            if (draft.level >= 7) Pick("Захисна тактика", CharacterRangerRules.Defense(draft), draft.rangerHunterDefense, value => draft.rangerHunterDefense = value, "defense.");
            if (!CharacterRangerRules.Revised(draft) && draft.level >= 11) Pick("Мультиатака", new[] { "volley", "whirlwind" }, draft.rangerHunterMultiattack, value => draft.rangerHunterMultiattack = value, "");
            if (!CharacterRangerRules.Revised(draft) && draft.level >= 15) Pick("Вищий захист", new[] { "evasion", "tide", "dodge" }, draft.rangerHunterSuperior, value => draft.rangerHunterSuperior = value, "");
        }

        private void DrawHitPointChoices()
        {
            if (draft.hitPointChoices == null || draft.hitPointChoices.Length < 19) draft.hitPointChoices = new int[19];
            if (draft.level < 2) return;
            int count = draft.level - 1;
            int hitDie = CharacterCreationRules.HitDie(draft);
            int fixedHp = CharacterCreationRules.FixedHitPoints(draft);
            ui.Text(content, "ОЗ за рівні", 25, CreationWizardUi.Gold, 42);
            Transform actions = compact ? content : ui.Row("HitPointBulkActions", content, 54);
            ui.Button(actions, "Середнє для всіх", () => {
                for (int i = 0; i < count; i++) draft.hitPointChoices[i] = -1;
                Draw(false);
            }, draft.hitPointChoices.Take(count).All(choice => choice == -1), 54, true);
            var rollButton = ui.Button(actions, "Кинути для всіх", () => {
                for (int i = 0; i < count; i++) draft.hitPointChoices[i] = UnityEngine.Random.Range(1, hitDie + 1);
                Draw(false);
            }, draft.hitPointChoices.Take(count).All(choice => choice > 0), 54, true);
            scrollTarget = (RectTransform)rollButton.transform;
            int filled = draft.hitPointChoices.Take(count).Count(choice => choice == -1 || choice > 0 && choice <= hitDie);
            string status = LF("Вибрано рівнів: {0}/{1}.", filled, count);
            if (filled == count && draft.abilities != null && draft.abilities.Length == 6 && draft.abilities.All(value => value >= 3))
                status += " " + LF("Поточний підсумок ОЗ: {0}.", CharacterCreationRules.Calculate(draft).hitPoints);
            ui.Text(content, status, 23, CreationWizardUi.Gold, 0);
            ui.Button(content, showIndividualHitPoints ? "Згорнути окремі рівні" : "Налаштувати окремі рівні", () => {
                showIndividualHitPoints = !showIndividualHitPoints; Draw(false);
            }, false, 48);
            if (!showIndividualHitPoints) return;
            for (int level = 2; level <= draft.level; level++)
            {
                int index = level - 2;
                var row = ui.Row("HitPointsLevel" + level, content, 46);
                row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                var label = ui.Text(row, LF("Рівень {0}", level), 21, CreationWizardUi.Gold, 46);
                label.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 85 : 120;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                var values = new[] { 0, -1 }.Concat(Enumerable.Range(1, hitDie)).ToArray();
                var names = new[] { "—", LF("Середнє: {0}", fixedHp) }.Concat(Enumerable.Range(1, hitDie).Select(value => value.ToString())).ToArray();
                ui.Dropdown(row, "HitPoints" + level, names, Math.Max(0, Array.IndexOf(values, draft.hitPointChoices[index])),
                    choice => { draft.hitPointChoices[index] = values[choice]; Draw(false); }, compact ? 165 : 200, 42);
                ui.Button(row, LF("Кинути к{0}", hitDie), () => { draft.hitPointChoices[index] = UnityEngine.Random.Range(1, hitDie + 1); Draw(false); }, false, 46)
                    .GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = compact ? 115 : 150;
            }
        }
        private void OriginPage()
        {
            if ((int)draft.edition < 0) { Note("Спочатку обери редакцію правил на першій сторінці."); return; }
            bool modern = draft.edition == RulesEdition.Rules2024;
            var speciesOptions = CharacterSpeciesRules.Available(draft.edition);
            DropdownKey("Раса / вид", speciesOptions,
                speciesOptions.Select(id => L(CharacterSpeciesRules.Name(id, draft.edition))).ToArray(), draft.species,
                value => CharacterCreationChoices.SetSpecies(draft, value), 300);
            ExpandedOriginChoices();
            if (draft.species == "halfling")
            {
                if (!modern) DropdownKey("Різновид напіврослика", new[] { "lightfoot", "stout" }, new[] { L("Легконогий"), L("Кремезний") }, draft.speciesLineage, value => draft.speciesLineage = value, 240);
                Note(modern ? "Напіврослик: швидкість 30 футів, Малий розмір, везіння, хоробрість і природна скритність."
                    : "Напіврослик: +2 Спритності, швидкість 25 футів. Легконогий: +1 Харизми й природна скритність; кремезний: +1 Статури й стійкість до отрути.", 105);
            }
            if (CharacterElfRules.Elf(draft))
            {
                DropdownKey("Ельфійський різновид", CharacterElfRules.Lineages, CharacterElfRules.LineageNames.Select(L).ToArray(), draft.speciesLineage, value => draft.speciesLineage = value, 270);
                if (modern) DropdownInt("Характеристика ельфійської магії", new[] { 3, 4, 5 }, draft.speciesMagicAbility, value => draft.speciesMagicAbility = value, value => CharacterCreationRules.AbilityNames[value], 240);
                if (draft.speciesLineage == "high")
                {
                    string[] cantrips = CharacterSpellCatalog.Available(draft.edition, "wizard", 0).Select(spell => spell.name).Where(name => !draft.cantrips.Contains(name) && !draft.tomeCantrips.Contains(name)).ToArray();
                    DropdownKey("Заговір високого ельфа", cantrips, cantrips.Select(L).ToArray(), draft.speciesCantrip, value => draft.speciesCantrip = value, 320);
                }
                Note("Ельфійська магія йде окремо від класових виборів; комірок заклинань сама по собі не дає.", 70);
            }
            if (CharacterGnomeRules.Gnome(draft))
            {
                DropdownKey("Різновид гнома", CharacterGnomeRules.Lineages, CharacterGnomeRules.LineageNames.Select(L).ToArray(), draft.speciesLineage, value => draft.speciesLineage = value, 270);
                if (modern) DropdownInt("Характеристика гном’ячої магії", new[] { 3, 4, 5 }, draft.speciesMagicAbility, value => draft.speciesMagicAbility = value, value => CharacterCreationRules.AbilityNames[value], 240);
                if (draft.speciesLineage.Length > 0) Note(CharacterCreationPresentation.FeatureLines(CharacterGnomeRules.Features(draft).TrimStart('\n'), L), compact ? 375 : 245);
            }
            if (draft.species == "dragonborn") DropdownInt("Драконячий предок раси", Enumerable.Range(0, 10).ToArray(), draft.speciesAncestor, value => draft.speciesAncestor = value, value => CharacterSorcererRules.DragonNames[value], 300);
            if (draft.species == "tiefling" && modern)
            {
                DropdownKey("Спадщина тифлінга", CharacterLegacySpeciesRules.TieflingLineages, CharacterLegacySpeciesRules.TieflingNames.Select(L).ToArray(), draft.speciesLineage, value => draft.speciesLineage = value, 280);
                DropdownInt("Характеристика спадкової магії", new[] { 3, 4, 5 }, draft.speciesMagicAbility, value => draft.speciesMagicAbility = value, value => CharacterCreationRules.AbilityNames[value], 240);
            }
            if (draft.species == "goliath") DropdownKey("Велетенська спадщина", CharacterLegacySpeciesRules.GiantLineages, CharacterLegacySpeciesRules.GiantNames.Select(L).ToArray(), draft.speciesLineage, value => draft.speciesLineage = value, 280);
            if (CharacterLegacySpeciesRules.NeedsSize(draft)) DropdownKey("Розмір", new[] { "small", "medium" }, new[] { L("Малий"), L("Середній") }, draft.speciesSize, value => draft.speciesSize = value, 200);
            if (CharacterLegacySpeciesRules.HalfElf(draft))
            {
                for (int i = 0; i < 2; i++)
                {
                    int slot = i;
                    DropdownInt(i == 0 ? "Перша характеристика напівельфа +1" : "Друга характеристика напівельфа +1", Enumerable.Range(0, 5).Where(value => value != draft.speciesIncreases[1 - slot]).ToArray(), draft.speciesIncreases[slot], value => draft.speciesIncreases[slot] = value, value => CharacterCreationRules.AbilityNames[value], 230);
                }
            }
            if (CharacterWarforgedRules.Warforged(draft))
            {
                if (!modern) DropdownInt("Додаткова характеристика кованого +1", new[] { 0, 1, 3, 4, 5 }, draft.speciesIncreases[0], value => draft.speciesIncreases[0] = value, value => CharacterCreationRules.AbilityNames[value], 240);
                Note(CharacterCreationPresentation.FeatureLines(CharacterWarforgedRules.Features(draft).TrimStart('\n'), L));
            }
            string otherTraits = CharacterLegacySpeciesRules.Features(draft).TrimStart('\n');
            if (otherTraits.Length > 0) Note(CharacterCreationPresentation.FeatureLines(otherTraits, L), compact ? 330 : 220);

            if (draft.species == "halforc") Note("Напіворк: +2 Сили, +1 Статури, темнозір 60 футів, Залякування, Невблаганна витривалість і Люті атаки.", 90);
            if (draft.species == "orc") Note("Орк: темнозір 120 футів, Невблаганна витривалість і Приплив адреналіну. Бонуси характеристик дає передісторія.", 90);
            if (draft.species == "mountaindwarf") Note("Гірський дварф: +2 Сили, +2 Статури; володіння легкими й середніми обладунками. Додаткових ОЗ пагорбового дварфа немає.", 90);
            var backgrounds = CharacterBackgroundRules.Available(draft.edition);
            DropdownKey("Передісторія", backgrounds.Select(background => background.id).ToArray(), backgrounds.Select(background => L(background.name)).ToArray(), draft.background,
                value => CharacterCreationChoices.SetBackground(draft, value), 300);
            var selectedBackground = CharacterBackgroundRules.Find(draft);
            if (selectedBackground != null && selectedBackground.feature.Length > 0) Note(L(selectedBackground.feature), compact ? 130 : 95);
            if (CharacterOriginFeatRules.HumanChoice(draft))
            {
                string[] feats = CharacterOriginFeatRules.HumanOptions(draft);
                DropdownKey("Додаткова риса людини", feats, feats.Select(id => L(CharacterOriginFeatRules.Name(id))).ToArray(), draft.humanFeat.id, value => draft.humanFeat = new CharacterOriginFeatChoice { id = value }, 320);
            }
            if (modern && selectedBackground != null) Note(LF("Риса передісторії: {0}.", L(CharacterOriginFeatRules.Name(selectedBackground.feat))), 55);
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
                    if (index == 2) { PrepareRolls(); revealChoice = true; }
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
                scrollTarget = rollAction;
                rollAction.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                bool allRolled = draft.rolledCount >= 6;
                var action = ui.Button(rollAction, allRolled ? "Скинути кидки" : LF("Кинути 4к6 • кинуто {0}/6", draft.rolledCount),
                    () => { if (allRolled) PrepareRolls(); else RollNext(); revealChoice = true; Draw(false); }, height: 52);
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
            DrawLevelAdvancements();
            if (draft.secondaryClass?.IsSelected == true)
            {
                var track = draft.secondaryClass.ClassDraft(draft.edition);
                DrawLevelAdvancements(track, saveChoices: () => draft.secondaryClass.SaveChoices(track));
            }
        }
        private void DrawLevelAdvancements(CharacterCreationDraft track = null, int? previousLevel = null, Action saveChoices = null)
        {
            track ??= draft;
            void Save(Action assign) { assign(); saveChoices?.Invoke(); }
            if (!CharacterLevelAdvancement.Active(track) || track.level < 4) return;
            CharacterLevelAdvancement.EnsureArrays(track);
            CharacterLevelAdvancement.RepairChoices(draft, track);
            if (IsLevelUp && !CharacterLevelAdvancement.LevelsFor(track).Contains(track.level)) return;
            ui.Text(content, track != draft ? LF("Підвищення характеристик — {0}", L(CharacterClassCatalog.Find(track.characterClass).name)) : IsLevelUp ? "Підвищення характеристик" : "Підвищення за рівні", 25, CreationWizardUi.Gold, 44);
            for (int i = 0; i < CharacterLevelAdvancement.Count(track); i++)
            {
                if (IsLevelUp && CharacterLevelAdvancement.LevelsFor(track)[i] <= (previousLevel ?? advancementFrom)) continue;
                int slot = i;
                if (!IsLevelUp) ui.Text(content, LF("Рівень {0}", CharacterLevelAdvancement.LevelsFor(track)[i]), 23, CreationWizardUi.Gold, 38);
                if (CharacterLevelAdvancement.LevelsFor(track)[i] == 19 && CharacterLevelAdvancement.Epic(track))
                {
                    DropdownKey("Епічний дар", CharacterLevelAdvancement.BoonsFor(track), CharacterLevelAdvancement.BoonsFor(track).Select(id => L(CharacterLevelAdvancement.BoonNames[Array.IndexOf(CharacterLevelAdvancement.Boons, id)])).ToArray(), track.epicBoon, value => Save(() => track.epicBoon = value), 300);
                    if (track.epicBoon.Length == 0) continue;
                    if (track.epicBoon != "asi")
                    {
                        DropdownInt("+1 до характеристики дару", CharacterLevelAdvancement.EpicAbilities(draft, track), track.epicAbility, value => Save(() => track.epicAbility = value), value => CharacterCreationRules.AbilityNames[value], 190);
                        if (track.epicBoon == "skill") DropdownInt("Експертиза дару", Enumerable.Range(0, 18).ToArray(), track.epicSkill, value => Save(() => track.epicSkill = value), value => CharacterCreationRules.SkillNames[value], 250);
                        if (track.epicBoon == "energy")
                            for (int r = 0; r < 2; r++)
                            {
                                int resistance = r;
                                DropdownInt(LF("Опір {0}", r + 1), Enumerable.Range(0, CharacterLevelAdvancement.DamageTypes.Length).Where(value => value != track.epicResistances[1 - resistance]).ToArray(), track.epicResistances[r], value => Save(() => track.epicResistances[resistance] = value), value => CharacterLevelAdvancement.DamageTypes[value], 190);
                            }
                        Note(CharacterLevelAdvancement.BoonFeature(track.epicBoon));
                        continue;
                    }
                }
                Choose("+2 до однієї", "+1 до двох", track.advancementModes[i] < 0 ? null : track.advancementModes[i] == 0, first => Save(() => track.advancementModes[slot] = first ? 0 : 1), 54);
                if (track.advancementModes[i] < 0) continue;
                DropdownInt(track.advancementModes[i] == 0 ? "+2 до характеристики" : "Перша характеристика +1", CharacterLevelAdvancement.AbilityOptions(draft, i, track: track), track.advancementFirst[i], value => Save(() => track.advancementFirst[slot] = value), value => CharacterCreationRules.AbilityNames[value], 190);
                if (track.advancementModes[i] == 1)
                    DropdownInt("Друга характеристика +1", CharacterLevelAdvancement.AbilityOptions(draft, i, true, track), track.advancementSecond[i], value => Save(() => track.advancementSecond[slot] = value), value => CharacterCreationRules.AbilityNames[value], 190);
            }
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
            ExpandedSkillChoices();
            if (CharacterWarforgedRules.Warforged(draft))
            {
                DropdownInt("Навичка кованого", Enumerable.Range(0, 18).Except(CharacterCreationRules.OriginalBackgroundSkills(draft)).Except(draft.classSkills).Except(CharacterOriginFeatRules.Skills(draft)).ToArray(), draft.speciesSkill, value => draft.speciesSkill = value, value => CharacterCreationRules.SkillNames[value], 300);
                DropdownText("Інструмент кованого", CharacterWarforgedRules.ToolOptions(draft), draft.speciesTool, value => draft.speciesTool = value);
            }
            if (CharacterLegacySpeciesRules.HalfElf(draft))
            {
                for (int i = 0; i < 2; i++)
                {
                    int slot = i;
                    DropdownInt(i == 0 ? "Перша навичка напівельфа" : "Друга навичка напівельфа", Enumerable.Range(0, 18).Where(skill => !CharacterCreationRules.OriginalBackgroundSkills(draft).Contains(skill) && !draft.classSkills.Contains(skill) && skill != draft.speciesSkills[1 - slot]).ToArray(), draft.speciesSkills[slot], value => draft.speciesSkills[slot] = value, value => CharacterCreationRules.SkillNames[value], 300);
                }
            }
            if (CharacterGnomeRules.NeedsToolReplacement(draft))
            {
                string[] replacements = CharacterGnomeRules.ToolReplacements(draft);
                DropdownKey("Заміна повторних інструментів", replacements, replacements.Select(L).ToArray(), draft.originToolReplacement, value => draft.originToolReplacement = value, 320);
            }
            if (CharacterElfRules.Modern(draft)) DropdownInt("Гострі чуття ельфа", CharacterElfRules.KeenSkills, draft.speciesSkill, value => draft.speciesSkill = value, value => CharacterCreationRules.SkillNames[value], 280);
            if (CharacterSpeciesRules.GrantedSkills(draft).Length > 0) Note(LF("Від раси / виду: {0}.", string.Join(", ", CharacterSpeciesRules.GrantedSkills(draft).Select(skill => L(CharacterCreationRules.SkillNames[skill])))), 48);
            for(int duplicate=0;duplicate<CharacterSpeciesRules.RepeatedBackgroundSkills(draft).Length;duplicate++)
            {
                int slot=duplicate;int current=slot==0?draft.originSkillReplacement:draft.secondOriginSkillReplacement;int other=slot==0?draft.secondOriginSkillReplacement:draft.originSkillReplacement;
                DropdownInt("Заміна повторної навички передісторії",CharacterSpeciesRules.ReplacementOptions(draft).Where(skill=>!draft.classSkills.Contains(skill)&&skill!=other).ToArray(),current,value=>{if(slot==0)draft.originSkillReplacement=value;else draft.secondOriginSkillReplacement=value;},value=>CharacterCreationRules.SkillNames[value],280);
            }
            if (draft.characterClass == "artificer" && !drawingSecondaryTrack) DropdownKey("Ремісничі інструменти винахідника", CharacterArtificerRules.Tools.Where(tool => tool != draft.originToolReplacement).ToArray(), CharacterArtificerRules.Tools.Where(tool => tool != draft.originToolReplacement).Select(L).ToArray(), draft.artificerTool, value => draft.artificerTool = value, 320);
            if (CharacterArtificerRules.Active(draft))
                for (int i = 0; i < CharacterArtificerRules.ToolReplacementCount(draft); i++)
                {
                    int slot = i;
                    var toolOptions = CharacterArtificerRules.ToolReplacementOptions(draft, slot);
                    DropdownKey(LF(CharacterArtificerText.Get("ui.tool.replacement"), i + 1), toolOptions, toolOptions.Select(L).ToArray(), draft.artificerToolReplacements[slot], value => draft.artificerToolReplacements[slot] = value, 320);
                }
            if (!drawingSecondaryTrack && ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.species) || string.IsNullOrEmpty(draft.background)))
            { Note("Спочатку обери редакцію, расу й передісторію."); return; }
            int[] bg = CharacterCreationRules.BackgroundSkills(draft);
            if ((!IsLevelUp || drawingSecondaryTrack && advancementOriginal.level < 1) && CharacterCreationRules.ClassSkillCount(draft) > 0)
            {
            int requiredSkills = CharacterCreationRules.ClassSkillCount(draft);
            if (drawingSecondaryTrack)
                Note(LF("Навички нового класу: {0}/{1}.", draft.classSkills.Count, requiredSkills), 48);
            else
                Note(LF("Від передісторії: {0}. Навички класу: <b><color=#FF9A00>{1}/{2}</color></b>. {3}",
                    string.Join(", ", bg.Select(x => L(CharacterCreationRules.SkillNames[x]))), draft.classSkills.Count, requiredSkills,
                    draft.classSkills.Count == requiredSkills ? L("Щоб замінити навичку, спочатку зніми один вибір.") : ""), 88);
            var options = CharacterCreationRules.ClassSkillOptions(draft).Where(x => !bg.Contains(x)).ToArray();
            for (int i = 0; i < options.Length; i += 2)
            {
                var row = ui.Row("SkillChoices", content, 65);
                foreach (int skill in options.Skip(i).Take(2))
                {
                    int selected = skill;
                    var button = ui.Button(row, L(CharacterCreationRules.SkillNames[skill]), () => {
                        if (draft.classSkills.Contains(selected)) draft.classSkills.Remove(selected);
                        else if (draft.classSkills.Count < requiredSkills) draft.classSkills.Add(selected);
                        CharacterCreationRules.RepairDependentChoices(draft); Draw(false);
                    }, draft.classSkills.Contains(skill), showCheck: true);
                    if (!draft.classSkills.Contains(skill) && draft.classSkills.Count == requiredSkills) button.interactable = false;
                }
            }
            }
            if(CharacterBardRules.LoreSkillCount(draft)>0) {
                while(draft.bardLoreSkills.Count<3)draft.bardLoreSkills.Add(-1);
                for(int i=0;i<3;i++) {
                    int slot=i;
                    var granted=bg.Concat(draft.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(draft)).Concat(CharacterOriginFeatRules.Skills(draft)).ToArray();
                    var choices=Enumerable.Range(0,18).Where(x=>!granted.Contains(x)&&(draft.bardLoreSkills[slot]==x||!draft.bardLoreSkills.Contains(x))).ToArray();
                    DropdownInt(LF(CharacterBardText.Get("ui.lore"),i+1),choices,draft.bardLoreSkills[slot],value=>draft.bardLoreSkills[slot]=value,x=>CharacterCreationRules.SkillNames[x],290);
                }
            }
            if (IsLevelUp ? LevelUpAddsExpertise() : draft.characterClass == "rogue" || CharacterRangerRules.ExpertiseCount(draft) > 0 || CharacterBardRules.ExpertiseCount(draft)>0)
            {
                ui.Text(content, draft.characterClass == "rogue" ? "Експертиза плута" : CharacterBardRules.Active(draft) ? "Експертиза барда" : "Експертиза слідопита", 25, CreationWizardUi.Gold, 42);
                int expertiseCount = draft.characterClass == "rogue" ? CharacterRogueRules.ExpertiseCount(draft) : CharacterBardRules.Active(draft) ? CharacterBardRules.ExpertiseCount(draft) : CharacterRangerRules.ExpertiseCount(draft);
                Note(LF("Обери експертизи для подвоєного бонусу: {0}/{1}.", draft.expertise.Count, expertiseCount), 50);
                int[] expertiseOptions = bg.Concat(CharacterRangerRules.ExpertiseOptions(draft)).Concat(CharacterBardRules.ExtraSkills(draft)).Concat(CharacterBardRules.Active(draft)&&draft.species=="human"&&draft.edition==RulesEdition.Rules2024&&draft.humanSkill>=0?new[]{draft.humanSkill}:Array.Empty<int>()).Concat(draft.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(draft)).Concat(CharacterOriginFeatRules.Skills(draft)).Distinct().OrderBy(x => x).ToArray();
                if (draft.characterClass == "rogue" && draft.edition == RulesEdition.Rules2014) expertiseOptions = expertiseOptions.Concat(new[] { -1 }).ToArray();
                for (int i = 0; i < expertiseOptions.Length; i += 2)
                {
                    var row = ui.Row("ExpertiseChoices", content, 60);
                    foreach (int expertise in expertiseOptions.Skip(i).Take(2))
                    {
                        if (IsLevelUp && advancementOriginal.expertise.Contains(expertise)) continue;
                        int selectedExpertise = expertise;
                        bool selected = draft.expertise.Contains(expertise);
                        var button = ui.Button(row, L(expertise < 0 ? "Злодійські інструменти" : CharacterCreationRules.SkillNames[expertise]), () => {
                            if (draft.expertise.Contains(selectedExpertise)) draft.expertise.Remove(selectedExpertise);
                            else if (draft.expertise.Count < expertiseCount) draft.expertise.Add(selectedExpertise);
                            Draw(false);
                        }, selected, 60, showCheck: true);
                        if (!selected && draft.expertise.Count == expertiseCount) button.interactable = false;
                    }
                }
            }
            if (draft.characterClass == "barbarian" && draft.edition == RulesEdition.Rules2024 && draft.level >= 3)
                DropdownInt("Первісне знання — додаткова навичка", CharacterCreationRules.BarbarianSkills.Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x) && !CharacterSpeciesRules.GrantedSkills(draft).Contains(x)).ToArray(),
                    draft.primalKnowledgeSkill, value => draft.primalKnowledgeSkill = value, x => CharacterCreationRules.SkillNames[x], 300);
            if (draft.species == "human" && draft.edition == RulesEdition.Rules2024)
            {
                if (!IsLevelUp) ui.Text(content, "Додаткова навичка людини", 25, CreationWizardUi.Gold, 44);
                DropdownInt("Навичка людини", Enumerable.Range(0, 18).Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x) && x != draft.primalKnowledgeSkill && !CharacterRangerRules.FeySkills(draft).Contains(x) && !CharacterBardRules.ExtraSkills(draft).Contains(x)).ToArray(),
                    draft.humanSkill, value => draft.humanSkill = value, x => CharacterCreationRules.SkillNames[x], 300);
            }
            if (CharacterRangerRules.Is(draft, "feywanderer"))
                DropdownInt("Навичка Мандрівника фей", CharacterRangerRules.FeySkillOptions(draft), draft.rangerFeySkill, value => draft.rangerFeySkill = value, i => CharacterCreationRules.SkillNames[i], 300);
            if (!IsLevelUp) ui.Text(content, "Мови й інструменти", 26, CreationWizardUi.Gold, 45);
            if (CharacterLegacySpeciesRules.HalfElf(draft) || CharacterElfRules.Elf(draft) && !CharacterElfRules.Modern(draft) && draft.speciesLineage == "high")
            {
                string[] extraLanguages = { "Дварфська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча", "Небесна", "Інфернальна", "Безодня", "Первісна", "Сильванська", "Підземна" };
                DropdownLanguage(draft.species == "halfelf" ? "Мова напівельфа" : "Мова високого ельфа", extraLanguages, draft.speciesLanguage, value => draft.speciesLanguage = value, 280);
            }
            Note(L("Уже відомі мови:") + " " + string.Join(", ", CharacterSpeciesRules.AutomaticLanguages(draft).Select(L)), 45);
            if (!drawingSecondaryTrack && CharacterSpeciesRules.NeedsExtraLanguage(draft))
            {
                string[] languages = { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" };
                DropdownLanguage("Мова", languages, draft.language, value => draft.language = value);
                if (draft.edition == RulesEdition.Rules2024) DropdownLanguage("Друга мова", languages, draft.secondLanguage, value => draft.secondLanguage = value);
            }
            if (draft.edition == RulesEdition.Rules2024 && draft.characterClass == "rogue")
                DropdownLanguage("Мова плута", new[] { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" }, draft.rogueLanguage, value => draft.rogueLanguage = value);
            if ((!IsLevelUp || drawingSecondaryTrack && advancementOriginal.level < 1) && draft.characterClass == "ranger" && draft.edition == RulesEdition.Rules2014)
                DropdownLanguage("Мова обраного ворога (якщо є)", new[] { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча", "Небесна", "Інфернальна", "Безодня", "Первісна", "Сильванська", "Підземна", "Глибинна мова" }, draft.rangerLanguage, value => draft.rangerLanguage = value);
            if (CharacterRangerRules.Active(draft)) {
                string[] languages = { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча", "Небесна", "Інфернальна", "Безодня", "Первісна", "Сильванська", "Підземна", "Глибинна мова" };
                if (CharacterRangerRules.Revised(draft) && draft.level >= 2) for (int i = 0; i < 2; i++) {
                    int slot = i; DropdownLanguage(LF("Мова дослідника {0}", i + 1), languages, draft.rangerDeftLanguages[slot], value => draft.rangerDeftLanguages[slot] = value, 280);
                }
                if (!CharacterRangerRules.Revised(draft)) for (int i = IsLevelUp ? Math.Max(1, CharacterRangerRules.EnemyCount(advancementOriginal)) : 1; i < CharacterRangerRules.EnemyCount(draft); i++) {
                    int slot = i - 1; DropdownLanguage(LF("Мова ворога {0} (якщо є)", i + 1), languages, draft.rangerExtraLanguages[slot], value => draft.rangerExtraLanguages[slot] = value, 280);
                }
            }
            if (CharacterBackgroundRules.NeedsGaming(draft)) DropdownText("Ігровий набір", new[] { "Гральні кості", "Карти", "Шахи" }, draft.gamingSet, value => draft.gamingSet = value);
            if (CharacterSpeciesRules.IsDwarf(draft) && draft.edition == RulesEdition.Rules2014) DropdownText("Ремесло дварфа", new[] { "Інструменти коваля", "Приладдя пивовара", "Інструменти каменяра" }, draft.artisanTool, value => draft.artisanTool = value);
            if (CharacterMonkSubclassRules.Is(draft, "mercy"))
            {
                if (draft.mercySkillReplacements == null || draft.mercySkillReplacements.Length != 2) draft.mercySkillReplacements = new[] { -1, -1 };
                var fixedSkills = new[] { 6, 9 };
                for (int i = 0; i < 2; i++)
                {
                    int slot = i;
                    if (!CharacterCreationRules.BackgroundSkills(draft).Concat(CharacterSpeciesRules.GrantedSkills(draft)).Contains(fixedSkills[i])) continue;
                    int[] used = CharacterCreationRules.BackgroundSkills(draft).Concat(CharacterSpeciesRules.GrantedSkills(draft)).Concat(draft.classSkills).Concat(new[] { fixedSkills[1 - i], draft.mercySkillReplacements[1 - i] }).ToArray();
                    DropdownInt("Заміна навички Милосердя", Enumerable.Range(0, 18).Except(used).ToArray(), draft.mercySkillReplacements[i], value => draft.mercySkillReplacements[slot] = value, value => CharacterCreationRules.SkillNames[value], 250);
                }
            }
            if (!drawingSecondaryTrack && draft.characterClass == "monk") DropdownText("Інструмент монаха", CharacterCreationRules.MonkTools.Where(tool=>draft.species!="satyr"||tool!=draft.expandedInstrument).ToArray(), draft.monkTool, value => draft.monkTool = value);
            if (CharacterClericRules.Classic(draft))
            {
                if (draft.subclass == "knowledge")
                {
                    while (draft.knowledgeSkills.Count < 2) draft.knowledgeSkills.Add(-1);
                    for (int i = 0; i < 2; i++)
                    {
                        int slot = i;
                        int[] choices = CharacterClericRules.KnowledgeOptions.Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x) && (x == draft.knowledgeSkills[slot] || !draft.knowledgeSkills.Contains(x))).ToArray();
                        DropdownInt(LF("Навичка домену Знання {0}", i + 1), choices, draft.knowledgeSkills[i], value => draft.knowledgeSkills[slot] = value, x => CharacterCreationRules.SkillNames[x], 260);
                    }
                    while (draft.knowledgeLanguages.Count < 2) draft.knowledgeLanguages.Add("");
                    string[] domainLanguages = { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" };
                    for (int i = 0; i < 2; i++)
                    {
                        int slot = i;
                        string[] choices = domainLanguages.Where(x => x != draft.language && x != draft.speciesLanguage && x != CharacterSpeciesRules.FixedLanguage(draft) && (x == draft.knowledgeLanguages[slot] || !draft.knowledgeLanguages.Contains(x))).ToArray();
                        DropdownLanguage(LF("Мова домену Знання {0}", i + 1), choices, draft.knowledgeLanguages[i], value => draft.knowledgeLanguages[slot] = value, 260);
                    }
                }
                if (draft.subclass == "nature") DropdownInt("Навичка домену Природа", CharacterClericRules.NatureOptions.Where(x => !bg.Contains(x) && !draft.classSkills.Contains(x)).ToArray(), draft.natureSkill, value => draft.natureSkill = value, x => CharacterCreationRules.SkillNames[x], 260);
            }
            if (draft.characterClass == "bard")
            {
                while (draft.bardInstruments.Count < (drawingSecondaryTrack ? 1 : 3)) draft.bardInstruments.Add("");
                for (int i = 0; i < (drawingSecondaryTrack ? 1 : 3); i++)
                {
                    int slot = i;
                    string[] instrumentOptions = CharacterCreationRules.MusicalInstruments.Where(name => (name == draft.bardInstruments[slot] || !draft.bardInstruments.Contains(name))&&(draft.species!="satyr"||name!=draft.expandedInstrument)).ToArray();
                    DropdownKey(LF("Музичний інструмент {0}", i + 1), instrumentOptions, instrumentOptions.Select(L).ToArray(), draft.bardInstruments[i], value => draft.bardInstruments[slot] = value, 260);
                }
            }
            var origin = CharacterBackgroundRules.Find(draft);
            if (origin != null)
            {
                string[] toolChoices = CharacterBackgroundRules.ToolOptions(draft).Except(CharacterBackgroundRules.ClassTools(draft)).ToArray();
                if (toolChoices.Length > 0) DropdownText("Інструмент передісторії", toolChoices, draft.backgroundTool, value => draft.backgroundTool = value);
                string[] duplicateTools = CharacterBackgroundRules.DuplicateTools(draft);
                for (int i = 0; i < duplicateTools.Length; i++)
                {
                    int slot = i;
                    string[] replacements = CharacterBackgroundRules.AllTools.Except(CharacterBackgroundRules.ClassTools(draft)).Except(origin.tools).Where(tool => tool != CharacterBackgroundRules.ToolChoice(draft) && !draft.backgroundToolReplacements.Where((_, index) => index != slot).Contains(tool)).ToArray();
                    DropdownText("Заміна повторного інструмента", replacements, draft.backgroundToolReplacements[slot], value => draft.backgroundToolReplacements[slot] = value);
                }
                for (int i = 0; i < origin.languages; i++)
                {
                    int slot = i;
                    string[] languages = new[] { "Дварфська", "Ельфійська", "Гномська", "Гоблінська", "Велетенська", "Оркська", "Галфлінгська", "Драконяча" }.Except(CharacterBackgroundRules.BaseLanguages(draft)).Where(language => language != draft.backgroundLanguages[1 - slot]).ToArray();
                    DropdownLanguage(i == 0 ? "Перша мова передісторії" : "Друга мова передісторії", languages, draft.backgroundLanguages[slot], value => draft.backgroundLanguages[slot] = value);
                }
            }
        }
        private void DropdownInt(string label, int[] values, int value, Action<int> change, Func<int, string> display, float width = 160)
        {
            if (!ShowLevelChoice(label, values.Length > 0 && !values.Contains(value))) return;
            ui.Text(content, label, 22, CreationWizardUi.Gold, 36);
            var choices = new[] { "—" }.Concat(values.Select(display)).ToArray();
            ui.Dropdown(content, label, choices, Array.IndexOf(values, value) + 1,
                choice => { change(choice == 0 ? -1 : values[choice - 1]); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, width, 42);
        }
        private void DropdownText(string label, string[] values, string value, Action<string> change)
        {
            if (!ShowLevelChoice(label, values.Length > 0 && (string.IsNullOrEmpty(value) || !values.Contains(value)))) return;
            ui.Text(content, label, 22, CreationWizardUi.Gold, 36);
            var choices = new[] { "—" }.Concat(values).ToArray();
            ui.Dropdown(content, label, choices, Array.IndexOf(values, value) + 1,
                choice => { change(choice == 0 ? "" : values[choice - 1]); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); });
        }
        private void DropdownLanguage(string label, string[] candidates, string value, Action<string> change, float width = 300)
        {
            string[] choices = CharacterSpeciesRules.LanguageOptions(draft, candidates, value);
            DropdownKey(label, choices, choices.Select(L).ToArray(), value, change, width);
        }
        private void DropdownKey(string label, string[] keys, string[] names, string value, Action<string> change, float width, Transform parent = null)
        {
            if (!ShowLevelChoice(label, keys.Length > 0 && (string.IsNullOrEmpty(value) || !keys.Contains(value)))) return;
            parent = parent != null ? parent : content;
            ui.Text(parent, label, 22, CreationWizardUi.Gold, 36);
            var choices = new[] { "—" }.Concat(names).ToArray();
            ui.Dropdown(parent, label, choices, Array.IndexOf(keys, value) + 1,
                choice => { change(choice == 0 ? "" : keys[choice - 1]); CharacterCreationRules.RepairDependentChoices(draft); Draw(false); }, width, 42);

        }
        private static string HighlightChoice(string label)
        {
            int colon = label.IndexOf(':');
            return colon < 0 ? label : label.Substring(0, colon + 1) + " <size=115%><color=#FF9A00>" + label.Substring(colon + 1).Trim() + "</color></size>";

        }
        private void EquipmentPage()
        {
            if(CharacterNaturalArmor.Active(draft))
            {
                if(draft.species=="tortle")Note(CharacterExpandedSpeciesRules.Text("ui.shellarmor"));
                else if(CharacterNaturalArmor.StarterArmor(draft))DropdownInt(CharacterExpandedSpeciesRules.Text("ui.armorwear"),new[]{0,1},draft.expandedUnarmored?1:0,value=>draft.expandedUnarmored=value==1,value=>CharacterExpandedSpeciesRules.Text(value==0?"ui.wearing":"ui.unarmored"),300);
            }
            if (!drawingSecondaryTrack && ((int)draft.edition < 0 || string.IsNullOrEmpty(draft.background)))
            { Note("Спочатку обери редакцію та передісторію."); return; }
            if (draft.characterClass == "fighter")
            {
                if (!IsLevelUp) ui.Text(content, "Бойовий стиль", 25, CreationWizardUi.Gold, 40);
                DropdownInt("Бойовий стиль", CharacterFighterRules.StyleOptions(draft), (int)draft.fightingStyle,
                    value => { draft.fightingStyle = (FighterStyle)value; if (draft.fighterSecondStyle == value) draft.fighterSecondStyle = -1; }, i => CharacterFighterRules.StyleNames[i], 280);
            }

            if (CharacterRangerRules.Active(draft) && draft.level >= 2)
                DropdownInt("Бойовий стиль", CharacterRangerRules.StyleOptions(draft), (int)draft.fightingStyle, value => { draft.fightingStyle = (FighterStyle)value; draft.cantrips.Clear(); }, CharacterRangerRules.StyleName, 280);
            if (CharacterPaladinRules.Active(draft) && draft.level >= 2)
                DropdownInt("Бойовий стиль", CharacterPaladinRules.StyleOptions(draft), (int)draft.fightingStyle, value => { draft.fightingStyle = (FighterStyle)value; draft.cantrips.Clear(); }, CharacterPaladinRules.StyleName, 280);

            if (CharacterFighterRules.SecondStyle(draft))
                DropdownInt("Другий бойовий стиль", CharacterFighterRules.StyleOptions(draft).Where(i => i != (int)draft.fightingStyle).ToArray(), draft.fighterSecondStyle, value => draft.fighterSecondStyle = value, i => CharacterFighterRules.StyleNames[i], 260);
            if (CharacterFighterRules.Is(draft, "battlemaster"))
            {
                DropdownKey("Ремісничий інструмент Майстра бою", CharacterCreationRules.MonkTools.Take(17).ToArray(), CharacterCreationRules.MonkTools.Take(17).Select(L).ToArray(), draft.fighterTool, value => draft.fighterTool = value, 280);
                if (draft.edition == RulesEdition.Rules2024)
                    DropdownInt("Навичка Майстра бою", CharacterFighterRules.StudentSkills(draft), draft.fighterSkill, value => draft.fighterSkill = value, i => CharacterCreationRules.SkillNames[i], 260);
                if (!IsLevelUp) ui.Text(content, "Маневри Майстра бою", 25, CreationWizardUi.Gold, 42);
                int required = CharacterFighterRules.ManeuverCount(draft);
                while (draft.fighterManeuvers.Count < required) draft.fighterManeuvers.Add("");
                if (draft.fighterManeuvers.Count > required) draft.fighterManeuvers.RemoveRange(required, draft.fighterManeuvers.Count - required);
                for (int i = 0; i < required; i++)
                {
                    int slot = i;
                    var ids = CharacterFighterManeuvers.Options(draft).Where(id => id == draft.fighterManeuvers[slot] || !draft.fighterManeuvers.Contains(id)).ToArray();
                    DropdownKey(LF("Маневр {0}", i + 1), ids, ids.Select(id => L(CharacterFighterManeuvers.Name(draft, id))).ToArray(), draft.fighterManeuvers[slot], value => draft.fighterManeuvers[slot] = value, 290);
                }
            }
            if (!IsLevelUp && !drawingSecondaryTrack)
            {
            ui.Text(content, "Набір спорядження", 25, CreationWizardUi.Gold, 42);
            bool modernFighter = draft.characterClass == "fighter" && draft.edition == RulesEdition.Rules2024;
            var kitFrame = modernFighter || (draft.characterClass == "bard" || draft.characterClass == "druid" || draft.characterClass == "cleric" || draft.characterClass == "wizard" || draft.characterClass == "sorcerer" || draft.characterClass == "warlock" || draft.characterClass == "artificer" || draft.characterClass == "bloodhunter") && draft.edition == RulesEdition.Rules2014 ? null : FramedChoiceGroup("EquipmentKitFrame", compact ? 268 : 150);
            if (draft.characterClass == "bloodhunter")
            {
                if(draft.level>=2)DropdownInt(CharacterBloodHunterRules.Text("ui.style"),CharacterBloodHunterRules.Styles,(int)draft.fightingStyle,value=>draft.fightingStyle=(FighterStyle)value,value=>CharacterFighterRules.StyleNames[value],280);
                DropdownInt("Набір зброї мисливця крові", new[] { 0, 1 }, draft.bloodWeaponMode, value => { draft.bloodWeaponMode = value; draft.bloodWeapon = ""; draft.bloodSecondWeapon = ""; draft.equipment = value >= 0 ? 0 : -1; }, value => value == 0 ? "Одна військова зброя" : "Дві прості зброї", 280);
                if (draft.bloodWeaponMode >= 0)
                {
                    var weapons = draft.bloodWeaponMode == 0 ? CharacterBloodHunterRules.MartialWeapons : CharacterCreationRules.MonkSimpleWeapons;
                    DropdownKey("Зброя мисливця крові", weapons, weapons.Select(L).ToArray(), draft.bloodWeapon, value => draft.bloodWeapon = value, 260);
                    if (draft.bloodWeaponMode == 1) DropdownKey("Друга зброя мисливця крові", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.bloodSecondWeapon, value => draft.bloodSecondWeapon = value, 260);
                }
                string[] armor = { "Проклепана шкіра", "Лускатий обладунок" };
                DropdownKey("Обладунок мисливця крові", armor, armor.Select(L).ToArray(), draft.bloodArmor, value => draft.bloodArmor = value, 280);
            }
            else if (draft.characterClass == "artificer")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Проклепана шкіра • кинджал • інструменти • набір підземельника"), L("НАБІР B") + "\n" + L("150 зм замість класового спорядження."), draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    DropdownKey("Зброя винахідника", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.artificerWeapon, value => { draft.artificerWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    DropdownKey("Друга зброя винахідника", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.artificerSecondWeapon, value => draft.artificerSecondWeapon = value, 260);
                    string[] armor = { "Проклепана шкіра", "Лускатий обладунок" };
                    DropdownKey("Обладунок винахідника", armor, armor.Select(L).ToArray(), draft.artificerArmor, value => draft.artificerArmor = value, 280);
                }
            }
            else if (draft.characterClass == "warlock")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Шкіра • серп • кинджали • сфера • набір ученого"), L("НАБІР B") + "\n" + L("100 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    DropdownKey("Зброя чаклуна", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.warlockWeapon, value => { draft.warlockWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    DropdownKey("Друга зброя чаклуна", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.warlockSecondWeapon, value => draft.warlockSecondWeapon = value, 260);
                    string[] focus = { "Сумка компонентів", "Арканний фокус" };
                    DropdownKey("Фокус чаклуна", focus, focus.Select(L).ToArray(), draft.warlockFocus, value => draft.warlockFocus = value, 260);
                    DropdownInt("Набір чаклуна", new[] { 0, 1 }, draft.warlockPack, value => draft.warlockPack = value, value => value == 0 ? "Набір ученого" : "Набір підземельника", 260);
                }
                if (CharacterWarlockRules.Blade(draft)) DropdownInt("Зброя договору", Enumerable.Range(0, CharacterWarlockRules.PactWeapons.Length).ToArray(), draft.pactWeapon, value => draft.pactWeapon = value, x => CharacterWarlockRules.PactWeapons[x], 320);
            }
            else if (draft.characterClass == "sorcerer")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Спис • кинджали • кристал • набір підземельника"), L("НАБІР B") + "\n" + L("50 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    DropdownKey("Зброя чародія", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.sorcererWeapon, value => { draft.sorcererWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    if (!string.IsNullOrEmpty(draft.sorcererWeapon) && !CharacterSorcererRules.WeaponProficient(draft, draft.sorcererWeapon)) Note("Чародій не володіє обраною зброєю: бонус майстерності до атаки не додається.", 65);
                    string[] focus = { "Сумка компонентів", "Арканний фокус" };
                    DropdownKey("Фокус чародія", focus, focus.Select(L).ToArray(), draft.sorcererFocus, value => draft.sorcererFocus = value, 260);
                    DropdownInt("Набір чародія", new[] { 0, 1 }, draft.sorcererPack, value => draft.sorcererPack = value, value => value == 0 ? "Набір підземельника" : "Набір мандрівника", 260);
                }
            }
            else if (draft.characterClass == "wizard")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Кинджали • посох • мантія • книга • набір ученого"), L("НАБІР B") + "\n" + L("55 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    string[] weapons = { "Бойовий посох", "Кинджал" };
                    DropdownKey("Зброя чарівника", weapons, weapons.Select(L).ToArray(), draft.wizardWeapon, value => { draft.wizardWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    string[] focus = { "Сумка компонентів", "Арканний фокус" };
                    DropdownKey("Фокус чарівника", focus, focus.Select(L).ToArray(), draft.wizardFocus, value => draft.wizardFocus = value, 260);
                    DropdownInt("Набір чарівника", new[] { 0, 1 }, draft.wizardPack, value => draft.wizardPack = value, value => value == 0 ? "Набір ученого" : "Набір мандрівника", 260);
                }
            }
            else if (draft.characterClass == "cleric")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Кольчужна сорочка • щит • булава • набір жерця"), L("НАБІР B") + "\n" + L("110 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    string[] weapons = CharacterClericRules.Warhammer(draft) ? new[] { "Булава", "Бойовий молот" } : new[] { "Булава" };
                    DropdownKey("Основна зброя жерця", weapons, weapons.Select(L).ToArray(), draft.clericWeapon, value => { draft.clericWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    string[] armors = CharacterClericRules.Armors(draft);
                    DropdownKey("Обладунок жерця", armors, armors.Select(L).ToArray(), draft.clericArmor, value => draft.clericArmor = value, 260);
                    DropdownKey("Друга зброя жерця", CharacterCreationRules.MonkSimpleWeapons, CharacterCreationRules.MonkSimpleWeapons.Select(L).ToArray(), draft.clericSecondWeapon, value => draft.clericSecondWeapon = value, 260);
                    DropdownInt("Набір жерця", new[] { 0, 1 }, draft.clericPack, value => draft.clericPack = value, value => value == 0 ? "Набір жерця" : "Набір мандрівника", 260);
                }
            }
            else if (draft.characterClass == "druid")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Шкіра • щит • серп • посох • набір травника"), L("НАБІР B") + "\n" + L("50 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    DropdownKey("Щит або проста зброя", CharacterCreationRules.DruidFirstItems, CharacterCreationRules.DruidFirstItems.Select(L).ToArray(), draft.druidFirstItem,
                        value => { draft.druidFirstItem = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    DropdownKey("Основна зброя друїда", CharacterCreationRules.DruidWeapons, CharacterCreationRules.DruidWeapons.Select(L).ToArray(), draft.druidWeapon, value => draft.druidWeapon = value, 260);
                }
            }
            else if (draft.characterClass == "bard")
            {
                if (draft.edition == RulesEdition.Rules2024)
                    Choose(L("НАБІР A") + "\n" + L("Шкіра • 2 кинджали • інструмент • набір артиста"), L("НАБІР B") + "\n" + L("90 зм замість класового спорядження."),
                        draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
                else
                {
                    DropdownKey("Зброя барда", CharacterCreationRules.BardWeapons, CharacterCreationRules.BardWeapons.Select(L).ToArray(), draft.bardWeapon,
                        value => { draft.bardWeapon = value; draft.equipment = value.Length > 0 ? 0 : -1; }, 260);
                    DropdownInt("Набір барда", new[] { 0, 1 }, draft.bardPack, value => draft.bardPack = value,
                        value => value == 0 ? "Набір дипломата" : "Набір артиста", 260);
                }
                if (draft.edition == RulesEdition.Rules2014 || draft.equipment == 0)
                    DropdownKey("Інструмент у спорядженні", CharacterCreationRules.MusicalInstruments, CharacterCreationRules.MusicalInstruments.Select(L).ToArray(), draft.bardCarriedInstrument, value => draft.bardCarriedInstrument = value, 260);
            }
            else if (draft.characterClass == "rogue")
                Choose(draft.edition == RulesEdition.Rules2024 ? "НАБІР A\nШкіра • кинджали • короткий меч і лук • злодійські інструменти" : "НАБІР A\nРапіра • короткий лук • шкіра • злодійські інструменти",
                    draft.edition == RulesEdition.Rules2024 ? "НАБІР B\n100 зм замість класового спорядження" : "НАБІР B\nДва короткі мечі • набір мандрівника • шкіра • злодійські інструменти",
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            else if (draft.characterClass == "monk")
                Choose(draft.edition == RulesEdition.Rules2024 ? "НАБІР A\nСпис • 5 кинджалів • інструмент • набір мандрівника" : "Короткий меч",
                    draft.edition == RulesEdition.Rules2024 ? "НАБІР B\n50 зм замість класового спорядження" : "Проста зброя",
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            else if (draft.characterClass == "barbarian")
                Choose(draft.edition == RulesEdition.Rules2024 ? "НАБІР A\nСокира дворучна • 4 ручні сокири • набір мандрівника • 15 зм" : "СОКИРА\nДворучна сокира • 2 ручні сокири • 4 метальні списи",
                    draft.edition == RulesEdition.Rules2024 ? "НАБІР B\n75 зм замість класового спорядження" : "МЕЧ\nДовгий меч • 2 ручні сокири • 4 метальні списи",
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            else if (draft.characterClass == "paladin")
                Choose(draft.edition == RulesEdition.Rules2024 ? L("НАБІР A") + "\n" + L("Кольчуга • щит • довгий меч • 6 списів") : L("МЕЧ І ЩИТ") + "\n" + L("Кольчуга • довгий меч • щит • набір жерця"),
                    draft.edition == RulesEdition.Rules2024 ? L("НАБІР B") + "\n" + L("150 зм замість класового спорядження") : L("ДВА МЕЧІ") + "\n" + L("Кольчуга • два довгі мечі • набір мандрівника"),
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            else if (draft.characterClass == "ranger")
                Choose(draft.edition == RulesEdition.Rules2024 ? L("НАБІР A") + "\n" + L("Проклепана шкіра • скімітар • короткий меч • довгий лук") : L("ЛУСКАТИЙ ОБЛАДУНОК") + "\n" + L("Два короткі мечі • довгий лук • набір підземельника"),
                    draft.edition == RulesEdition.Rules2024 ? L("НАБІР B") + "\n" + L("150 зм замість класового спорядження") : L("ШКІРЯНИЙ ОБЛАДУНОК") + "\n" + L("Два короткі мечі • довгий лук • набір мандрівника"),
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            else if (modernFighter)
            {
                var labels = new[] { "Кольчуга й дворучний меч", "Проклепана шкіра й довгий лук", "155 зм" };
                DropdownInt("Стартовий набір воїна", new[] { 0, 1, CharacterFighterRules.GoldOnlyEquipment },
                    draft.equipment, value => draft.equipment = value, index => labels[index], 380);
            }
            else
                Choose("БЛИЖНІЙ БІЙ\nКольчуга • меч і щит • 2 сокири", "ЛУЧНИК\nШкіра • довгий лук • меч і щит",
                    draft.equipment < 0 ? null : draft.equipment == 0, value => draft.equipment = value ? 0 : 1, 120, 4, kitFrame);
            if (draft.characterClass == "monk" && draft.edition == RulesEdition.Rules2014)
            {
                if (draft.equipment == 1) DropdownText("Проста зброя монаха", CharacterCreationRules.MonkSimpleWeapons, draft.monkWeapon, value => draft.monkWeapon = value);
                ui.Text(content, "Набір монаха", 25, CreationWizardUi.Gold, 42);
                Choose("Набір підземельника", "Набір мандрівника", draft.monkPack < 0 ? null : draft.monkPack == 0,
                    value => draft.monkPack = value ? 0 : 1, 66);
            }
            }
            if ((IsLevelUp || drawingSecondaryTrack) && CharacterWarlockRules.Blade(draft))
                DropdownInt("Зброя договору", Enumerable.Range(0, CharacterWarlockRules.PactWeapons.Length).ToArray(), draft.pactWeapon, value => draft.pactWeapon = value, x => CharacterWarlockRules.PactWeapons[x], 320);
            if ((IsLevelUp || drawingSecondaryTrack) && CharacterBloodHunterRules.Active(draft) && draft.level >= 2)
                DropdownInt(CharacterBloodHunterRules.Text("ui.style"), CharacterBloodHunterRules.Styles, (int)draft.fightingStyle, value => draft.fightingStyle = (FighterStyle)value, value => CharacterFighterRules.StyleNames[value], 280);
            if (draft.edition == RulesEdition.Rules2024 && CharacterCreationRules.MasteryCount(draft) > 0)
            {
                int required = CharacterCreationRules.MasteryCount(draft);
                if (!IsLevelUp) ui.Text(content, "Майстерність зброї", 25, CreationWizardUi.Gold, 42);
                while (draft.weaponMasteries.Count < required) draft.weaponMasteries.Add(-1);
                Note(LF("Обери види зброї: {0}/{1}.", draft.weaponMasteries.Count(x => x >= 0), required), 42);
                int first = IsLevelUp ? advancementOriginal.weaponMasteries?.Count ?? 0 : 0;
                for (int i = first; i < required; i++)
                {
                    int slot = i;
                    int[] options = CharacterCreationRules.MasteryOptions(draft).Where(x => x == draft.weaponMasteries[slot] || !draft.weaponMasteries.Contains(x)).ToArray();
                    string label = IsLevelUp && required - first == 1
                        ? "Нова майстерність зброї" : LF("Майстерність зброї {0}", i + 1);
                    DropdownInt(label, options, draft.weaponMasteries[i], value => draft.weaponMasteries[slot] = value, x => CharacterCreationRules.MasteryWeapons[x], 260);
                }
            }
            if(CharacterBardRules.SecretCount(draft)>0) {
                ui.Text(content,CharacterBardText.Get("ui.secrets"),25,CreationWizardUi.Gold,42);
                Note(CharacterBardText.Get("ui.secret.note"),70);
                while(draft.bardSecrets.Count<CharacterBardRules.SecretCount(draft))draft.bardSecrets.Add("");
                RectTransform secretRow = null;
                for(int i=0;i<draft.bardSecrets.Count;i++) {
                    int slot=i;
                    var choices=CharacterSpellCatalog.BardSecrets(draft,slot).Where(s=>s.name==draft.bardSecrets[slot]||!draft.bardSecrets.Contains(s.name)).ToArray();
                    DropdownKey(LF(CharacterBardText.Get("ui.secret"),i+1),choices.Select(s=>s.name).ToArray(),choices.Select(s=>s.level+" · "+L(s.name)).ToArray(),draft.bardSecrets[slot],value=>draft.bardSecrets[slot]=value,320,SpellChoiceHost(i,ref secretRow));
                }
            }
            if (CharacterSorcererRules.Active(draft) && CharacterSorcererRules.MetamagicCount(draft) > 0) {
                int count = CharacterSorcererRules.MetamagicCount(draft);
                while (draft.sorcererMetamagic.Count < count) draft.sorcererMetamagic.Add("");
                for (int i = 0; i < count; i++) {
                    int slot = i;
                    string[] options = CharacterSorcererRules.MetamagicOptions(draft).Where(id => id == draft.sorcererMetamagic[slot] || !draft.sorcererMetamagic.Contains(id)).ToArray();
                    DropdownKey(LF("Метамагія {0}", i + 1), options, options.Select(id => L(CharacterSorcererRules.MetamagicText(draft, id).Split(':')[0])).ToArray(), draft.sorcererMetamagic[slot], value => draft.sorcererMetamagic[slot] = value, 290);
                    if (CharacterSorcererRules.MetamagicOptions(draft).Contains(draft.sorcererMetamagic[slot])) Note(CharacterSorcererRules.MetamagicText(draft, draft.sorcererMetamagic[slot]));
                }
            }
            if (CharacterArtificerRules.Active(draft)) ArtificerItemPickers();
            if (CharacterCreationRules.SpellChoiceCount(draft) > 0)
            {
                string[] domainSpells = CharacterClericRules.DomainSpells(draft).Concat(CharacterPaladinRules.AutomaticSpells(draft)).Concat(CharacterRangerRules.AutomaticSpells(draft)).Concat(CharacterSorcererRules.AutomaticSpells(draft)).Concat(CharacterBardRules.AutomaticSpells(draft)).Concat(CharacterWarlockRules.AutomaticSpells(draft)).ToArray();
                if (!IsLevelUp && domainSpells.Length > 0) Note(L("Завжди підготовлені:") + " " + string.Join(", ", domainSpells.Select(L)), 64);
                if (!IsLevelUp && CharacterClericRules.Classic(draft) && draft.subclass == "light") Note(L("Додатковий заговір:") + " " + L("Light"), 42);
                if (CharacterWizardRules.Active(draft))
                {
                    SpellPickers(draft.spellbookSpells, 1, CharacterWizardRules.BookCount(draft), true);
                    DrawWizardChoices();
                }
                if (CharacterFighterRules.Caster(draft)) Note(draft.edition == RulesEdition.Rules2014 ? "Обери спели чарівника: переважно школи огородження й втілення, з винятками та законними замінами." : "Обери спели чарівника доступних кругів; базова характеристика — Інтелект.");
                if (CharacterTricksterSpells.Active(draft)) Note(draft.edition == RulesEdition.Rules2014 ? "Магічна рука надана підкласом. Списки враховують школи чарування й ілюзії, винятки та законні заміни при підвищенні рівня." : "Магічна рука надана підкласом. Обери спели чарівника доступних кругів; базова характеристика — Інтелект.");
                int preparedCount = CharacterCreationRules.SpellChoiceCount(draft);
                if (IsLevelUp && !drawingSecondaryTrack && CharacterArtificerRules.Active(draft) && CharacterArtificerRules.Revised(draft))
                    preparedCount = Math.Min(preparedCount, CharacterArtificerRules.SpellCount(advancementOriginal));
                SpellPickers(draft.preparedSpells, 1, preparedCount);
                if (CharacterCreationRules.CantripCount(draft) > 0) SpellPickers(draft.cantrips, 0, CharacterCreationRules.CantripCount(draft));
                if (CharacterClericRules.Classic(draft) && draft.subclass == "nature")
                {
                    string[] names = CharacterSpellCatalog.Available(draft.edition, "druid", 0).Select(x => x.name).Where(x => !draft.cantrips.Contains(x)).ToArray();
                    DropdownKey("Заговір домену Природа", names, names.Select(L).ToArray(), draft.natureCantrip, value => draft.natureCantrip = value, 320);
                }
                if (CharacterWarlockRules.Tome(draft))
                {
                    TomePickers(draft.tomeCantrips, 0, 3);
                    if(CharacterWarlockRules.Modern(draft)||CharacterWarlockRules.Has(draft,"ancient"))TomePickers(draft.tomeRituals, 1, 2);
                }
            }
            if(CharacterWarlockRules.Active(draft))DrawWarlockChoices();
            if (IsLevelUp || drawingSecondaryTrack) return;
            var originItems = CharacterBackgroundRules.Find(draft)?.items ?? Array.Empty<string>();
            if (originItems.Length > 0) DropdownText("Предмет передісторії", originItems, draft.backgroundItem, value => draft.backgroundItem = value);
            if (draft.edition == RulesEdition.Rules2024)
            {
                ui.Text(content, "Спорядження передісторії", 25, CreationWizardUi.Gold, 42);
                string pack = L("Спорядження передісторії") + "\n" + CharacterBackgroundRules.Equipment(draft, L);
                Choose(pack, "ЗОЛОТО\n50 зм замість спорядження передісторії", draft.backgroundEquipmentChoice < 0 ? null : draft.backgroundEquipmentChoice == 0,
                    value => draft.backgroundEquipmentChoice = value ? 0 : 1, 96);
            }
            Note(draft.equipment < 0 ? "Обери набір спорядження." : CharacterCreationPresentation.Equipment(draft, L), 150);
            if (draft.characterClass == "rogue") Note("Експертиза подвоює бонус майстерності вибраних навичок. Прихована атака використовує фехтувальну або далекобійну зброю.", 75);
            else if (draft.characterClass == "monk") Note("Бойові мистецтва: бонусний беззбройний удар, якщо монах без обладунку та щита.", 75);
            else if (draft.characterClass == "barbarian") Note("Варвар без обладунку має КО 10 + Спритність + Статура. Лють і спорядження перенесуться в картку.", 75);
            else if (draft.characterClass == "paladin") Note(LF("Покладання рук: запас {0} ОЗ; відновлення після довгого відпочинку.", CharacterPaladinRules.LayOnHands(draft)), 75);
            else if (draft.characterClass == "ranger") Note(CharacterRangerRules.Revised(draft) ? CharacterRangerText.Get("mark") : CharacterRangerText.Get("primeval"), 75);
            else if (draft.characterClass == "bard") Note(CharacterBardText.Get("inspiration"+(CharacterBardRules.Revised(draft)?"24":"14")), 95);
            else if (draft.characterClass == "druid") Note("Комірки відновлюються після довгого відпочинку. Базова характеристика магії — Мудрість.", 75);
            else if (draft.characterClass == "cleric") Note("Комірки відновлюються після довгого відпочинку. Базова характеристика магії — Мудрість.", 75);
            else if (draft.characterClass == "wizard") Note(CharacterWizardText.Get("ritual"), 75);
            else if (draft.characterClass == "sorcerer") Note("Комірки відновлюються після довгого відпочинку. Базова характеристика магії — Харизма.", 75);
            else if (draft.characterClass == "warlock") Note(LF(CharacterWarlockRules.Text("pactmagic"),CharacterWarlockRules.SlotCount(draft),CharacterWarlockRules.SlotCircle(draft)),75);
            else if (draft.characterClass == "artificer") Note("Комірки відновлюються після довгого відпочинку. Базова характеристика магії — Інтелект.", 75);
            else if (draft.characterClass == "bloodhunter") Note(CharacterBloodHunterRules.Text("profane.focus"),75);
            else Note("КО враховує надягнений набір: у лучника щит лежить у спорядженні. Стиль діє лише за своїх умов. Перелік речей буде записаний у картку; окремі комірки інвентарю — наступний етап.", 110);
            if (draft.characterClass == "fighter" && ((draft.fightingStyle == FighterStyle.Archery && draft.equipment == 0 ||
                draft.fightingStyle == FighterStyle.GreatWeapon && draft.equipment == 1 ||
                draft.fightingStyle == FighterStyle.Dueling && draft.edition == RulesEdition.Rules2024 && draft.equipment == 0)))
                Note("Увага: у вибраному наборі немає зброї, з якою цей бойовий стиль діятиме одразу. Стиль залишиться доступним для іншої зброї.", 88);
            foreach (var feat in CharacterOriginFeatRules.Choices(draft)) DrawOriginFeat(feat);
            if (draft.secondaryClass?.IsSelected == true)
            {
                var secondary = draft.secondaryClass;
                var track = secondary.ClassDraft(draft);
                ui.Text(content, L("Мультиклас") + " · " + L(CharacterClassCatalog.Find(track.characterClass).name),
                    26, CreationWizardUi.Gold, 46);
                DrawSecondaryTrackChoices(secondary, track, 0);
            }
        }
        private void DrawOriginFeat(CharacterOriginFeatChoice choice)
        {
            ui.Text(content, CharacterOriginFeatRules.Name(choice.id), 26, CreationWizardUi.Gold, 46);
            string feature = CharacterOriginFeatRules.Feature(choice.id);
            if (feature.Length > 0) Note(L(feature), compact ? 180 : 100);
            foreach (string craftNote in CharacterOriginFeatRules.CraftNotes(choice)) Note(L(craftNote));
            if (CharacterOriginFeatRules.Magic(choice))
            {
                DropdownInt("Характеристика магії риси", new[] { 3, 4, 5 }, choice.ability, value => choice.ability = value, value => CharacterCreationRules.AbilityNames[value], 240);
                for (int i = 0; i < 2; i++)
                {
                    int slot = i;
                    var used = draft.cantrips.Concat(draft.tomeCantrips).Concat(CharacterSpeciesRules.Spells(draft)).Concat(CharacterOriginFeatRules.Choices(draft).SelectMany(feat => feat.cantrips.Where((_, index) => feat != choice || index != slot)));
                    string[] options = CharacterSpellCatalog.Available(draft.edition, CharacterOriginFeatRules.SpellClass(choice), 0).Select(spell => spell.name).Except(used).ToArray();
                    DropdownKey(LF("Заговір риси {0}", slot + 1), options, options.Select(L).ToArray(), choice.cantrips[slot], value => choice.cantrips[slot] = value, 310);
                }
                string[] spells = CharacterSpellCatalog.Available(draft.edition, CharacterOriginFeatRules.SpellClass(choice), 1).Select(spell => spell.name).ToArray();
                DropdownKey("Спел риси 1 рівня", spells, spells.Select(L).ToArray(), choice.spell, value => choice.spell = value, 310);
            }
            if (choice.id == "skilled" || choice.id == "crafter" || choice.id == "musician")
            {
                for (int i = 0; i < 3; i++)
                {
                    int slot = i;
                    string[] used = CharacterOriginFeatRules.Choices(draft).SelectMany(feat => feat.selections.Where((_, index) => feat != choice || index != slot)).ToArray();
                    string[] options = CharacterOriginFeatRules.Options(draft, choice).Except(used).ToArray();
                    string Display(string key) => key.StartsWith("skill:", StringComparison.Ordinal) ? L(CharacterCreationRules.SkillNames[int.Parse(key.Substring(6))]) : L(key.Substring(5));
                    DropdownKey(LF("Володіння риси {0}", slot + 1), options, options.Select(Display).ToArray(), choice.selections[slot], value => choice.selections[slot] = value, 310);
                }
            }
        }
        private void ArtificerItemPickers()
        {
            CharacterArtificerEquipment.Repair(draft);
            int count = CharacterArtificerRules.PlansKnown(draft);
            if (count > 0)
            {
                if (!IsLevelUp) ui.Text(content, CharacterArtificerText.Get("ui.plans"), 25, CreationWizardUi.Gold, 42);
                Note(LF(CharacterArtificerText.Get(CharacterArtificerRules.Revised(draft) ? "plans" : "infusions"), count, CharacterArtificerRules.ItemLimit(draft)), compact ? 155 : 95);
                RectTransform row = null;
                for (int i = 0; i < count; i++)
                {
                    int slot = i;
                    if (IsLevelUp && i < advancementOriginal.artificerPlans.Count &&
                        !string.IsNullOrEmpty(draft.artificerPlans[slot])) continue;
                    var options = CharacterArtificerRules.PlanOptions(draft, slot);
                    var host = SpellChoiceHost(i, ref row);
                    DropdownKey(LF(CharacterArtificerText.Get("ui.plan"), i + 1), options.Select(p => p.id).ToArray(), options.Select(p => L(p.name)).ToArray(), draft.artificerPlans[slot], value => { draft.artificerPlans[slot] = value; draft.artificerEquippedTargets[slot]=""; }, 320, host);
                    var equipmentTargets=CharacterArtificerEquipment.AvailableTargets(draft,slot);
                    if(equipmentTargets.Length>0||CharacterArtificerEquipment.Target(draft,slot).Length>0)
                    {
                        if(row != null){var sizing=row.GetComponent<UnityEngine.UI.LayoutElement>();sizing.minHeight=sizing.preferredHeight=-1;row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandHeight=false;}
                        string targetLabel = IsLevelUp
                            ? LF("Предмет для плану {0} (необов’язково)", slot + 1)
                            : CharacterDescriptionText.Get("equip.target");
                        DropdownKey(targetLabel,equipmentTargets,equipmentTargets.Select(target=>L(CharacterArtificerEquipment.Label(draft,target))+CharacterArtificerEquipment.TargetNumber(draft,target)).ToArray(),CharacterArtificerEquipment.Target(draft,slot),value=>draft.artificerEquippedTargets[slot]=value,270,host);
                        if (equipmentTargets.Contains("shield"))
                            ui.Text(host, "План створює магічний щит. Коли його споряджено, КО зростає на 2 за щит і на бонус плану.", 17);
                    }
                    if (draft.artificerPlans[slot].StartsWith("custom.", StringComparison.Ordinal))
                    {
                        // This column sizes itself; its neighbour must not stretch to match the explanation.
                        if (row != null) {
                            var sizing = row.GetComponent<UnityEngine.UI.LayoutElement>(); sizing.minHeight = sizing.preferredHeight = -1;
                            row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandHeight = false;
                        }
                        ui.Input(host, draft.artificerPlanDetails[slot], L(CharacterArtificerText.Get("ui.custom.name")), value => draft.artificerPlanDetails[slot] = value);
                        var explanation = ui.Text(host, CharacterArtificerText.Get(CharacterArtificerRules.Revised(draft) ? "ui.custom24" : "ui.custom14"), 17);
                        var explanationLayout = explanation.GetComponent<UnityEngine.UI.LayoutElement>();
                        if (explanationLayout != null) { explanationLayout.minHeight = -1; explanationLayout.preferredHeight = -1; }
                    }
                }
                Note(L(CharacterDescriptionText.Get("equip.help")),compact?180:105);
            }
            if (draft.level >= 11)
            {
                var options = CharacterSpellCatalog.ArtificerStoredSpellChoices(draft);
                DropdownKey(CharacterArtificerText.Get("ui.store"), options.Select(p => p.name).ToArray(), options.Select(p => L(p.name)).ToArray(), draft.artificerStoredSpell, value => draft.artificerStoredSpell = value, 320);
            }
        }
        private Transform SpellChoiceHost(int index, ref RectTransform row)
        {
            if (compact || IsLevelUp) return content;
            if (index % 2 == 0) {
                row = ui.Row("SpellChoices", content, 96);
                row.GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 0;
            }
            var column = ui.Column("SpellChoice", row, 6);
            var size = column.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.minWidth = 0; size.preferredWidth = 0; size.flexibleWidth = 1; size.flexibleHeight = 0;
            if (index % 2 == 0)
            {
                // Reserve the right half even for a single last choice.
                var spacer = ui.Rect("EmptySpellChoice", row);
                var space = spacer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                space.minWidth = 0; space.preferredWidth = 0; space.flexibleWidth = 1;
            }
            else UnityEngine.Object.DestroyImmediate(row.Find("EmptySpellChoice").gameObject);
            return column;
        }
        private void SpellPickers(System.Collections.Generic.List<string> chosen, int level, int count, bool bookMode = false, int firstSlot = 0)
        {
            if (!IsLevelUp) ui.Text(content, bookMode ? "Заклинання у книзі" : level == 0 ? "Заговори" : (draft.characterClass == "bloodhunter" || draft.characterClass == "bard" || draft.characterClass == "sorcerer" || draft.characterClass == "warlock") && draft.edition == RulesEdition.Rules2014 ? "Відомі заклинання" : "Підготовлені заклинання", 25, CreationWizardUi.Gold, 42);
            while (chosen.Count < count) chosen.Add("");
            var available = CharacterSpellCatalog.AvailableFor(draft, level)
                .Where(spell => (draft.characterClass != "ranger" || draft.edition != RulesEdition.Rules2024 || spell.name != "Hunter's Mark") &&
                    (draft.characterClass != "druid" || draft.edition != RulesEdition.Rules2024 || spell.name != "Speak with Animals") &&
                    (level != 1 || !CharacterClericRules.DomainSpells(draft).Contains(spell.name)) &&
                    (level != 1 || !CharacterArtificerRules.AutomaticSpells(draft).Contains(spell.name)) &&
                    !(level == 0 && CharacterClericRules.Classic(draft) && draft.subclass == "light" && spell.name == "Light") &&
                    !(level == 0 && CharacterClericRules.Classic(draft) && draft.subclass == "nature" && spell.name == draft.natureCantrip) &&
                    !(CharacterWizardRules.Active(draft) && level == 1 && (bookMode
                        ? draft.wizardSavant.Contains(spell.name)
                        : !CharacterWizardRules.Book(draft).Contains(spell.name) || CharacterWizardRules.Automatic(draft).Contains(spell.name))) &&
                    !(CharacterWarlockRules.Tome(draft) && (level == 0 ? draft.tomeCantrips.Contains(spell.name) : draft.tomeRituals.Contains(spell.name))) &&
                    !(draft.characterClass == "artificer" && draft.edition == RulesEdition.Rules2024 && level == 0 && spell.name == "Mending") &&
                    !(CharacterSpeciesRules.Spells(draft).Contains(spell.name) && (level == 0 || CreationSpellPreparation.RacialPrepared(draft,spell.name))) && !(level == 0 && CharacterOriginFeatRules.Choices(draft).SelectMany(CharacterOriginFeatRules.Spells).Contains(spell.name))).ToArray();
            RectTransform spellRow = null;
            for (int i = firstSlot; i < count; i++)
            {
                int slot = i;
                string[] names = CharacterTricksterSpells.ThirdCaster(draft) && level == 1 ? CharacterTricksterSpells.Options(draft, chosen, slot).Where(name => available.Any(spell => spell.name == name)).ToArray() : available.Select(spell => spell.name).Where(name => name == chosen[slot] || !chosen.Contains(name)).ToArray();
                if (bookMode)
                    names = names.Where(name => CharacterSpellCatalog.Find(draft.edition, name)?.level <= CharacterWizardRules.BookCircle(slot)).ToArray();
                if(CharacterWarlockRules.Active(draft)&&level==1)names=names.Where(name=>CharacterWarlockRules.CanLearn(draft,chosen.Where((value,index)=>index!=slot&&!string.IsNullOrEmpty(value)).Concat(new[]{name}).Select(value=>CharacterSpellCatalog.Find(draft.edition,value)?.level??99))).ToArray();
                if (CharacterBardRules.Active(draft) && level == 1) names = names.Where(name => CharacterSpellCatalog.BardCanLearn(draft, chosen.Where((value,index)=>index!=slot&&!string.IsNullOrEmpty(value)).Concat(new[]{name}))).ToArray();
                if (CharacterSorcererRules.Active(draft) && level == 1) names = names.Where(name => CharacterSorcererRules.CanLearn(draft,
                    chosen.Where((value, index) => index != slot && !string.IsNullOrEmpty(value)).Concat(new[] { name }).Select(value => CharacterSpellCatalog.Find(draft.edition, value)?.level ?? 99))).ToArray();
                if(CharacterBloodHunterRules.Profane(draft) && level==1)names=names.Where(name=>CharacterBloodHunterRules.CanLearn(draft,chosen.Where((_,index)=>index!=slot).Where(value=>!string.IsNullOrEmpty(value)).Concat(new[]{name}).Select(value=>CharacterSpellCatalog.Find(draft.edition,value)?.level??99))).ToArray();
                DropdownKey(LF(bookMode ? "Книга — заклинання {0}" : level == 0 ? "Заговір {0}" : "Заклинання {0}", i + 1), names, names.Select(L).ToArray(), chosen[i], value => chosen[slot] = value, 320, SpellChoiceHost(i, ref spellRow));
            }
        }

        private void TomePickers(System.Collections.Generic.List<string> chosen, int level, int count)
        {
            ui.Text(content, level == 0 ? "Заговори книги тіней" : "Ритуали книги тіней", 25, CreationWizardUi.Gold, 42);
            while (chosen.Count < count) chosen.Add("");
            RectTransform spellRow = null;
            for (int i = 0; i < count; i++)
            {
                int slot = i;
                string[] names = CharacterSpellCatalog.AnyClass(draft.edition, level).Where(spell => level == 0 || spell.ritual).Select(spell => spell.name)
                    .Where(name => !CharacterWarlockRules.AutomaticSpells(draft).Contains(name) && !CharacterSpeciesRules.Spells(draft).Contains(name) && !(level == 0 ? draft.cantrips.Contains(name) : draft.preparedSpells.Contains(name)) && (name == chosen[slot] || !chosen.Contains(name))).ToArray();
                DropdownKey(LF(level == 0 ? "Книга тіней — заговір {0}" : "Книга тіней — ритуал {0}", i + 1), names, names.Select(L).ToArray(), chosen[i], value => chosen[slot] = value, 320, SpellChoiceHost(i, ref spellRow));
            }
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
            DrawNameControls();
            nameError = ui.Text(content, "", 18, new Color(1f, .5f, .38f), 28);
            DropdownText("Світогляд", new[] { "Нейтральний", "Законно-добрий", "Нейтрально-добрий", "Хаотично-добрий", "Законно-нейтральний", "Хаотично-нейтральний", "Законно-злий", "Нейтрально-злий", "Хаотично-злий" }, draft.alignment, value => draft.alignment = value);
            if (!CharacterCreationValidation.CanSummarize(draft))
            { Note("Спершу заверши вибір на попередніх сторінках, щоб побачити підсумок."); return; }
            var s = CharacterCreationRules.Calculate(draft);
            Note(L(s.speciesName) + " • " + L(s.backgroundName) + "\n" + ClassSummary() + " • " + LF("рівень {0} • D&D {1}", draft.TotalLevel, draft.edition == RulesEdition.Rules2024 ? "2024" : "2014"), 66);
            ui.Text(content, LF("ОЗ {0}     КО {1}     Ініціатива {2}     Швидкість {3}", s.hitPoints, s.armorClass, CharacterCreationRules.Signed(s.initiative), s.speed), 25, CreationWizardUi.Gold, 60);
            ui.Text(content, HighlightSummaryLabels(CharacterCreationPresentation.FeatureLines(s.features, L)), 21, null, 0);
            ui.Text(content, HighlightSummaryLabels(CharacterCreationPresentation.Proficiencies(draft, s, L)), 21, null, 0);
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
            if (!CharacterCreationValidation.CanSummarize(draft))
            {
                summary.text = "<color=#E8AF58>" + L("ТВІЙ ПЕРСОНАЖ") + "</color>\n\n" + L("Розподіли всі шість значень характеристик, щоб побачити підсумок.");
                return;
            }
            var s = CharacterCreationRules.Calculate(draft);
            summary.text = "<color=#E8AF58>" + L("ТВІЙ ПЕРСОНАЖ") + "</color>\n\n<color=#FF9A00>" + (string.IsNullOrWhiteSpace(draft.characterName) ? L("Новий герой") : draft.characterName) + "</color>\n" + ClassSummary() + " • " + LF("рівень {0}", draft.TotalLevel) + "\nD&D " + (draft.edition == RulesEdition.Rules2024 ? "2024" : "2014") + "\n\n" + L(s.speciesName) + "\n" + L(s.backgroundName) + "\n\n<color=#E8AF58>" + s.hitPoints + " " + L("ОЗ") + "     " + s.armorClass + " " + L("КО") + "</color>\n" + L("Ініціатива") + " " + CharacterCreationRules.Signed(s.initiative) + "\n" + L("Швидкість") + " " + s.speed + " " + L("футів") + "\n" + LF("Майстерність {0}", CharacterCreationRules.Signed(CharacterLegacySpeciesRules.Proficiency(draft))) + "\n\n" + string.Join("\n", Enumerable.Range(0, 6).Select(i => "<color=#FF9A00>" + L(CharacterCreationRules.AbilityNames[i]) + "</color>  " + s.scores[i] + " (<color=#FF9A00>" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i])) + "</color>)"));
        }
        private void Next()
        {
            if (created) { SceneManager.LoadScene(AppConfig.Scenes.CharacterSheet); return; }
            if (IsLevelUp) { SaveLevelUp(); return; }
            var errors = CharacterCreationRules.Validate(draft, step == 5);
            string pageError = StepError(step);
            if (step == 5) pageError = errors.FirstOrDefault() ?? (draft.secondaryClass?.IsSelected == true
                ? draft.secondaryClass.ChoiceError(draft, true) : null);
            if (pageError != null)
            {
                if (step == 5 && pageError.Contains("ім’я") && nameError != null) nameError.text = L(pageError);
                else feedback.text = L(pageError);
                return;
            }
            if (step < 5) { ShowStep(step + 1); return; }
            next.interactable = false;
            if (Commit(out string error))
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
                    .ShowCompact(L("Повернутися до головного меню?"), L("Чернетка персонажа ще не збережена. Вийти з майстра?"), L("Вийти"), LeaveWizard);
                return;
            }
            LeaveWizard();
        }
    }
}
