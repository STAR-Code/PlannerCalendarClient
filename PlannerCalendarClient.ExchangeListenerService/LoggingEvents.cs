using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeListenerService
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.ExchangeListenerService;

        /// <summary>
        /// Error event ids
        /// </summary>
        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static ErrorEvent ExceptionWhenStartingTheService(string startupMode)
            {
                return new ErrorEvent(RangeStart + 901, string.Format("Unexpected Exception when starting the service in {0} mode.", startupMode));
            }

            internal static ErrorEvent ExceptionInTheInitialSetupOfTheService()
            {
                return new ErrorEvent(RangeStart + 902, "Unexpected Exception when in the initial setup of the service");
            }

            internal static ErrorEvent ExceptionInOnStart()
            {
                return new ErrorEvent(RangeStart + 903, "Unexpected Exception in service OnStart event"); 
            }

            internal static ErrorEvent UnknownServiceCommand(int command)
            {
                return new ErrorEvent(RangeStart + 904, string.Format("Unknown service command received: {0}", command));
            }

            internal static ErrorEvent ErrorRunningServiceCommand(int command, string commandName)
            {
                return new ErrorEvent(RangeStart + 906, string.Format("Error running the service command: {0} \"{1}\"", command, commandName));
            }
        }

        /// <summary>
        /// Info event ids
        /// </summary>
        internal class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static InfoEvent ServiceStart(string name, string version, string path, string releaseBuild)
            {
                return new InfoEvent(RangeStart + 101, string.Format("Service started. {0} version: {1} Executable path: \"{2}\" {3}", name, version, path, releaseBuild));
            }

            internal static InfoEvent ServiceStop(int exitCode)
            {
                return new InfoEvent(RangeStart + 103, string.Format("Service stopped. Exit code {0}", exitCode));
            }

            internal static InfoEvent ConfigurationInfo(string info)
            {
                return new InfoEvent(RangeStart + 111, info);
            }

            internal static InfoEvent RunServiceCommand(int command, string commandName)
            {
                return new InfoEvent(RangeStart + 112, string.Format("Running service command: {0} \"{1}\"", command, commandName));
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            { }
        }

        internal class DebugEvent : DebugEventIdBase
        {
            private DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static DebugEvent General(string info)
            {
                return new DebugEvent(RangeStart + 501, info);
            }
        }
    }
}