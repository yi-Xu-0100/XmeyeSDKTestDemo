using Wpf.Ui;
using Wpf.Ui.Controls;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static INavigationService NavigationService => GetRequiredService<INavigationService>();

    public static bool NavigateTo<T>()
        where T : class
    {
        return NavigationService.Navigate(typeof(T));
    }

    public static void SetNavigationControl(NavigationView navigationControl) =>
        NavigationService.SetNavigationControl(navigationControl);
}
