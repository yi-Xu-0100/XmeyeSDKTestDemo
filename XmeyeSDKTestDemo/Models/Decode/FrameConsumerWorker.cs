using System.Collections.Concurrent;
using NLog;

namespace XmeyeSDKTestDemo.Models.Decode;

internal sealed class FrameConsumerWorker
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public string Name { get; set; }

    private readonly BlockingCollection<DecodedFrame> _queue;
    private event Action<DecodedFrame>? _handler;

    public FrameConsumerWorker(string name, int capacity, Action<DecodedFrame>? handler)
    {
        Name = name;
        _queue = new BlockingCollection<DecodedFrame>(capacity);
        _handler = handler;

        Task.Run(ConsumeLoop);
    }

    public void TryPost(DecodedFrame frame)
    {
        //_logger.Info($"frame[{frame.PixelFormat}]准备推送消费者[{Name}]!");
        if (!_queue.TryAdd(frame))
        {
            _logger.Warn($"frame[{frame.PixelFormat}]推送消费者[{Name}]失败!");
            frame.Dispose(); // 慢的消费者自己丢
        }
    }

    private void ConsumeLoop()
    {
        _logger.Info($"开启了消费[{Name}]线程!");
        foreach (var frame in _queue.GetConsumingEnumerable())
        {
            //_logger.Info($"[{Name}]线程准备消费frame[{frame.PixelFormat}]!");
            try
            {
                _handler?.Invoke(frame);
                //_logger.Info($"[{Name}]线程完成消费frame[{frame.PixelFormat}]!");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[{Name}]线程消费frame[{frame.PixelFormat}]异常!");
            }
            finally
            {
                frame.Dispose();
            }
        }
    }
}
