using System.Windows;
using Microsoft.Extensions.Logging;
using Moyu.LogExtensions.LogHelpers;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using XmeyeSDKTestDemo.Helpers;
using XmeyeSDKTestDemo.ViewModels;

namespace XmeyeSDKTestDemo.Views;

public partial class MainWindow
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(MainWindowViewModel viewModel, ILogger<MainWindow> logger)
    {
        _logger = logger;
        SystemThemeWatcher.Watch(this);
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
        DataContext = viewModel;
        InitializeComponent();
        AppHelper.SetSnackbarPresenter(SnackbarPresenter);
        AppHelper.SetNavigationControl(NavigationView);
        AppHelper.SetDialogHost(RootContentDialog);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.Info($"{nameof(MainWindow)} 加载完成!");
        AppHelper.NavigateTo<CameraPage>();
    }

    private async void MainWindow_UnLoaded(object sender, RoutedEventArgs e)
    {
        _logger.Info($"{nameof(MainWindow)} 关闭!");
    }

    public void NavigateToWelcomePage() { }
}
