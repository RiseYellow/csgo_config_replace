using System.IO;

namespace CsgoConfigReplace.App.Services;

public class BackupService : IBackupService
{
    public Task<string?> CreateBackupAsync(string targetCfgPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(targetCfgPath))
            return Task.FromResult<string?>(null);

        var localDir = Directory.GetParent(targetCfgPath)?.FullName;
        if (string.IsNullOrEmpty(localDir))
            throw new InvalidOperationException($"无法确定备份目录: {targetCfgPath}");

        var backupDirName = $"cfg.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
        var backupPath = Path.Combine(localDir, backupDirName);

        CopyDirectory(targetCfgPath, backupPath, ct);
        return Task.FromResult<string?>(backupPath);
    }

    private static void CopyDirectory(string sourceDir, string destDir, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir))
        {
            ct.ThrowIfCancellationRequested();
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var subDir in Directory.EnumerateDirectories(sourceDir))
        {
            ct.ThrowIfCancellationRequested();
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir, ct);
        }
    }
}
