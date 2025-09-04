using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MauiApp1.Services;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _filePath));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private static readonly object _lock = new object();

    public FileLogger(string categoryName, string filePath)
    {
        _categoryName = categoryName;
        _filePath = filePath;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
        
        if (exception != null)
            logEntry += Environment.NewLine + exception.ToString();

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_filePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors to prevent logging from crashing the app
            }
        }
    }
}
