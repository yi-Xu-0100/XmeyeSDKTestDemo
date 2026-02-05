using XmeyeSDKTestDemo.Models.Decode;

namespace XmeyeSDKTestDemo.Interfaces;

public interface IPixelConverter<TOutput>
{
    bool CanConvert(DecodedFrame frame);

    void EnsureOutput(DecodedFrame frame, ref TOutput output);

    void Convert(DecodedFrame frame, TOutput output);
}
