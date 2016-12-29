using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.DataAccess
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.DataAccess;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static ErrorEvent VerifyDatabaseConnectionFailed(string databaseName)
            {
                return new ErrorEvent(RangeStart + 901, string.Format("Failed to open the database: '{0}'.", databaseName));
            }

            internal static ErrorEvent ConfigurationOfECSClientExchangeDbEntitiesFailed()
            {
                return new ErrorEvent(RangeStart + 902, "Configuration ECSClientExchangeDbEntities is not found!");
            }

            internal static ErrorEvent DataSaveExceptionDetail(string exceptionInfo)
            {
                return new ErrorEvent(RangeStart + 903, string.Format("Unexpected database save exception: {0}", exceptionInfo));
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            { }
        }

        internal class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }
        }

        internal class DebugEvent : DebugEventIdBase
        {
            private DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static DebugEvent DataSource(string databaseName)
            {
                return new DebugEvent(RangeStart + 501, string.Format("Open the database: '{0}'.", databaseName));
            }
        }
    }
}