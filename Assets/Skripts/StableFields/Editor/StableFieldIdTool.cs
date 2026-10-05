using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StableFieldIdTool
{
    public const string MapPath = "Assets/Refactoring/Baseline/LegacyFieldMap_v1.csv";

    public static List<Dictionary<string, string>> ReadMap()
    {
        var lines = File.ReadAllLines(MapPath);
        var header = ParseLine(lines[0]);
        var rows = new List<Dictionary<string, string>>();
        foreach (string line in lines.Skip(1))
        {
            var values = ParseLine(line);
            if (values.Count != header.Count) throw new InvalidDataException("Invalid A0 CSV row");
            var row = new Dictionary<string, string>();
            for (int i = 0; i < header.Count; i++) row.Add(header[i], values[i]);
            rows.Add(row);
        }
        return rows;
    }

    public static void InstallPilot() => Install("informForPerson");
    public static void InstallRemaining() => Install(null);

    private static void Install(string onlyScene)
    {
        RequireCleanScenes();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var group in ReadMap().GroupBy(row => row["scenePath"]))
            {
                if (onlyScene != null && group.First()["sceneName"] != onlyScene) continue;
                var scene = EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single);
                var controls = Components(scene);
                bool changed = false;
                foreach (var row in group)
                {
                    string id = row["proposedStableId"];
                    var existing = Fields(scene).Where(field => field.Id == id).ToArray();
                    if (existing.Length > 1) throw new InvalidDataException("Duplicate ID: " + id);
                    if (existing.Length == 1) continue;
                    var matches = controls.Where(c => c != null && c.GetType().Name == row["controlType"]
                        && ControlPath(c.transform) == row["getControlPath"]).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("Ambiguous or missing A0 control: " + id);
                    if (PersistentFieldId.For(matches[0]) != null)
                        throw new InvalidDataException("Control already has a different ID: " + id);
                    var field = matches[0].gameObject.AddComponent<PersistentFieldId>();
                    field.Configure(id, matches[0], row["legacyCollection"],
                        int.Parse(row["legacyIndex"]), row["legacyKey"]);
                    EditorUtility.SetDirty(field);
                    changed = true;
                }
                ValidateScene(scene, group.ToList());
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + group.Key);
                }
                Debug.Log("A2 validated " + scene.name + ": " + group.Count() + " stable fields");
            }
        }
        finally { Restore(setup); }
    }

    [MenuItem("Tools/DnD/Persistence/Validate Stable Field IDs")]
    public static void ValidateAll()
    {
        RequireCleanScenes();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var map = ReadMap();
            if (map.Select(r => r["proposedStableId"]).Distinct().Count() != map.Count)
                throw new InvalidDataException("Duplicate IDs in A0 map");
            foreach (var group in map.GroupBy(row => row["scenePath"]))
                ValidateScene(EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single), group.ToList());
            Debug.Log("A2: validated all " + map.Count + " stable field bindings.");
        }
        finally { Restore(setup); }
    }

    public static void ValidateScene(Scene scene, List<Dictionary<string, string>> rows)
    {
        var fields = Fields(scene);
        if (fields.Any(f => string.IsNullOrWhiteSpace(f.Id))
            || fields.Select(f => f.Id).Distinct().Count() != fields.Count)
            throw new InvalidDataException("Empty or duplicate stable IDs in " + scene.name);
        var controls = new HashSet<Component>();
        foreach (var row in rows)
        {
            var matches = fields.Where(f => f.Id == row["proposedStableId"]).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Missing/duplicate ID: " + row["proposedStableId"]);
            var field = matches[0];
            if (field.Control == null || field.Control.gameObject != field.gameObject
                || field.Control.GetType().Name != row["controlType"] || !controls.Add(field.Control)
                || field.LegacyIndex != int.Parse(row["legacyIndex"])
                || field.LegacyCollection != row["legacyCollection"] || field.LegacyKey != row["legacyKey"])
                throw new InvalidDataException("Invalid binding: " + field.Id);
        }
        bool sheetController = rows.Any(r => r["controller"] == "CharacterSheetManagerScene1");
        var frozenIds = new HashSet<string>(rows.Select(r => r["proposedStableId"]));
        foreach (var field in fields.Where(f => !frozenIds.Contains(f.Id)))
        {
            string expectedCollection = field.Control is InputField || field.Control is TMP_InputField ? "inputData"
                : field.Control is Slider ? "sliderData" : field.Control is Toggle ? "toggleData" : "dropdownData";
            if (field.Control == null || field.Control.gameObject != field.gameObject
                || !IsPersistedControl(field.Control, sheetController) || !controls.Add(field.Control)
                || field.LegacyIndex != -1 || !string.IsNullOrEmpty(field.LegacyKey)
                || field.LegacyCollection != expectedCollection)
                throw new InvalidDataException("Invalid new field binding: " + field.Id);
        }
        foreach (var control in Components(scene))
            if (IsPersistedControl(control, sheetController) && !controls.Contains(control))
                throw new InvalidDataException("Missing stable ID: " + ControlPath(control.transform));
    }

    private static bool IsPersistedControl(Component control, bool sheetController)
    {
        if (control == null) return false;
        bool textOrSlider = control is InputField || control is TMP_InputField || control is Slider;
        if (!textOrSlider && !(control is Toggle) && !(control is Dropdown) && !(control is TMP_Dropdown))
            return false;
        if (textOrSlider && control.GetComponentsInParent<MonoBehaviour>(true)
            .Any(c => c != null && (c.GetType().Name == "HealthBar" || c.GetType().Name == "HealthBar1")))
            return false;
        if (control is Toggle && !sheetController
            && (control.GetComponentInParent<Dropdown>(true) != null
                || control.GetComponentInParent<TMP_Dropdown>(true) != null))
            return false;
        return true;
    }

    public static List<PersistentFieldId> Fields(Scene scene) =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PersistentFieldId>(true)).ToList();

    private static List<Component> Components(Scene scene) =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToList();

    private static void RequireCleanScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scene changes before running the stable ID tool.");
    }

    private static void Restore(SceneSetup[] setup)
    {
        if (setup.Length > 0 && setup.Any(s => s.isLoaded && s.isActive))
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        else
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static string ControlPath(Transform item)
    {
        var parts = new List<string>();
        while (item != null)
        {
            parts.Add(item.GetSiblingIndex().ToString("D4") + "_" + item.name);
            item = item.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (line[i] == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(line[i]);
        }
        values.Add(value.ToString());
        return values;
    }
}
