using System.Windows.Media.Imaging;
using XmeyeSDKTestDemo.Models.Decode;
using XmeyeSDKTestDemo.Models.PixelConverters;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static PixelConverterDispatcher<WriteableBitmap> WriteableBitmapConverterDispatcher =>
        GetRequiredService<PixelConverterDispatcher<WriteableBitmap>>();

    public static void EnsureWriteableBitmap(DecodedFrame frame, ref WriteableBitmap bitmap)
    {
        WriteableBitmapConverterDispatcher.EnsureOutput(frame, ref bitmap);
    }

    public static void ConvertToWriteableBitmap(DecodedFrame frame, WriteableBitmap bitmap)
    {
        WriteableBitmapConverterDispatcher.Convert(frame, bitmap);
    }
}
