using Celsius.Core.Settings;

namespace Celsius.Core.Logging;

/// <summary>
/// Minimal, dependency-free file logger that records only error and critical
/// messages to <c>%AppData%\Celsius\logs\celsius.log</c>.
/// </summary>
public sealed class FileLogger
{
    private readonly object _sync = new();
    private readonly string _filePath;

    /// <summary>Creates a logger writing to the default log directory.</summary>
    public FileLogger()
        : this(Path.Combine(AppPaths.LogDirectory, "celsius.log"))
    {
    }

    /// <summary>Creates a logger writing to an explicit file path (testable).</summary>
    public FileLogger(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>Path of the log file.</summary>
    public string FilePath => _filePath;

    /// <summary>Records an error message.</summary>
    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", message, exception);

    /// <summary>Records a critical message (exceptions that stopped an operation).</summary>
    public void Critical(string message, Exception? exception = null) =>
        Write("CRITICAL", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var line =
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}" +
                (exception is null ? string.Empty : Environment.NewLine + exception);

            lock (_sync)
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }
}
