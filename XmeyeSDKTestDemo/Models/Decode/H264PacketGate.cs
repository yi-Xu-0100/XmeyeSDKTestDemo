using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Models.Decode;

public sealed class H264PacketGate : IPacketGate
{
    private bool hasSps;
    private bool hasPps;
    private bool ready;

    public bool TryAccept(ReadOnlySpan<byte> data, bool externalKeyFlag)
    {
        ScanNal(data, out bool sps, out bool pps, out bool idr);

        hasSps |= sps;
        hasPps |= pps;

        if (!ready)
        {
            if (hasSps && hasPps && (idr || externalKeyFlag))
            {
                ready = true;
                return true;
            }
            return false;
        }

        return true;
    }

    private static void ScanNal(ReadOnlySpan<byte> data, out bool sps, out bool pps, out bool idr)
    {
        sps = pps = idr = false;
        for (int i = 0; i + 4 < data.Length; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0 && (data[i + 2] == 1 || (data[i + 2] == 0 && data[i + 3] == 1)))
            {
                int off = data[i + 2] == 1 ? i + 3 : i + 4;
                byte type = (byte)(data[off] & 0x1F);
                if (type == 7)
                    sps = true;
                else if (type == 8)
                    pps = true;
                else if (type == 5)
                    idr = true;
            }
        }
    }
}
