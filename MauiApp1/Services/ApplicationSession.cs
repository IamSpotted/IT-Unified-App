using System;

namespace MauiApp1.Services
{
    /// <summary>
    /// Provides a static application session ID that persists for the entire application lifecycle
    /// </summary>
    public static class ApplicationSession
    {
        private static readonly Guid _sessionId = Guid.NewGuid();
        private static readonly DateTime _sessionStartTime = DateTime.Now;

        /// <summary>
        /// Gets the unique session ID for this application instance.
        /// This ID remains constant until the application is closed and reopened.
        /// </summary>
        public static Guid SessionId => _sessionId;

        /// <summary>
        /// Gets the time when this application session started
        /// </summary>
        public static DateTime SessionStartTime => _sessionStartTime;

        /// <summary>
        /// Gets a human-readable session info string for logging purposes
        /// </summary>
        public static string SessionInfo => $"Session {_sessionId} started at {_sessionStartTime:yyyy-MM-dd HH:mm:ss}";

        /// <summary>
        /// Gets the current session duration
        /// </summary>
        public static TimeSpan SessionDuration => DateTime.Now - _sessionStartTime;
    }
}
