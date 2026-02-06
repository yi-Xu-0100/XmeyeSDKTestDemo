using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Moyu.LogExtensions.LogHelpers;
using User.NetSDK;
using XmeyeSDKTestDemo.Helpers;
using XmeyeSDKTestDemo.Models.Decode;
using XmeyeSDKTestDemo.XmeyeService;

namespace XmeyeSDKTestDemo.ViewModels;

public partial class CameraPageViewModel : ViewModelBase
{
    private readonly ILogger<CameraPageViewModel> _logger;

    [ObservableProperty]
    private Dictionary<string, long> _latestReceiveFrameIndex = [];

    [ObservableProperty]
    private Dictionary<string, long> _latestIReceiveFrameIndex = [];

    /// <inheritdoc/>
    public CameraPageViewModel(ILogger<CameraPageViewModel> logger)
    {
        _logger = logger;
    }

    private WriteableBitmap _currentAFrame;

    public WriteableBitmap CurrentAFrame
    {
        get { return _currentAFrame; }
        set
        {
            SetProperty(ref _currentAFrame, value);
            OnPropertyChanged(nameof(CurrentAFrame));
        }
    }
    private WriteableBitmap _currentBFrame;

    public WriteableBitmap CurrentBFrame
    {
        get { return _currentBFrame; }
        set
        {
            SetProperty(ref _currentBFrame, value);
            OnPropertyChanged(nameof(CurrentBFrame));
        }
    }

    private Wpf.Ui.Controls.ControlAppearance isACameraOpenAppearance;

    public Wpf.Ui.Controls.ControlAppearance IsACameraOpenAppearance
    {
        get => isACameraOpenAppearance;
        set => SetProperty(ref isACameraOpenAppearance, value);
    }

    private string currentAResult;

    public string CurrentAResult
    {
        get => currentAResult;
        set => SetProperty(ref currentAResult, value);
    }

    [RelayCommand]
    private void SetACamera()
    {
        StartCameraLoop("相机A");
        StartCameraLoop("相机B");
    }

    private RelayCommand loadAFrameFromFileCommand;
    public ICommand LoadAFrameFromFileCommand => loadAFrameFromFileCommand ??= new RelayCommand(LoadAFrameFromFile);

    private void LoadAFrameFromFile() { }

    #region Camera Parse Loop

    private readonly Dictionary<string, CameraLoopContext> _cameraLoops = new();
    private readonly object _lock = new();

    private class CameraLoopContext
    {
        public CancellationTokenSource Cts { get; init; } = new();
        public Task? LoopTask { get; set; }
    }

    public void StartCameraLoop(string cameraKey)
    {
        lock (_lock)
        {
            var camera = AppHelper.GetDevice(cameraKey);
            if (camera == null)
            {
                _logger.Warn($"相机[{cameraKey}]不存在, 无法启动后台解析线程!");
                return;
            }
            if (_cameraLoops.TryGetValue(cameraKey, out var ctx))
            {
                if (ctx.LoopTask is { IsCompleted: false })
                {
                    _logger.Info($"{camera}后台解析线程已在运行中, 无需重复启动!");
                    return;
                }
            }

            _logger.Info($"{camera}后台解析线程准备启动!");

            var cts = new CancellationTokenSource();

            if (!LatestReceiveFrameIndex.TryAdd(camera.DeviceAlias, 0))
            {
                LatestReceiveFrameIndex[camera.DeviceAlias] = 0;
            }

            var task = Task.Run(() => CameraParseLoop(camera, cts.Token), cts.Token);

            _cameraLoops[cameraKey] = new CameraLoopContext { Cts = cts, LoopTask = task };
        }
    }

