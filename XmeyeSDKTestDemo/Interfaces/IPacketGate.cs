namespace XmeyeSDKTestDemo.Interfaces;

public interface IPacketGate
{
    bool TryAccept(ReadOnlySpan<byte> data, bool externalKeyFlag);
}
