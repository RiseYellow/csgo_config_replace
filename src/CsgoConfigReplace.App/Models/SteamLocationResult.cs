namespace CsgoConfigReplace.App.Models;

public class SteamLocationResult
{
    public bool Success { get; set; }
    public string SteamPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static SteamLocationResult Ok(string path) => new()
    {
        Success = true,
        SteamPath = path,
        Message = "Steam 路径定位成功"
    };

    public static SteamLocationResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
