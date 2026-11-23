using System.IO;
using CsgoConfigReplace.App.Services;
using FluentAssertions;

namespace CsgoConfigReplace.Tests;

public class AccountScannerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AccountScanner _scanner = new();

    public AccountScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ScanAsync_ReturnsAccounts_WithAndWithoutCfg()
    {
        // Arrange
        var userData = Path.Combine(_tempDir, "userdata");
        var accountWithCfg = Path.Combine(userData, "111111", "730", "local", "cfg");
        var accountWithoutCfg = Path.Combine(userData, "222222");
        Directory.CreateDirectory(accountWithCfg);
        Directory.CreateDirectory(accountWithoutCfg);
        File.WriteAllText(Path.Combine(accountWithCfg, "config.cfg"), "// test config");
        File.WriteAllText(Path.Combine(accountWithCfg, "autoexec.cfg"), "// test autoexec");

        // Act
        var result = await _scanner.ScanAsync(_tempDir);

        // Assert
        result.Should().HaveCount(2);

        var withCfg = result.First(a => a.SteamId3 == "111111");
        withCfg.HasCfgDirectory.Should().BeTrue();
        withCfg.ConfigFiles.Should().HaveCount(2);
        withCfg.LastWriteTime.Should().NotBeNull();

        var withoutCfg = result.First(a => a.SteamId3 == "222222");
        withoutCfg.HasCfgDirectory.Should().BeFalse();
        withoutCfg.ConfigFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_IgnoresNonNumericAndZeroDirectories()
    {
        // Arrange
        var userData = Path.Combine(_tempDir, "userdata");
        Directory.CreateDirectory(Path.Combine(userData, "0"));
        Directory.CreateDirectory(Path.Combine(userData, "anonymous"));
        Directory.CreateDirectory(Path.Combine(userData, "123abc"));
        Directory.CreateDirectory(Path.Combine(userData, "333333"));

        // Act
        var result = await _scanner.ScanAsync(_tempDir);

        // Assert
        result.Should().HaveCount(1);
        result[0].SteamId3.Should().Be("333333");
    }

    [Fact]
    public async Task ScanAsync_ReturnsEmpty_WhenUserDataDoesNotExist()
    {
        var result = await _scanner.ScanAsync(_tempDir);
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
