using System.Windows;
using XmeyeSDKTestDemo.ViewModels;
using Microsoft.Extensions.Logging;
using Moyu.LogExtensions.LogHelpers;

namespace XmeyeSDKTestDemo.Views;

public partial class CameraPage
{
    private readonly CameraPageViewModel _viewModel;
    private readonly ILogger<CameraPage> _logger;

    public CameraPage(CameraPageViewModel viewModel, ILogger<CameraPage> logger)
    {
        DataContext = viewModel;
        _viewModel = viewModel;
        _logger = logger;
        InitializeComponent();
        Unloaded += CameraPage_Unloaded;
    }

    private void CameraPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _logger.Info($"关闭相机后台线程!");
    }
}
