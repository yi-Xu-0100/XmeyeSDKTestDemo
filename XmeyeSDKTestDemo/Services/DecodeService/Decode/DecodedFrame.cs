using FFmpeg.AutoGen;

namespace XmeyeSDKTestDemo.Services.DecodeService.Decode;

public sealed unsafe class DecodedFrame : IDisposable
{
    public int Width { get; init; }
    public int Height { get; init; }
    public AVPixelFormat PixelFormat { get; init; }
    public long Pts { get; init; }

    internal AVFrame* Frame { get; private set; }

    public static DecodedFrame From(AVFrame* src)
    {
        AVFrame* dst = ffmpeg.av_frame_alloc();
        if (dst == null)
            throw new InvalidOperationException();

        dst->format = src->format;
        dst->width = src->width;
        dst->height = src->height;
        dst->pts = src->pts;

        ffmpeg.av_frame_get_buffer(dst, 32);
        ffmpeg.av_frame_copy(dst, src);
        ffmpeg.av_frame_copy_props(dst, src);

        return new DecodedFrame
        {
            Width = dst->width,
            Height = dst->height,
            PixelFormat = (AVPixelFormat)dst->format,
            Pts = dst->pts,
            Frame = dst,
        };
    }

    public DecodedFrame Clone()
    {
        return From(Frame);
    }

    public void Dispose()
    {
        unsafe
        {
            if (Frame != null)
            {
                AVFrame* tmp = Frame;
                ffmpeg.av_frame_free(&tmp);
                Frame = null;
            }
        }
    }
}
