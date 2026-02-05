using XmeyeSDKTestDemo.Models.Decode;

namespace XmeyeSDKTestDemo.Interfaces;

public interface IFrameConsumerRegister
{
    void Register(string name, Action<DecodedFrame>? onFrame, int queueSize = 1);

    void Unregister(string name);
    void ClearConsumers();
}
