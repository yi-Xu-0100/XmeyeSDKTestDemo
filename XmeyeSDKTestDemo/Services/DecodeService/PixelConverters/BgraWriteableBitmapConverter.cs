using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;
using XmeyeSDKTestDemo.Services.DecodeService.Decode;

namespace XmeyeSDKTestDemo.Services.DecodeService.PixelConverters;

public sealed unsafe class BgraWriteableBitmapConverter : SwScalePixelConverter<WriteableBitmap>
{
    protected override AVPixelFormat TargetFormat => AVPixelFormat.AV_PIX_FMT_BGRA;

    public override void EnsureOutput(DecodedFrame frame, ref WriteableBitmap bmp)
    {
        if (bmp == null || bmp.PixelWidth != frame.Width || bmp.PixelHeight != frame.Height)
        {
            bmp = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null);
        }
    }

    public override bool CanConvert(DecodedFrame frame) => frame.Frame != null;

    protected override void AllocateTargetBuffer(
        ref byte_ptrArray4 data,
        ref int_array4 linesize,
        int width,
        int height
    )
    {
        ffmpeg.av_image_alloc(ref data, ref linesize, width, height, TargetFormat, 1);
    }

    protected override void FillOutput(
        WriteableBitmap bitmap,
        byte_ptrArray4 dstData,
        int_array4 dstLineSize,
        int width,
        int height
    )
    {
        bitmap.Lock();

        try
        {
            byte* src = dstData[0];
            int srcStride = dstLineSize[0];

            byte* dst = (byte*)bitmap.BackBuffer;
            int dstStride = bitmap.BackBufferStride;

            int copyBytes = Math.Min(dstStride, width * 4);

            for (int y = 0; y < height; y++)
            {
                Buffer.MemoryCopy(src + y * srcStride, dst + y * dstStride, dstStride, copyBytes);
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    protected override void FreeTargetBuffer(ref byte_ptrArray4 data)
    {
        byte* ptr = data[0];
        if (ptr != null)
        {
            ffmpeg.av_freep(&ptr);
            data[0] = null;
        }
    }
}
