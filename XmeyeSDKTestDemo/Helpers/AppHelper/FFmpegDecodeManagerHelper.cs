using XmeyeSDKTestDemo.Interfaces;
using XmeyeSDKTestDemo.Models.Decode;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal static IFFmpegDecodeManager FFmpegDecodeManager => GetRequiredService<IFFmpegDecodeManager>();

}
