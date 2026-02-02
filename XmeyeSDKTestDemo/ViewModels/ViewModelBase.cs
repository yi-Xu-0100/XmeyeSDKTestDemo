using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Abstractions.Controls;
using XmeyeSDKTestDemo.Helpers;

namespace XmeyeSDKTestDemo.ViewModels;

public partial class ViewModelBase : ObservableObject, INavigationAware
{
    [ObservableProperty]
    private CancellationTokenSource _navigatedFromTokenSource = new();

    [ObservableProperty]
    private CancellationTokenSource _navigatedToTokenSource = new();

    public virtual async Task OnNavigatedToAsync()
    {
        using (NavigatedToTokenSource = new())
            await DispatchAsync(OnNavigatedTo, NavigatedToTokenSource.Token);
    }

    public virtual async Task OnNavigatedFromAsync()
    {
        using (NavigatedFromTokenSource = new())
            await DispatchAsync(OnNavigatedFrom, NavigatedFromTokenSource.Token);
    }

    protected virtual void OnNavigatedTo() { }

    protected virtual void OnNavigatedFrom() { }

    private static async Task DispatchAsync(Action action, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        await AppHelper.Dispatcher.InvokeAsync(action);
    }
}
