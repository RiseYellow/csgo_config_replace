using System.IO;
using CsgoConfigReplace.App.Services;
using FluentAssertions;

namespace CsgoConfigReplace.Tests;

public class AccountMonitorServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AccountMonitorService _monitor = new();

    public AccountMonitorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CsgoConfigReplaceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Start_SetsIsMonitoring()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "userdata"));

        _monitor.Start(_tempDir);
        _monitor.IsMonitoring.Should().BeTrue();

        _monitor.Stop();
        _monitor.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void Start_Throws_WhenUserDataMissing()
    {
        Action act = () => _monitor.Start(_tempDir);
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task NewAccountDetected_FiresForNewNumericDirectory()
    {
        // Arrange
        var userData = Path.Combine(_tempDir, "userdata");
        Directory.CreateDirectory(userData);

        string? detectedId = null;
        var tcs = new TaskCompletionSource<string>();
        _monitor.NewAccountDetected += (_, e) =>
        {
            detectedId = e.SteamId3;
            tcs.TrySetResult(e.SteamId3);
        };

        _monitor.Start(_tempDir);

        // Act
        Directory.CreateDirectory(Path.Combine(userData, "123456789"));

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(8000));
        completed.Should().Be(tcs.Task);
        detectedId.Should().Be("123456789");
    }

    [Fact]
    public async Task NewAccountDetected_DoesNotFireForNonNumericDirectory()
    {
        // Arrange
        var userData = Path.Combine(_tempDir, "userdata");
        Directory.CreateDirectory(userData);

        var fired = false;
        _monitor.NewAccountDetected += (_, _) => fired = true;

        _monitor.Start(_tempDir);

        // Act
        Directory.CreateDirectory(Path.Combine(userData, "anonymous"));
        await Task.Delay(5000);

        // Assert
        fired.Should().BeFalse();
    }

    public void Dispose()
    {
        _monitor.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
