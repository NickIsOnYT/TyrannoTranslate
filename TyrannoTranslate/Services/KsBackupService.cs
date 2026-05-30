using System.IO;

namespace TyrannoTranslate.Services;

public static class KsBackupService
{
    public static string GetBackupPath(string filePath) => filePath + ".bak";

    /// <summary>
    /// Copies the current on-disk file to path.bak before it is overwritten.
    /// </summary>
    public static void CreateBackupIfExists(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var backupPath = GetBackupPath(filePath);
        File.Copy(filePath, backupPath, overwrite: true);
    }
}
