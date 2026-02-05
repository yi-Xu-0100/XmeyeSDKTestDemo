using FFmpeg.AutoGen;
using XmeyeSDKTestDemo.Interfaces;
using XmeyeSDKTestDemo.Models.Decode;

namespace XmeyeSDKTestDemo.Models.PixelConverters;

public abstract unsafe class SwScalePixelConverter<TOutput> : IPixelConverter<TOutput>, IDisposable
{
    private protected SwsContext* _swsCtx;
    private int _srcW;
    private int _srcH;
    private AVPixelFormat _srcFmt;

    protected abstract AVPixelFormat TargetFormat { get; }

    public virtual void EnsureOutput(DecodedFrame frame, ref TOutput output) => throw new NotImplementedException();

    protected abstract void FillOutput(
        TOutput output,
        byte_ptrArray4 dstData,
        int_array4 dstLineSize,
        int width,
        int height
    );

    private void EnsureSwsContext(AVFrame* src)
    {
        var srcFmt = (AVPixelFormat)src->format;

        if (_swsCtx != null && _srcW == src->width && _srcH == src->height && _srcFmt == srcFmt)
        {
            return;
        }

        if (_swsCtx != null)
        {
            ffmpeg.sws_freeContext(_swsCtx);
            _swsCtx = null;
        }

        _swsCtx = ffmpeg.sws_getContext(
            src->width,
            src->height,
            srcFmt,
            src->width,
            src->height,
            TargetFormat,
            ffmpeg.SWS_BILINEAR,
            null,
            null,
            null
        );

        if (_swsCtx == null)
            throw new InvalidOperationException("sws_getContext failed.");

        _srcW = src->width;
        _srcH = src->height;
        _srcFmt = srcFmt;
    }

    public virtual bool CanConvert(DecodedFrame frame) => frame.Frame != null;

    public void Convert(DecodedFrame frame, TOutput output)
    {
        var src = frame.Frame;
        if (_swsCtx == null)
        {
            _swsCtx = ffmpeg.sws_getContext(
                src->width,
                src->height,
                (AVPixelFormat)src->format,
                src->width,
                src->height,
                TargetFormat,
                1,
                null,
                null,
                null
            );
        }

        byte_ptrArray4 dstData = default;
        int_array4 dstLineSize = default;

        AllocateTargetBuffer(ref dstData, ref dstLineSize, src->width, src->height);

        ffmpeg.sws_scale(_swsCtx, src->data, src->linesize, 0, src->height, dstData, dstLineSize);

        FillOutput(output, dstData, dstLineSize, src->width, src->height);

        FreeTargetBuffer(ref dstData);

        return;
    }

    protected abstract void AllocateTargetBuffer(
        ref byte_ptrArray4 data,
        ref int_array4 linesize,
        int width,
        int height
    );

    protected abstract void FreeTargetBuffer(ref byte_ptrArray4 data);

    public void Dispose()
    {
        if (_swsCtx != null)
        {
            ffmpeg.sws_freeContext(_swsCtx);
            _swsCtx = null;
        }
    }
}
