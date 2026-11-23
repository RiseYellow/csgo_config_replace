using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsgoConfigReplace.App.Models;
using CsgoConfigReplace.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace CsgoConfigReplace.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISteamLocator _steamLocator;
    private readonly IAccountScanner _accountScanner;
    private readonly ISteamUserService _steamUserService;
    private readonly IConfigService _configService;
    private readonly ISettingsService _settingsService;
    private readonly IAccountMonitorService _accountMonitorService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly HashSet<string> _knownAccounts = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string _steamPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private BaselineConfig? _baseline;

    [ObservableProperty]
    private string _baselineSummary = "尚未设置基准配置";

    [ObservableProperty]
    private bool _isAutoMonitorEnabled;

    [ObservableProperty]
    private string _monitorStatus = "自动监控已关闭";

    public ObservableCollection<AccountItemViewModel> Accounts { get; } = new();
    public ObservableCollection<string> OperationLog { get; } = new();

    public MainViewModel(
        ISteamLocator steamLocator,
        IAccountScanner accountScanner,
        ISteamUserService steamUserService,
        IConfigService configService,
        ISettingsService settingsService,
        IAccountMonitorService accountMonitorService,
        ILogger<MainViewModel> logger)
    {
        _steamLocator = steamLocator;
        _accountScanner = accountScanner;
        _steamUserService = steamUserService;
        _configService = configService;
        _settingsService = settingsService;
        _accountMonitorService = accountMonitorService;
        _logger = logger;

        _accountMonitorService.NewAccountDetected += OnNewAccountDetected;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        if (string.IsNullOrWhiteSpace(SteamPath))
        {
            await LocateSteamAsync();
        }
        else
        {
            await ScanAccountsAsync();
            if (IsAutoMonitorEnabled)
                StartMonitor();
        }
    }

    [RelayCommand]
    private async Task LocateSteamAsync()
    {
        IsBusy = true;
        StatusMessage = "正在定位 Steam...";
        try
        {
            var result = await _steamLocator.LocateAsync();
            if (result.Success)
            {
                SteamPath = result.SteamPath;
                await SaveSettingsAsync();
                await ScanAccountsAsync();
            }
            else
            {
                StatusMessage = result.Message;
                AddLog(result.Message);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BrowseSteamPath()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择 Steam 安装目录",
            SelectedPath = SteamPath,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SteamPath = dialog.SelectedPath;
            _ = SaveAndScanAsync();
        }
    }

    [RelayCommand]
    private async Task ScanAccountsAsync()
    {
        if (string.IsNullOrWhiteSpace(SteamPath))
        {
            StatusMessage = "请先设置 Steam 路径";
            return;
        }

        IsBusy = true;
        StatusMessage = "正在扫描账号...";
        try
        {
            var result = await _steamLocator.LocateFromPathAsync(SteamPath);
            if (!result.Success)
            {
                StatusMessage = result.Message;
                AddLog(result.Message);
                return;
            }

            var accounts = await _accountScanner.ScanAsync(SteamPath);
            var nicknames = await _steamUserService.GetNicknamesAsync(SteamPath);

            Accounts.Clear();
            _knownAccounts.Clear();
            foreach (var account in accounts)
            {
                if (nicknames.TryGetValue(account.SteamId3, out var nickname))
                    account.Nickname = nickname;
                Accounts.Add(new AccountItemViewModel(account));
                _knownAccounts.Add(account.SteamId3);
            }

            await LoadBaselineAsync();
            StatusMessage = $"扫描完成，共 {Accounts.Count} 个账号";
            AddLog(StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败: {ex.Message}";
            AddLog(StatusMessage);
            _logger.LogError(ex, "Scan accounts failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetBaselineAsync()
    {
        if (SelectedAccount is null)
        {
            StatusMessage = "请先选择一个账号作为基准";
            return;
        }

        if (!SelectedAccount.Account.HasCfgDirectory)
        {
            StatusMessage = "该账号没有 cfg 目录，无法设为基准";
            return;
        }

        var files = SelectedAccount.Account.ConfigFiles
            .Where(f => f.IsSelected)
            .Select(f => f.FileName)
            .ToList();

        if (files.Count == 0)
        {
            StatusMessage = "请至少勾选一个配置文件";
            return;
        }

        await _configService.SetBaselineAsync(SelectedAccount.Account, files);
        await LoadBaselineAsync();
        StatusMessage = $"已将 {SelectedAccount.SteamId3} 设为基准配置";
        AddLog(StatusMessage);
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (Baseline is null)
        {
            StatusMessage = "请先设置基准配置";
            return;
        }

        var targets = Accounts
            .Where(a => a.IsSelected)
            .Select(a => a.Account)
            .ToList();

        if (targets.Count == 0)
        {
            StatusMessage = "请至少勾选一个目标账号";
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            $"确定要将基准配置应用到 {targets.Count} 个账号吗？\n应用前会自动备份目标账号原有配置。",
            "确认应用",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        StatusMessage = "正在应用配置...";
        try
        {
            var progress = new Progress<ApplyProgress>(p =>
            {
                StatusMessage = $"{p.SteamId3}: {p.Status}";
            });

            var result = await _configService.ApplyToAccountsAsync(Baseline, targets, progress);

            foreach (var r in result.AccountResults)
            {
                var message = $"{r.SteamId3}: {(r.Success ? "成功" : "失败")} - {r.Message}";
                if (!string.IsNullOrEmpty(r.BackupPath))
                    message += $"（备份: {r.BackupPath}）";
                AddLog(message);
            }

            StatusMessage = $"应用完成：成功 {result.SuccessCount} 个，失败 {result.FailureCount} 个";
            await ScanAccountsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用失败: {ex.Message}";
            AddLog(StatusMessage);
            _logger.LogError(ex, "Apply config failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnIsAutoMonitorEnabledChanged(bool value)
    {
        if (value)
            StartMonitor();
        else
            StopMonitor();
        _ = SaveSettingsAsync();
    }

    private void StartMonitor()
    {
        if (string.IsNullOrWhiteSpace(SteamPath))
        {
            MonitorStatus = "自动监控：等待 Steam 路径";
            return;
        }

        try
        {
            _accountMonitorService.Start(SteamPath);
            MonitorStatus = "自动监控：正在运行";
            AddLog("已开启新账号自动监控");
        }
        catch (Exception ex)
        {
            MonitorStatus = "自动监控：启动失败";
            AddLog($"自动监控启动失败: {ex.Message}");
            _logger.LogError(ex, "Start monitor failed");
        }
    }

    private void StopMonitor()
    {
        _accountMonitorService.Stop();
        MonitorStatus = "自动监控已关闭";
        AddLog("已关闭新账号自动监控");
    }

    private async void OnNewAccountDetected(object? sender, NewAccountDetectedEventArgs e)
    {
        await App.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (_knownAccounts.Contains(e.SteamId3))
                return;

            AddLog($"检测到新登录账号: {e.SteamId3}");
            _knownAccounts.Add(e.SteamId3);

            if (Baseline is null)
            {
                AddLog("未设置基准配置，跳过自动应用");
                await ScanAccountsAsync();
                return;
            }

            try
            {
                StatusMessage = $"正在为新账号 {e.SteamId3} 自动应用配置...";
                var target = new SteamAccount
                {
                    SteamId3 = e.SteamId3,
                    UserDataPath = e.UserDataPath,
                    CfgPath = Path.Combine(e.UserDataPath, "730", "local", "cfg")
                };

                var result = await _configService.ApplyToAccountsAsync(Baseline, new[] { target });
                var r = result.AccountResults.FirstOrDefault();
                if (r is not null)
                {
                    var message = $"新账号 {e.SteamId3}: {(r.Success ? "自动应用成功" : "自动应用失败")} - {r.Message}";
                    if (!string.IsNullOrEmpty(r.BackupPath))
                        message += $"（备份: {r.BackupPath}）";
                    AddLog(message);
                }

                await ScanAccountsAsync();
            }
            catch (Exception ex)
            {
                AddLog($"新账号 {e.SteamId3} 自动应用异常: {ex.Message}");
                _logger.LogError(ex, "Auto apply to new account failed");
            }
        });
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SteamPath = settings.SteamPath;
        Baseline = settings.Baseline;
        IsAutoMonitorEnabled = settings.IsAutoMonitorEnabled;
        UpdateBaselineSummary();
    }

    private async Task SaveSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        settings.SteamPath = SteamPath;
        settings.Baseline = Baseline;
        settings.IsAutoMonitorEnabled = IsAutoMonitorEnabled;
        await _settingsService.SaveAsync(settings);
    }

    private async Task SaveAndScanAsync()
    {
        await SaveSettingsAsync();
        await ScanAccountsAsync();
    }

    private async Task LoadBaselineAsync()
    {
        Baseline = await _configService.GetBaselineAsync();
        UpdateBaselineSummary();
    }

    private void UpdateBaselineSummary()
    {
        if (Baseline is null)
        {
            BaselineSummary = "尚未设置基准配置";
            return;
        }

        var files = Baseline.IncludedFiles.Count == 0
            ? "全部文件"
            : string.Join(", ", Baseline.IncludedFiles);
        BaselineSummary = $"源账号: {Baseline.SourceSteamId3}\n文件: {files}";
    }

    private void AddLog(string message)
    {
        var logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (App.Current.Dispatcher.CheckAccess())
        {
            OperationLog.Insert(0, logMessage);
            if (OperationLog.Count > 100)
                OperationLog.RemoveAt(OperationLog.Count - 1);
        }
        else
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                OperationLog.Insert(0, logMessage);
                if (OperationLog.Count > 100)
                    OperationLog.RemoveAt(OperationLog.Count - 1);
            });
        }
    }
}
