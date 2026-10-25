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
        var view = ui.Rect("FormScrollView", shell); CreationWizardUi.Fill(view, 235, 155, 330, 135);
        var scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
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
        feedback.rectTransform.anchorMin = new Vector2(.16f,0); feedback.rectTransform.anchorMax = new Vector2(.79f,0); feedback.rectTransform.pivot = new Vector2(.5f,0); feedback.rectTransform.anchoredPosition = new Vector2(0,122); feedback.rectTransform.sizeDelta = new Vector2(0,60);
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
             SpriteAt("Assets/newUI/_ca5ab790-13a1-483e-b789-dcb3f99c6d39111.png"));
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
                int index = Array.IndexOf(CharacterCreationRules.SkillNames, name);
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
            result.healthPrefix = "HealthBar_" + new SerializedObject(health).FindProperty("customIndex").intValue + "_";
            result.entries = entries.ToArray();
            if (entries.Count(x=>x.key.StartsWith("skill."))!=36 || entries.Count(x=>x.key.StartsWith("save."))!=12) throw new InvalidOperationException("Incomplete skill/save bindings.");
            return result;
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }
    private static void Add(List<CreationSheetBindings.Entry> entries, PersistentFieldId f, string key)
    {
        entries.Add(new CreationSheetBindings.Entry { key=key, scene="cartaPersonaj", stableId=f.Id, index=f.LegacyIndex, collection=f.LegacyCollection });
    }
}
