namespace XmeyeSDKTestDemo.Interfaces;

public interface IDecodeChannel : IDisposable
{
    string ChannelId { get; }

    void PushPacket(ReadOnlySpan<byte> data, bool isKeyFrame);

    IFrameConsumerRegister Consumers { get; }
}
