using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InputFieldTabOrderTool
{
    private sealed class FieldEntry
    {
        public GameObject GameObject;
        public Vector2 Position;
        public float Height;
    }

    private sealed class FieldRow
    {
        public readonly List<FieldEntry> Fields = new List<FieldEntry>();
        public float CenterY;
        public float Tolerance;
    }

    [MenuItem("Tools/DnD/Tab Navigation/Assign Orders In Open Scenes")]
    private static void AssignOrdersInOpenScenes()
    {
        Canvas.ForceUpdateCanvases();

        List<FieldEntry> fields = CollectSceneFields();
        Dictionary<Scene, List<FieldEntry>> fieldsByScene = GroupByScene(fields);

        Undo.SetCurrentGroupName("Assign Input Field Tab Orders");
        int undoGroup = Undo.GetCurrentGroup();
        HashSet<Scene> changedScenes = new HashSet<Scene>();
        int assignedCount = 0;

        foreach (KeyValuePair<Scene, List<FieldEntry>> sceneFields in fieldsByScene)
        {
            List<FieldEntry> orderedFields = SortByScreenPosition(sceneFields.Value);
            for (int i = 0; i < orderedFields.Count; i++)
            {
                FieldEntry field = orderedFields[i];
                InputFieldTabOrder tabOrder = field.GameObject.GetComponent<InputFieldTabOrder>();
                if (tabOrder == null)
                    tabOrder = Undo.AddComponent<InputFieldTabOrder>(field.GameObject);

                Undo.RecordObject(tabOrder, "Set Input Field Tab Order");
                tabOrder.Order = i + 1;
                EditorUtility.SetDirty(tabOrder);
                changedScenes.Add(field.GameObject.scene);
                assignedCount++;
            }
        }

        foreach (Scene scene in changedScenes)
            EditorSceneManager.MarkSceneDirty(scene);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Assigned Tab Order values to {assignedCount} input fields in the open scenes.");
    }

    [MenuItem("Tools/DnD/Tab Navigation/Assign Orders In Open Scenes", true)]
    private static bool CanAssignOrdersInOpenScenes()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static List<FieldEntry> CollectSceneFields()
    {
        List<FieldEntry> fields = new List<FieldEntry>();
        HashSet<GameObject> addedObjects = new HashSet<GameObject>();

        foreach (InputField input in Object.FindObjectsByType<InputField>(FindObjectsInactive.Include))
            TryAddField(input != null ? input.gameObject : null, fields, addedObjects);

        foreach (TMP_InputField input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            TryAddField(input != null ? input.gameObject : null, fields, addedObjects);

        return fields;
    }

    private static void TryAddField(GameObject gameObject, List<FieldEntry> fields, HashSet<GameObject> addedObjects)
    {
        if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded || !addedObjects.Add(gameObject))
            return;

        RectTransform rectTransform = gameObject.transform as RectTransform;
        if (rectTransform == null)
            return;

        Canvas canvas = gameObject.GetComponentInParent<Canvas>(true);
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float height = Mathf.Abs(
            RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y -
            RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y);

        fields.Add(new FieldEntry
        {
            GameObject = gameObject,
            Position = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position),
            Height = Mathf.Max(1f, height)
        });
    }

    private static Dictionary<Scene, List<FieldEntry>> GroupByScene(List<FieldEntry> fields)
    {
        Dictionary<Scene, List<FieldEntry>> result = new Dictionary<Scene, List<FieldEntry>>();

        foreach (FieldEntry field in fields)
        {
            Scene scene = field.GameObject.scene;
            if (!result.TryGetValue(scene, out List<FieldEntry> sceneFields))
            {
                sceneFields = new List<FieldEntry>();
                result.Add(scene, sceneFields);
            }

            sceneFields.Add(field);
        }

        return result;
    }

    private static List<FieldEntry> SortByScreenPosition(List<FieldEntry> fields)
    {
        fields.Sort((left, right) => right.Position.y.CompareTo(left.Position.y));

        List<FieldRow> rows = new List<FieldRow>();
        foreach (FieldEntry field in fields)
        {
            FieldRow matchingRow = null;
            float closestDistance = float.MaxValue;

            foreach (FieldRow row in rows)
            {
                float distance = Mathf.Abs(field.Position.y - row.CenterY);
                float allowedDistance = Mathf.Max(row.Tolerance, field.Height * 0.5f);
                if (distance <= allowedDistance && distance < closestDistance)
                {
                    matchingRow = row;
                    closestDistance = distance;
                }
            }

            if (matchingRow == null)
            {
                matchingRow = new FieldRow
                {
                    CenterY = field.Position.y,
                    Tolerance = Mathf.Max(8f, field.Height * 0.5f)
                };
                rows.Add(matchingRow);
            }

            matchingRow.Fields.Add(field);
            matchingRow.CenterY = AverageY(matchingRow.Fields);
            matchingRow.Tolerance = Mathf.Max(matchingRow.Tolerance, field.Height * 0.5f);
        }

        rows.Sort((left, right) => right.CenterY.CompareTo(left.CenterY));
        List<FieldEntry> result = new List<FieldEntry>();

        foreach (FieldRow row in rows)
        {
            row.Fields.Sort((left, right) => left.Position.x.CompareTo(right.Position.x));
            result.AddRange(row.Fields);
        }

        return result;
    }

    private static float AverageY(List<FieldEntry> fields)
    {
        float total = 0f;
        foreach (FieldEntry field in fields)
            total += field.Position.y;

        return total / fields.Count;
    }
}
