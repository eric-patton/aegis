using System.Threading.Channels;

namespace Aegis.Cli;

/// <summary>Blocking console key reader on its own thread; feeds the host channel.</summary>
public static class ConsoleInput
{
    public static void Start(ChannelWriter<HostMessage> writer, CancellationToken ct)
    {
        var thread = new Thread(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                ConsoleKeyInfo info;
                try
                {
                    info = Console.ReadKey(intercept: true);
                }
                catch (InvalidOperationException)
                {
                    return; // No console attached.
                }

                char key = info.Key switch
                {
                    ConsoleKey.UpArrow => 'k',
                    ConsoleKey.DownArrow => 'j',
                    ConsoleKey.LeftArrow => 'h',
                    ConsoleKey.RightArrow => 'l',
                    ConsoleKey.Escape => 'q',
                    _ => char.ToLowerInvariant(info.KeyChar),
                };

                if (key != '\0') writer.TryWrite(new HostMessage.Key(key));
            }
        })
        {
            IsBackground = true,
            Name = "console-input",
        };
        thread.Start();
    }
}
