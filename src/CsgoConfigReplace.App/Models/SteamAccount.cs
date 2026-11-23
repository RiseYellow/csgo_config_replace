namespace CsgoConfigReplace.App.Models;

public class SteamAccount
{
    public string SteamId3 { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string UserDataPath { get; set; } = string.Empty;
    public string CfgPath { get; set; } = string.Empty;
    public bool HasCfgDirectory { get; set; }
    public List<ConfigFileInfo> ConfigFiles { get; set; } = new();
    public DateTime? LastWriteTime { get; set; }

    public string DisplayStatus => HasCfgDirectory ? "已配置" : "未运行过 CS2";
}
