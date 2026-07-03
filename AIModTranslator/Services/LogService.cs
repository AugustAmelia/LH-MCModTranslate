using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace AIModTranslator.Services;

public class LogService
{
    public ObservableCollection<LogEntry> Logs { get; } = new();

    public void Info(string message) => Add("INFO", message);
    public void Warn(string message) => Add("WARN", message);
    public void Error(string message) => Add("ERROR", message);

    private void Add(string level, string message)
    {
        var entry = new LogEntry(DateTime.Now, level, message);
        
        if (Dispatcher.UIThread.CheckAccess())
        {
            Logs.Add(entry);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() => Logs.Add(entry));
        }
    }
}

public sealed record LogEntry(DateTime Time, string Level, string Message);
