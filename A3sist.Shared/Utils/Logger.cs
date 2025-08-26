using System;
using System.IO;

namespace A3sist.Shared.Utils
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "application.log");
        private static readonly object LockObject = new object();

        static Logger()
        {
            // Ensure the logs directory exists
            var logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void LogInfo(string message)
        {
            LogMessage("INFO", message);
        }

        public static void LogWarning(string message)
        {
            LogMessage("WARNING", message);
        }

        public static void LogError(string message)
        {
            LogMessage("ERROR", message);
        }

        public static void LogException(Exception ex)
        {
            LogMessage("EXCEPTION", $"{ex.Message}\n{ex.StackTrace}");
        }

        private static void LogMessage(string level, string message)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";

            lock (LockObject)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                    Console.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        public static string[] GetRecentLogs(int lineCount = 100)
        {
            if (!File.Exists(LogFilePath))
                return Array.Empty<string>();

            try
            {
                var allLines = File.ReadAllLines(LogFilePath);
                var startIndex = Math.Max(0, allLines.Length - lineCount);
                var count = Math.Min(lineCount, allLines.Length - startIndex);

                var result = new string[count];
                Array.Copy(allLines, startIndex, result, 0, count);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read log file: {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}