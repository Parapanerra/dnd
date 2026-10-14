using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Afix2ItemValidationTests
{
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
    private const string ValidPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
    private static InventoryItemExportData Valid() => new InventoryItemExportData { itemName = "Potion", itemDescription = "One dose", category = 0 };
    private static InventoryItemOptionCounts Options() => new InventoryItemOptionCounts
    {
        Category = 7, Weapon = 20, Armor = 20, Bags = 20, Magic = 20, Other = 20, Cheger = 20
    };

    [Test]
    public void RejectsUnknownCategoryAndEveryNegativeIndex()
    {
        InventoryItemExportData item = Valid();
        item.category = 7;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item));
        item.category = -1;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item));
        item.category = 0;
        foreach (FieldInfo field in IndexFields())
        {
            field.SetValue(item, -1);
            Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item), field.Name);
            field.SetValue(item, 0);
        }
    }

    [Test]
    public void RejectsEveryOutOfRangeIndexBeforeUiCanClampIt()
    {
        InventoryItemExportData item = Valid();
        item.category = 6;
        InventoryItemValidator.Validate(item, Options());
        item.category = 7;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item, Options()));
        item.category = 0;
        foreach (FieldInfo field in IndexFields())
        {
            field.SetValue(item, 20);
            Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item, Options()), field.Name);
            field.SetValue(item, 0);
        }
        InventoryItemOptionCounts missing = Options();
        missing.Weapon = 0;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item, missing));
    }

    [Test]
    public void RejectsOversizedTextAndKeepsLegacyFixtureValid()
    {
        InventoryItemExportData item = Valid();
        item.itemName = new string('n', InventoryItemValidator.MaximumNameLength + 1);
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item));
        item = Valid();
        item.itemDescription = new string('d', InventoryItemValidator.MaximumDescriptionLength + 1);
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.Validate(item));
        string fixture = File.ReadAllText(Path.Combine(Application.dataPath, "Tests", "Fixtures", "Legacy", "legacy_item_v1.json"));
        Assert.AreEqual("Тестовий артефакт", new LegacyJsonImporter().Item(fixture).itemName);
    }

    [Test]
    public void RejectsUnsafeImageDimensionsBeforeNativeDecode()
    {
        byte[] png = Convert.FromBase64String(ValidPng);
        InventoryItemValidator.ValidateImage(png);
        byte[] width = (byte[])png.Clone();
        width[16] = 0; width[17] = 0; width[18] = 0x20; width[19] = 0;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.ValidateImage(width));
        byte[] pixels = (byte[])png.Clone();
        pixels[16] = 0; pixels[17] = 0; pixels[18] = 0x10; pixels[19] = 0;
        pixels[20] = 0; pixels[21] = 0; pixels[22] = 0x10; pixels[23] = 0;
        InventoryItemValidator.ValidateImage(pixels); // exactly 4096 x 4096 is allowed
        pixels[16] = 0;
        pixels[17] = 0;
        pixels[18] = 0x10;
        pixels[19] = 1;
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.ValidateImage(pixels));
        Assert.Throws<InvalidDataException>(() => InventoryItemValidator.ValidateImage(new byte[] { 0x89, 0x50, 0x4E, 0x47, 13, 10, 26, 10 }));
    }

    [Test]
    public void BothLegacyAndBinaryReadersRejectLogicalCorruption()
    {
        InventoryItemExportData invalid = Valid();
        invalid.category = 99;
        string json = JsonUtility.ToJson(invalid);
        Assert.Throws<InvalidDataException>(() => new LegacyJsonImporter().Item(json));

        Type codec = typeof(TaruckBinaryCodec);
        byte[] body = (byte[])codec.GetMethod("EncodeItemBody", Hidden).Invoke(null, new object[] { invalid, false });
        Type fileType = Type.GetType("TaruckFileType, TaruckShip.Persistence", true);
        byte[] file = (byte[])codec.GetMethod("Envelope", Hidden).Invoke(null,
            new[] { Enum.Parse(fileType, "ItemExport"), (object)body, "test", null });
        Assert.Throws<InvalidDataException>(() => TaruckBinaryCodec.DecodeItemExport(file));
    }

    [Test]
    public void ImportFailureRestoresPreviousUiAndSavedCandidate()
    {
        Type committer = Type.GetType("InventoryItemImportCommitter, Assembly-CSharp", true);
        MethodInfo commit = committer.GetMethod("Commit", BindingFlags.Static | BindingFlags.NonPublic);
        InventoryItemExportData previous = Valid();
        InventoryItemExportData candidate = Valid(); candidate.itemName = "Replacement";
        InventoryItemExportData displayed = previous;
        InventoryItemExportData saved = previous;
        Action<InventoryItemExportData> validate = item => InventoryItemValidator.Validate(item, Options());
        Action<InventoryItemExportData> apply = item => { displayed = item; saved = item; };
        Action flush = () => throw new IOException("Simulated write failure");
        Action<InventoryItemExportData> restore = item => { displayed = item; saved = item; };
        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() =>
            commit.Invoke(null, new object[] { candidate, previous, validate, apply, flush, restore }));
        Assert.IsInstanceOf<IOException>(failure.InnerException);
        Assert.AreSame(previous, displayed);
        Assert.AreSame(previous, saved);
        candidate.weaponIndex = 20;
        Assert.Throws<TargetInvocationException>(() =>
            commit.Invoke(null, new object[] { candidate, previous, validate, apply, flush, restore }));
        Assert.AreSame(previous, displayed);
    }

    [Test]
    public void DestinationCellRejectsOversizedIndexWithoutChangingFields()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/personag/inventory.unity");
        try
        {
            Type markerType = Type.GetType("UiControlMarker, Assembly-CSharp", true);
            Type cellType = Type.GetType("InventoryItemCell, Assembly-CSharp", true);
            Component marker = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren(markerType, true))
                .First(m => m.GetType().GetProperty("Role").GetValue(m).ToString() == "InventoryCell");
            Component cell = marker.GetComponent(cellType) ?? marker.gameObject.AddComponent(cellType);
            BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            cellType.GetMethod("Initialize", instance).Invoke(cell, new object[] { 0, 0, null, null });
            InventoryItemExportData previous = Valid();
            cellType.GetMethod("ApplyData", instance).Invoke(cell, new object[] { previous, false });
            InventoryItemExportData bad = Valid(); bad.weaponIndex = 20;
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(() =>
                cellType.GetMethod("ValidateImportedData", instance).Invoke(cell, new object[] { bad }));
            Assert.IsInstanceOf<InvalidDataException>(error.InnerException);
            InventoryItemExportData current = (InventoryItemExportData)cellType.GetMethod("ReadCurrentData", instance).Invoke(cell, null);
            Assert.AreEqual(previous.itemName, current.itemName);
            Assert.AreEqual(0, current.weaponIndex);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    private static FieldInfo[] IndexFields() => typeof(InventoryItemExportData).GetFields()
        .Where(field => field.Name.EndsWith("Index", StringComparison.Ordinal)).ToArray();
}
