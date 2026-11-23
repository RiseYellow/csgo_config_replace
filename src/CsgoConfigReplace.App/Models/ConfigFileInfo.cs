namespace CsgoConfigReplace.App.Models;

public class ConfigFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastWriteTime { get; set; }
    public bool IsSelected { get; set; } = true;
}