    private async Task CameraParseLoop(XmeyeCamera camera, CancellationToken token)
    {
        _logger.Info($"{camera}后台解析线程已启动!");

        try
        {
            var channel = AppHelper.FFmpegDecodeManager.GetOrCreate(
                camera.DeviceAlias,
                new DecodeChannelOptions { CodecId = camera.AVCodeID }
            );
            _logger.Info($"{camera}拿到解析通道[{channel.ChannelId}:{camera.AVCodeID}]");
            const string consumerName = "UI";
            AppHelper.AddFrameUpdated(
                camera.DeviceAlias,
                consumerName,
                decodeFrame =>
                {
                    //_logger.Info($"{camera}进入{consumerName}处理!");
                    AppHelper.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (camera.DeviceAlias == "相机A")
                            {
                                AppHelper.EnsureWriteableBitmap(decodeFrame, ref _currentAFrame);
                                //_logger.Info($"保证WriteableBitmap!");
                                AppHelper.ConvertToWriteableBitmap(decodeFrame, _currentAFrame);
                                //_logger.Info($"解析WriteableBitmap!");
                                OnPropertyChanged(nameof(CurrentAFrame));
                                //_logger.Info($"完成更新CurrentAFrame!");
                            }
                            else
                            {
                                AppHelper.EnsureWriteableBitmap(decodeFrame, ref _currentBFrame);
                                //_logger.Info($"保证WriteableBitmap!");
                                AppHelper.ConvertToWriteableBitmap(decodeFrame, _currentBFrame);
                                //_logger.Info($"解析WriteableBitmap!");
                                OnPropertyChanged(nameof(CurrentBFrame));
                                //_logger.Info($"完成更新CurrentAFrame!");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"未完成更新{nameof(CurrentAFrame)}");
                        }
                    });
                }
            );

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10, token);

                var currentLatestFrame = camera.LatestFrame?.AddRef();
                if (currentLatestFrame == null)
                {
                    continue;
                }

                #region 处理 Frame

                try
                {
                    if (currentLatestFrame.ReceiveFrameIndex == LatestReceiveFrameIndex[camera.DeviceAlias])
                    {
                        continue;
                    }

                    //在这里处理帧数据
                    _logger.Debug(
                        $"{camera}后台解析线程处理帧数据[{currentLatestFrame.ReceiveAt:hhmmss_fff}({currentLatestFrame.ReceiveFrameIndex})]:"
                            + $" 类型={currentLatestFrame.PacketType}[{currentLatestFrame.EncodeType}], 大小={currentLatestFrame.PacketSize}"
                    );

                    LatestReceiveFrameIndex[camera.DeviceAlias] = currentLatestFrame.ReceiveFrameIndex;
                    //if (currentLatestFrame.PacketType == MEDIA_PACK_TYPE.VIDEO_I_FRAME)
                    {
                        channel.PushPacket(
                            currentLatestFrame.Buffer,
                            currentLatestFrame.PacketType == MEDIA_PACK_TYPE.VIDEO_I_FRAME
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"{camera}后台解析线程处理帧数据时发生异常!");
                }
                finally
                {
                    currentLatestFrame.Release();
                }

                #endregion
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"{camera}后台解析线程收到停止信号!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"{camera}后台解析线程异常退出!");
        }
        finally
        {
            _logger.Info($"{camera}后台解析线程已结束!");
        }
    }

    public async Task StopCameraLoopAsync(XmeyeCamera camera)
    {
        CameraLoopContext? ctx;

        lock (_lock)
        {
            if (!_cameraLoops.TryGetValue(camera.DeviceAlias, out ctx))
                return;
        }

        _logger.Info($"{camera}后台解析线程准备停止!");

        await ctx.Cts.CancelAsync();

        try
        {
            if (ctx.LoopTask != null)
                await ctx.LoopTask;
        }
        finally
        {
            lock (_lock)
            {
                _cameraLoops.Remove(camera.DeviceAlias);
            }

            ctx.Cts.Dispose();
            _logger.Info($"{camera}后台解析线程已完全停止!");
        }
    }

    #endregion
}
