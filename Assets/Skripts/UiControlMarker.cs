using UnityEngine;

// Repeated controls are scoped to their owning page or inventory cell.
public enum UiControlRole
{
    InventoryCell, ItemName, ItemDescription, ItemCategory, Weapon, Armor, Bags,
    Magic, Other, Cheger, SelectItemImage, ExportItem, ImportItem, ClearItem,
    ItemImage, ItemImagePanel, PageTitle, PageButton, InactiveInventoryPage
}

[DisallowMultipleComponent]
public sealed class UiControlMarker : MonoBehaviour
{
    [SerializeField] private UiControlRole role;
    [SerializeField] private int index;
    public UiControlRole Role => role;
    public int Index => index;
    public void Configure(UiControlRole value, int order = 0)
    {
        role = value;
        index = order;
    }
}
