using System.IO;
using CsgoConfigReplace.App.Services;
using FluentAssertions;

namespace CsgoConfigReplace.Tests;

public class SteamUserServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SteamUserService _service = new();

    public SteamUserServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task GetNicknamesAsync_ReturnsEmpty_WhenLoginUsersDoesNotExist()
    {
        var result = await _service.GetNicknamesAsync(_tempDir);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNicknamesAsync_ParsesLoginUsersVdf()
    {
        // Arrange
        var configDir = Path.Combine(_tempDir, "config");
        Directory.CreateDirectory(configDir);
        var vdfPath = Path.Combine(configDir, "loginusers.vdf");

        var vdfContent = @"
""users""
{
    ""76561197960265729""
    {
        ""AccountName""    ""account_a""
        ""PersonaName""    ""玩家A""
    }
    ""76561197960265730""
    {
        ""AccountName""    ""account_b""
        ""PersonaName""    ""PlayerB""
    }
}
";
        File.WriteAllText(vdfPath, vdfContent);

        // Act
        var result = await _service.GetNicknamesAsync(_tempDir);

        // Assert
        result.Should().HaveCount(2);
        result["1"].Should().Be("玩家A");
        result["2"].Should().Be("PlayerB");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
