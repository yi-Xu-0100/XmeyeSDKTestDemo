using Microsoft.Extensions.Hosting;
using Moyu.LogExtensions.LogHelpers;
using User.NetSDK;
using XmeyeSDKTestDemo.XmeyeService;
using static User.NetSDK.NetSDK;

namespace XmeyeSDKTestDemo.Services;

public partial class XmeyeHostService : IHostedService, IDisposable
{
    private CancellationTokenSource? _connectLoopCts;
    private Task _connectLoopTask;

    public async Task ConnectLoopAsync(CancellationToken ctsToken)
    {
        logger.Info("启动设备连接状态监测循环任务!");
        while (!ctsToken.IsCancellationRequested)
        {
            await Task.Delay(100, ctsToken);

            foreach ((string _, XmeyeCamera Device) in DeviceDic)
            {
                if (Device.LoginId >= 0)
                {
                    continue;
                }
                logger.Warn($"{Device}状态异常, 等待重新连接!");
                try
                {
                    H264_DVR_DEVICEINFO outDev = new();
                    outDev.Init();
                    int lLogin = H264_DVR_Login(
                        Device.DeviceIP,
                        Device.DevicePort,
                        Device.LoginName,
                        Device.Password,
                        ref outDev,
                        out int nError,
                        SocketStyle.TCPSOCKET
                    );

                    if (lLogin <= 0)
                    {
                        logger.Error($"{Device}重新连接过程中异常: {CameraDvrError(nError)}");
                        continue;
                    }

                    Device.LoginId = lLogin;
                    Device.DeviceInfo = outDev;
                    logger.Info($"{Device}重新连接成功!");

                    OpenRealPlay(Device);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"{Device}重新连接过程中异常!");
                }
            }
        }
        return;
    }

    private void OpenRealPlay(XmeyeCamera camera)
    {
        if (camera.PlayHandle != -1)
        {
            H264_DVR_DelRealDataCallBack_V2(camera.PlayHandle, camera.FRealDataCallBack, IntPtr.Zero);
            if (H264_DVR_StopRealPlay(camera.PlayHandle, 0) != 0)
            {
                logger.Warn($"{camera}关闭播放句柄异常!");
            }
        }

        bool result = H264_DVR_SetKeepLifeTime(camera.LoginId, 1, 3);
        if (!result)
        {
            logger.Error($"设置{camera}心跳保活失败: {CameraDvrError()}");
        }

        H264_DVR_CLIENTINFO h264DvrClientinfo = new H264_DVR_CLIENTINFO
        {
            nChannel = camera.DeviceChannel,
            nStream = camera.DeviceStream,
            nMode = camera.DeviceMode,
            hWnd = IntPtr.Zero,
        };

        camera.PlayHandle = H264_DVR_RealPlay(camera.LoginId, ref h264DvrClientinfo);
        if (camera.PlayHandle <= 0)
        {
            logger.Error($"{camera}打开播放通道[{h264DvrClientinfo}]失败: {CameraDvrError()}");
            camera.LoginId = -1;
        }
        else
        {
            logger.Info($"{camera}打开播放通道[{h264DvrClientinfo}]成功!");
            camera.FRealDataCallBack = new fRealDataCallBack_V2(camera.FRealDataCallBack_V2);
            result = H264_DVR_SetRealDataCallBack_V2(camera.PlayHandle, camera.FRealDataCallBack, IntPtr.Zero);
            if (!result)
            {
                logger.Error($"设置{camera}数据回调失败: {CameraDvrError()}");
                camera.LoginId = -1;
            }
            else
            {
                logger.Info($"设置{camera}数据回调成功!");
            }
        }
    }
}
