using System.IO;
using CsgoConfigReplace.App.Models;
using Microsoft.Win32;

namespace CsgoConfigReplace.App.Services;

public class SteamLocator : ISteamLocator
{
    private static readonly string[] DefaultPaths =
    {
        @"C:\Program Files (x86)\Steam",
        @"C:\Program Files\Steam"
    };

    public Task<SteamLocationResult> LocateAsync(CancellationToken ct = default)
    {
        // 1. Try current user registry
        var path = ReadRegistryPath(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
        if (IsValidSteamPath(path))
            return Task.FromResult(SteamLocationResult.Ok(path!));

        // 2. Try local machine registry (32-bit view on 64-bit Windows)
        path = ReadRegistryPath(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
        if (IsValidSteamPath(path))
            return Task.FromResult(SteamLocationResult.Ok(path!));

        // 3. Try local machine registry (64-bit view)
        path = ReadRegistryPath(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");
        if (IsValidSteamPath(path))
            return Task.FromResult(SteamLocationResult.Ok(path!));

        // 4. Try default installation paths
        foreach (var defaultPath in DefaultPaths)
        {
            if (IsValidSteamPath(defaultPath))
                return Task.FromResult(SteamLocationResult.Ok(defaultPath));
        }

        return Task.FromResult(SteamLocationResult.Fail("未能自动定位 Steam 安装路径，请手动选择。"));
    }

    public Task<SteamLocationResult> LocateFromPathAsync(string path, CancellationToken ct = default)
    {
        if (IsValidSteamPath(path))
            return Task.FromResult(SteamLocationResult.Ok(path));

        return Task.FromResult(SteamLocationResult.Fail("所选路径不是有效的 Steam 安装目录（未找到 userdata 文件夹）。"));
    }

    private static string? ReadRegistryPath(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidSteamPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized = path!.Replace('/', '\\').TrimEnd('\\');
        return Directory.Exists(Path.Combine(normalized, "userdata"));    }
}
