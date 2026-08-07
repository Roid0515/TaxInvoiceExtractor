using System.Text;

namespace TaxInvoiceExtractor.Logging;

public static class AppLogger
{
    private static readonly object Gate = new();

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message) => Write("WARN", message, null);
    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{DateTime.Now:yyyyMMdd}.log");
            var detail = exception is null ? string.Empty : $" | {exception.GetType().Name}: {exception.Message}";
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{detail}{Environment.NewLine}";
            lock (Gate) File.AppendAllText(path, line, new UTF8Encoding(false));
        }
        catch
        {
            // Logging must never terminate business processing.
        }
    }
}
