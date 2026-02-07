using System.Collections.Concurrent;

namespace XmeyeSDKTestDemo.Services.XmeyeService;

public static class XmeyeFramePool
{
    private static readonly ConcurrentBag<XmeyeFrame> s_pool = [];

    public static XmeyeFrame Rent(int size)
    {
        if (!s_pool.TryTake(out var frame))
        {
            frame = new XmeyeFrame();
        }

        if (frame.Buffer.Length < size)
        {
            frame.Buffer = new byte[size];
        }

        frame.Length = size;
        return frame;
    }

    public static void Return(XmeyeFrame frame)
    {
        frame.Length = 0;
        s_pool.Add(frame);
    }
}
