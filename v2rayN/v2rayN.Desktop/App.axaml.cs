using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Splat;
using System.Reactive.Disposables;
using v2rayN.Desktop.Common;
using v2rayN.Desktop.Views;

namespace v2rayN.Desktop;

public partial class App : Application
{
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        var ViewModel = new StatusBarViewModel(null);
        Locator.CurrentMutable.RegisterLazySingleton(() => ViewModel, typeof(StatusBarViewModel));
        DataContext = ViewModel;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppHandler.Instance.InitComponents();

            desktop.Exit += OnExit;
            desktop.MainWindow = new MainWindow();

#if __MACOS__
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon("v2rayN.Desktop/v2rayN.png"),
                ToolTipText = "v2rayN",
                IsVisible = true
            };

            _trayIcon.Menu = new NativeMenu();
            var showHideMenuItem = new NativeMenuItem("显示/隐藏窗口");
            showHideMenuItem.Click += (sender, args) =>
            {
                if (desktop.MainWindow.IsVisible)
                {
                    desktop.MainWindow.Hide();
                }
                else
                {
                    desktop.MainWindow.Show();
                }
            };
            ((NativeMenu)_trayIcon.Menu).Items.Add(showHideMenuItem);

            var importFromClipboardMenuItem = new NativeMenuItem("从剪贴板导入");
            importFromClipboardMenuItem.Click += MenuAddServerViaClipboardClick;
            ((NativeMenu)_trayIcon.Menu).Items.Add(importFromClipboardMenuItem);

            var exitMenuItem = new NativeMenuItem("退出");
            exitMenuItem.Click += MenuExit_Click;
            ((NativeMenu)_trayIcon.Menu).Items.Add(exitMenuItem);
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject != null)
        {
            Logging.SaveLog("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logging.SaveLog("TaskScheduler_UnobservedTaskException", e.Exception);
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
#if __MACOS__
        _trayIcon?.Dispose();
#endif
    }

    private async void MenuAddServerViaClipboardClick(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow != null)
            {
                var clipboardData = await AvaUtils.GetClipboardData(desktop.MainWindow);
                if (clipboardData.IsNullOrEmpty())
                {
                    return;
                }
                var service = Locator.Current.GetService<MainWindowViewModel>();
                if (service != null)
                {
                    _ = service.AddServerViaClipboardAsync(clipboardData);
                }
            }
        }
    }

    private async void MenuExit_Click(object? sender, EventArgs e)
    {
        var service = Locator.Current.GetService<MainWindowViewModel>();
        if (service != null)
        {
            await service.MyAppExitAsync(true);
        }
        service?.Shutdown(true);
    }
}
