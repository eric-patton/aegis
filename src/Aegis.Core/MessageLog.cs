namespace Aegis.Core;

public readonly record struct LogEntry(int Turn, string Text, LogTone Tone);

public enum LogTone { Info, Combat, Danger, Aegis, Reward }

public sealed class MessageLog
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public void Add(int turn, string text, LogTone tone = LogTone.Info)
        => _entries.Add(new LogEntry(turn, text, tone));

    public IEnumerable<LogEntry> Recent(int count)
        => _entries.Skip(Math.Max(0, _entries.Count - count));

    internal void Truncate(int count)
    {
        if (count < 0 || count > _entries.Count)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (count < _entries.Count)
            _entries.RemoveRange(count, _entries.Count - count);
    }
}
