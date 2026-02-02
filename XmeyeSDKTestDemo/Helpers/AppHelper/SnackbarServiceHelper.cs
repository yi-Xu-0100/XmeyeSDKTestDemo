using Wpf.Ui;
using Wpf.Ui.Controls;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static ISnackbarService SnackbarService => GetRequiredService<ISnackbarService>();

    public static void SetSnackbarPresenter(SnackbarPresenter snackbarPresenter) =>
        SnackbarService.SetSnackbarPresenter(snackbarPresenter);
}
