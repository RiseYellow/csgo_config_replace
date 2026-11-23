using System.IO;
using CsgoConfigReplace.App.Models;
using Microsoft.Extensions.Logging;

namespace CsgoConfigReplace.App.Services;

public class ConfigService : IConfigService
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;
    private readonly ICloudSyncService _cloudSyncService;
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(
        ISettingsService settingsService,
        IBackupService backupService,
        ICloudSyncService cloudSyncService,
        ILogger<ConfigService> logger)
    {
        _settingsService = settingsService;
        _backupService = backupService;
        _cloudSyncService = cloudSyncService;
        _logger = logger;
    }

    public async Task SetBaselineAsync(SteamAccount account, IEnumerable<string> includedFiles)
    {
        var settings = await _settingsService.LoadAsync();
        settings.Baseline = new BaselineConfig
        {
            SourceSteamId3 = account.SteamId3,
            SourceCfgPath = account.CfgPath,
            IncludedFiles = includedFiles.ToList()
        };
        await _settingsService.SaveAsync(settings);
    }

    public async Task<BaselineConfig?> GetBaselineAsync()
    {
        var settings = await _settingsService.LoadAsync();
        return settings.Baseline;
    }

    public async Task<ApplyResult> ApplyToAccountsAsync(
        BaselineConfig baseline,
        IEnumerable<SteamAccount> targets,
        IProgress<ApplyProgress>? progress = null,
        CancellationToken ct = default)
    {
        var result = new ApplyResult();

        if (!Directory.Exists(baseline.SourceCfgPath))
        {
            result.AccountResults.Add(new AccountApplyResult
            {
                Success = false,
                Message = $"基准配置目录不存在: {baseline.SourceCfgPath}"
            });
            return result;
        }

        var sourceFiles = GetSourceFiles(baseline);

        foreach (var target in targets)
        {
            ct.ThrowIfCancellationRequested();

            if (target.SteamId3 == baseline.SourceSteamId3)
            {
                result.AccountResults.Add(new AccountApplyResult
                {
                    SteamId3 = target.SteamId3,
                    Success = false,
                    Message = "跳过源账号自身"
                });
                continue;
            }

            progress?.Report(new ApplyProgress { SteamId3 = target.SteamId3, Status = "正在应用..." });
            var accountResult = await ApplyToSingleAccountAsync(baseline, target, sourceFiles, ct);
            result.AccountResults.Add(accountResult);

            progress?.Report(new ApplyProgress
            {
                SteamId3 = target.SteamId3,
                Status = accountResult.Success ? "成功" : "失败"
            });
        }

        return result;
    }

    private async Task<AccountApplyResult> ApplyToSingleAccountAsync(
        BaselineConfig baseline,
        SteamAccount target,
        List<string> sourceFiles,
        CancellationToken ct)
    {
        var accountResult = new AccountApplyResult { SteamId3 = target.SteamId3 };

        try
        {
            // Create cfg directory recursively, even if 730/local does not exist.
            Directory.CreateDirectory(target.CfgPath);

            // Backup existing config if present.
            accountResult.BackupPath = await _backupService.CreateBackupAsync(target.CfgPath, ct);

            foreach (var sourceFile in sourceFiles)
            {
                ct.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(sourceFile);
                var destFile = Path.Combine(target.CfgPath, fileName);
                File.Copy(sourceFile, destFile, overwrite: true);
                accountResult.CopiedFiles.Add(fileName);
            }

            accountResult.Success = true;
            accountResult.Message = $"成功复制 {accountResult.CopiedFiles.Count} 个文件";

            // Disable Steam Cloud sync for CS2 to prevent cloud from overwriting local config.
            try
            {
                await _cloudSyncService.DisableCloudSyncAsync(target.UserDataPath, ct);
                accountResult.Message += "，已关闭云同步";
            }
            catch (Exception cloudEx)
            {
                accountResult.Message += $"，关闭云同步失败: {cloudEx.Message}";
                _logger.LogWarning(cloudEx, "Failed to disable cloud sync for {SteamId3}", target.SteamId3);
            }

            _logger.LogInformation("Applied baseline to {SteamId3}. Files: {Files}", target.SteamId3, string.Join(", ", accountResult.CopiedFiles));
        }
        catch (Exception ex)
        {
            accountResult.Success = false;
            accountResult.Message = ex.Message;
            _logger.LogError(ex, "Failed to apply baseline to {SteamId3}", target.SteamId3);
        }

        return accountResult;
    }

    private static List<string> GetSourceFiles(BaselineConfig baseline)
    {
        if (baseline.IncludedFiles.Count == 0)
            return Directory.EnumerateFiles(baseline.SourceCfgPath).ToList();

        return baseline.IncludedFiles
            .Select(f => Path.Combine(baseline.SourceCfgPath, f))
            .Where(File.Exists)
            .ToList();
    }
}
