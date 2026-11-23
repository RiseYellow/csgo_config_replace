using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public interface IAccountMonitorService
{
    event EventHandler<NewAccountDetectedEventArgs>? NewAccountDetected;
    bool IsMonitoring { get; }
    void Start(string steamPath);
    void Stop();
}

public class NewAccountDetectedEventArgs : EventArgs
{
    public string SteamId3 { get; set; } = string.Empty;
    public string UserDataPath { get; set; } = string.Empty;
}
