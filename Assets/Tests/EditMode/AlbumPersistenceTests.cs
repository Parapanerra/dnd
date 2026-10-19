using System;
using System.IO;
using System.Reflection;
using System.IO.Compression;
using NUnit.Framework;

public sealed class AlbumPersistenceTests
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Test]
    public void AlbumMetadata_RoundTrips_AndLegacyCharacterStartsEmpty()
    {
        var character = new CharacterData("hero") { characterName = "Hero" };
        byte[] oldFile = TaruckBinaryCodec.EncodeCharacterExport(character, "test");
        Assert.That(TaruckBinaryCodec.DecodeCharacterExport(oldFile).albumImages, Is.Empty);

        var image = new AlbumImageData
        {
            id = Guid.NewGuid().ToString("N"), relativeFilePath = "Media/hero/image.png",
            thumbnailRelativePath = "Media/hero/image-thumb.png", title = "Map",
            description = "Dungeon", createdAtUtcTicks = DateTime.UtcNow.Ticks,
            width = 1, height = 1, fileSize = TinyPng.Length, pageIndex = 2,
            contentHash = new string('a', 64)
        };
        character.albumImages.Add(image);
        CharacterData decoded = TaruckBinaryCodec.DecodeCharacterExport(
            TaruckBinaryCodec.EncodeCharacterExport(character, "test"));
        Assert.That(decoded.albumImages.Count, Is.EqualTo(1));
        Assert.That(decoded.albumImages[0].title, Is.EqualTo("Map"));
        Assert.That(decoded.albumImages[0].relativeFilePath, Is.EqualTo(image.relativeFilePath));
        Assert.That(decoded.albumImages[0].pageIndex, Is.EqualTo(2));
    }

    [Test]
    public void AlbumMetadata_RoundTrips_FiftyTwoImagesOnEachPage()
    {
        var character = new CharacterData("hero") { characterName = "Hero" };
        for (int page = 0; page < 5; page++)
        for (int index = 0; index < 52; index++)
        {
            string id = Guid.NewGuid().ToString("N");
            character.albumImages.Add(new AlbumImageData
            {
                id = id, relativeFilePath = "Media/hero/" + id + ".png",
                thumbnailRelativePath = "Media/hero/" + id + "-thumb.png",
                createdAtUtcTicks = DateTime.UtcNow.Ticks,
                width = 1, height = 1, fileSize = TinyPng.Length,
                pageIndex = page, sortOrder = index, contentHash = new string('a', 64)
            });
        }

        CharacterData decoded = TaruckBinaryCodec.DecodeCharacterExport(
            TaruckBinaryCodec.EncodeCharacterExport(character, "test"));
        Assert.That(decoded.albumImages.Count, Is.EqualTo(260));
        for (int page = 0; page < 5; page++)
            Assert.That(decoded.albumImages.FindAll(image => image.pageIndex == page).Count, Is.EqualTo(52));
    }

    [Test]
    public void CharacterExport_CarriesAlbumMedia_AndRestoresFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "TaruckAlbumTransfer_" + Guid.NewGuid().ToString("N"));
        try
        {
            var character = new CharacterData("source");
            var image = new AlbumImageData
            {
                id = Guid.NewGuid().ToString("N"), relativeFilePath = "Media/old/image.png",
                thumbnailRelativePath = "Media/old/thumb.png", createdAtUtcTicks = DateTime.UtcNow.Ticks,
                width = 1, height = 1, fileSize = TinyPng.Length, contentHash = new string('a', 64),
                exportImageBytes = TinyPng, exportThumbnailBytes = TinyPng
            };
            character.albumImages.Add(image);
            CharacterData imported = TaruckBinaryCodec.DecodeCharacterExport(
                TaruckBinaryCodec.EncodeCharacterExport(character, "test", true));
            CollectionAssert.AreEqual(TinyPng, imported.albumImages[0].exportImageBytes);
            CollectionAssert.AreEqual(TinyPng, imported.albumImages[0].exportThumbnailBytes);

            Type storeType = Type.GetType("AlbumMediaStore, Assembly-CSharp", true);
            object store = Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { root }, null);
            Type importType = Type.GetType("CharacterCollectionImportService, Assembly-CSharp", true);
            importType.GetMethod("RestoreAlbumMedia", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                new object[] { imported, store, new System.Collections.Generic.List<AlbumImageData>() });

            Assert.That(imported.albumImages[0].relativeFilePath, Does.StartWith("Media/"));
            Assert.That(imported.albumImages[0].relativeFilePath, Is.Not.EqualTo(image.relativeFilePath));
            CollectionAssert.AreEqual(TinyPng, (byte[])storeType.GetMethod("Read", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(store, new object[] { imported.albumImages[0].relativeFilePath }));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void AlbumArchive_ReadsPhotosFromTransferFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "TaruckAlbumArchive_" + Guid.NewGuid().ToString("N") + ".tchar");
        try
        {
            var character = new CharacterData("hero");
            character.albumImages.Add(new AlbumImageData
            {
                id = Guid.NewGuid().ToString("N"), relativeFilePath = "Media/old/image.png",
                thumbnailRelativePath = "Media/old/thumb.png", createdAtUtcTicks = DateTime.UtcNow.Ticks,
                width = 1, height = 1, fileSize = TinyPng.Length, contentHash = new string('a', 64)
            });
            Type archiveType = Type.GetType("TaruckAlbumArchive, Assembly-CSharp", true);
            MethodInfo entry = archiveType.GetMethod("Entry", BindingFlags.Static | BindingFlags.NonPublic);
            using (var file = File.Create(path))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "taruck/metadata", TaruckBinaryCodec.EncodeCharacterExport(character, "test"));
                WriteEntry(archive, (string)entry.Invoke(null, new object[] { character.albumImages[0].relativeFilePath }), TinyPng);
                WriteEntry(archive, (string)entry.Invoke(null, new object[] { character.albumImages[0].thumbnailRelativePath }), TinyPng);
            }
            CharacterData imported = (CharacterData)archiveType.GetMethod("ReadCharacter",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path });
            byte[] actual = (byte[])archiveType.GetMethod("ReadImage", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { imported.albumImages[0].exportArchivePath,
                    imported.albumImages[0].exportImageEntry });
            CollectionAssert.AreEqual(TinyPng, actual);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        using (Stream stream = archive.CreateEntry(name).Open())
            stream.Write(bytes, 0, bytes.Length);
    }

    [Test]
    public void AlbumImageImport_RollsBackMediaWhenSaveFails()
    {
        string root = Path.Combine(Path.GetTempPath(), "TaruckAlbumImport_" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "source.png");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllBytes(source, TinyPng);
            var character = new CharacterData("hero");
            Type storeType = Type.GetType("AlbumMediaStore, Assembly-CSharp", true);
            object store = Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { root }, null);
            Type serviceType = Type.GetType("AlbumImageImportService, Assembly-CSharp", true);
            object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { character, store, (Action)(() => { }),
                    (Func<string>)(() => "Simulated save failure"), 40 }, null);
            MethodInfo add = serviceType.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(((TargetInvocationException)Assert.Throws<TargetInvocationException>(() =>
                add.Invoke(service, new object[] { source, 0 }))).InnerException,
                Is.InstanceOf<IOException>());
            Assert.That(character.albumImages, Is.Empty);
            string media = Path.Combine(root, "Media");
            Assert.That(Directory.Exists(media) ? Directory.GetFiles(media, "*", SearchOption.AllDirectories).Length : 0,
                Is.Zero);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void AlbumStore_WritesAndReadsWithoutEscapingCharacterDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "TaruckAlbumTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Type type = Type.GetType("AlbumMediaStore, Assembly-CSharp", true);
            object store = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { root }, null);
            MethodInfo add = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo read = type.GetMethod("Read", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo delete = type.GetMethod("Delete", BindingFlags.Instance | BindingFlags.NonPublic);
            var image = (AlbumImageData)add.Invoke(store, new object[] { "hero", TinyPng, TinyPng, 1, 1 });
            CollectionAssert.AreEqual(TinyPng, (byte[])read.Invoke(store, new object[] { image.relativeFilePath }));
            Assert.That(image.relativeFilePath, Does.StartWith("Media/"));
            Assert.That(((TargetInvocationException)Assert.Throws<TargetInvocationException>(() =>
                read.Invoke(store, new object[] { "Media/../outside.png" }))).InnerException,
                Is.InstanceOf<InvalidDataException>());
            delete.Invoke(store, new object[] { image });
            Assert.That(((TargetInvocationException)Assert.Throws<TargetInvocationException>(() =>
                read.Invoke(store, new object[] { image.relativeFilePath }))).InnerException,
                Is.InstanceOf<FileNotFoundException>());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void ResetAlbum_RemovesEveryPageAndItsMediaAfterSaving()
    {
        string root = Path.Combine(Path.GetTempPath(), "TaruckAlbumReset_" + Guid.NewGuid().ToString("N"));
        try
        {
            Type storeType = Type.GetType("AlbumMediaStore, Assembly-CSharp", true);
            object store = Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { root }, null);
            MethodInfo add = storeType.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            var character = new CharacterData("hero");
            for (int page = 0; page < 5; page += 4)
            {
                var image = (AlbumImageData)add.Invoke(store, new object[] { "hero", TinyPng, TinyPng, 1, 1 });
                image.pageIndex = page;
                character.albumImages.Add(image);
            }
            AlbumImageData[] images = character.albumImages.ToArray();
            Type resetType = Type.GetType("AlbumResetService, Assembly-CSharp", true);
            object reset = Activator.CreateInstance(resetType, true);
            int saves = 0;
            resetType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reset,
                new object[] { character, store, (Action)(() => saves++), (Func<string>)(() => null) });

            Assert.That(saves, Is.EqualTo(1));
            Assert.That(character.albumImages, Is.Empty);
            foreach (AlbumImageData image in images)
            {
                Assert.That(File.Exists(Path.Combine(root, image.relativeFilePath.Replace('/', Path.DirectorySeparatorChar))), Is.False);
                Assert.That(File.Exists(Path.Combine(root, image.thumbnailRelativePath.Replace('/', Path.DirectorySeparatorChar))), Is.False);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void ResetAlbum_KeepsMetadataAndFilesWhenSavingFails()
    {
        string root = Path.Combine(Path.GetTempPath(), "TaruckAlbumReset_" + Guid.NewGuid().ToString("N"));
        try
        {
            Type storeType = Type.GetType("AlbumMediaStore, Assembly-CSharp", true);
            object store = Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { root }, null);
            var image = (AlbumImageData)storeType.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(store, new object[] { "hero", TinyPng, TinyPng, 1, 1 });
            var character = new CharacterData("hero");
            character.albumImages.Add(image);
            Type resetType = Type.GetType("AlbumResetService, Assembly-CSharp", true);
            object reset = Activator.CreateInstance(resetType, true);

            var error = Assert.Throws<TargetInvocationException>(() =>
                resetType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reset,
                    new object[] { character, store, (Action)(() => { }), (Func<string>)(() => "write failed") }));
            Assert.That(error.InnerException, Is.InstanceOf<IOException>());
            Assert.That(character.albumImages, Does.Contain(image));
            Assert.That(File.Exists(Path.Combine(root, image.relativeFilePath.Replace('/', Path.DirectorySeparatorChar))), Is.True);
            Assert.That(File.Exists(Path.Combine(root, image.thumbnailRelativePath.Replace('/', Path.DirectorySeparatorChar))), Is.True);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
