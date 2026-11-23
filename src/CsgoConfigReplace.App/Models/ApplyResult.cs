namespace CsgoConfigReplace.App.Models;

public class ApplyResult
{
    public List<AccountApplyResult> AccountResults { get; set; } = new();

    public int SuccessCount => AccountResults.Count(r => r.Success);
    public int FailureCount => AccountResults.Count(r => !r.Success);
}

public class AccountApplyResult
{
    public string SteamId3 { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? BackupPath { get; set; }
    public List<string> CopiedFiles { get; set; } = new();
}

public class ApplyProgress
{
    public string SteamId3 { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
