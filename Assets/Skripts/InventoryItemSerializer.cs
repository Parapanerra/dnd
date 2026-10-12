// Maps inventory item data to and from its saved scene fields.
internal sealed class InventoryItemSerializer
{
    internal void WriteScene(CharacterSceneData sceneData, string cellKey, InventoryItemExportData data)
    {
        if (sceneData == null || data == null)
            return;

        sceneData.SetString(cellKey + "_Name", data.itemName ?? "");
        sceneData.SetString(cellKey + "_Description", data.itemDescription ?? "");
        sceneData.SetInt(cellKey + "_Category", data.category);
        sceneData.SetInt(cellKey + "_Weapon", data.weaponIndex);
        sceneData.SetInt(cellKey + "_Armor", data.armorIndex);
        sceneData.SetInt(cellKey + "_Bags", data.bagsIndex);
        sceneData.SetInt(cellKey + "_Magic", data.magicIndex);
        sceneData.SetInt(cellKey + "_Other", data.otherIndex);
        sceneData.SetInt(cellKey + "_Cheger", data.chegerIndex);

        if (string.IsNullOrWhiteSpace(data.customImageBase64))
            sceneData.DeleteString(cellKey + "_CustomImage");
        else
            sceneData.SetString(cellKey + "_CustomImage", data.customImageBase64);
    }

    internal InventoryItemExportData ReadScene(CharacterSceneData sceneData, string cellKey)
    {
        if (sceneData == null)
            return new InventoryItemExportData();

        return new InventoryItemExportData
        {
            itemName = sceneData.GetString(cellKey + "_Name", ""),
            itemDescription = sceneData.GetString(cellKey + "_Description", ""),
            category = sceneData.GetInt(cellKey + "_Category", 0),
            weaponIndex = sceneData.GetInt(cellKey + "_Weapon", 0),
            armorIndex = sceneData.GetInt(cellKey + "_Armor", 0),
            bagsIndex = sceneData.GetInt(cellKey + "_Bags", 0),
            magicIndex = sceneData.GetInt(cellKey + "_Magic", 0),
            otherIndex = sceneData.GetInt(cellKey + "_Other", 0),
            chegerIndex = sceneData.GetInt(cellKey + "_Cheger", 0),
            customImageBase64 = sceneData.GetString(cellKey + "_CustomImage", "")
        };
    }

}
