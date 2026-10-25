using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Apply the same item feedback to legacy and TMP dropdowns in every scene.
internal static class DropdownItemMotionInstaller
{
    private static float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Canvas.willRenderCanvases -= ScanForRuntimeDropdowns;
        Canvas.willRenderCanvases += ScanForRuntimeDropdowns;
        nextScan = 0f;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void ScanForRuntimeDropdowns()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 1f;
        Install();
    }

    private static void Install()
    {
        foreach (Dropdown dropdown in Object.FindObjectsByType<Dropdown>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            AddToItem(dropdown.template, dropdown.itemText, dropdown.itemImage);
            ConfigurePotionImageDropdown(dropdown);
        }

        foreach (TMP_Dropdown dropdown in Object.FindObjectsByType<TMP_Dropdown>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddToItem(dropdown.template, dropdown.itemText, dropdown.itemImage);
    }

    private static void AddToItem(RectTransform template, Component itemText, Component itemImage)
    {
        if (template == null)
            return;

        Component itemVisual = itemText != null ? itemText : itemImage;
        Toggle item = itemVisual != null
            ? itemVisual.GetComponentInParent<Toggle>(true)
            : template.GetComponentInChildren<Toggle>(true);

        if (item != null && item.transform.IsChildOf(template) &&
            item.GetComponent<DropdownItemMotion>() == null)
            item.gameObject.AddComponent<DropdownItemMotion>();
    }

    private static void ConfigurePotionImageDropdown(UnityEngine.UI.Dropdown dropdown)
    {
        Transform row = dropdown.transform.parent;
        if (row == null || row.parent == null || row.parent.name != "potionsPanel" ||
            !dropdown.name.StartsWith("Dropdown poition"))
            return;

        UnityEngine.UI.ScrollRect scrollRect = dropdown.template != null
            ? dropdown.template.GetComponent<UnityEngine.UI.ScrollRect>() : null;
        if (scrollRect != null)
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

        if (dropdown.GetComponent<CameraPanBlockOnScrollView>() == null)
            dropdown.gameObject.AddComponent<CameraPanBlockOnScrollView>();
        if (dropdown.template != null &&
            dropdown.template.GetComponent<CameraPanBlockOnScrollView>() == null)
            dropdown.template.gameObject.AddComponent<CameraPanBlockOnScrollView>();
    }
}
