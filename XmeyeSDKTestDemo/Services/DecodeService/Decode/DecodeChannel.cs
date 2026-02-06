using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using NLog;
using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Services.DecodeService.Decode;

public sealed unsafe class DecodeChannel : IDecodeChannel, IFrameConsumerRegister
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public string ChannelId { get; }
    private readonly BlockingCollection<IntPtr> _packetQueue;
    private readonly BlockingCollection<DecodedFrame> _frameQueue;

    private readonly List<FrameConsumerWorker> _consumers = [];

    private AVCodecContext* _codecCtx;
    private readonly IPacketGate _gate;
    private readonly CancellationTokenSource _cts = new();

    public IFrameConsumerRegister Consumers => this;

    public DecodeChannel(string channelId, DecodeChannelOptions options)
    {
        ChannelId = channelId;

        _packetQueue = new BlockingCollection<IntPtr>(options.PacketQueueSize);
        _frameQueue = new BlockingCollection<DecodedFrame>(options.FrameQueueSize);

        var codec = ffmpeg.avcodec_find_decoder(options.CodecId);
        _codecCtx = ffmpeg.avcodec_alloc_context3(codec);
        int ret = ffmpeg.avcodec_open2(_codecCtx, codec, null);
        if (ret < 0)
        {
            // 将错误码转换成可读字符串
            byte* errBuf = stackalloc byte[1024];
            ffmpeg.av_strerror(ret, errBuf, 1024);
            string msg = Marshal.PtrToStringAnsi((IntPtr)errBuf) ?? "Unknown error";
            throw new InvalidOperationException($"avcodec_open2 failed: {msg}");
        }
        _gate = options.CodecId switch
        {
            AVCodecID.AV_CODEC_ID_H264 => new H264PacketGate(),
            AVCodecID.AV_CODEC_ID_HEVC => new H265PacketGate(),
            _ => throw new NotSupportedException(),
        };
        _logger.Info(
            $"初始化了通道[{channelId}]的解码器[{options.CodecId}],"
                + $" {nameof(options.PacketQueueSize)}:{options.PacketQueueSize},"
                + $" {nameof(options.FrameQueueSize)}:{options.FrameQueueSize}"
        );

        StartDecodeLoop();
        StartDispatchLoop();
    }

    #region Packet Input (SDK Callback)

    public void PushPacket(ReadOnlySpan<byte> data, bool isKeyFrame)
    {
        if (!_gate.TryAccept(data, isKeyFrame))
            return;
        if (_packetQueue.Count >= _packetQueue.BoundedCapacity)
            return; // 永不阻塞回调

        var pkt = ffmpeg.av_packet_alloc();
        ffmpeg.av_new_packet(pkt, data.Length);

        fixed (byte* src = data)
            Buffer.MemoryCopy(src, pkt->data, data.Length, data.Length);

        if (isKeyFrame)
            pkt->flags |= ffmpeg.AV_PKT_FLAG_KEY;

        _packetQueue.TryAdd((IntPtr)pkt);
    }

    #endregion

    #region Decode Loop

    private void StartDecodeLoop()
    {
        Task.Run(
            () =>
            {
                var frame = ffmpeg.av_frame_alloc();
                try
                {
                    foreach (var pktPtr in _packetQueue.GetConsumingEnumerable(_cts.Token))
                    {
                        var pkt = (AVPacket*)pktPtr;
                        int sret = ffmpeg.avcodec_send_packet(_codecCtx, pkt);
                        ffmpeg.av_packet_free(&pkt);
                        if (sret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        {
                            // 不 break，直接进入 receive
                            _logger.Warn($"通道[{ChannelId}]解析线程发送包返回{nameof(ffmpeg.EAGAIN)}");
                        }
                        else if (sret < 0)
                        {
                            _logger.Info($"通道[{ChannelId}]解析线程发送包失败! {nameof(sret)}: {sret}");
                            break;
                        }
                        while (!_cts.IsCancellationRequested)
                        {
                            int ret = ffmpeg.avcodec_receive_frame(_codecCtx, frame);
                            //_logger.Info($"通道[{ChannelId}]({GetHashCode()})解析线程接收包[{ret}]!");
                            if (ret == 0)
                            {
                                var decoded = DecodedFrame.From(frame);
                                //_logger.Info($"通道[{ChannelId}]解析线程接收包[{decoded.PixelFormat}]!");
                                if (!_frameQueue.TryAdd(decoded))
                                    decoded.Dispose();

                                ffmpeg.av_frame_unref(frame);
                            }
                            else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                            {
                                break;
                            }
                            else if (ret == ffmpeg.AVERROR_EOF)
                            {
                                break;
                            }
                            else
                            {
                                _logger.Error($"通道[{ChannelId}]接收包失败! {nameof(ret)}:{ret}");
                                break;
                            }
                        }
                    }

                    // flush decoder
                    ffmpeg.avcodec_send_packet(_codecCtx, null);

                    while (!_cts.IsCancellationRequested)
                    {
                        int ret = ffmpeg.avcodec_receive_frame(_codecCtx, frame);
                        if (ret == 0)
                        {
                            var decoded = DecodedFrame.From(frame);
                            if (!_frameQueue.TryAdd(decoded))
                                decoded.Dispose();

                            ffmpeg.av_frame_unref(frame);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"通道[{ChannelId}]解析线程异常退出!");
                }
                finally
                {
                    ffmpeg.av_frame_free(&frame);
                }
            },
            _cts.Token
        );
    }

    #endregion

    #region Dispatch Loop

    private void StartDispatchLoop()
    {
        Task.Run(
            () =>
            {
                _logger.Info($"通道[{ChannelId}]准备分发!");
                foreach (var frame in _frameQueue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        foreach (var consumer in _consumers)
                        {
                            var clone = frame.Clone();
                            //_logger.Info($"通道[{ChannelId}]分发帧[{clone.PixelFormat}]给消费者[{consumer.Name}]!");
                            consumer.Post(clone);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"通道[{ChannelId}]分发帧[{frame.PixelFormat}]给消费者异常!");
                    }
                    finally
                    {
                        frame.Dispose();
                    }
                }
            },
            _cts.Token
        );
    }

    #endregion

    #region Consumer Register

    public void Register(string name, Action<DecodedFrame>? onFrame, int queueSize = 1)
    {
        _consumers.Add(new FrameConsumerWorker(name, queueSize, onFrame));
    }

    public void Unregister(string name)
    {
        _consumers.RemoveAll(item => item.Name == name);
    }

    public void ClearConsumers()
    {
        _consumers.Clear();
    }

    #endregion

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _packetQueue.CompleteAdding();
        _frameQueue.CompleteAdding();

        unsafe
        {
            AVCodecContext* ctx = _codecCtx; // 局部变量
            ffmpeg.avcodec_free_context(&ctx);
            _codecCtx = null; // 把类字段置空
        }
    }
}
