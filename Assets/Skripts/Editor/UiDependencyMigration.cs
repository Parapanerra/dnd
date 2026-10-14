using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Names are interpreted once during migration, never rewritten into save keys.
public static class UiDependencyMigration
{
    private static readonly Dictionary<string, UiControlRole> ItemRoles = new Dictionary<string, UiControlRole>
    {
        {"itemNameInput", UiControlRole.ItemName}, {"itemDescriptionInput", UiControlRole.ItemDescription},
        {"itemCategoryDropdown", UiControlRole.ItemCategory}, {"Dropdown weapon", UiControlRole.Weapon},
        {"Dropdown armor", UiControlRole.Armor}, {"Dropdown bags", UiControlRole.Bags},
        {"Dropdown magic", UiControlRole.Magic}, {"Dropdown other", UiControlRole.Other},
        {"Dropdown cheger", UiControlRole.Cheger}, {"customItemImageButton", UiControlRole.SelectItemImage},
        {"exportItemButton", UiControlRole.ExportItem}, {"importItemButton", UiControlRole.ImportItem},
        {"clearItemButton", UiControlRole.ClearItem}, {"customItemImage", UiControlRole.ItemImage},
        {"PanelForphoto", UiControlRole.ItemImagePanel}
    };

    public static void Migrate(Scene scene)
    {
        Transform[] transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        MapCameraController[] cameras = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapCameraController>(true)).ToArray();
        foreach (Transform t in transforms)
        {
            bool inventory = scene.name == "inventory";
            bool book = scene.name == "spelBook";
            if ((inventory || book) && t.name == "Page")
            {
                SceneRoleMarker marker = t.GetComponent<SceneRoleMarker>() ?? t.gameObject.AddComponent<SceneRoleMarker>();
                marker.Configure(SceneRole.MainPage, false);
                EditorUtility.SetDirty(marker);
            }
            if (inventory && t.name.StartsWith("Page (", StringComparison.Ordinal))
                Mark(t, UiControlRole.InactiveInventoryPage);
            if (inventory && (t.name == "Cels" || t.name.StartsWith("Cels (", StringComparison.Ordinal)))
                Mark(t, UiControlRole.InventoryCell);
            if (inventory && ItemRoles.TryGetValue(t.name, out UiControlRole role))
                Mark(t, role);
            if (t.GetComponent<Button>() != null && TryPageIndex(t.name, out int pageIndex))
                Mark(t, UiControlRole.PageButton, pageIndex);
            if (t.GetComponentInParent<Button>(true) == null)
            {
                string text = t.GetComponent<Text>()?.text ?? t.GetComponent<TMP_Text>()?.text ?? "";
                bool inventoryTitle = inventory && (text.IndexOf("Сторінка", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Page #", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Страница №", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.StartsWith("pageTitle", StringComparison.OrdinalIgnoreCase));
                bool bookTitle = book && t.parent != null && t.parent.name == "Image" &&
                    t.parent.parent != null && t.parent.parent.name == "Page" && t.name.StartsWith("Text", StringComparison.OrdinalIgnoreCase);
                if (inventoryTitle || bookTitle) Mark(t, UiControlRole.PageTitle);
            }
            Toggle toggle = t.GetComponent<Toggle>();
            if (toggle != null && (t.name == "Toggle" || t.name.StartsWith("Toggle (", StringComparison.Ordinal)) &&
                t.GetComponentInParent<Dropdown>(true) == null && t.GetComponentInParent<TMP_Dropdown>(true) == null)
            {
                ResourceToggleMarker marker = t.GetComponent<ResourceToggleMarker>() ?? t.gameObject.AddComponent<ResourceToggleMarker>();
                // Capture the number before any visual rename.
                int number = 0;
                int open = t.name.LastIndexOf('(');
                if (open >= 0) int.TryParse(t.name.Substring(open + 1).TrimEnd(')'), out number);
                marker.Configure(number);
                EditorUtility.SetDirty(marker);
            }
            Slider slider = t.GetComponent<Slider>();
            if (slider != null && slider.fillRect == null)
            {
                RectTransform fill = t.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "fild");
                if (fill != null) { slider.fillRect = fill; EditorUtility.SetDirty(slider); }
            }
            OverlayBoundsFitter overlay = t.GetComponent<OverlayBoundsFitter>();
            if (overlay != null && cameras.Length == 1)
            {
                SerializedObject serialized = new SerializedObject(overlay);
                serialized.FindProperty("mapBounds").objectReferenceValue = cameras[0];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        // Reuse the legacy installer to capture precisely its existing group order.
        Type installer = Type.GetType("ToggleSequenceSceneInstaller, Assembly-CSharp", true);
        object instance = Activator.CreateInstance(installer, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { null }, null);
        installer.GetMethod("CaptureConfiguration", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
        Validate(scene);
    }

    public static void Validate(Scene scene)
    {
        var errors = new List<string>();
        UiControlMarker[] markers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UiControlMarker>(true)).ToArray();
        if (scene.name == "inventory" || scene.name == "spelBook")
        {
            if (new SceneRoleLookup(scene).Find(SceneRole.MainPage) == null) errors.Add("Missing main page role");
            if (!markers.Any(m => m.Role == UiControlRole.PageButton)) errors.Add("Missing page buttons");
            if (!markers.Any(m => m.Role == UiControlRole.PageTitle)) errors.Add("Missing page title");
        }
        if (scene.name == "inventory" && !markers.Any(m => m.Role == UiControlRole.InventoryCell))
            errors.Add("Missing inventory cell markers");
        foreach (UiControlMarker cell in markers.Where(m => m.Role == UiControlRole.InventoryCell))
        {
            UiControlMarker[] controls = cell.GetComponentsInChildren<UiControlMarker>(true);
            foreach (UiControlRole role in ItemRoles.Values)
            {
                UiControlMarker[] matches = controls.Where(m => m.Role == role).ToArray();
                if (matches.Length != 1) errors.Add(cell.name + ": expected one " + role + ", found " + matches.Length);
                else if (!HasExpectedComponent(matches[0])) errors.Add(cell.name + ": invalid component for " + role);
            }
        }
        foreach (var group in markers.Where(m => m.Role == UiControlRole.PageButton).GroupBy(m => m.Index))
            if (group.Count() > 1) errors.Add("Duplicate page index " + group.Key);
        foreach (UiControlMarker marker in markers.Where(m => m.Role == UiControlRole.PageButton))
            if (marker.Index < 0 || marker.GetComponent<Button>() == null) errors.Add("Invalid page button " + marker.name);
        foreach (ToggleSequenceConfig config in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ToggleSequenceConfig>(true)))
        {
            if (config.Toggles == null || config.Toggles.Length == 0 || config.Toggles.Any(t => t == null) ||
                config.Toggles.Distinct().Count() != config.Toggles.Length)
                errors.Add("Invalid toggle sequence " + config.name);
        }
        if (errors.Count > 0) throw new InvalidOperationException(scene.path + ": " + string.Join("; ", errors));
    }

    private static bool HasExpectedComponent(UiControlMarker marker)
    {
        switch (marker.Role)
        {
            case UiControlRole.ItemName: case UiControlRole.ItemDescription:
                return marker.GetComponent<InputField>() != null || marker.GetComponent<TMP_InputField>() != null;
            case UiControlRole.ItemCategory: case UiControlRole.Weapon: case UiControlRole.Armor:
            case UiControlRole.Bags: case UiControlRole.Magic: case UiControlRole.Other: case UiControlRole.Cheger:
                return marker.GetComponent<Dropdown>() != null || marker.GetComponent<TMP_Dropdown>() != null;
            case UiControlRole.SelectItemImage: case UiControlRole.ExportItem: case UiControlRole.ImportItem: case UiControlRole.ClearItem:
                return marker.GetComponent<Button>() != null;
            case UiControlRole.ItemImage: return marker.GetComponent<Image>() != null;
            default: return true;
        }
    }
    private static void Mark(Transform t, UiControlRole role, int index = 0)
    {
        UiControlMarker marker = t.GetComponent<UiControlMarker>() ?? t.gameObject.AddComponent<UiControlMarker>();
        marker.Configure(role, index);
        EditorUtility.SetDirty(marker);
    }
    private static bool TryPageIndex(string name, out int index)
    {
        index = 0;
        if (name == "page") return true;
        return name.StartsWith("page (", StringComparison.Ordinal) && name.EndsWith(")", StringComparison.Ordinal) &&
            int.TryParse(name.Substring(6).TrimEnd(')'), out index);
    }
}
