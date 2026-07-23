using System.Threading.Channels;
using Aegis.Host;

namespace Aegis.Cli;

/// <summary>
/// Polls the console size and posts a redraw when it changes. The engine loop is
/// input-driven, so without this a resized window would not repaint until a key press.
/// </summary>
public static class ResizeWatcher
{
    public static void Start(ChannelWriter<HostMessage> writer, CancellationToken ct)
    {
        var thread = new Thread(() =>
        {
            int lastWidth = -1, lastHeight = -1;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int width = Console.WindowWidth, height = Console.WindowHeight;
                    if (width != lastWidth || height != lastHeight)
                    {
                        lastWidth = width;
                        lastHeight = height;
                        writer.TryWrite(HostMessage.Redraw.Instance);
                    }
                }
                catch (IOException)
                {
                    return; // No console to watch.
                }
                Thread.Sleep(150);
            }
        })
        {
            IsBackground = true,
            Name = "resize-watcher",
        };
        thread.Start();
    }
}
