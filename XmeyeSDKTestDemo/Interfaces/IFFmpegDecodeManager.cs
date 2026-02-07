using XmeyeSDKTestDemo.Services.DecodeService.Decode;

namespace XmeyeSDKTestDemo.Interfaces;

public interface IFFmpegDecodeManager
{
    IDecodeChannel GetOrCreate(string channelId, DecodeChannelOptions options);
    void Remove(string channelId);
}
