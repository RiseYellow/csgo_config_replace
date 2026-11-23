using System.IO;
using System.Timers;
using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public class AccountMonitorService : IAccountMonitorService, IDisposable
{
    private const int DebounceMilliseconds = 3000;

    private FileSystemWatcher? _watcher;
    private string _steamPath = string.Empty;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly object _lock = new();
    private string? _pendingSteamId3;
    private string? _pendingUserDataPath;

    public event EventHandler<NewAccountDetectedEventArgs>? NewAccountDetected;

    public bool IsMonitoring { get; private set; }

    public AccountMonitorService()
    {
        _debounceTimer = new System.Timers.Timer(DebounceMilliseconds)
        {
            AutoReset = false
        };
        _debounceTimer.Elapsed += OnDebounceElapsed;
    }

    public void Start(string steamPath)
    {
        Stop();

        var userDataPath = Path.Combine(steamPath, "userdata");
        if (!Directory.Exists(userDataPath))
            throw new DirectoryNotFoundException($"userdata 目录不存在: {userDataPath}");

        _steamPath = steamPath;
        _watcher = new FileSystemWatcher(userDataPath)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };
        _watcher.Created += OnDirectoryCreated;
        IsMonitoring = true;
    }

    public void Stop()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnDirectoryCreated;
            _watcher.Dispose();
            _watcher = null;
        }

        _debounceTimer.Stop();
        lock (_lock)
        {
            _pendingSteamId3 = null;
            _pendingUserDataPath = null;
        }

        IsMonitoring = false;
    }

    private void OnDirectoryCreated(object sender, FileSystemEventArgs e)
    {
        var dirName = Path.GetFileName(e.FullPath);
        if (!IsSteamId3(dirName))
            return;

        lock (_lock)
        {
            _pendingSteamId3 = dirName;
            _pendingUserDataPath = e.FullPath;
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        string? steamId3;
        string? userDataPath;

        lock (_lock)
        {
            steamId3 = _pendingSteamId3;
            userDataPath = _pendingUserDataPath;
            _pendingSteamId3 = null;
            _pendingUserDataPath = null;
        }

        if (string.IsNullOrEmpty(steamId3) || string.IsNullOrEmpty(userDataPath))
            return;

        NewAccountDetected?.Invoke(this, new NewAccountDetectedEventArgs
        {
            SteamId3 = steamId3!,
            UserDataPath = userDataPath!
        });
    }

    private static bool IsSteamId3(string dirName)
    {
        return !string.IsNullOrEmpty(dirName)
               && dirName.All(char.IsDigit)
               && dirName != "0";
    }

    public void Dispose()
    {
        Stop();
        _debounceTimer.Dispose();
    }
}
