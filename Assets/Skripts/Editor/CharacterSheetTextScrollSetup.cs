using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Reuse the note reader without replacing saved fields or their stable identities.
public static class CharacterSheetTextScrollSetup
{
    [MenuItem("Taruck/Character creation/Configure sheet text scrolling")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Expected edit mode.");
        const string path = "Assets/Scenes/personag/cartaPersonaj.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var root = scene.GetRootGameObjects().Single(g => g.name == "karta");
            var donor = root.transform.Find("zamitki (1)/Panel/NotesScroll").GetComponent<ScrollRect>();
            foreach (string fieldPath in new[] {
                "navichkiTaVminya/spasKidkiSkore/inputYminaOsodb",
                "Inventar/spasKidkiSkore/InputFieldspaspaw",
                "inhiVolodinya/spasKidkiSkore/InputFieldspaspaw" })
            {
                var field = root.transform.Find(fieldPath)?.GetComponent<InputField>();
                if (field != null) Wrap(field, donor);
            }
            var attacks=root.GetComponentsInChildren<InputField>(true).Single(f=>PersistentFieldId.For(f)?.LegacyIndex==75);
            if(attacks.GetComponentInParent<NoteScrollInput>(true)==null)Wrap(attacks,donor);
            foreach (var reader in root.GetComponentsInChildren<NoteScrollInput>(true))
            {
                var serialized = new SerializedObject(reader);
                serialized.FindProperty("characterLimit").intValue = 3000;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var field = reader.GetComponentInChildren<InputField>(true);
                if (field != null) field.characterLimit = 3000;
                FitHandle(reader.GetComponent<ScrollRect>());
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            foreach (string noteScene in new[]{"Spels", "petsesn"})
                ConfigureOtherNotes("Assets/Scenes/personag/" + noteScene + ".unity", donor);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void ConfigureOtherNotes(string path, ScrollRect donor)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var fields = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InputField>(true))
                .Where(f => StableFieldPath.For(f.transform).ToLowerInvariant().Contains("zamit")) .ToArray();
            foreach (var field in fields)
            {
                if (field.GetComponentInParent<NoteScrollInput>(true) == null) Wrap(field, donor);
                field.characterLimit = 3000;
                FitHandle(field.GetComponentInParent<ScrollRect>(true));
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void Wrap(InputField field, ScrollRect donor)
    {
        var original = (RectTransform)field.transform;
        var parent = original.parent;
        int index = original.GetSiblingIndex();
        bool active = field.gameObject.activeSelf;
        var view = UnityEngine.Object.Instantiate(donor.gameObject, parent).GetComponent<ScrollRect>();
        view.name = field.name + "Scroll";
        var rect = (RectTransform)view.transform;
        rect.anchorMin = original.anchorMin;
        rect.anchorMax = original.anchorMax;
        rect.pivot = original.pivot;
        rect.sizeDelta = original.sizeDelta;
        rect.anchoredPosition3D = original.anchoredPosition3D;
        rect.localRotation = original.localRotation;
        rect.localScale = original.localScale;
        rect.SetSiblingIndex(index);
        // The donor's saved input must never survive with a duplicate field ID.
        UnityEngine.Object.DestroyImmediate(view.GetComponentInChildren<InputField>(true).gameObject);
        var sourceImage = field.GetComponent<UnityEngine.UI.Image>();
        var background = view.gameObject.AddComponent<UnityEngine.UI.Image>();
        EditorUtility.CopySerialized(sourceImage, background);
        background.raycastTarget = false;
        sourceImage.sprite = null;
        sourceImage.color = Color.clear;
        sourceImage.raycastTarget = true;
        view.viewport.offsetMin = new Vector2(8, 8);
        view.viewport.offsetMax = new Vector2(-22, -8);
        var bar = (RectTransform)view.verticalScrollbar.transform;
        bar.offsetMin = new Vector2(-18, 8);
        bar.offsetMax = new Vector2(-7, -8);
        original.SetParent(view.content, false);
        original.localScale = Vector3.one;
        original.localRotation = Quaternion.identity;
        original.anchorMin = Vector2.zero;
        original.anchorMax = Vector2.one;
        original.offsetMin = Vector2.zero;
        original.offsetMax = Vector2.zero;
        foreach (var text in field.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(4, 4);
            text.rectTransform.offsetMax = new Vector2(-4, -4);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;
        }
        field.lineType = InputField.LineType.MultiLineNewline;
        field.gameObject.SetActive(true);
        view.content.sizeDelta = new Vector2(0, Mathf.Max(1, rect.rect.height - 16));
        view.content.anchoredPosition = Vector2.zero;
        view.movementType = ScrollRect.MovementType.Clamped;
        view.GetComponent<NoteScrollInput>().Configure(view, field);
        foreach (var controller in parent.gameObject.scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<PanelGroupController>(true)))
        {
            var serialized = new SerializedObject(controller);
            var panels = serialized.FindProperty("panels");
            for (int i = 0; i < panels.arraySize; i++)
                if (panels.GetArrayElementAtIndex(i).objectReferenceValue == field.gameObject)
                    panels.GetArrayElementAtIndex(i).objectReferenceValue = view.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        view.gameObject.SetActive(active);
    }

    private static void FitHandle(ScrollRect view)
    {
        if (view == null || view.verticalScrollbar == null) return;
        var handle = view.verticalScrollbar.handleRect;
        var area = handle.parent as RectTransform;
        if (area == null) return;
        area.offsetMin = new Vector2(2, 4);
        area.offsetMax = new Vector2(-2, -4);
        handle.sizeDelta = Vector2.zero;
    }
}
