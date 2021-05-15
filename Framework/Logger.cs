using System;
using System.IO;

namespace PracticeMakesPerfect.Framework
{
    internal class Logger
    {
        private static StreamWriter logStreamWriter;
        private readonly bool enableLogging;

        public Logger(string modDir, string fileName, bool enableLogging)
        {
            var filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStreamWriter = File.AppendText(filePath);
            logStreamWriter.AutoFlush = true;

            this.enableLogging = enableLogging;
        }

        public void LogMessage(string message)
        {
            if (enableLogging)
            {
                var ts = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"INFO: {ts} - {message}");
            }
        }


        public static void LogError(string message)
        {
            var ts = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"ERROR: {ts} - {message}");
        }

        public static void LogException(Exception exception)
        {
            var ts = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"CRITICAL: {ts} - {exception}");
        }
    }
}
