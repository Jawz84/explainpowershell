using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class LoggerDouble<T> : ILogger, ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

        // Add more of these if they make life easier.
        public IEnumerable<LogEntry> InformationEntries =>
            LogEntries.Where(e => e.LogLevel == LogLevel.Information);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LogEntries.Add(new LogEntry(logLevel, eventId, state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new LoggingScope();
        }

        public class LoggingScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public class LogEntry
    {
        public LogEntry(LogLevel logLevel, EventId eventId, object state, Exception exception)
        {
            LogLevel = logLevel;
            EventId = eventId;
            State = state;
            Exception = exception;
        }

        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
        public object State { get; }
        public Exception Exception { get; }
    }
}