namespace CsgoConfigReplace.App.Services;

public interface ISteamUserService
{
    Task<Dictionary<string, string>> GetNicknamesAsync(string steamPath, CancellationToken ct = default);
}
