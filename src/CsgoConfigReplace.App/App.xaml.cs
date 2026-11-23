using System;
using System.Windows;
using CsgoConfigReplace.App.Services;
using CsgoConfigReplace.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Application = System.Windows.Application;

namespace CsgoConfigReplace.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<ISteamLocator, SteamLocator>();
        services.AddSingleton<IAccountScanner, AccountScanner>();
        services.AddSingleton<ISteamUserService, SteamUserService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAccountMonitorService, AccountMonitorService>();
        services.AddSingleton<ICloudSyncService, CloudSyncService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError(e.Exception);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowFatalError(ex);
    }

    private static void ShowFatalError(Exception ex)
    {
        System.Windows.MessageBox.Show(
            $"程序启动失败：\n\n{ex.Message}\n\n详细信息：\n{ex}",
            "CS2 Config 迁移工具 - 错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
