using System;

namespace PlannerCalendarClient.Logging
{
    public interface ILogger
    {
        void LogError(EventIdBase logEvent, params object[] data);

        void LogError(Exception exception, EventIdBase logEvent, params object[] data);

        void LogWarning(WarningEventIdBase logEvent, params object[] data);

        void LogWarning(Exception exception, WarningEventIdBase logEvent, params object[] data);

        void LogInfo(InfoEventIdBase logEvent, params object[] data);

        void LogDebug(DebugEventIdBase logEvent, params object[] data);
    }
}