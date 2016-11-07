using System;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace SamyazaSSIS
{
    /// <summary>
    /// Basic implementation of a logger class
    /// </summary>
    public static class Logger
    {
        // Collection containing all log entries
        private static BlockingCollection<LogEntry> _log;

        static Logger()
        {
            _log = new BlockingCollection<LogEntry>();
        }

        #region Event

        /// <summary>
        /// Event raised when a new log entry is inserted
        /// </summary>
        /// <param name="e">LogEventArgs</param>
        public delegate void NewLogEntryEventHandler(LogEventArgs e);

        public class LogEventArgs : EventArgs
        {
            public LogEntry LogEntry { get; private set; }

            public LogEventArgs(LogEntry logEntry)
            {
                LogEntry = logEntry;
            }
        }

        #endregion

        public enum Level : byte
        {
            DEBUG, INFO, WARN, ERROR,
        }

        public struct LogEntry
        {
            public DateTime Time { get; private set; }
            public Level Level { get; private set; }
            public string Message { get; private set; }
            public Brush Color { get; private set; }

            public LogEntry(Level level, string message, Brush color)
            {
                Time = DateTime.Now;
                Level = level;
                Message = message;
                Color = color;
            }
        }

        public static event NewLogEntryEventHandler OnNewEntry;

        /// <summary>
        /// Debug log entry
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Debug(string msg)
        {
            LogEntry entry = new LogEntry(Level.DEBUG, $"{msg}", Brushes.DarkCyan);
            _log.Add(entry);
            OnNewEntry?.Invoke(new LogEventArgs(entry));
        }

        /// <summary>
        /// Common log entry
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Common(string msg)
        {
            LogEntry entry = new LogEntry(Level.INFO, $"{msg}", Brushes.Black);
            _log.Add(entry);
            OnNewEntry?.Invoke(new LogEventArgs(entry));
        }

        /// <summary>
        /// Warning
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Warn(string msg)
        {
            LogEntry entry = new LogEntry(Level.WARN, $"{msg}", Brushes.DarkOrange);
            _log.Add(entry);
            OnNewEntry?.Invoke(new LogEventArgs(entry));
        }

        /// <summary>
        /// Error
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Error(string msg)
        {
            LogEntry entry = new LogEntry(Level.ERROR, $"{msg}", Brushes.Red);
            _log.Add(entry);
            OnNewEntry?.Invoke(new LogEventArgs(entry));
        }
    }
}