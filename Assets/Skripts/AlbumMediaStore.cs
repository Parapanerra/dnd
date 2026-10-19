using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Stores album files separately from the character's binary metadata.
internal sealed class AlbumMediaStore
{
    private readonly string root;

    internal AlbumMediaStore(string persistentDataPath)
    {
        root = Path.GetFullPath(persistentDataPath ?? throw new ArgumentNullException(nameof(persistentDataPath)));
    }

    internal AlbumImageData Add(string characterId, byte[] image, byte[] thumbnail, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(characterId)) throw new InvalidDataException("Missing character ID");
        InventoryItemValidator.ValidateImage(image);
        InventoryItemValidator.ValidateImage(thumbnail);
        if (width < 1 || height < 1 || width > 4096 || height > 4096 ||
            (long)width * height > 16L * 1024 * 1024)
            throw new InvalidDataException("Album image dimensions are too large");

        string id = Guid.NewGuid().ToString("N");
        string directory = "Media/" + Hash(Encoding.UTF8.GetBytes(characterId));
        string extension = image[0] == 0x89 ? ".png" : ".jpg";
        string relative = directory + "/" + id + extension;
        string thumbnailRelative = directory + "/" + id + "-thumb" + (thumbnail[0] == 0x89 ? ".png" : ".jpg");
        string originalPath = Resolve(relative);
        string thumbPath = Resolve(thumbnailRelative);
        Directory.CreateDirectory(Path.GetDirectoryName(originalPath));
        string originalTemp = originalPath + ".tmp";
        string thumbTemp = thumbPath + ".tmp";
        try
        {
            File.WriteAllBytes(originalTemp, image);
            File.WriteAllBytes(thumbTemp, thumbnail);
            File.Move(originalTemp, originalPath);
            File.Move(thumbTemp, thumbPath);
        }
        catch
        {
            DeleteIfExists(originalTemp);
            DeleteIfExists(thumbTemp);
            DeleteIfExists(originalPath);
            DeleteIfExists(thumbPath);
            throw;
        }
        return new AlbumImageData
        {
            id = id, relativeFilePath = relative, thumbnailRelativePath = thumbnailRelative,
            createdAtUtcTicks = DateTime.UtcNow.Ticks, width = width, height = height,
            fileSize = image.Length, contentHash = Hash(image),
            title = "", description = ""
        };
    }

    internal byte[] Read(string relativePath)
    {
        string path = Resolve(relativePath);
        byte[] bytes = File.ReadAllBytes(path);
        InventoryItemValidator.ValidateImage(bytes);
        return bytes;
    }

    internal long FileSize(string relativePath)
    {
        return new FileInfo(Resolve(relativePath)).Length;
    }

    internal void Delete(AlbumImageData image)
    {
        if (image == null) return;
        DeleteIfExists(Resolve(image.relativeFilePath));
        DeleteIfExists(Resolve(image.thumbnailRelativePath));
    }

    private string Resolve(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || !relative.StartsWith("Media/", StringComparison.Ordinal) ||
            relative.Contains("..") || relative.Contains(":") || relative.Contains("\\"))
            throw new InvalidDataException("Unsafe album media path");
        string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        string mediaRoot = Path.Combine(root, "Media") + Path.DirectorySeparatorChar;
        if (!path.StartsWith(mediaRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Album media path escapes storage");
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
    }
}
