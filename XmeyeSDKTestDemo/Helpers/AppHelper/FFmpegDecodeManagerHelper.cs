using XmeyeSDKTestDemo.Interfaces;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static IFFmpegDecodeManager FFmpegDecodeManager => GetRequiredService<IFFmpegDecodeManager>();

}
