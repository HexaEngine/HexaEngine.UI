namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Provides logging functionality for the HexaEngine.UI XAML Generator extension.
    /// </summary>
    public static class Logger
    {
        private const string LogSource = "HexaEngine.UI.XamlGen";
        private static readonly Lock lockObj = new();
        private static FileStream? logFileStream;
        private static TextWriter? logWriter;
        
        public static void Init()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            logFileStream = File.Create(Path.Combine(dir, "HexaXamlGenCli.log"));
            logWriter = new StreamWriter(logFileStream, encoding: Encoding.UTF8, leaveOpen: true);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Shutdown();
        }

        public static void Shutdown()
        {
            lock (lockObj)
            {
                if (logWriter == null) return;
                logWriter?.Dispose();
                logFileStream?.Flush();
                logFileStream?.Close();
            }
        }

        private static void WriteLog(string message)
        {
            lock (lockObj)
            {
                Console.WriteLine(message);
                logWriter?.WriteLine(message);
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message)
        {
            string fullMessage = $"[{LogSource}] INFO: {message}";
            WriteLog(fullMessage);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message)
        {
            string fullMessage = $"[{LogSource}] WARNING: {message}";
            WriteLog(fullMessage);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message)
        {
            string fullMessage = $"[{LogSource}] ERROR: {message}";
            WriteLog(fullMessage);
        }

        /// <summary>
        /// Logs an error message with exception details.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public static void LogError(string message, Exception exception)
        {
            string fullMessage = $"{message}: {exception.GetType().Name} - {exception.Message}\n{exception.StackTrace}";
            WriteLog($"[{LogSource}] ERROR: {fullMessage}");
        }
    }
}
