using System.Collections.Concurrent;
using FFmpeg.AutoGen;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moyu.LogExtensions.LogHelpers;
using User.NetSDK;
using XmeyeSDKTestDemo.Models.Decode;
using XmeyeSDKTestDemo.XmeyeService;
using static User.NetSDK.NetSDK;

namespace XmeyeSDKTestDemo.Services;

public partial class XmeyeHostService(ILogger<XmeyeHostService> logger) : IHostedService, IDisposable
{
    private fDisConnect? _disCallback;
    private bool _disposed;
    public bool IsRunning { get; private set; }
    public Dictionary<string, XmeyeCamera> DeviceDic { get; set; } = [];

    public ConcurrentDictionary<
        string,
        ConcurrentDictionary<string, Action<DecodedFrame>?>
    > FrameUpdatedDic { get; set; } = [];

    #region Start Stop

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //initialize
        g_config.nSDKType = SDK_TYPE.SDK_TYPE_GENERAL;

        _disCallback = new fDisConnect(DisConnectBackCallFunc);
        GC.KeepAlive(_disCallback);
        H264_DVR_Init(_disCallback, IntPtr.Zero);
        H264_DVR_SetConnectTime(1000, 1);

        #region 启动连接后台
        logger.Info($"启动后台连接线程!");
        _connectLoopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _connectLoopTask = Task.Run(() => ConnectLoopAsync(_connectLoopCts.Token), cancellationToken);
        #endregion

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.Info($"服务[{nameof(XmeyeService)}]等待关闭...");
        if (!IsRunning)
        {
            logger.Warn($"服务[{nameof(XmeyeService)}]已经关闭!");
            return;
        }

        #region 取消连接循环

        _connectLoopCts?.CancelAsync();
        _connectLoopCts?.Dispose();
        _connectLoopCts = null;
        await _connectLoopTask;

        #endregion

        foreach (var device in DeviceDic)
        {
            logger.Info($"停止相机[{device.Value.DeviceIP}({device.Value.LoginId})]数据回调...");
            H264_DVR_DelRealDataCallBack_V2(device.Value.PlayHandle, device.Value.FRealDataCallBack, IntPtr.Zero);
            logger.Info($"停止相机[{device.Value.DeviceIP}({device.Value.LoginId})]播放...");
            H264_DVR_StopRealPlay(device.Value.PlayHandle, 0);
            logger.Info($"注销相机[{device.Value.DeviceIP}({device.Value.LoginId})]登录...");
            H264_DVR_Logout(device.Value.LoginId);
        }
        DeviceDic.Clear();
        H264_DVR_Cleanup();
        logger.Info($"服务[{nameof(XmeyeService)}]被关闭!");

        IsRunning = false;
        return;
    }

    #endregion

    #region dispose
    public void Dispose()
    {
        if (_disposed)
            return;
        foreach (var device in DeviceDic)
        {
            logger.Info($"停止相机[{device.Value.DeviceIP}({device.Value.LoginId})]数据回调...");
            H264_DVR_DelRealDataCallBack_V2(device.Value.PlayHandle, device.Value.FRealDataCallBack, IntPtr.Zero);
            logger.Info($"停止相机[{device.Value.DeviceIP}({device.Value.LoginId})]播放...");
            H264_DVR_StopRealPlay(device.Value.PlayHandle, 0);
            logger.Info($"注销相机[{device.Value.DeviceIP}({device.Value.LoginId})]登录...");
            H264_DVR_Logout(device.Value.LoginId);
        }
        H264_DVR_Cleanup();
        DeviceDic.Clear();
        logger.Info($"服务[{nameof(XmeyeService)}]被清除!");
        _disposed = true;
    }
    #endregion

    private void DisConnectBackCallFunc(int lLoginID, string pchDVRIP, int nDVRPort, nint dwUser)
    {
        foreach (var deviceKvp in DeviceDic)
        {
            var device = deviceKvp.Value;
            if (device.LoginId != lLoginID)
            {
                continue;
            }

            string strDisconnectInfo = $"{device}掉线!{CameraDvrError()}";
            logger.Warn(strDisconnectInfo);
            if (device.PlayHandle > 0)
            {
                H264_DVR_DelRealDataCallBack_V2(device.PlayHandle, device.FRealDataCallBack, IntPtr.Zero);
                H264_DVR_StopRealPlay(device.PlayHandle, 0);
                device.PlayHandle = -1;
            }
            logger.Info($"{device}退出登录句柄[{device.LoginId}]");
            H264_DVR_Logout(device.LoginId);
            device.LoginId = -1;
            return;
        }
    }

    private static string CameraDvrError(int? nError = null)
    {
        int nErr = nError ?? H264_DVR_GetLastError();
        return Enum.IsDefined(typeof(SDK_RET_CODE), nErr) ? $"{(SDK_RET_CODE)nErr}({nErr})" : $"Unknown({nErr})";
    }

    public bool MakeDeviceKeyFrame(string deviceAlias)
    {
        if (!DeviceDic.TryGetValue(deviceAlias, out var device))
        {
            logger.Warn($"设备[{deviceAlias}]不存在!");
            return false;
        }
        if (!device.IsConnected)
        {
            logger.Warn($"设备[{device}]未连接!");
            return false;
        }
        if (!device.IsPlayed)
        {
            logger.Warn($"设备[{device}]未播放!");
            return false;
        }
        if (!H264_DVR_MakeKeyFrame(device.LoginId, device.DeviceChannel, device.DeviceStream))
        {
            logger.Error($"{device}生成关键帧失败: {CameraDvrError()}");
            return false;
        }
        logger.Info($"{device}生成关键帧成功!");
        return true;
    }
}
