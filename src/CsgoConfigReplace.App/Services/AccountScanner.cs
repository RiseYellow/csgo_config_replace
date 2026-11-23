using System.IO;
using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public class AccountScanner : IAccountScanner
{
    private const int CsgoAppId = 730;

    public Task<IReadOnlyList<SteamAccount>> ScanAsync(string steamPath, CancellationToken ct = default)
    {
        var result = new List<SteamAccount>();
        var userDataPath = Path.Combine(steamPath, "userdata");

        if (!Directory.Exists(userDataPath))
            return Task.FromResult<IReadOnlyList<SteamAccount>>(result);

        foreach (var directory in Directory.EnumerateDirectories(userDataPath))
        {
            ct.ThrowIfCancellationRequested();

            var dirName = Path.GetFileName(directory);
            if (!IsSteamId3(dirName))
                continue;

            var cfgPath = Path.Combine(directory, CsgoAppId.ToString(), "local", "cfg");
            var hasCfg = Directory.Exists(cfgPath);

            var account = new SteamAccount
            {
                SteamId3 = dirName,
                UserDataPath = directory,
                CfgPath = cfgPath,
                HasCfgDirectory = hasCfg,
                ConfigFiles = hasCfg ? EnumerateConfigFiles(cfgPath) : new List<ConfigFileInfo>(),
                LastWriteTime = hasCfg ? GetLastWriteTime(cfgPath) : null
            };

            result.Add(account);
        }

        return Task.FromResult<IReadOnlyList<SteamAccount>>(result.OrderBy(a => a.SteamId3).ToList());
    }

    private static bool IsSteamId3(string dirName)
    {
        return !string.IsNullOrEmpty(dirName)
               && dirName.All(char.IsDigit)
               && dirName != "0";
    }

    private static List<ConfigFileInfo> EnumerateConfigFiles(string cfgPath)
    {
        return Directory.EnumerateFiles(cfgPath)
            .Select(f => new FileInfo(f))
            .Select(fi => new ConfigFileInfo
            {
                FileName = fi.Name,
                FullPath = fi.FullName,
                Size = fi.Length,
                LastWriteTime = fi.LastWriteTime
            })
            .OrderBy(f => f.FileName)
            .ToList();
    }

    private static DateTime? GetLastWriteTime(string cfgPath)
    {
        var files = Directory.EnumerateFiles(cfgPath).Select(f => new FileInfo(f)).ToList();
        return files.Count == 0 ? null : files.Max(f => f.LastWriteTime);
    }
}
