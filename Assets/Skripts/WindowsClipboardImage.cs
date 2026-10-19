#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// Reads common Windows image clipboard formats without requiring Windows Forms in the player.
internal static class WindowsClipboardImage
{
    private const uint CfDib = 8;
    private const uint CfHdrop = 15;
    private const uint CfDibV5 = 17;
    private const uint BiRgb = 0;
    private const uint BiBitfields = 3;
    private const int MaxClipboardBytes = 64 * 1024 * 1024;

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterClipboardFormat(string format);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "DragQueryFileW")]
    private static extern uint DragQueryFile(IntPtr drop, uint index, System.Text.StringBuilder path, uint length);

    internal static bool TryRead(out byte[] encodedImage, out string[] filePaths)
    {
        encodedImage = null;
        filePaths = null;
        if (!OpenClipboard(IntPtr.Zero)) throw new IOException("Не вдалося відкрити буфер обміну.");
        try
        {
            uint png = RegisterClipboardFormat("PNG");
            if (png != 0 && TryCopy(png, out byte[] bytes) && IsPng(bytes))
            {
                encodedImage = bytes;
                return true;
            }
            if (TryCopy(CfDibV5, out bytes) || TryCopy(CfDib, out bytes))
            {
                encodedImage = EncodeDibAsPng(bytes);
                return true;
            }
            IntPtr drop = GetClipboardData(CfHdrop);
            if (drop != IntPtr.Zero)
            {
                uint count = DragQueryFile(drop, uint.MaxValue, null, 0);
                var paths = new List<string>();
                for (uint i = 0; i < count; i++)
                {
                    uint length = DragQueryFile(drop, i, null, 0);
                    var buffer = new System.Text.StringBuilder((int)length + 1);
                    DragQueryFile(drop, i, buffer, length + 1);
                    string path = buffer.ToString();
                    string extension = Path.GetExtension(path).ToLowerInvariant();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                        paths.Add(path);
                }
                if (paths.Count > 0)
                {
                    filePaths = paths.ToArray();
                    return true;
                }
            }
            return false;
        }
        finally { CloseClipboard(); }
    }

    private static bool TryCopy(uint format, out byte[] bytes)
    {
        bytes = null;
        IntPtr handle = GetClipboardData(format);
        if (handle == IntPtr.Zero) return false;
        ulong size = GlobalSize(handle).ToUInt64();
        if (size == 0 || size > MaxClipboardBytes) throw new InvalidDataException("Зображення в буфері завелике.");
        IntPtr pointer = GlobalLock(handle);
        if (pointer == IntPtr.Zero) throw new IOException("Не вдалося прочитати буфер обміну.");
        try
        {
            bytes = new byte[(int)size];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return true;
        }
        finally { GlobalUnlock(handle); }
    }

    private static bool IsPng(byte[] data) => data.Length >= 8 && data[0] == 0x89 &&
        data[1] == 0x50 && data[2] == 0x4e && data[3] == 0x47;

    private static byte[] EncodeDibAsPng(byte[] data)
    {
        if (data.Length < 40) throw new InvalidDataException("Пошкоджене зображення в буфері.");
        int header = checked((int)BitConverter.ToUInt32(data, 0));
        int width = BitConverter.ToInt32(data, 4);
        int signedHeight = BitConverter.ToInt32(data, 8);
        ushort planes = BitConverter.ToUInt16(data, 12);
        ushort depth = BitConverter.ToUInt16(data, 14);
        uint compression = BitConverter.ToUInt32(data, 16);
        if (header < 40 || header > data.Length || width <= 0 || signedHeight == 0 ||
            signedHeight == int.MinValue || planes != 1 || (depth != 24 && depth != 32) ||
            (compression != BiRgb && compression != BiBitfields))
            throw new InvalidDataException("Формат зображення в буфері не підтримується.");
        int height = Math.Abs(signedHeight);
        if ((long)width * height > 16L * 1024 * 1024)
            throw new InvalidDataException("Зображення в буфері завелике.");
        int offset = header;
        uint redMask = 0x00ff0000, greenMask = 0x0000ff00, blueMask = 0x000000ff, alphaMask = 0;
        if (compression == BiBitfields)
        {
            int maskAt = header >= 52 ? 40 : header;
            if (data.Length < maskAt + 12) throw new InvalidDataException("Пошкоджене зображення в буфері.");
            redMask = BitConverter.ToUInt32(data, maskAt);
            greenMask = BitConverter.ToUInt32(data, maskAt + 4);
            blueMask = BitConverter.ToUInt32(data, maskAt + 8);
            if (header >= 56) alphaMask = BitConverter.ToUInt32(data, 52);
            if (header == 40) offset += 12;
        }
        long stride = (((long)width * depth + 31) / 32) * 4;
        if (offset + stride * height > data.Length)
            throw new InvalidDataException("Пошкоджені пікселі в буфері.");
        var pixels = new Color32[width * height];
        bool bottomUp = signedHeight > 0;
        for (int y = 0; y < height; y++)
        {
            int row = checked(offset + (int)(stride * y));
            int targetY = bottomUp ? y : height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                int at = row + x * (depth / 8);
                uint value = depth == 32 ? BitConverter.ToUInt32(data, at) :
                    (uint)(data[at] | data[at + 1] << 8 | data[at + 2] << 16);
                pixels[targetY * width + x] = new Color32(
                    Channel(value, redMask), Channel(value, greenMask), Channel(value, blueMask),
                    alphaMask == 0 ? (byte)255 : Channel(value, alphaMask));
            }
        }
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture.EncodeToPNG();
        }
        finally { UnityEngine.Object.Destroy(texture); }
    }

    private static byte Channel(uint value, uint mask)
    {
        if (mask == 0) return 0;
        int shift = 0;
        while (((mask >> shift) & 1) == 0) shift++;
        uint maximum = mask >> shift;
        return (byte)(((value & mask) >> shift) * 255 / maximum);
    }
}
#endif
