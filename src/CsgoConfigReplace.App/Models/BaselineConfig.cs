namespace CsgoConfigReplace.App.Models;

public class BaselineConfig
{
    public string SourceSteamId3 { get; set; } = string.Empty;
    public string SourceCfgPath { get; set; } = string.Empty;
    public List<string> IncludedFiles { get; set; } = new();
}
