using System;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.UnitTest
{
    public class TestLogger : ILogger
    {
        public void LogError(EventIdBase logEvent, params object[] data)
        {
        }

        public void LogError(Exception exception, EventIdBase logEvent, params object[] data)
        {
        }

        public void LogWarning(WarningEventIdBase logEvent, params object[] data)
        {
        }

        public void LogWarning(Exception exception, WarningEventIdBase logEvent, params object[] data)
        {
        }

        public void LogInfo(InfoEventIdBase logEvent, params object[] data)
        {
        }

        public void LogDebug(DebugEventIdBase logEvent, params object[] data)
        {
        }
    }
}
