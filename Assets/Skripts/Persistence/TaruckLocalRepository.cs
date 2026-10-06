using System;
using System.IO;
using UnityEngine;

public enum TaruckLocalSource { Empty, Primary, Backup, LegacyPrimary, LegacyBackup, Unreadable }

public sealed class TaruckLocalLoadResult
{
    public AppSaveData Data { get; }
    public TaruckLocalSource Source { get; }
    public Exception Error { get; }
    public bool CanWrite => Source != TaruckLocalSource.Unreadable;

    public TaruckLocalLoadResult(AppSaveData data, TaruckLocalSource source, Exception error)
    {
        Data = data; Source = source; Error = error;
    }
}

public static class TaruckLocalRepository
{
    public static TaruckLocalLoadResult Load(string binaryPath, string legacyPath, string appVersion)
    {
        string backup = binaryPath + ".bak";
        bool binaryExists = File.Exists(binaryPath) || File.Exists(backup) || File.Exists(binaryPath + ".tmp");
        if (binaryExists)
        {
            Exception primaryError = null;
            if (File.Exists(binaryPath))
            {
                try { return new TaruckLocalLoadResult(TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binaryPath)), TaruckLocalSource.Primary, null); }
                catch (Exception e) { primaryError = e; }
            }
            if (primaryError is UnsupportedTaruckVersionException)
            {
                LegacyJsonLoadResult older = LegacyJsonSaveRepository.Load(legacyPath, legacyPath + ".bak");
                AppSaveData olderData = older.Data != null ? SaveMigrationService.FromLegacy(older.Data) : null;
                return new TaruckLocalLoadResult(olderData, TaruckLocalSource.Unreadable, primaryError);
            }
            if (File.Exists(backup))
            {
                try
                {
                    AppSaveData recovered = TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(backup));
                    if (File.Exists(binaryPath))
                    {
                        File.Copy(binaryPath, binaryPath + ".corrupt", true);
                        File.Copy(backup, binaryPath, true);
                    }
                    return new TaruckLocalLoadResult(recovered, TaruckLocalSource.Backup, primaryError);
                }
                catch (Exception e) { primaryError = new AggregateException(primaryError ?? e, e); }
            }
            // A leftover .tmp is not trusted as committed data. Keep any readable legacy data
            // available in memory, but block writes until the damaged binary is resolved.
            LegacyJsonLoadResult fallback = LegacyJsonSaveRepository.Load(legacyPath, legacyPath + ".bak");
            AppSaveData legacyData = fallback.Data != null ? SaveMigrationService.FromLegacy(fallback.Data) : null;
            return new TaruckLocalLoadResult(legacyData, TaruckLocalSource.Unreadable,
                primaryError ?? new InvalidDataException("Uncommitted or damaged binary save exists"));
        }

        bool legacyExists = File.Exists(legacyPath) || File.Exists(legacyPath + ".bak");
        if (!legacyExists) return new TaruckLocalLoadResult(new AppSaveData(), TaruckLocalSource.Empty, null);

        LegacyJsonLoadResult legacy = LegacyJsonSaveRepository.Load(legacyPath, legacyPath + ".bak");
        if (legacy.Source == LegacyJsonSaveSource.None || legacy.Data == null)
            return new TaruckLocalLoadResult(null, TaruckLocalSource.Unreadable,
                legacy.PrimaryError ?? legacy.BackupError ?? new InvalidDataException("Legacy save unreadable"));

        try
        {
            AppSaveData data = SaveMigrationService.FromLegacy(legacy.Data);
            Save(binaryPath, data, appVersion);
            // Reread the actual promoted file before declaring migration complete.
            AppSaveData verified = TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binaryPath));
            return new TaruckLocalLoadResult(verified,
                legacy.Source == LegacyJsonSaveSource.Primary ? TaruckLocalSource.LegacyPrimary : TaruckLocalSource.LegacyBackup,
                legacy.PrimaryError);
        }
        catch (Exception e)
        {
            // Legacy data remains readable in memory. The caller must not write again until migration succeeds.
            return new TaruckLocalLoadResult(legacy.Data, TaruckLocalSource.Unreadable, e);
        }
    }

    public static void Save(string binaryPath, AppSaveData data, string appVersion)
    {
        byte[] bytes = TaruckBinaryCodec.EncodeLocal(data, appVersion);
        string directory = Path.GetDirectoryName(binaryPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string tempPath = binaryPath + ".tmp";
        string backupPath = binaryPath + ".bak";
        File.WriteAllBytes(tempPath, bytes);
        byte[] checkedBytes = File.ReadAllBytes(tempPath);
        TaruckBinaryCodec.DecodeLocal(checkedBytes);
        if (checkedBytes.Length != bytes.Length) throw new InvalidDataException("Temporary save changed");
        if (File.Exists(binaryPath))
        {
            // Validate the current primary before allowing it to become the rollback copy.
            TaruckBinaryCodec.DecodeLocal(File.ReadAllBytes(binaryPath));
            File.Copy(binaryPath, backupPath, true);
            File.Delete(binaryPath);
        }
        try { File.Move(tempPath, binaryPath); }
        catch
        {
            if (!File.Exists(binaryPath) && File.Exists(backupPath)) File.Copy(backupPath, binaryPath);
            throw;
        }
    }
}
