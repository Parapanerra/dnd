using System;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;

public static class TaruckTransferFileReader
{
    private const int MaxImportBytes = 128 * 1024 * 1024;

    public static byte[] Read(string selectedPath)
    {
        if (string.IsNullOrEmpty(selectedPath)) throw new InvalidDataException("No file selected");
        if (File.Exists(selectedPath)) return ReadLocal(selectedPath);

        // Android SAF gives a URI rather than a normal path. Copy to private cache so
        // the size can be checked before allocating the managed byte array.
        string tempPath = Path.Combine(Application.temporaryCachePath,
            "taruck-import-" + Guid.NewGuid().ToString("N"));
        try
        {
            FileBrowserHelpers.CopyFile(selectedPath, tempPath);
            return ReadLocal(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static byte[] ReadLocal(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > MaxImportBytes) throw new InvalidDataException("Import file is too large");
            byte[] bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0) throw new EndOfStreamException("Import file changed while reading");
                offset += read;
            }
            if (stream.ReadByte() != -1) throw new InvalidDataException("Import file changed while reading");
            return bytes;
        }
    }
}
