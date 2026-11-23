using System.IO;
using CsgoConfigReplace.App.Services;
using FluentAssertions;

namespace CsgoConfigReplace.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BackupService _backupService = new();

    public BackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsNull_WhenCfgDoesNotExist()
    {
        var result = await _backupService.CreateBackupAsync(Path.Combine(_tempDir, "nonexistent"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateBackupAsync_CopiesCfgDirectory()
    {
        // Arrange
        var cfgPath = Path.Combine(_tempDir, "730", "local", "cfg");
        Directory.CreateDirectory(cfgPath);
        File.WriteAllText(Path.Combine(cfgPath, "config.cfg"), "test");

        // Act
        var backupPath = await _backupService.CreateBackupAsync(cfgPath);

        // Assert
        backupPath.Should().NotBeNull();
        Directory.Exists(backupPath).Should().BeTrue();
        File.Exists(Path.Combine(backupPath!, "config.cfg")).Should().BeTrue();
        Path.GetFileName(backupPath).Should().StartWith("cfg.backup_");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
