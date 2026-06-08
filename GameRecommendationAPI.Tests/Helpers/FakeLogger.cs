using Microsoft.Extensions.Logging;

namespace GameRecommendation.SteamImporter.Helpers
{
    /// <summary>
    /// Test implementation of ILogger<T> that records log entries (log level and formatted message) for inspection in
    /// tests.
    /// </summary>
    /// <remarks>Entries are stored in the Entries property as (LogLevel, Message) tuples. Not thread-safe;
    /// intended for single-threaded test scenarios.</remarks>
    /// <typeparam name="T">The type used to categorize log messages; its name is used as the logger category.</typeparam>
    public class FakeLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
