using System.IO;

namespace TyrannoTranslate.Services;

public static class KsBackupService
{
    public static string GetOriginalBackupPath(string filePath) => filePath + ".bak";

    public static string GetProgressBackupPath(string filePath) => filePath + ".baktl";

    /// <summary>
    /// Copies the on-disk file to path.bak only if that backup does not exist yet,
    /// preserving the first pre-translation version across repeated saves.
    /// </summary>
    /// <returns>True if a new backup was created.</returns>
    public static bool TryCreateOriginalBackup(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var backupPath = GetOriginalBackupPath(filePath);
        if (File.Exists(backupPath))
            return false;

        File.Copy(filePath, backupPath, overwrite: false);
        return true;
    }

    /// <summary>
    /// Writes the current in-memory translated content to path.baktl (overwritten each save).
    /// </summary>
    public static void SaveProgressSnapshot(string filePath, string content) =>
        File.WriteAllText(GetProgressBackupPath(filePath), content);
}
