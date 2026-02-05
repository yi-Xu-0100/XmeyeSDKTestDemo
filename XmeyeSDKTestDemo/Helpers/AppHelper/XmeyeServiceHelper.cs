using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Windows.Media.Media3D;
using NLog;
using XmeyeSDKTestDemo.Models.Decode;
using XmeyeSDKTestDemo.Services;
using XmeyeSDKTestDemo.XmeyeService;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    internal static XmeyeHostService XmeyeService => GetRequiredService<XmeyeHostService>();

    public static bool TryAddDevice(XmeyeCamera camera)
    {
        if (XmeyeService.DeviceDic.TryAdd(camera.DeviceAlias, camera))
        {
            var channel = FFmpegDecodeManager.GetOrCreate(
                camera.DeviceAlias,
                new DecodeChannelOptions { CodecId = camera.AVCodeID }
            );
            XmeyeService.FrameUpdatedDic.TryAdd(
                camera.DeviceAlias,
                new ConcurrentDictionary<string, Action<DecodedFrame>?>() { ["UI"] = null, ["AI"] = null }
            );
            XmeyeService.FrameUpdatedDic.TryAdd(
                camera.DeviceAlias,
                new ConcurrentDictionary<string, Action<DecodedFrame>?>() { ["UI"] = null, ["AI"] = null }
            );
            // UI
            channel.Consumers.Register("UI", XmeyeService.FrameUpdatedDic[camera.DeviceAlias]["UI"], queueSize: 1);
            // AI
            channel.Consumers.Register("AI", XmeyeService.FrameUpdatedDic[camera.DeviceAlias]["AI"], queueSize: 1);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static XmeyeCamera? GetDevice(string cameraAlias)
    {
        return XmeyeService.DeviceDic.TryGetValue(cameraAlias, out XmeyeCamera? camera) ? camera : null;
    }

    public static void AddFrameUpdated(string cameraAlias, string consumerName, Action<DecodedFrame>? frameUpdated)
    {
        if (XmeyeService.DeviceDic.TryGetValue(cameraAlias, out XmeyeCamera? camera))
        {
            var channel = FFmpegDecodeManager.GetOrCreate(
                camera.DeviceAlias,
                new DecodeChannelOptions { CodecId = camera.AVCodeID }
            );
            channel.Consumers.Unregister(consumerName);
            XmeyeService
                .FrameUpdatedDic[cameraAlias]
                .AddOrUpdate(
                    consumerName,
                    frameUpdated,
                    (_, existing) =>
                    {
                        return existing != null
                            ? frameUpdated != null
                                ? existing + frameUpdated
                                : existing
                            : frameUpdated ?? default;
                    }
                );
            channel.Consumers.Register(consumerName, XmeyeService.FrameUpdatedDic[camera.DeviceAlias][consumerName], queueSize: 1);
        }
        else
        {
            _logger.Warn($"设备[{cameraAlias}]未注册!");
        }
    }

    public static void RemoveFrameUpdatedInUI(
        string cameraAlias,
        string consumerName,
        Action<DecodedFrame>? frameUpdated
    )
    {
        if (XmeyeService.DeviceDic.TryGetValue(cameraAlias, out XmeyeCamera? camera))
        {
            var channel = FFmpegDecodeManager.GetOrCreate(
                camera.DeviceAlias,
                new DecodeChannelOptions { CodecId = camera.AVCodeID }
            );
            channel.Consumers.Unregister(consumerName);
            XmeyeService
                .FrameUpdatedDic[cameraAlias]
                .AddOrUpdate(
                    consumerName,
                    frameUpdated,
                    (_, existing) =>
                    {
                        return existing != null
                            ? frameUpdated != null
                                ? existing - frameUpdated
                                : existing
                            : default;
                    }
                );
            channel.Consumers.Register(consumerName, XmeyeService.FrameUpdatedDic[camera.DeviceAlias][consumerName], queueSize: 1);
        }
        else
        {
            _logger.Warn($"设备[{cameraAlias}]未注册!");
        }
    }
}
