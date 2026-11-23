namespace CsgoConfigReplace.App.Services;

public interface ICloudSyncService
{
    Task DisableCloudSyncAsync(string userDataPath, CancellationToken ct = default);
    Task<bool> IsCloudSyncEnabledAsync(string userDataPath, CancellationToken ct = default);
}
