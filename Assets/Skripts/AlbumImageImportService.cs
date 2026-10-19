using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Owns validation, conversion and rollback for one album image.
internal sealed class AlbumImageImportService
{
    private const int MaximumSourceBytes = 64 * 1024 * 1024;
    private readonly CharacterData character;
    private readonly AlbumMediaStore mediaStore;
    private readonly Action save;
    private readonly Func<string> saveError;
    private readonly int imagesPerPage;

    internal AlbumImageImportService(CharacterData character, AlbumMediaStore mediaStore,
        Action save, Func<string> saveError, int imagesPerPage)
    {
        this.character = character ?? throw new ArgumentNullException(nameof(character));
        this.mediaStore = mediaStore ?? throw new ArgumentNullException(nameof(mediaStore));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
        this.saveError = saveError ?? throw new ArgumentNullException(nameof(saveError));
        this.imagesPerPage = imagesPerPage;
    }

    internal void Add(string path, int page)
    {
        Texture2D source = null;
        Texture2D thumbnail = null;
        Texture2D resized = null;
        AlbumImageData added = null;
        try
        {
            int count = character.albumImages.Count(x => x.pageIndex == page);
            if (count >= imagesPerPage) throw new InvalidDataException("Album page is full");
            if (new FileInfo(path).Length > MaximumSourceBytes)
                throw new InvalidDataException("Image file is too large");
            byte[] bytes = File.ReadAllBytes(path);
            source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(bytes)) throw new InvalidDataException("Image cannot be decoded");
            float scale = Mathf.Min(1f, Mathf.Min(
                4096f / Mathf.Max(source.width, source.height),
                Mathf.Sqrt(16f * 1024f * 1024f / ((float)source.width * source.height))));
            Texture2D stored = source;
            if (scale < 1f)
            {
                resized = new TextureResizer().Resize(source,
                    Mathf.Max(1, Mathf.FloorToInt(source.width * scale)),
                    Mathf.Max(1, Mathf.FloorToInt(source.height * scale)));
                stored = resized;
            }
            bool png = bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50;
            byte[] storedBytes = png ? stored.EncodeToPNG() : stored.EncodeToJPG(85);
            if (storedBytes.Length > InventoryItemValidator.MaximumImageBytes)
                storedBytes = stored.EncodeToJPG(80);
            InventoryItemValidator.ValidateImage(storedBytes);
            float thumbScale = Mathf.Min(1f, 256f / Mathf.Max(stored.width, stored.height));
            thumbnail = new TextureResizer().Resize(stored,
                Mathf.Max(1, Mathf.RoundToInt(stored.width * thumbScale)),
                Mathf.Max(1, Mathf.RoundToInt(stored.height * thumbScale)));
            added = mediaStore.Add(character.id, storedBytes, thumbnail.EncodeToJPG(80),
                stored.width, stored.height);
            added.pageIndex = page;
            added.sortOrder = count;
            character.albumImages.Add(added);
            save();
            string error = saveError();
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
        catch
        {
            if (added != null)
            {
                character.albumImages.Remove(added);
                try { mediaStore.Delete(added); }
                catch (Exception cleanupError) { Debug.LogError(cleanupError); }
            }
            throw;
        }
        finally
        {
            DestroyTexture(source);
            DestroyTexture(resized);
            DestroyTexture(thumbnail);
        }
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
        else UnityEngine.Object.DestroyImmediate(texture);
    }
}
