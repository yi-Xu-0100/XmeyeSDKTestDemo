using CommunityToolkit.Mvvm.ComponentModel;
using XmeyeSDKTestDemo.Helpers;

namespace XmeyeSDKTestDemo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _appTitle = AppHelper.AppNameAlias;

    [ObservableProperty]
    private string _appVersion = AppHelper.AppVersion;

    [ObservableProperty]
    private string _appVersionType = AppHelper.VersionType;
}
