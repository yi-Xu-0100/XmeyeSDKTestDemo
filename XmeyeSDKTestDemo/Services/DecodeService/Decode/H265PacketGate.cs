using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Services.DecodeService.Decode;

public sealed class H265PacketGate : IPacketGate
{
    private bool _hasVps;
    private bool _hasSps;
    private bool _hasPps;
    private bool _ready;

    public bool TryAccept(ReadOnlySpan<byte> data, bool externalKeyFlag)
    {
        ScanNal(data, out bool vps, out bool sps, out bool pps, out bool idr);

        _hasVps |= vps;
        _hasSps |= sps;
        _hasPps |= pps;

        if (!_ready)
        {
            if (_hasVps && _hasSps && _hasPps && idr)
            {
                _ready = true;
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
