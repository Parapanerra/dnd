using SimpleFileBrowser;

// Owns both persisted forms of an inventory item; the cell only reads and displays it.
public static class InventoryItemSerializer
{
    public static void WriteScene(CharacterSceneData sceneData, string cellKey, InventoryItemExportData data)
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

    public static InventoryItemExportData ReadScene(CharacterSceneData sceneData, string cellKey)
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

    public static void WriteFile(string path, InventoryItemExportData data, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(
            TaruckTransferFileUtility.EnsureExtension(path, ".titem"),
            TaruckBinaryCodec.EncodeItemExport(data, version));
    }

    public static InventoryItemExportData ReadFile(string path, out byte[] bytes)
    {
        bytes = TaruckTransferFileReader.Read(path);
        return TaruckTransferFileUtility.HasMagic(bytes)
            ? TaruckBinaryCodec.DecodeItemExport(bytes)
            : LegacyJsonImporter.Item(LegacyJsonImporter.DecodeFile(bytes));
    }
}
