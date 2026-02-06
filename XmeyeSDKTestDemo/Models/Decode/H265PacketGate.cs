using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Models.Decode;

public sealed class H265PacketGate : IPacketGate
{
    private bool hasVps;
    private bool hasSps;
    private bool hasPps;
    private bool ready;

    public bool TryAccept(ReadOnlySpan<byte> data, bool externalKeyFlag)
    {
        ScanNal(data, out bool vps, out bool sps, out bool pps, out bool idr);

        hasVps |= vps;
        hasSps |= sps;
        hasPps |= pps;

        if (!ready)
        {
            if (hasVps && hasSps && hasPps && idr)
            {
                ready = true;
                return true;
            }
            return false;
        }

        return true;
    }

    private static void ScanNal(ReadOnlySpan<byte> data, out bool vps, out bool sps, out bool pps, out bool idr)
    {
        vps = sps = pps = idr = false;
        for (int i = 0; i + 5 < data.Length; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0 && (data[i + 2] == 1 || (data[i + 2] == 0 && data[i + 3] == 1)))
            {
                int off = data[i + 2] == 1 ? i + 3 : i + 4;
                byte nalType = (byte)((data[off] >> 1) & 0x3F);
                if (nalType == 32)
                    vps = true;
                else if (nalType == 33)
                    sps = true;
                else if (nalType == 34)
                    pps = true;
                else if (nalType == 19 || nalType == 20)
                    idr = true;
            }
        }
    }
}
