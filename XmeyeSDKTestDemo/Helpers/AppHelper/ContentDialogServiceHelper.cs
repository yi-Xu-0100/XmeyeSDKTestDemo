using Wpf.Ui;
using Wpf.Ui.Controls;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static IContentDialogService ContentDialogService => GetRequiredService<IContentDialogService>();

    public static void SetDialogHost(ContentDialogHost contentDialogHost) =>
        ContentDialogService.SetDialogHost(contentDialogHost);
}
