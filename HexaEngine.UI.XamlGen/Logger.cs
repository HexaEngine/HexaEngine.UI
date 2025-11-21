#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Provides logging functionality for the HexaEngine.UI XAML Generator extension.
    /// </summary>
    internal static class Logger
    {
        private const string LogSource = "HexaEngine.UI.XamlGen";
        private static IVsOutputWindowPane? outputPane;
        private static Guid outputPaneGuid = new Guid("49a7add4-0024-4919-a7f1-082964553e62");
        private static bool initAttempted = false;

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message)
        {
            string fullMessage = $"[{LogSource}] INFO: {message}";
            Debug.WriteLine(fullMessage);
            WriteToOutputWindow($"INFO: {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message)
        {
            string fullMessage = $"[{LogSource}] WARNING: {message}";
            Debug.WriteLine(fullMessage);
            WriteToOutputWindow($"WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message)
        {
            string fullMessage = $"[{LogSource}] ERROR: {message}";
            Debug.WriteLine(fullMessage);
            WriteToOutputWindow($"ERROR: {message}");
        }

        /// <summary>
        /// Logs an error message with exception details.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public static void LogError(string message, Exception exception)
        {
            string fullMessage = $"{message}: {exception.GetType().Name} - {exception.Message}\n{exception.StackTrace}";
            Debug.WriteLine($"[{LogSource}] ERROR: {fullMessage}");
            WriteToOutputWindow($"ERROR: {fullMessage}");
        }

        /// <summary>
        /// Writes a message to the Visual Studio Output Window.
        /// </summary>
        /// <param name="message">The message to write.</param>
        private static void WriteToOutputWindow(string message)
        {
            try
            {
                if (outputPane == null && !initAttempted)
                {
                    initAttempted = true;
                    Debug.WriteLine($"[{LogSource}] Attempting to initialize output pane...");
                    
                    try
                    {
                        ThreadHelper.JoinableTaskFactory.Run(async delegate
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            
                            Debug.WriteLine($"[{LogSource}] On UI thread, getting SVsOutputWindow service...");
                            IVsOutputWindow? outWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                            
                            if (outWindow != null)
                            {
                                Debug.WriteLine($"[{LogSource}] Got SVsOutputWindow, getting pane...");
                                int hr = outWindow.GetPane(ref outputPaneGuid, out outputPane);
                                
                                if (outputPane == null)
                                {
                                    Debug.WriteLine($"[{LogSource}] Pane not found (hr={hr}), creating new pane...");
                                    hr = outWindow.CreatePane(ref outputPaneGuid, "HexaEngine.UI XAML Generator", 1, 1);
                                    Debug.WriteLine($"[{LogSource}] CreatePane result: hr={hr}");
                                    
                                    hr = outWindow.GetPane(ref outputPaneGuid, out outputPane);
                                    Debug.WriteLine($"[{LogSource}] GetPane after create result: hr={hr}, pane={outputPane != null}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[{LogSource}] Found existing pane");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[{LogSource}] FAILED: Could not get SVsOutputWindow service");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{LogSource}] EXCEPTION during initialization: {ex.Message}");
                        Debug.WriteLine($"[{LogSource}] Stack: {ex.StackTrace}");
                    }
                }

                if (outputPane != null)
                {
                    outputPane.OutputStringThreadSafe($"[{LogSource}] {message}\r\n");
                }
                else if (initAttempted)
                {
                    Debug.WriteLine($"[{LogSource}] Output pane is null after init attempt - using Debug only");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{LogSource}] LOGGER ERROR: {ex.Message}");
                Debug.WriteLine($"[{LogSource}] {message}");
            }
        }
    }
}
