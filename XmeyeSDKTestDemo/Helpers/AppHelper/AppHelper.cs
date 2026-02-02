using System.Reflection;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace XmeyeSDKTestDemo.Helpers;

public static partial class AppHelper
{
    internal const string AppNameAlias = "相机SDK测试Demo";

    internal static string AppName { get; } = GetAssemblyName();

    internal static string AppVersion { get; } = GetAssemblyVersion();

    internal static Dispatcher Dispatcher  => GetRequiredService<Dispatcher>();

    internal static IHost Host { set; get; } = null!;

#if DEBUG
    internal const string VersionType = "[测试版]";
#else
    internal const string VersionType = "";
#endif

    internal static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }

    internal static string GetAssemblyName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
    }

    public static T GetRequiredService<T>()
        where T : class
    {
        return Host.Services.GetRequiredService<T>();
    }
}
