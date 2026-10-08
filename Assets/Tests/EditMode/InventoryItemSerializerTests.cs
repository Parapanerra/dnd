using System;
using NUnit.Framework;

public class InventoryItemSerializerTests
{
    [Test]
    public void ExistingCellKeysSurviveSerializerRoundTrip()
    {
        Type sceneType = Type.GetType("CharacterSceneData, TaruckShip.Domain");
        Type serializer = Type.GetType("InventoryItemSerializer, Assembly-CSharp");
        Assert.NotNull(sceneType);
        Assert.NotNull(serializer);

        object existing = Activator.CreateInstance(sceneType, "Inventory");
        sceneType.GetMethod("SetString").Invoke(existing, new object[] { "Inventory_Page_2_Cell_4_Name", "Старий меч" });
        sceneType.GetMethod("SetString").Invoke(existing, new object[] { "Inventory_Page_2_Cell_4_CustomImage", "image-data" });
        sceneType.GetMethod("SetInt").Invoke(existing, new object[] { "Inventory_Page_2_Cell_4_Category", 3 });

        object item = serializer.GetMethod("ReadScene").Invoke(null, new[] { existing, "Inventory_Page_2_Cell_4" });
        Assert.AreEqual("Старий меч", item.GetType().GetField("itemName").GetValue(item));
        Assert.AreEqual("image-data", item.GetType().GetField("customImageBase64").GetValue(item));
        Assert.AreEqual(3, item.GetType().GetField("category").GetValue(item));

        object restored = Activator.CreateInstance(sceneType, "Inventory");
        serializer.GetMethod("WriteScene").Invoke(null, new[] { restored, "Inventory_Page_2_Cell_4", item });
        Assert.AreEqual("Старий меч", sceneType.GetMethod("GetString").Invoke(restored,
            new object[] { "Inventory_Page_2_Cell_4_Name", "" }));
        Assert.AreEqual("image-data", sceneType.GetMethod("GetString").Invoke(restored,
            new object[] { "Inventory_Page_2_Cell_4_CustomImage", "" }));
    }
}
