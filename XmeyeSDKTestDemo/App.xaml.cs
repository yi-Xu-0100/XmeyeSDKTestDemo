using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moyu.JsonExtensions.STJ;
using Moyu.LogExtensions.LogHelpers;
using NLog;
using NLog.Config;
using NLog.Web;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using XmeyeSDKTestDemo.Helpers;
using XmeyeSDKTestDemo.Interfaces;
using XmeyeSDKTestDemo.Services;
using XmeyeSDKTestDemo.Services.DecodeService.Decode;
using XmeyeSDKTestDemo.Services.DecodeService.PixelConverters;
using XmeyeSDKTestDemo.ViewModels;
using XmeyeSDKTestDemo.Views;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace XmeyeSDKTestDemo;

public partial class App : Application
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
    private static Logger _logger = null!;

#if DEBUG
    private static readonly HashSet<string> s_disabledTargetNames = ["traceFileTarget"];
    // private static readonly HashSet<string> s_disabledTargetNames = ["debugFileTarget"];
#else
    private static readonly HashSet<string> DisabledTargetNames = ["traceFileTarget", "consoleTarget"];
#endif

    public App()
    {
        // 设置 NLog 配置文件路径
        string logConfigPath = Path.Combine(AppContext.BaseDirectory, "NLog.config");
        LogManager
            .Setup()
            .LoadConfigurationFromFile(logConfigPath, false)
            .SetupLogFactory(setup =>
            {
                setup.AddCallSiteHiddenClassType(typeof(LogHelper));
            });

        _logger = LogManager.GetCurrentClassLogger();

        LoggingConfiguration? config = LogManager.Configuration;
        if (config is null)
        {
            _logger.Error("NLog 配置加载失败! 内容为 null");
            return;
        }
        _logger.Info("NLog 配置完成!");

        // 找到需要禁用的规则
        List<LoggingRule> rulesToRemove =
        [
            .. config.LoggingRules.Where(rule => rule.Targets.Any(t => s_disabledTargetNames.Contains(t.Name))),
        ];

        if (rulesToRemove.Count > 0)
            foreach (LoggingRule rule in rulesToRemove)
            {
                config.LoggingRules.Remove(rule);
                LogManager.ReconfigExistingLoggers();
                _logger.Debug(
                    $"为应用程序 [{AppHelper.AppName} {AppHelper.AppVersion}] 禁用了规则(含目标): {string.Join(", ", rule.Targets.Select(t => t.Name))}"
                );
            }

        _logger.Debug($"启用的日志规则:\n{JsonHelper.ToJson(config.LoggingRules)}");
        _logger.Info("NLog 配置加载成功!");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logger.Info($"应用程序[{AppHelper.AppName}_{AppHelper.AppVersion}]准备加载...");

        try
        {
            AppHelper.Host = CreateHostBuilder([]).Build();
            await AppHelper.Host.StartAsync().ConfigureAwait(true);

            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            MainWindow = AppHelper.GetRequiredService<MainWindow>();
            MainWindow.Show();
            _logger.Info($"[{AppHelper.AppName}_{AppHelper.AppVersion}]主界面已经准备好!");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "程序启动失败!");
            await ShowCrashMessage(ex);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _logger.Info($"应用程序[{AppHelper.AppName}_{AppHelper.AppVersion}]准备关闭服务...");
        await AppHelper.Host.StopAsync().ConfigureAwait(false);
        _logger.Info($"应用程序[{AppHelper.AppName}_{AppHelper.AppVersion}]服务关闭完成!");
        LogManager.Shutdown();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // 清除默认的日志提供器
                logging.SetMinimumLevel(LogLevel.Trace);
                // logging.AddNLog(); // 添加NLog作为日志提供器
            })
            .UseNLog()
            .ConfigureServices(
                (hostContext, services) =>
                {
                    services.AddNavigationViewPageProvider();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton(sp =>
                    {
                        return new MainWindow(
                            sp.GetRequiredService<MainWindowViewModel>(),
                            sp.GetRequiredService<ILogger<MainWindow>>()
                        );
                    });
                    //services.AddSingleton<MainWindow>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ISnackbarService, SnackbarService>();
                    services.AddSingleton<IContentDialogService, ContentDialogService>();
                    services.AddSingleton<IContentDialogService, ContentDialogService>();
                    services.AddSingleton<CameraPageViewModel>();
                    services.AddSingleton<CameraPage>();

                    services.AddSingleton<WeakReferenceMessenger>();
                    services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider =>
                        provider.GetRequiredService<WeakReferenceMessenger>()
                    );

                    services.AddSingleton(_ => Current.Dispatcher);

                    services.AddSingleton<XmeyeHostService>();
                    services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<XmeyeHostService>());

                    services.AddSingleton<IFFmpegDecodeManager, FFmpegDecodeManager>();

                    services.AddSingleton(sp =>
                    {
                        return new PixelConverterDispatcher<WriteableBitmap>([new BgraWriteableBitmapConverter()]);
                    });


                }
            );
    }

    private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            _logger.Fatal(e.Exception, "未处理的 UI 线程异常!");
            e.Handled = true;
            await ShowCrashMessage(e.Exception);
            _logger.Info($"准备退出应用...");
            Environment.Exit(1);
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, $"未处理的 UI 线程异常来着{nameof(OnDispatcherUnhandledException)}!");
            // 异常处理失败，强制退出
            Environment.Exit(-1);
        }
    }

    private static async void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Exception? ex = e.ExceptionObject as Exception;
            _logger.Fatal(ex, "未处理的非 UI 线程异常 (Domain)!");
            await ShowCrashMessage(ex);
            _logger.Info($"准备退出应用...");
            Environment.Exit(1);
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, $"未处理的非 UI 线程异常 (Domain)来着{nameof(OnDomainUnhandledException)}!");
            // 异常处理失败，强制退出
            Environment.Exit(-1);
        }
    }

    private static async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            _logger.Fatal(e.Exception, "未处理的 Task 异常!");
            await ShowCrashMessage(e.Exception);
            _logger.Info($"准备退出应用...");
            e.SetObserved();
            Environment.Exit(1);
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, $"未处理的 Task 异常来着{nameof(OnUnobservedTaskException)}!");
            // 异常处理失败，强制退出
            Environment.Exit(-1);
        }
    }

    private static async Task ShowCrashMessage(Exception? exception)
    {
        try
        {
            MessageBox uiMessageBox = new() { Title = "系统错误", Content = exception?.ToString() };

            await uiMessageBox.ShowDialogAsync();
            _logger.Error(exception, "全局异常处理失败!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "全局异常处理异常, 无法正常关闭!");
            // 异常处理失败，强制退出
            Environment.Exit(-1);
        }
    }
}
