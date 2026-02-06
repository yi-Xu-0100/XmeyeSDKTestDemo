using System.Collections.Concurrent;
using FFmpeg.AutoGen;
using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Models.Decode;

public sealed class FFmpegDecodeManager : IFFmpegDecodeManager
{
    private readonly ConcurrentDictionary<string, IDecodeChannel> _channels = new();

    public FFmpegDecodeManager()
    {
        ffmpeg.RootPath = "./Services/DecodeService/";
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
    }

    public IDecodeChannel GetOrCreate(string channelId, DecodeChannelOptions options)
    {
        return _channels.GetOrAdd(channelId, _ =>
            new DecodeChannel(channelId, options));
    }

    public void Remove(string channelId)
    {
        if (_channels.TryRemove(channelId, out var ch))
            ch.Dispose();
    }
}
