using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FFmpeg.AutoGen;
using User.NetSDK;
using XmeyeSDKTestDemo.Models;
using static User.NetSDK.NetSDK;

namespace XmeyeSDKTestDemo.XmeyeService;

public partial class XmeyeCamera : ObservableObject
{
    [ObservableProperty]
    private AVCodecID _aVCodeID = AVCodecID.AV_CODEC_ID_HEVC;

    [ObservableProperty]
    private string _deviceAlias = string.Empty;

    [ObservableProperty]
    private string _deviceIP = string.Empty;

    [ObservableProperty]
    private ushort _devicePort = 34567;

    [ObservableProperty]
    private string _loginName = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private int _loginId = -1;

    public bool IsConnected => LoginId >= 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayed))]
    private int _playHandle = -1;
    public bool IsPlayed => PlayHandle >= 0;

    [ObservableProperty]
    private H264_DVR_DEVICEINFO _deviceInfo;

    /// <summary>
    /// 通道号
    /// </summary>
    [ObservableProperty]
    private int _deviceChannel = 0;

    /// <summary>
    /// 0表示主码流，为1表示子码流
    /// </summary>
    [ObservableProperty]
    private int _deviceStream = 0;

    /// <summary>
    ///  0：TCP方式,1：UDP方式,2：多播方式,3 - RTP方式，4-音视频分开(TCP)
    /// </summary>
    [ObservableProperty]
    private int _deviceMode;

    private XmeyeFrame? _latestFrame;

    private XmeyeFrame? _latestIFrame;

    public XmeyeFrame? LatestFrame => _latestFrame;

    public XmeyeFrame? LatestIFrame => _latestIFrame;

    public int FRealDataCallBack_V2(int lRealHandle, ref PACKET_INFO_EX pFrame, int dwUser)
    {
        if (lRealHandle != PlayHandle)
            return (int)SDK_RET_CODE.H264_DVR_NOERROR;

        var len = (int)pFrame.dwPacketSize;
        var pBuf = pFrame.pPacketBuffer;
        XmeyeFrame frame = XmeyeFramePool.Rent(len);
        frame.Reset(len);
        frame.PacketType = pFrame.nPacketType;
        frame.PacketSize = pFrame.dwPacketSize;
        frame.EncodeType = pFrame.nEncodeType;
        frame.Year = pFrame.nYear;
        frame.Month = pFrame.nMonth;
        frame.Day = pFrame.nDay;
        frame.Hour = pFrame.nHour;
        frame.Minute = pFrame.nMinute;
        frame.Second = pFrame.nSecond;
        frame.TimeStamp = pFrame.dwTimeStamp;
        frame.TimeStampHigh = pFrame.dwTimeStampHigh;
        frame.FrameNum = pFrame.dwFrameNum;
        frame.FrameRate = pFrame.dwFrameRate;
        frame.Width = pFrame.uWidth;
        frame.Height = pFrame.uHeight;
        frame.ReceiveFrameIndex = (_latestFrame?.ReceiveFrameIndex ?? 0) + 1;
        frame.ReceiveAt = DateTimeOffset.Now;
        Marshal.Copy(pBuf, frame.Buffer, 0, len);

        var old = Interlocked.Exchange(ref _latestFrame, frame);
        old?.Release();

        if (frame.PacketType == MEDIA_PACK_TYPE.VIDEO_I_FRAME)
        {
            XmeyeFrame frame1 = XmeyeFramePool.Rent(len);
            frame1.Reset(len);
            frame1.PacketType = pFrame.nPacketType;
            frame1.PacketSize = pFrame.dwPacketSize;
            frame1.EncodeType = pFrame.nEncodeType;
            frame1.Year = pFrame.nYear;
            frame1.Month = pFrame.nMonth;
            frame1.Day = pFrame.nDay;
            frame1.Hour = pFrame.nHour;
            frame1.Minute = pFrame.nMinute;
            frame1.Second = pFrame.nSecond;
            frame1.TimeStamp = pFrame.dwTimeStamp;
            frame1.TimeStampHigh = pFrame.dwTimeStampHigh;
            frame1.FrameNum = pFrame.dwFrameNum;
            frame1.FrameRate = pFrame.dwFrameRate;
            frame1.Width = pFrame.uWidth;
            frame1.Height = pFrame.uHeight;
            frame1.ReceiveFrameIndex = (_latestIFrame?.ReceiveFrameIndex ?? 0) + 1;
            frame1.ReceiveAt = DateTimeOffset.Now;
            Marshal.Copy(pBuf, frame1.Buffer, 0, len);
            var oldI = Interlocked.Exchange(ref _latestIFrame, frame1);
            oldI?.Release();
        }
        return (int)SDK_RET_CODE.H264_DVR_SUCCESS;
    }

    public fRealDataCallBack_V2 FRealDataCallBack { get; set; } = null!;

    public XmeyeCamera(string deviceAlias, string ip)
    {
        DeviceAlias = deviceAlias;
        DeviceIP = ip;
        DeviceInfo = new H264_DVR_DEVICEINFO();
    }

    public override string ToString() => $"{DeviceAlias}[{DeviceIP}:{DevicePort}][{LoginId}|{PlayHandle}]";
}
