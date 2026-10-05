using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LegacyFieldMapTool
{
    private const string OutputDirectory = "Assets/Refactoring/Baseline";
    private const string CsvPath = OutputDirectory + "/LegacyFieldMap_v1.csv";
    private const string ReportPath = OutputDirectory + "/LegacyFieldMap_v1_REPORT.md";
    private const string RequestFileName = ".codex-generate-legacy-map";

    private sealed class MapRow
    {
        public string SourceCommit;
        public string SceneGuid;
        public string ScenePath;
        public string SceneName;
        public string Controller;
        public string ControlType;
        public string LegacyCollection;
        public int LegacyIndex;
        public int TypeIndex;
        public string LegacyKey;
        public string ControlPath;
        public string HierarchyPath;
        public string ProposedStableId;
    }

    private sealed class SceneSummary
    {
        public string SceneName;
        public string ScenePath;
        public string SceneGuid;
        public string Controller;
        public int InputFieldCount;
        public int TmpInputFieldCount;
        public int ToggleCount;
        public int SliderCount;
        public int DropdownCount;
        public int TmpDropdownCount;
        public int ExcludedHealthControls;
        public int ExcludedDropdownTemplateToggles;
    }

    [MenuItem("Tools/DnD/Persistence/Generate Legacy Field Map v1")]
    private static void GenerateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Generate();
        EditorUtility.DisplayDialog(
            "Legacy field map",
            "Generated:\n" + CsvPath + "\n" + ReportPath,
            "OK");
    }

    public static void GenerateFromCommandLine()
    {
        Generate();
    }

    [InitializeOnLoadMethod]
    private static void QueueRequestedGeneration()
    {
        string requestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RequestFileName);
        if (!File.Exists(requestPath))
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    UnityEngine.Debug.LogError(
                        "Legacy field map request was not run because an open scene has unsaved changes. " +
                        "Save or discard those changes, then use Tools/DnD/Persistence/Generate Legacy Field Map v1.");
                    return;
                }
            }

            try
            {
                Generate();
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        };
    }

    private static void Generate()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        List<MapRow> rows = new List<MapRow>();
        List<SceneSummary> summaries = new List<SceneSummary>();
        string sourceCommit = ReadSourceCommit();

        try
        {
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && IsCharacterSheetScene(Path.GetFileNameWithoutExtension(scene.path)))
                .Select(scene => scene.path)
                .ToArray();

            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                CollectScene(scene, scenePath, sourceCommit, rows, summaries);
            }

            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(CsvPath, BuildCsv(rows), new UTF8Encoding(true));
            File.WriteAllText(ReportPath, BuildReport(rows, summaries, sourceCommit), new UTF8Encoding(true));
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log(
                $"Generated legacy persistence baseline: {rows.Count} controls across {summaries.Count} scenes. " +
                $"CSV: {CsvPath}; report: {ReportPath}");
        }
        finally
        {
            if (previousSetup != null && previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static void CollectScene(
        Scene scene,
        string scenePath,
        string sourceCommit,
        List<MapRow> rows,
        List<SceneSummary> summaries)
    {
        bool usesCharacterSheetManager = FindInScene<CharacterSheetManagerScene1>(scene).Count > 0;
        string controller = usesCharacterSheetManager
            ? nameof(CharacterSheetManagerScene1)
            : nameof(CharacterSceneAutoSave);

        List<InputField> inputFields = FindInScene<InputField>(scene);
        List<TMP_InputField> tmpInputFields = FindInScene<TMP_InputField>(scene);
        List<Toggle> toggles = FindInScene<Toggle>(scene);
        List<Slider> sliders = FindInScene<Slider>(scene);
        List<Dropdown> dropdowns = FindInScene<Dropdown>(scene);
        List<TMP_Dropdown> tmpDropdowns = FindInScene<TMP_Dropdown>(scene);

        int excludedHealthControls = inputFields.RemoveAll(item => IsManagedByHealthBar(item.transform));
        excludedHealthControls += tmpInputFields.RemoveAll(item => IsManagedByHealthBar(item.transform));
        excludedHealthControls += sliders.RemoveAll(item => IsManagedByHealthBar(item.transform));

        int excludedDropdownTemplateToggles = 0;
        if (!usesCharacterSheetManager)
            excludedDropdownTemplateToggles = toggles.RemoveAll(item => IsDropdownTemplatePart(item.transform));

        SortByLegacyPath(inputFields);
        SortByLegacyPath(tmpInputFields);
        SortByLegacyPath(toggles);
        SortByLegacyPath(sliders);
        SortByLegacyPath(dropdowns);
        SortByLegacyPath(tmpDropdowns);

        string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            inputFields, "InputField", "inputData", 0, "");
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            tmpInputFields, "TMP_InputField", "inputData", inputFields.Count, "");
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            toggles, "Toggle", "toggleData", 0, "Toggle_");
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            sliders, "Slider", "sliderData", 0, "");
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            dropdowns, "Dropdown", "dropdownData", 0, "Dropdown_");
        AddRows(rows, sourceCommit, sceneGuid, scenePath, scene.name, controller,
            tmpDropdowns, "TMP_Dropdown", "dropdownData", dropdowns.Count, "TMPDropdown_");

        summaries.Add(new SceneSummary
        {
            SceneName = scene.name,
            ScenePath = scenePath,
            SceneGuid = sceneGuid,
            Controller = controller,
            InputFieldCount = inputFields.Count,
            TmpInputFieldCount = tmpInputFields.Count,
            ToggleCount = toggles.Count,
            SliderCount = sliders.Count,
            DropdownCount = dropdowns.Count,
            TmpDropdownCount = tmpDropdowns.Count,
            ExcludedHealthControls = excludedHealthControls,
            ExcludedDropdownTemplateToggles = excludedDropdownTemplateToggles
        });
    }

    private static void AddRows<T>(
        List<MapRow> rows,
        string sourceCommit,
        string sceneGuid,
        string scenePath,
        string sceneName,
        string controller,
        List<T> controls,
        string controlType,
        string legacyCollection,
        int legacyOffset,
        string keyPrefix) where T : Component
    {
        for (int typeIndex = 0; typeIndex < controls.Count; typeIndex++)
        {
            Transform transform = controls[typeIndex].transform;
            string controlPath = GetControlPath(transform);
            rows.Add(new MapRow
            {
                SourceCommit = sourceCommit,
                SceneGuid = sceneGuid,
                ScenePath = scenePath,
                SceneName = sceneName,
                Controller = controller,
                ControlType = controlType,
                LegacyCollection = legacyCollection,
                LegacyIndex = legacyOffset + typeIndex,
                TypeIndex = typeIndex,
                LegacyKey = string.IsNullOrEmpty(keyPrefix) ? "" : keyPrefix + controlPath,
                ControlPath = controlPath,
                HierarchyPath = GetHierarchyPath(transform),
                ProposedStableId = BuildStableId(sceneName, controlType, transform.name, sceneGuid, controlPath)
            });
        }
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        List<T> result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<T>(true));

        return result;
    }

    private static void SortByLegacyPath<T>(List<T> controls) where T : Component
    {
        controls.Sort((left, right) => string.Compare(
            GetControlPath(left.transform),
            GetControlPath(right.transform),
            StringComparison.Ordinal));
    }

    private static bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null &&
               (transform.GetComponentInParent<HealthBar>(true) != null ||
                transform.GetComponentInParent<HealthBar1>(true) != null);
    }

    private static bool IsDropdownTemplatePart(Transform transform)
    {
        while (transform != null)
        {
            if (transform.GetComponent<Dropdown>() != null || transform.GetComponent<TMP_Dropdown>() != null)
                return true;

            transform = transform.parent;
        }

        return false;
    }

    private static string GetControlPath(Transform transform)
    {
        string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name + "/" + path;
        }

        return path;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static string BuildStableId(
        string sceneName,
        string controlType,
        string objectName,
        string sceneGuid,
        string controlPath)
    {
        string source = sceneGuid + "|" + controlType + "|" + controlPath;
        string hash;
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            hash = BitConverter.ToString(bytes, 0, 6).Replace("-", "").ToLowerInvariant();
        }

        return "dnd." + Slug(sceneName) + "." + Slug(controlType) + "." + Slug(objectName) + "." + hash;
    }

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "field";

        StringBuilder result = new StringBuilder();
        bool previousWasSeparator = false;
        foreach (char character in value.ToLowerInvariant())
        {
            if ((character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9'))
            {
                result.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && result.Length > 0)
            {
                result.Append('-');
                previousWasSeparator = true;
            }
        }

        string slug = result.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "field" : slug;
    }

    private static string BuildCsv(List<MapRow> rows)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("mapVersion,sourceCommit,sceneGuid,scenePath,sceneName,controller,controlType,legacyCollection,legacyIndex,typeIndex,legacyKey,getControlPath,hierarchyPath,proposedStableId");
        foreach (MapRow row in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv("1"),
                Csv(row.SourceCommit),
                Csv(row.SceneGuid),
                Csv(row.ScenePath),
                Csv(row.SceneName),
                Csv(row.Controller),
                Csv(row.ControlType),
                Csv(row.LegacyCollection),
                Csv(row.LegacyIndex.ToString()),
                Csv(row.TypeIndex.ToString()),
                Csv(row.LegacyKey),
                Csv(row.ControlPath),
                Csv(row.HierarchyPath),
                Csv(row.ProposedStableId)
            }));
        }

        return csv.ToString();
    }

    private static string BuildReport(
        List<MapRow> rows,
        List<SceneSummary> summaries,
        string sourceCommit)
    {
        List<string> duplicatePaths = rows
            .GroupBy(row => row.SceneGuid + "|" + row.ControlType + "|" + row.ControlPath)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        List<string> duplicateIds = rows
            .GroupBy(row => row.ProposedStableId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        StringBuilder report = new StringBuilder();
        report.AppendLine("# Legacy field map v1 — baseline report");
        report.AppendLine();
        report.AppendLine("- Source commit: `" + sourceCommit + "`");
        report.AppendLine("- Generated (UTC): `" + DateTime.UtcNow.ToString("O") + "`");
        report.AppendLine("- Unity version: `" + Application.unityVersion + "`");
        report.AppendLine("- Persisted controls: `" + rows.Count + "`");
        report.AppendLine();
        report.AppendLine("The generator only reads scenes. It does not reorder, rename, dirty, or save scene objects.");
        report.AppendLine();
        report.AppendLine("| Scene | GUID | Controller | Input | TMP input | Toggle | Slider | Dropdown | TMP dropdown | Excluded health controls | Excluded template toggles |");
        report.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (SceneSummary summary in summaries)
        {
            report.AppendLine(
                "| `" + summary.SceneName + "` | `" + summary.SceneGuid + "` | `" + summary.Controller + "` | " +
                summary.InputFieldCount + " | " + summary.TmpInputFieldCount + " | " + summary.ToggleCount + " | " +
                summary.SliderCount + " | " + summary.DropdownCount + " | " + summary.TmpDropdownCount + " | " +
                summary.ExcludedHealthControls + " | " + summary.ExcludedDropdownTemplateToggles + " |");
        }

        report.AppendLine();
        report.AppendLine("## Validation");
        report.AppendLine();
        report.AppendLine("- Duplicate legacy paths within the same scene and control type: `" + duplicatePaths.Count + "`");
        report.AppendLine("- Duplicate proposed stable IDs: `" + duplicateIds.Count + "`");
        report.AppendLine();
        report.AppendLine("The `proposedStableId` column is the frozen ID source for stage A2. Review it before adding `PersistentFieldId` components.");

        if (duplicatePaths.Count > 0 || duplicateIds.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("## Problems requiring review");
            foreach (string duplicatePath in duplicatePaths)
                report.AppendLine("- Duplicate legacy path: `" + duplicatePath + "`");
            foreach (string duplicateId in duplicateIds)
                report.AppendLine("- Duplicate proposed ID: `" + duplicateId + "`");
        }

        return report.ToString();
    }

    private static string Csv(string value)
    {
        string safe = value ?? "";
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsCharacterSheetScene(string sceneName)
    {
        return sceneName.Contains("cartaPersonaj") ||
               sceneName.Contains("inventory") ||
               sceneName.Contains("informForPerson") ||
               sceneName.Contains("Spels") ||
               sceneName.Contains("spelBook") ||
               sceneName.Contains("petsesn");
    }

    private static string ReadSourceCommit()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index].Equals("-taruckSourceCommit", StringComparison.OrdinalIgnoreCase))
                return arguments[index + 1];
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    return "unknown";

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : "unknown";
            }
        }
        catch (Exception)
        {
            return "unknown";
        }
    }
}
