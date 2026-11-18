using System;
using System.Collections.Generic;
using System.Linq;
using Taruck.CharacterCreation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class CharacterCreationSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/CharacterCreationPrototype.unity";
    private const string DataFolder = "Assets/CharacterCreation";

    [MenuItem("Taruck/Character creation/Create prototype scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building the scene.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("The prototype scene already exists. Open it instead of replacing it.");
        EditorSceneManager.SaveOpenScenes();
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets", "CharacterCreation");
        var bindings = CaptureBindings();
        AssetDatabase.CreateAsset(bindings, DataFolder + "/SheetBindings.asset");
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Gentleland/SteampunkUI/Fonts/RobotoSlab/RobotoSlab.ttf");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 48, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        font.name = "CreationWizard Cyrillic";
        // Seed the test UI's Cyrillic alphabet before saving the dedicated font asset.
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя•−+>/():,.!?—–’\"% ");
        AssetDatabase.CreateAsset(font, DataFolder + "/CreationWizardFont.asset");
        foreach (Texture2D texture in font.atlasTextures) if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
        if (!AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
        Sprite button = AssetDatabase.LoadAllAssetsAtPath("Assets/newUI/_e9b3eaa1-0d2f-4b55-a80c-6ef68fd4c5da 1.png").OfType<Sprite>().First();
        var ui = new CreationWizardUi(font, button);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = CreationWizardUi.Ink;
        var canvas = new GameObject("CreationCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 1000); scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var background = ui.Rect("TavernBackground", canvas.transform); CreationWizardUi.Fill(background);
        var bg = background.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        bg.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/newUI/Backgrounds_16x9/tavern.jpg"); bg.raycastTarget = false;
        var backgroundFit = background.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        backgroundFit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        backgroundFit.aspectRatio = 2560f / 1440f;
        var shade = ui.Panel("BackdropShade", canvas.transform, new Color(.018f, .02f, .036f, .64f)); CreationWizardUi.Fill(shade);
        var shell = ui.Panel("WizardPanel", canvas.transform, new Color(.035f, .035f, .06f, .95f));
        shell.anchorMin = new Vector2(.035f, .035f); shell.anchorMax = new Vector2(.965f, .965f); shell.offsetMin = shell.offsetMax = Vector2.zero;
        var outline = shell.gameObject.AddComponent<UnityEngine.UI.Outline>(); outline.effectColor = new Color(.58f, .39f, .2f); outline.effectDistance = new Vector2(2, -2);
        var header = ui.Rect("Header", shell); header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1); header.sizeDelta = new Vector2(0, 140);
        var title = ui.Text(header, "Створення персонажа", 38, CreationWizardUi.Gold); CreationWizardUi.Fill(title.rectTransform, 30, 18, 30, 65);
        var subtitle = ui.Text(header, "", 19); CreationWizardUi.Fill(subtitle.rectTransform, 32, 83, 32, 18);
        var line = ui.Panel("GoldRule", shell, new Color(.45f, .3f, .13f)); line.anchorMin = new Vector2(0, 1); line.anchorMax = Vector2.one; line.pivot = new Vector2(.5f, 1); line.anchoredPosition = new Vector2(0, -135); line.sizeDelta = new Vector2(0, 1);
        var sidebar = ui.Panel("StepsSidebar", shell, new Color(.025f, .027f, .046f, .75f));
        sidebar.anchorMin = Vector2.zero; sidebar.anchorMax = new Vector2(0, 1); sidebar.offsetMin = new Vector2(18, 130); sidebar.offsetMax = new Vector2(220, -155);
        var steps = ui.Column("Steps", sidebar, 14); CreationWizardUi.Fill(steps, 8, 10, 8, 10);
        var previewPanel = ui.Panel("PreviewPanel", shell, new Color(.06f, .052f, .065f, .8f));
        previewPanel.anchorMin = new Vector2(1, 0); previewPanel.anchorMax = Vector2.one; previewPanel.offsetMin = new Vector2(-310, 135); previewPanel.offsetMax = new Vector2(-20, -155);
        var preview = ui.Text(previewPanel, "", 23); CreationWizardUi.Fill(preview.rectTransform, 23, 24, 18, 24);
        var view = ui.Rect("FormScrollView", shell); CreationWizardUi.Fill(view, 235, 155, 330, 200);
        var scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 90;
        var viewport = ui.Rect("Viewport", view); CreationWizardUi.Fill(viewport);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(1,1,1,.01f);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        var content = ui.Column("Content", viewport, 14); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.padding = new RectOffset(10, 18, 8, 24);
        content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content; scroll.viewport = viewport;
        view.gameObject.AddComponent<CameraPanBlockOnScrollView>();
        var footer = ui.Rect("Footer", shell); footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1,0); footer.pivot = new Vector2(.5f,0); footer.sizeDelta = new Vector2(0,120);
        var progress = ui.Text(footer, "", 20, CreationWizardUi.Gold); progress.rectTransform.anchorMin = new Vector2(0,.65f); progress.rectTransform.anchorMax = new Vector2(1,1); progress.rectTransform.offsetMin = new Vector2(30,0); progress.rectTransform.offsetMax = new Vector2(-30,0);
        var previous = ui.Button(footer, "<  Назад", () => {}); previous.GetComponent<RectTransform>().anchorMin = new Vector2(0,0); previous.GetComponent<RectTransform>().anchorMax = new Vector2(.3f,.62f); previous.GetComponent<RectTransform>().offsetMin = new Vector2(26,14); previous.GetComponent<RectTransform>().offsetMax = new Vector2(-10,0);
        var next = ui.Button(footer, "Далі  >", () => {}, true); next.GetComponent<RectTransform>().anchorMin = new Vector2(.5f,0); next.GetComponent<RectTransform>().anchorMax = new Vector2(1,.62f); next.GetComponent<RectTransform>().offsetMin = new Vector2(10,14); next.GetComponent<RectTransform>().offsetMax = new Vector2(-26,0);
        var feedback = ui.Text(shell, "", 18, new Color(1,.5f,.38f));
        feedback.rectTransform.anchorMin = new Vector2(.16f,0); feedback.rectTransform.anchorMax = new Vector2(.79f,0); feedback.rectTransform.pivot = new Vector2(.5f,0); feedback.rectTransform.anchoredPosition = new Vector2(0,130); feedback.rectTransform.sizeDelta = new Vector2(0,50);
        var wizard = canvas.gameObject.AddComponent<CharacterCreationWizard>();
        wizard.Configure(font, button, bindings, content, steps, sidebar, previewPanel, view, title, subtitle, preview, feedback, progress, next, previous, scroll);
        wizard.ConfigureVisuals(
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d.jpg"),
            SpriteAt("Assets/newUI/3156413215.png", "3156413215_10"),
            SpriteAt("Assets/newUI/exec-71db3d84-c1d5-4def-b9c1-c05d15c57308.png", "exec-71db3d84-c1d5-4def-b9c1-c05d15c57308_0"),
            SpriteAt("Assets/newUI/exec-08f2e6bc-c987-4fe9-8488-b37e50026874.png"),
            SpriteAt("Assets/newUI/_59b7a82e-e760-4963-9141-913d1f8420c91.png"),
            SpriteAt("Assets/newUI/3156413215.png", "3156413215_6"),
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d.jpg"),
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d11.png"),
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d333.png"),
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d11123.png"),
             SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d1112312.png"),
             SpriteAt("Assets/newUI/_ca5ab790-13a1-483e-b789-dcb3f99c6d39111.png"),
             SpriteAt("Assets/newUI/_6a298549-76ea-44c0-87f9-905e185dde581.png"));
        var panelBackground = SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d333.png");
        var panelFrame = SpriteAt("Assets/newUI/_ea42a262-7b02-4bdb-b2bc-02527599ab122d11.png");
        foreach (var panel in new[] { shell.GetComponent<UnityEngine.UI.Image>(), sidebar.GetComponent<UnityEngine.UI.Image>(), previewPanel.GetComponent<UnityEngine.UI.Image>() })
        {
            panel.sprite = null; panel.color = Color.clear;
            var oldOutline = panel.GetComponent<UnityEngine.UI.Outline>();
            if (oldOutline != null) oldOutline.enabled = false;
            foreach (var layerName in new[] { "WizardBackground", "WizardFrame" })
            {
                var layer = ui.Rect(layerName, panel.transform);
                CreationWizardUi.Fill(layer);
                var layerImage = layer.gameObject.AddComponent<UnityEngine.UI.Image>();
                layerImage.sprite = layerName == "WizardBackground" ? panelBackground : panelFrame;
                layerImage.type = layerName == "WizardBackground" ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
                layerImage.pixelsPerUnitMultiplier = layerName == "WizardBackground" ? 1 : 4;
                layerImage.raycastTarget = false;
                if (layerName == "WizardBackground") layer.SetAsFirstSibling(); else layer.SetAsLastSibling();
            }
        }
        wizard.Rebuild();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Character creation prototype created: " + ScenePath);
    }

    private static Sprite SpriteAt(string path, string name = null)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
        var sprite = name == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(x => x.name == name);
        if (sprite == null) throw new InvalidOperationException("Missing wizard sprite: " + path + " / " + name);
        return sprite;
    }

    private static CreationSheetBindings CaptureBindings()
    {
        const string sheetPath = "Assets/Scenes/personag/cartaPersonaj.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sheetPath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(sheetPath, OpenSceneMode.Additive);
        try
        {
            var result = ScriptableObject.CreateInstance<CreationSheetBindings>();
            var entries = new List<CreationSheetBindings.Entry>();
            var fields = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PersistentFieldId>(true)).ToArray();
            foreach (var f in fields.Where(x => x.LegacyCollection == "inputData")) Add(entries, f, "input." + f.LegacyIndex);
            Transform skills = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="navuki");
            var labels = skills.GetComponentsInChildren<UnityEngine.UI.Text>(true).Where(x=>x.name=="Label").ToArray();
            foreach (var f in fields.Where(x=>x.transform.IsChildOf(skills)))
            {
                var label = labels.OrderBy(x=>Math.Abs(x.transform.position.y-f.transform.position.y)).First();
                string name = label.text.Substring(0, label.text.IndexOf('(')).Trim();
                int index = Array.IndexOf(CharacterCreationRules.SkillNames.ToArray(), name);
                if (index < 0) throw new InvalidOperationException("Unknown skill: " + name);
                Add(entries, f, "skill." + index);
            }
            Transform saves = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="spasKidki");
            foreach (var f in fields.Where(x=>x.transform.IsChildOf(saves)))
            {
                var controls = fields.Where(x=>x.transform.IsChildOf(saves) && x.LegacyCollection==f.LegacyCollection).OrderByDescending(x=>x.transform.position.y).ToArray();
                Add(entries, f, "save." + Array.IndexOf(controls, f));
            }
            var health = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<HealthBar>(true)).First();
            result.SetHealthPrefix("HealthBar_" + new SerializedObject(health).FindProperty("customIndex").intValue + "_");
            result.ReplaceEntries(entries.ToArray());
            if (entries.Count(x=>x.key.StartsWith("skill."))!=36 || entries.Count(x=>x.key.StartsWith("save."))!=12) throw new InvalidOperationException("Incomplete skill/save bindings.");
            CaptureSpellbookBindings(result);
            CaptureMagicResourceBindings(result);
            CaptureMainResourceBindings(result);
            return result;
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }
    private static void Add(List<CreationSheetBindings.Entry> entries, PersistentFieldId f, string key)
    {
        entries.Add(new CreationSheetBindings.Entry { key=key, scene="cartaPersonaj", stableId=f.Id, index=f.LegacyIndex, collection=f.LegacyCollection });
    }

    public static void RefreshSpellbookBindings()
    {
        var bindings = AssetDatabase.LoadAssetAtPath<CreationSheetBindings>(DataFolder + "/SheetBindings.asset");
        if (bindings == null) throw new InvalidOperationException("Sheet bindings are missing.");
        CaptureSpellbookBindings(bindings);
        CaptureMagicResourceBindings(bindings);
        CaptureMainResourceBindings(bindings);
        CaptureFormBindings(bindings);
        UpdateSpellbookDurationOptions();
        EditorUtility.SetDirty(bindings);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateSpellbookDurationOptions()
    {
        // Five saved pages reuse the same scene controls.
        foreach (string sceneName in new[] { "spelBook" })
        {
            string path = "Assets/Scenes/personag/" + sceneName + ".unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save the spellbook scene before updating its duration options.");
                bool changed = false;
                foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)))
                {
                    var dropdown = field.GetComponent<UnityEngine.UI.Dropdown>();
                    if (dropdown == null || field.LegacyCollection != "dropdownData" || field.LegacyIndex < 0 || field.LegacyIndex % 3 != 1) continue;
                    if (dropdown.options.Count == 15)
                    {
                        Undo.RecordObject(dropdown, "Add special spell duration");
                        dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("Особлива"));
                        EditorUtility.SetDirty(dropdown); changed = true;
                    }
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }

    public static void PrepareDragonBreathCounter()
    {
        const string path = "Assets/Scenes/personag/cartaPersonaj.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save the character sheet scene before updating its breath counter.");
            var ids = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)).ToArray();
            var original = ids.Single(field => field.Id == "dnd.cartapersonaj.toggle.toggle.e8a857a6c6d0").GetComponent<UnityEngine.UI.Toggle>();
            Transform panel = original.transform.parent;
            if (panel.Find("DragonBreathCount") != null) return;
            var donor = ids.Single(field => field.Id == "dnd.cartapersonaj.dropdown.dropdown-legacy-2.41f3258c3293").GetComponent<UnityEngine.UI.Dropdown>();
            var count = UnityEngine.Object.Instantiate(donor, panel);
            count.name = "DragonBreathCount";
            count.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
            count.options = Enumerable.Range(0, 7).Select(i => new UnityEngine.UI.Dropdown.OptionData(i.ToString())).ToList();
            var countRect = (RectTransform)count.transform;
            countRect.anchorMin = countRect.anchorMax = ((RectTransform)original.transform).anchorMin;
            countRect.anchoredPosition = ((RectTransform)original.transform).anchoredPosition + new Vector2(-45, 110);
            count.SetValueWithoutNotify(1);
            PersistentFieldId.For(count).Configure("creation.dragonbreath.count", count, "dropdownData", -1, "");
            foreach (var nestedId in count.GetComponentsInChildren<PersistentFieldId>(true).Where(field => field != PersistentFieldId.For(count))) UnityEngine.Object.DestroyImmediate(nestedId);
            var toggles = new List<UnityEngine.UI.Toggle> { original };
            Vector2 start = ((RectTransform)original.transform).anchoredPosition;
            for (int i = 1; i < 6; i++)
            {
                var toggle = UnityEngine.Object.Instantiate(original, panel);
                toggle.name = "DragonBreathUse" + i;
                toggle.SetIsOnWithoutNotify(false);
                ((RectTransform)toggle.transform).anchoredPosition = start + new Vector2(i * 100, 0);
                PersistentFieldId.For(toggle).Configure("creation.dragonbreath.use." + i, toggle, "toggleData", -1, "");
                toggle.gameObject.SetActive(false);
                toggles.Add(toggle);
            }
            var manager = count.gameObject.AddComponent<DropdownManager>();
            var serialized = new SerializedObject(manager);
            var configs = serialized.FindProperty("dropdownConfigs"); configs.arraySize = 1;
            var config = configs.GetArrayElementAtIndex(0);
            config.FindPropertyRelative("dropdown").objectReferenceValue = count;
            var objects = config.FindPropertyRelative("tangles"); objects.arraySize = toggles.Count;
            for (int i = 0; i < toggles.Count; i++) objects.GetArrayElementAtIndex(i).objectReferenceValue = toggles[i].gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void PrepareRageCounter()
    {
        const string path = "Assets/Scenes/personag/cartaPersonaj.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save the character sheet scene before updating its Rage counter.");
            var field = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true))
                .Single(id => id.Id == "dnd.cartapersonaj.dropdown.dropdown-legacy-2.0a3575706a02");
            var count = field.GetComponent<UnityEngine.UI.Dropdown>();
            count.options = Enumerable.Range(0, 7).Select(n => new UnityEngine.UI.Dropdown.OptionData(n.ToString())).Concat(new[] { new UnityEngine.UI.Dropdown.OptionData("∞") }).ToList();
            Transform duplicate = count.transform.parent.Find("RageCount");
            if (duplicate != null) UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            foreach (var manager in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DropdownManager>(true)))
            {
                var serialized = new SerializedObject(manager);
                var configs = serialized.FindProperty("dropdownConfigs");
                for (int i=0;i<configs.arraySize;i++)
                {
                    var config = configs.GetArrayElementAtIndex(i);
                    if (config.FindPropertyRelative("dropdown").objectReferenceValue == count)
                        config.FindPropertyRelative("unlimitedValue").intValue = 7;
                }
                serialized.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(count);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void CaptureMainResourceBindings(CreationSheetBindings bindings)
    {
        const string path = "Assets/Scenes/personag/cartaPersonaj.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var entries = bindings.Entries.Where(entry => !entry.key.StartsWith("rest.main.", StringComparison.Ordinal) && entry.key != "main.dragon.count" && entry.key != "main.ki.count" && entry.key != "main.rage.count" && entry.key != "main.wild.count").ToList();
            foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)))
            {
                if (field.Id == "dnd.cartapersonaj.dropdown.dropdown-legacy-2.41f3258c3293") entries.Add(new CreationSheetBindings.Entry { key = "main.ki.count", scene = "cartaPersonaj", stableId = field.Id, index = field.LegacyIndex, collection = field.LegacyCollection });
                if (field.Id == "dnd.cartapersonaj.dropdown.dropdown-legacy-2.7f26f3431c74") entries.Add(new CreationSheetBindings.Entry { key = "main.wild.count", scene = "cartaPersonaj", stableId = field.Id, index = field.LegacyIndex, collection = field.LegacyCollection });
                if (field.Id == "dnd.cartapersonaj.dropdown.dropdown-legacy-2.0a3575706a02") entries.Add(new CreationSheetBindings.Entry { key = "main.rage.count", scene = "cartaPersonaj", stableId = field.Id, index = field.LegacyIndex, collection = field.LegacyCollection });
                if (field.Id == "creation.dragonbreath.count") entries.Add(new CreationSheetBindings.Entry { key = "main.dragon.count", scene = "cartaPersonaj", stableId = field.Id, index = field.LegacyIndex, collection = field.LegacyCollection });
                if (field.LegacyCollection != "toggleData" || field.GetComponent<UnityEngine.UI.Toggle>() == null || field.transform.parent == null ||
                    !StableFieldPath.For(field.transform).Contains("resursClas")) continue;
                string key = field.transform.parent.name == "Panel (1)" ? "wild" : field.transform.parent.name == "Panel (2)" ? "channel" : field.transform.parent.name == "Panel (8)" ? "other" : field.transform.parent.name == "Panel (9)" ? "stone" :
                    field.transform.parent.name == "Panel (10)" ? "heroic" : field.transform.parent.name == "Panel (7)" ? "dragon" : field.transform.parent.name == "Panel" ? "rage" : field.transform.parent.name == "Panel (3)" ? "ki" :
                    field.transform.parent.name == "Panel (5)" && (field.name == "Toggle" || Enumerable.Range(1, 7).Any(number => field.name == "Toggle (" + number + ")")) ? "blood" : null;
                if (key == null) continue;
                entries.Add(new CreationSheetBindings.Entry { key = "rest.main." + key, scene = "cartaPersonaj", stableId = field.Id,
                    index = field.LegacyIndex, collection = field.LegacyCollection });
            }
            bindings.ReplaceEntries(entries.ToArray());
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void CaptureMagicResourceBindings(CreationSheetBindings bindings)
    {
        const string path = "Assets/Scenes/personag/Spels.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var entries = bindings.Entries.Where(entry => !entry.key.StartsWith("rest.magic.", StringComparison.Ordinal) && !entry.key.StartsWith("magic.resource.", StringComparison.Ordinal) && !entry.key.StartsWith("magic.large.resource.", StringComparison.Ordinal) && !entry.key.StartsWith("magic.slot.", StringComparison.Ordinal) && !entry.key.StartsWith("magic.note.", StringComparison.Ordinal)).ToList();
            var fields = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true))
                .Where(field => StableFieldPath.For(field.transform).Contains("artefactsNavuk") && !StableFieldPath.For(field.transform).Contains("Template")).ToArray();
            var names = fields.Where(field => field.LegacyCollection == "inputData").OrderBy(field => field.LegacyIndex).ToArray();
            var counts = fields.Where(field => field.LegacyCollection == "dropdownData").OrderBy(field => field.LegacyIndex).ToArray();
            var toggles = fields.Where(field => field.LegacyCollection == "toggleData" && field.GetComponent<UnityEngine.UI.Toggle>() != null).OrderBy(field => field.LegacyIndex).ToArray();
            if (names.Length != 8 || counts.Length != 8 || toggles.Length != 40) throw new InvalidOperationException("Incomplete magic resource rows.");
            void AddField(PersistentFieldId field, string key) => entries.Add(new CreationSheetBindings.Entry { key = key, scene = "Spels", stableId = field.Id, index = field.LegacyIndex, collection = field.LegacyCollection });
            foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true))
                .Where(field => StableFieldPath.For(field.transform).Contains("zamitki") && field.LegacyIndex >= 0 && field.LegacyIndex < 4))
            {
                if (field.LegacyCollection == "inputData") AddField(field, "magic.note." + field.LegacyIndex);
                if (field.LegacyCollection == "toggleData") AddField(field, "magic.note." + field.LegacyIndex + ".open");
            }
            foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true))
                .Where(field => field.LegacyCollection == "dropdownData" && field.LegacyIndex >= 0 && field.LegacyIndex < 9 && field.GetComponent<UnityEngine.UI.Dropdown>() != null))
                AddField(field, "magic.slot." + field.LegacyIndex);
            for (int row = 0; row < 8; row++)
            {
                AddField(names[row], "magic.resource." + row + ".name");
                AddField(counts[row], "magic.resource." + row + ".count");
                foreach (var toggle in toggles.Skip(row * 5).Take(5))
                {
                    AddField(toggle, "rest.magic." + row);
                    if (row == 0) AddField(toggle, "rest.magic.first");
                }
            }
            var large = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true))
                .Where(field => StableFieldPath.For(field.transform).Contains("artefactInfuz") && !StableFieldPath.For(field.transform).Contains("Template")).ToArray();
            var largeNames = large.Where(field => field.LegacyCollection == "inputData").OrderBy(field => field.LegacyIndex).ToArray();
            var largeCounts = large.Where(field => field.LegacyCollection == "dropdownData").OrderBy(field => field.LegacyIndex).ToArray();
            var largeToggles = large.Where(field => field.LegacyCollection == "toggleData" && field.GetComponent<UnityEngine.UI.Toggle>() != null).OrderBy(field => field.LegacyIndex).ToArray();
            if (largeNames.Length != 8 || largeCounts.Length != 8 || largeToggles.Length != 64) throw new InvalidOperationException("Incomplete eight-use magic resource rows.");
            for (int row = 0; row < 8; row++)
            {
                AddField(largeNames[row], "magic.large.resource." + row + ".name");
                AddField(largeCounts[row], "magic.large.resource." + row + ".count");
                foreach (var toggle in largeToggles.Skip(row * 8).Take(8)) AddField(toggle, "rest.magic.large." + row);
            }
            bindings.ReplaceEntries(entries.ToArray());
            var magicRoot=scene.GetRootGameObjects().Single(root=>root.name=="spels");
            var noteUpdater=magicRoot.GetComponent<ArtificerInfusionNotes>() ?? magicRoot.AddComponent<ArtificerInfusionNotes>();
            var noteSettings=new SerializedObject(noteUpdater);
            noteSettings.FindProperty("bindings").objectReferenceValue=bindings;
            noteSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if(opened)EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void CaptureFormBindings(CreationSheetBindings bindings)
    {
        const string path = "Assets/Scenes/personag/petsesn.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var entries = bindings.Entries.Where(entry => !entry.key.StartsWith("form.", StringComparison.Ordinal)).ToList();
            foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)))
            {
                if (field.LegacyCollection != "inputData") continue;
                entries.Add(new CreationSheetBindings.Entry { key = "form.input." + field.LegacyIndex, scene = "petsesn", stableId = field.Id, collection = field.LegacyCollection, index = field.LegacyIndex });
            }
            var health = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HealthBar>(true)).Single();
            bindings.SetFormHealthPrefix("HealthBar_" + new SerializedObject(health).FindProperty("customIndex").intValue + "_");
            bindings.ReplaceEntries(entries.ToArray());
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void CaptureSpellbookBindings(CreationSheetBindings bindings)
    {
        const string path = "Assets/Scenes/personag/spelBook.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var entries = bindings.Entries.Where(entry => entry.scene != "spelBook").ToList();
            foreach (var field in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)))
            {
                string key = field.LegacyCollection == "inputData" ? "book.input." + field.LegacyIndex :
                    field.LegacyCollection == "dropdownData" ? "book.dropdown." + field.LegacyIndex :
                    field.LegacyCollection == "toggleData" && field.GetComponent<UnityEngine.UI.Toggle>() != null &&
                    !StableFieldPath.For(field.transform).Contains("Template") ? "book.prepared." + field.LegacyIndex / 4 : null;
                if (key == null || field.LegacyIndex < 0) continue;
                entries.Add(new CreationSheetBindings.Entry { key = key, scene = "spelBook", stableId = field.Id,
                    collection = field.LegacyCollection, index = field.LegacyIndex });
            }
            bindings.ReplaceEntries(entries.ToArray());
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
