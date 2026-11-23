using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public interface IConfigService
{
    Task SetBaselineAsync(SteamAccount account, IEnumerable<string> includedFiles);
    Task<BaselineConfig?> GetBaselineAsync();
    Task<ApplyResult> ApplyToAccountsAsync(
        BaselineConfig baseline,
        IEnumerable<SteamAccount> targets,
        IProgress<ApplyProgress>? progress = null,
        CancellationToken ct = default);
}
