using CommunityToolkit.Mvvm.ComponentModel;
using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.ViewModels;

public partial class AccountItemViewModel : ObservableObject
{
    public SteamAccount Account { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isBaseline;

    public AccountItemViewModel(SteamAccount account)
    {
        Account = account;
    }

    public string SteamId3 => Account.SteamId3;
    public string Nickname => string.IsNullOrWhiteSpace(Account.Nickname) ? "-" : Account.Nickname;
    public string DisplayStatus => Account.DisplayStatus;
    public string LastWriteTimeText => Account.LastWriteTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
    public int FileCount => Account.ConfigFiles.Count;
}
