using System.IO;
using CsgoConfigReplace.App.Services;
using FluentAssertions;

namespace CsgoConfigReplace.Tests;

public class CloudSyncServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CloudSyncService _service = new();

    public CloudSyncServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task DisableCloudSyncAsync_CreatesSharedConfig_WhenMissing()
    {
        // Act
        await _service.DisableCloudSyncAsync(_tempDir);

        // Assert
        var path = Path.Combine(_tempDir, "7", "remote", "sharedconfig.vdf");
        File.Exists(path).Should().BeTrue();
        var content = File.ReadAllText(path);
        content.Should().Contain("\"730\"");
        content.Should().Contain("\"cloudenabled\"");
        content.Should().Contain("\"0\"");
    }

    [Fact]
    public async Task DisableCloudSyncAsync_UpdatesExistingSharedConfig()
    {
        // Arrange
        var remoteDir = Path.Combine(_tempDir, "7", "remote");
        Directory.CreateDirectory(remoteDir);
        var path = Path.Combine(remoteDir, "sharedconfig.vdf");
        var content = @"
""UserRoamingConfigStore""
{
    ""Software""
    {
        ""Valve""
        {
            ""Steam""
            {
                ""apps""
                {
                    ""730""
                    {
                        ""cloudenabled""    ""1""
                    }
                }
            }
        }
    }
}
";
        File.WriteAllText(path, content);

        // Act
        await _service.DisableCloudSyncAsync(_tempDir);

        // Assert
        var newContent = File.ReadAllText(path);
        newContent.Should().Contain("\"cloudenabled\"");
        newContent.Should().Contain("\"0\"");
        newContent.Should().NotContain("\"cloudenabled\"\t\t\"1\"");
    }

    [Fact]
    public async Task IsCloudSyncEnabledAsync_ReturnsTrue_WhenFileMissing()
    {
        var result = await _service.IsCloudSyncEnabledAsync(_tempDir);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCloudSyncEnabledAsync_ReturnsFalse_WhenDisabled()
    {
        // Arrange
        await _service.DisableCloudSyncAsync(_tempDir);

        // Act
        var result = await _service.IsCloudSyncEnabledAsync(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
