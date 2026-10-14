using System;
using System.IO;

public enum InventoryItemCategory
{
    Weapon = 0, Armor = 1, Bags = 2, Magic = 3, Other = 4, Cheger = 5, Custom = 6
}

// Counts come from the destination cell; this model does not depend on Unity UI.
public sealed class InventoryItemOptionCounts
{
    public int Category, Weapon, Armor, Bags, Magic, Other, Cheger;
}

public static class InventoryItemValidator
{
    public const int MaximumNameLength = 256;
    public const int MaximumDescriptionLength = 8192;
    public const int MaximumImageBytes = 16 * 1024 * 1024;
    public const int MaximumImageDimension = 4096;
    public const long MaximumImagePixels = 16L * 1024 * 1024;

    public static void Validate(InventoryItemExportData item, InventoryItemOptionCounts options = null)
    {
        if (item == null) throw new InvalidDataException("Missing item data");
        if (string.IsNullOrWhiteSpace(item.itemName)) throw new InvalidDataException("Item name is empty");
        if (item.itemName.Length > MaximumNameLength) throw new InvalidDataException("Item name is too long");
        if (item.itemDescription != null && item.itemDescription.Length > MaximumDescriptionLength)
            throw new InvalidDataException("Item description is too long");
        if (item.category < (int)InventoryItemCategory.Weapon || item.category > (int)InventoryItemCategory.Custom)
            throw new InvalidDataException("Unknown item category: " + item.category);

        RequireNonnegative(item.weaponIndex, "weapon");
        RequireNonnegative(item.armorIndex, "armor");
        RequireNonnegative(item.bagsIndex, "bags");
        RequireNonnegative(item.magicIndex, "magic");
        RequireNonnegative(item.otherIndex, "other");
        RequireNonnegative(item.chegerIndex, "cheger");
        if (options != null)
        {
            RequireInOptions(item.category, options.Category, "category");
            RequireInOptions(item.weaponIndex, options.Weapon, "weapon");
            RequireInOptions(item.armorIndex, options.Armor, "armor");
            RequireInOptions(item.bagsIndex, options.Bags, "bags");
            RequireInOptions(item.magicIndex, options.Magic, "magic");
            RequireInOptions(item.otherIndex, options.Other, "other");
            RequireInOptions(item.chegerIndex, options.Cheger, "cheger");
        }

        if (!string.IsNullOrEmpty(item.customImageBase64))
        {
            if (item.customImageBase64.Length > ((MaximumImageBytes + 2L) / 3L) * 4L)
                throw new InvalidDataException("Item image is too large");
            byte[] image;
            try { image = Convert.FromBase64String(item.customImageBase64); }
            catch (FormatException error) { throw new InvalidDataException("Invalid item image encoding", error); }
            ValidateImage(image);
        }
    }

    public static void ValidateImage(byte[] image)
    {
        if (image == null || image.Length == 0 || image.Length > MaximumImageBytes)
            throw new InvalidDataException("Invalid item image size");

        int width;
        int height;
        if (IsPng(image))
        {
            if (image.Length < 33 || Read32(image, 8) != 13 || image[12] != 'I' || image[13] != 'H' ||
                image[14] != 'D' || image[15] != 'R')
                throw new InvalidDataException("Invalid PNG header");
            uint w = Read32(image, 16), h = Read32(image, 20);
            if (w > int.MaxValue || h > int.MaxValue) throw new InvalidDataException("Item image dimensions are too large");
            width = (int)w;
            height = (int)h;
        }
        else if (IsJpeg(image))
        {
            ReadJpegDimensions(image, out width, out height);
        }
        else throw new InvalidDataException("Item image must be PNG or JPEG");

        if (width <= 0 || height <= 0 || width > MaximumImageDimension || height > MaximumImageDimension ||
            (long)width * height > MaximumImagePixels)
            throw new InvalidDataException("Item image dimensions are too large");
    }

    private static void RequireNonnegative(int index, string name)
    {
        if (index < 0) throw new InvalidDataException("Negative " + name + " index");
    }
    private static void RequireInOptions(int index, int count, string name)
    {
        if (count <= 0 || index >= count)
            throw new InvalidDataException("Item " + name + " index is outside the available options");
    }
    private static uint Read32(byte[] bytes, int offset) =>
        ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) |
        ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    private static bool IsPng(byte[] b) => b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 &&
        b[2] == 0x4E && b[3] == 0x47 && b[4] == 13 && b[5] == 10 && b[6] == 26 && b[7] == 10;
    private static bool IsJpeg(byte[] b) => b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8 &&
        b[b.Length - 2] == 0xFF && b[b.Length - 1] == 0xD9;

    private static void ReadJpegDimensions(byte[] bytes, out int width, out int height)
    {
        width = height = 0;
        int position = 2;
        while (position < bytes.Length - 2)
        {
            if (bytes[position++] != 0xFF) break;
            while (position < bytes.Length && bytes[position] == 0xFF) position++;
            if (position >= bytes.Length) break;
            int marker = bytes[position++];
            if (marker == 0xD9 || marker == 0xDA) break;
            if (marker == 0xD8 || marker == 0x01 || marker >= 0xD0 && marker <= 0xD7) continue;
            if (position + 2 > bytes.Length) break;
            int segmentLength = (bytes[position] << 8) | bytes[position + 1];
            if (segmentLength < 2 || position + segmentLength > bytes.Length) break;
            if ((marker >= 0xC0 && marker <= 0xC3) || (marker >= 0xC5 && marker <= 0xC7) ||
                (marker >= 0xC9 && marker <= 0xCB) || (marker >= 0xCD && marker <= 0xCF))
            {
                if (segmentLength < 7) break;
                height = (bytes[position + 3] << 8) | bytes[position + 4];
                width = (bytes[position + 5] << 8) | bytes[position + 6];
                return;
            }
            position += segmentLength;
        }
        throw new InvalidDataException("Invalid JPEG dimensions");
    }
}
