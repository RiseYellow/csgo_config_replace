using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public interface ISteamLocator
{
    Task<SteamLocationResult> LocateAsync(CancellationToken ct = default);
    Task<SteamLocationResult> LocateFromPathAsync(string path, CancellationToken ct = default);
}
