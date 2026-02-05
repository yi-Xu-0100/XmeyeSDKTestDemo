using XmeyeSDKTestDemo.Interfaces;
using XmeyeSDKTestDemo.Models.Decode;

namespace XmeyeSDKTestDemo.Models.PixelConverters;

public sealed class PixelConverterDispatcher<TOutput>
{
    private readonly List<IPixelConverter<TOutput>> _converters = [];

    public PixelConverterDispatcher(List<IPixelConverter<TOutput>> converters)
    {
        _converters = [.. converters];
    }

    public void Register(IPixelConverter<TOutput> converter) => _converters.Add(converter);

    public void EnsureOutput(DecodedFrame frame, ref TOutput output)
    {
        var converter =
            _converters.FirstOrDefault(c => c.CanConvert(frame))
            ?? throw new NotSupportedException($"Unsupported format {frame.PixelFormat}");
        converter.EnsureOutput(frame, ref output);
        return;
    }

    public void Convert(DecodedFrame frame, TOutput output)
    {
        var converter =
            _converters.FirstOrDefault(c => c.CanConvert(frame))
            ?? throw new NotSupportedException($"Unsupported format {frame.PixelFormat}");

        converter.Convert(frame, output);
        return;
    }
}
