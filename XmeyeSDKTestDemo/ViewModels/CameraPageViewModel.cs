using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using Wpf.Ui;

namespace XmeyeSDKTestDemo.ViewModels;

public partial class CameraPageViewModel : ViewModelBase
{
    private readonly ILogger<CameraPageViewModel> _logger;
    private readonly ISnackbarService _snackbarService;

    /// <inheritdoc/>
    public CameraPageViewModel(ISnackbarService snackbarService, ILogger<CameraPageViewModel> logger)
    {
        _logger = logger;
        _snackbarService = snackbarService;
    }

    private System.Windows.Media.ImageSource currentAResultFrame;

    public System.Windows.Media.ImageSource CurrentAResultFrame { get => currentAResultFrame; set => SetProperty(ref currentAResultFrame, value); }

    private System.Windows.Media.ImageSource currentAFrame;

    public System.Windows.Media.ImageSource CurrentAFrame { get => currentAFrame; set => SetProperty(ref currentAFrame, value); }

    private Wpf.Ui.Controls.ControlAppearance isACameraOpenAppearance;

    public Wpf.Ui.Controls.ControlAppearance IsACameraOpenAppearance { get => isACameraOpenAppearance; set => SetProperty(ref isACameraOpenAppearance, value); }

    private string currentAResult;

    public string CurrentAResult { get => currentAResult; set => SetProperty(ref currentAResult, value); }

    private RelayCommand setACameraCommand;
    public ICommand SetACameraCommand => setACameraCommand ??= new RelayCommand(SetACamera);

    private void SetACamera()
    {
    }

    private RelayCommand loadAFrameFromFileCommand;
    public ICommand LoadAFrameFromFileCommand => loadAFrameFromFileCommand ??= new RelayCommand(LoadAFrameFromFile);

    private void LoadAFrameFromFile()
    {
    }
}
