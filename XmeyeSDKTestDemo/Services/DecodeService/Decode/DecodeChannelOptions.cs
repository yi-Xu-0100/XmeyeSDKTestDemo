using FFmpeg.AutoGen;

namespace XmeyeSDKTestDemo.Services.DecodeService.Decode;

public sealed class DecodeChannelOptions
{
    public AVCodecID CodecId { get; init; } = AVCodecID.AV_CODEC_ID_H264;

    public int PacketQueueSize { get; init; } = 100;
    public int FrameQueueSize { get; init; } = 3;

    public bool OnlyKeyFrame { get; init; }
}
