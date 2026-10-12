using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presents inventory categories without owning item data or persistence.
internal sealed class InventoryItemCategoryView
{
    private readonly int[] itemCategoryIds =
    {
        AppConfig.Inventory.WeaponCategory,
        AppConfig.Inventory.ArmorCategory,
        AppConfig.Inventory.BagsCategory,
        AppConfig.Inventory.MagicCategory,
        AppConfig.Inventory.OtherCategory,
        AppConfig.Inventory.ChegerCategory
    };
    private readonly Dropdown categoryDropdown;
    private readonly TMP_Dropdown categoryTmpDropdown;
    private readonly Dropdown[] itemDropdowns;
    private readonly TMP_Dropdown[] itemTmpDropdowns;
    private readonly GameObject customImagePanel;
    private readonly RuntimeLocalization localization;

    internal InventoryItemCategoryView(
        Dropdown categoryDropdown, TMP_Dropdown categoryTmpDropdown,
        Dropdown[] itemDropdowns, TMP_Dropdown[] itemTmpDropdowns,
        GameObject customImagePanel, RuntimeLocalization localization)
    {
        this.categoryDropdown = categoryDropdown;
        this.categoryTmpDropdown = categoryTmpDropdown;
        this.itemDropdowns = itemDropdowns;
        this.itemTmpDropdowns = itemTmpDropdowns;
        this.customImagePanel = customImagePanel;
        this.localization = localization;
    }

    internal void RefreshOptions()
    {
        if (localization == null)
            return;

        string[] labels =
        {
            LabelFor(AppConfig.Inventory.WeaponCategory, localization),
            LabelFor(AppConfig.Inventory.ArmorCategory, localization),
            LabelFor(AppConfig.Inventory.BagsCategory, localization),
            LabelFor(AppConfig.Inventory.MagicCategory, localization),
            LabelFor(AppConfig.Inventory.OtherCategory, localization),
            LabelFor(AppConfig.Inventory.ChegerCategory, localization),
            LabelFor(AppConfig.Inventory.CustomCategory, localization)
        };

        if (categoryDropdown != null)
        {
            int value = categoryDropdown.value;
            categoryDropdown.ClearOptions();
            categoryDropdown.AddOptions(new List<string>(labels));
            categoryDropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, categoryDropdown.options.Count - 1));
            categoryDropdown.RefreshShownValue();
        }

        if (categoryTmpDropdown != null)
        {
            int value = categoryTmpDropdown.value;
            categoryTmpDropdown.ClearOptions();
            categoryTmpDropdown.AddOptions(new List<string>(labels));
            categoryTmpDropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, categoryTmpDropdown.options.Count - 1));
            categoryTmpDropdown.RefreshShownValue();
        }
    }

    internal static string LabelFor(int category, RuntimeLocalization textCatalog)
    {
        string key;
        switch (category)
        {
            case AppConfig.Inventory.WeaponCategory: key = "Зброя"; break;
            case AppConfig.Inventory.ArmorCategory: key = "Броня"; break;
            case AppConfig.Inventory.BagsCategory: key = "Сумки"; break;
            case AppConfig.Inventory.MagicCategory: key = "Магія"; break;
            case AppConfig.Inventory.OtherCategory: key = "Інше"; break;
            case AppConfig.Inventory.ChegerCategory: key = "Скарби"; break;
            case AppConfig.Inventory.CustomCategory: key = "Своя картинка"; break;
            default: return category.ToString();
        }
        return textCatalog != null ? textCatalog.Translate(key) : key;
    }

    internal void ShowCategory(int category)
    {
        for (int index = 0; index < itemCategoryIds.Length; index++)
        {
            bool visible = category == itemCategoryIds[index];
            if (itemDropdowns[index] != null)
                itemDropdowns[index].gameObject.SetActive(visible);
            if (itemTmpDropdowns[index] != null)
                itemTmpDropdowns[index].gameObject.SetActive(visible);
        }

        if (customImagePanel != null)
            customImagePanel.SetActive(category == AppConfig.Inventory.CustomCategory);

        RefreshShownValue();
    }

    private void RefreshShownValue()
    {
        if (categoryDropdown != null)
            categoryDropdown.RefreshShownValue();
        if (categoryTmpDropdown != null)
            categoryTmpDropdown.RefreshShownValue();
    }
}