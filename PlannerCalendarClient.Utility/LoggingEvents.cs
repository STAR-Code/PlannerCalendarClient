using System;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.Utility
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.Utility;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static ErrorEvent IntervalCallbackTimerCallbackException(string timerName)
            {
                return new ErrorEvent(RangeStart + 901, string.Format("Exception received in the interval timer '{0}' callback.", timerName));
            }

            internal static ErrorEvent DailyCallbackTimerCallbackException(string timerName)
            {
                return new ErrorEvent(RangeStart + 902, string.Format("Exception received in the daily timer '{0}' callback.", timerName));
            }

            internal static ErrorEvent ErrorReactivationIntervalCallbackTimer(string timerName)
            {
                return new ErrorEvent(RangeStart + 903, string.Format("Error reactivating a waiting interval callback timer '{0}'. The timer is already setup for the next timing event.", timerName));
            }

            internal static ErrorEvent ErrorReactivationDailyCallbackTimer(string timerName)
            {
                return new ErrorEvent(RangeStart + 904, string.Format("Error reactivating a waiting daily callback timer '{0}'.", timerName));
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

            internal static DebugEvent DailyCallbackTimerStart(string timerName)
            {
                return new DebugEvent(RangeStart + 501, string.Format("'{0}' start.", timerName));
            }

            internal static DebugEvent DailyCallbackTimerStop(string timerName)
            {
                return new DebugEvent(RangeStart + 502, string.Format("'{0}' stopped.", timerName));
            }

            internal static DebugEvent DailyCallbackTimerRestart(string timerName)
            {
                return new DebugEvent(RangeStart + 503, string.Format("'{0}' restart.", timerName));
            }

            internal static DebugEvent DailyCallbackTimerNoRestart(string timerName)
            {
                return new DebugEvent(RangeStart + 504, string.Format("'{0}' NO restart.", timerName));
            }

            internal static DebugEvent DailyCallbackTimerCallback(string timerName)
            {
                return new DebugEvent(RangeStart + 505, string.Format("'{0}' callback.", timerName));
            }

            internal static DebugEvent DailyCallbackTimeWillRunAt(string timerName, DateTime? nextEvent, TimeSpan? interval, string autoRestart)
            {
                return new DebugEvent(RangeStart + 506, string.Format("'{0}' started. Next callback event: {1}. Interval set to: {2} ({3})", timerName, nextEvent, interval, autoRestart));
            }

            internal static DebugEvent IntervalCallbackTimerStart(string timerName)
            {
                return new DebugEvent(RangeStart + 507, string.Format("'{0}' start.", timerName));
            }

            internal static DebugEvent IntervalCallbackTimerStop(string timerName)
            {
                return new DebugEvent(RangeStart + 508, string.Format("'{0}' stopped.", timerName));
            }

            internal static DebugEvent IntervalCallbackTimerRestart(string timerName)
            {
                return new DebugEvent(RangeStart + 509, string.Format("'{0}' restart.", timerName));
            }

            internal static DebugEvent IntervalCallbackTimerNoRestart(string timerName)
            {
                return new DebugEvent(RangeStart + 510, string.Format("'{0}' NO restart.", timerName));
            }

            internal static DebugEvent IntervalCallbackTimerCallback(string timerName)
            {
                return new DebugEvent(RangeStart + 511, string.Format("'{0}' callback.", timerName));
            }

            internal static DebugEvent IntervalCallbackTimerCallbackWillRunAt(string timerName, DateTime? nextEvent, TimeSpan interval, string autoRestart)
            {
                return new DebugEvent(RangeStart + 512, string.Format("'{0}' started. Next callback event: {1}. Interval set to: {2} ({3})", timerName, nextEvent, interval, autoRestart));
            }
        }
    }
}