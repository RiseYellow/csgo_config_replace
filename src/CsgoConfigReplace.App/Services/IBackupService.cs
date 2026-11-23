namespace CsgoConfigReplace.App.Services;

public interface IBackupService
{
    Task<string?> CreateBackupAsync(string targetCfgPath, CancellationToken ct = default);
}
