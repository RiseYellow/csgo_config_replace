using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}

public class AppSettings
{
    public string SteamPath { get; set; } = string.Empty;
    public BaselineConfig? Baseline { get; set; }
    public List<string> LastSelectedTargets { get; set; } = new();
    public bool IsAutoMonitorEnabled { get; set; }
}
