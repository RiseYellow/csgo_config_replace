using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public interface IAccountScanner
{
    Task<IReadOnlyList<SteamAccount>> ScanAsync(string steamPath, CancellationToken ct = default);
}
