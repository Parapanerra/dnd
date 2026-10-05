using System;
using System.IO;
using UnityEngine;

public enum LegacyJsonSaveSource
{
    None,
    Primary,
    Backup
}

public sealed class LegacyJsonLoadResult
{
    public AppSaveData Data { get; }
    public LegacyJsonSaveSource Source { get; }
    public Exception PrimaryError { get; }
    public Exception BackupError { get; }

    public LegacyJsonLoadResult(
        AppSaveData data,
        LegacyJsonSaveSource source,
        Exception primaryError,
        Exception backupError)
    {
        Data = data;
        Source = source;
        PrimaryError = primaryError;
        BackupError = backupError;
    }
}

public static class LegacyJsonSaveRepository
{
    public static LegacyJsonLoadResult Load(string primaryPath, string backupPath)
    {
        Exception primaryError = null;
        Exception backupError = null;

        if (File.Exists(primaryPath))
        {
            try
            {
                AppSaveData data = Deserialize(File.ReadAllText(primaryPath));
                return new LegacyJsonLoadResult(data, LegacyJsonSaveSource.Primary, null, null);
            }
            catch (Exception exception)
            {
                primaryError = exception;

                if (File.Exists(backupPath))
                {
                    try
                    {
                        AppSaveData backupData = Deserialize(File.ReadAllText(backupPath));
                        return new LegacyJsonLoadResult(
                            backupData,
                            LegacyJsonSaveSource.Backup,
                            primaryError,
                            null);
                    }
                    catch (Exception backupException)
                    {
                        backupError = backupException;
                    }
                }
            }
        }

        return new LegacyJsonLoadResult(null, LegacyJsonSaveSource.None, primaryError, backupError);
    }

    public static void Save(string primaryPath, string backupPath, AppSaveData data)
    {
        string directory = Path.GetDirectoryName(primaryPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(data, true);
        string tempPath = primaryPath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(primaryPath))
        {
            File.Copy(primaryPath, backupPath, true);
            File.Delete(primaryPath);
        }

        File.Move(tempPath, primaryPath);
    }

    public static AppSaveData Deserialize(string json)
    {
        return JsonUtility.FromJson<AppSaveData>(json);
    }
}
