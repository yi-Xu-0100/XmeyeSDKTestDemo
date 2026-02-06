using User.NetSDK;

namespace XmeyeSDKTestDemo.Services.XmeyeService;

public sealed class XmeyeFrame
{
    /// <summary>
    /// 包类型,见 MEDIA_PACK_TYPE
    /// </summary>
    public MEDIA_PACK_TYPE PacketType { get; set; }

    /// <summary>
    /// 包的大小
    /// </summary>
    public uint PacketSize { get; set; }

    /// <summary>
    /// 流类型 见 SDK_ENCODE_TYPE
    /// </summary>
    public SDK_ENCODE_TYPE EncodeType { get; set; }

    /// <summary>
    /// 时标:年
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 时标:月
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// 时标:日
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// 时标:时
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// 时标:分
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    /// 时标:秒
    /// </summary>
    public int Second { get; set; }

    /// <summary>
    /// 相对时标低位，单位为毫秒
    /// </summary>
    public uint TimeStamp { get; set; }

    /// <summary>
    /// 相对时标高位，单位为毫秒
    /// </summary>
    public uint TimeStampHigh { get; set; }

    /// <summary>
    /// 帧序号
    /// </summary>
    public uint FrameNum { get; set; }

    /// <summary>
    /// 帧率
    /// </summary>
    public uint FrameRate { get; set; }

    /// <summary>
    /// 图像宽度
    /// </summary>
    public ushort Width { get; set; }

    /// <summary>
    /// 图像高度
    /// </summary>
    public ushort Height { get; set; }

    /// <summary>
    /// 帧数据缓冲
    /// </summary>
    public byte[] Buffer { get; set; } = [];

    /// <summary>
    /// 帧缓存长度
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// 引用计数
    /// </summary>
    private int _refCount;

    public long ReceiveFrameIndex { get; set; }

    public DateTimeOffset ReceiveAt { get; set; } = DateTimeOffset.Now;

    public void Reset(int size, PACKET_INFO_EX pFrame, long index)
    {
        if (Buffer.Length < size)
            Buffer = new byte[size];

        Length = size;
        _refCount = 1; // 初始持有者
        PacketType = pFrame.nPacketType;
        PacketSize = pFrame.dwPacketSize;
        EncodeType = pFrame.nEncodeType;
        Year = pFrame.nYear;
        Month = pFrame.nMonth;
        Day = pFrame.nDay;
        Hour = pFrame.nHour;
        Minute = pFrame.nMinute;
        Second = pFrame.nSecond;
        TimeStamp = pFrame.dwTimeStamp;
        TimeStampHigh = pFrame.dwTimeStampHigh;
        FrameNum = pFrame.dwFrameNum;
        FrameRate = pFrame.dwFrameRate;
        Width = pFrame.uWidth;
        Height = pFrame.uHeight;
        ReceiveFrameIndex = index;
        ReceiveAt = DateTimeOffset.Now;
    }

    public XmeyeFrame AddRef()
    {
        Interlocked.Increment(ref _refCount);
        return this;
    }

    public void Release()
    {
#if DEBUG
        if (_refCount < 0)
            throw new InvalidOperationException($"XmeyeFrame double released({_refCount})! {nameof(Length)}: {Length}");
#endif
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            Length = 0;
            XmeyeFramePool.Return(this);
        }
    }
}
