using System.IO;
using CsgoConfigReplace.App.Models;
using CsgoConfigReplace.App.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CsgoConfigReplace.Tests;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _settingsService = new();
    private readonly BackupService _backupService = new();
    private readonly CloudSyncService _cloudSyncService = new();
    private readonly ConfigService _configService;

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _configService = new ConfigService(_settingsService, _backupService, _cloudSyncService, NullLogger<ConfigService>.Instance);
    }

    [Fact]
    public async Task ApplyToAccountsAsync_CreatesMissingCfgDirectory()
    {
        // Arrange
        var sourceCfg = Path.Combine(_tempDir, "source", "730", "local", "cfg");
        Directory.CreateDirectory(sourceCfg);
        File.WriteAllText(Path.Combine(sourceCfg, "config.cfg"), "sensitivity 2.5");
        File.WriteAllText(Path.Combine(sourceCfg, "autoexec.cfg"), "exec autoexec");

        var targetAccount = new SteamAccount
        {
            SteamId3 = "999999",
            CfgPath = Path.Combine(_tempDir, "target", "730", "local", "cfg")
        };

        var baseline = new BaselineConfig
        {
            SourceSteamId3 = "111111",
            SourceCfgPath = sourceCfg,
            IncludedFiles = new List<string> { "config.cfg", "autoexec.cfg" }
        };

        // Act
        var result = await _configService.ApplyToAccountsAsync(baseline, new[] { targetAccount });

        // Assert
        result.SuccessCount.Should().Be(1);
        Directory.Exists(targetAccount.CfgPath).Should().BeTrue();
        File.Exists(Path.Combine(targetAccount.CfgPath, "config.cfg")).Should().BeTrue();
        File.Exists(Path.Combine(targetAccount.CfgPath, "autoexec.cfg")).Should().BeTrue();
        (File.ReadAllText(Path.Combine(targetAccount.CfgPath, "config.cfg"))).Should().Be("sensitivity 2.5");
    }

    [Fact]
    public async Task ApplyToAccountsAsync_BackupsExistingCfg()
    {
        // Arrange
        var sourceCfg = Path.Combine(_tempDir, "source", "730", "local", "cfg");
        Directory.CreateDirectory(sourceCfg);
        File.WriteAllText(Path.Combine(sourceCfg, "config.cfg"), "new config");

        var targetCfg = Path.Combine(_tempDir, "target", "730", "local", "cfg");
        Directory.CreateDirectory(targetCfg);
        File.WriteAllText(Path.Combine(targetCfg, "config.cfg"), "old config");

        var targetAccount = new SteamAccount { SteamId3 = "999999", CfgPath = targetCfg };
        var baseline = new BaselineConfig
        {
            SourceSteamId3 = "111111",
            SourceCfgPath = sourceCfg,
            IncludedFiles = new List<string> { "config.cfg" }
        };

        // Act
        var result = await _configService.ApplyToAccountsAsync(baseline, new[] { targetAccount });

        // Assert
        result.SuccessCount.Should().Be(1);
        result.AccountResults[0].BackupPath.Should().NotBeNull();
        Directory.Exists(result.AccountResults[0].BackupPath).Should().BeTrue();
        (File.ReadAllText(Path.Combine(targetCfg, "config.cfg"))).Should().Be("new config");
    }

    [Fact]
    public async Task ApplyToAccountsAsync_SkipsSourceAccount()
    {
        // Arrange
        var sourceCfg = Path.Combine(_tempDir, "source", "730", "local", "cfg");
        Directory.CreateDirectory(sourceCfg);
        File.WriteAllText(Path.Combine(sourceCfg, "config.cfg"), "config");

        var sourceAccount = new SteamAccount { SteamId3 = "111111", CfgPath = sourceCfg };
        var baseline = new BaselineConfig
        {
            SourceSteamId3 = "111111",
            SourceCfgPath = sourceCfg,
            IncludedFiles = new List<string> { "config.cfg" }
        };

        // Act
        var result = await _configService.ApplyToAccountsAsync(baseline, new[] { sourceAccount });

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.AccountResults[0].Message.Should().Contain("跳过源账号自身");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
