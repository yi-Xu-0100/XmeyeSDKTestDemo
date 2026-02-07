using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FFmpeg.AutoGen;
using User.NetSDK;
using XmeyeSDKTestDemo.Services.XmeyeService;
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

    public XmeyeFrame? LatestFrame => _latestFrame;

    public int FRealDataCallBack_V2(int lRealHandle, ref PACKET_INFO_EX pFrame, int dwUser)
    {
        if (lRealHandle != PlayHandle)
            return (int)SDK_RET_CODE.H264_DVR_NOERROR;

        var len = (int)pFrame.dwPacketSize;
        var pBuf = pFrame.pPacketBuffer;
        XmeyeFrame frame = XmeyeFramePool.Rent(len);
        frame.Reset(len, pFrame, (_latestFrame?.ReceiveFrameIndex ?? 0) + 1);
        Marshal.Copy(pBuf, frame.Buffer, 0, len);

        var old = Interlocked.Exchange(ref _latestFrame, frame);
        old?.Release();
        return (int)SDK_RET_CODE.H264_DVR_SUCCESS;
    }

    public fRealDataCallBack_V2 FRealDataCallBack { get; set; } = null!;

    public XmeyeCamera(string deviceAlias, string ip, AVCodecID codecID)
    {
        DeviceAlias = deviceAlias;
        DeviceIP = ip;
        AVCodeID = codecID;
        DeviceInfo = new H264_DVR_DEVICEINFO();
    }

    public override string ToString() => $"{DeviceAlias}[{DeviceIP}:{DevicePort}][{LoginId}|{PlayHandle}]";
}
