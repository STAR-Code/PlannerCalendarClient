using System;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ServiceDfdg
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.ServiceDfdg;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static ErrorEvent UnexpectedException
            {
                get { return new ErrorEvent(RangeStart + 901, "UnexpectedException thrown");}
            }

            internal static ErrorEvent FaultException
            {
                get { return new ErrorEvent(RangeStart + 902, "Service call resulted in a FaultException/Soap Exception"); }
            }

            internal static ErrorEvent FaultDetailException
            {
                get { return new ErrorEvent(RangeStart + 903, "Service call resulted in a FaultDetailException/Soap Exception"); }
            }

            internal static ErrorEvent TimeoutException
            {
                get { return new ErrorEvent(RangeStart + 904, "Service call resulted in a timeout Exception"); }
            }

            internal static ErrorEvent WebException
            {
                get { return new ErrorEvent(RangeStart + 905, "Service call resulted in a WebException"); }
            }
        }

        internal class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            public static InfoEvent EndpointUrl(string url)
            {
                return new InfoEvent(RangeStart + 101, string.Format("Url for the endpoint: {0}.", url));
            }

            internal static InfoEvent CallGetEvents(string departmentNumber, DateTime fromDate, DateTime toDate, string mailAddresses, string endpointName)
            {
                return new InfoEvent(RangeStart + 102, string.Format("Call GetEvents with departmentNumber {0} for the period {1} - {2} for the mail addresses {3} on the endpoint '{4}'.", departmentNumber, fromDate, toDate, mailAddresses, endpointName));
            }

            internal static InfoEvent CallServiceMethod(string method, int itemsCount, string endpoint)
            {
                return new InfoEvent(RangeStart + 103, string.Format("Call {0} with {1} calendarEventItems on the endpoint '{2}'.", method, itemsCount, endpoint));
            }

            internal static InfoEvent CallSuccess(Guid referenceId)
            {
                return new InfoEvent(RangeStart + 104, string.Format("Call completed successfully with call reference id '{0}'.", referenceId));
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            #region Planner ECS errors
            // 200 - 249 reserved for Planner service errors

            /// <summary>
            /// Creates a Warning event from the specified Planner error
            /// </summary>
            /// <param name="plannerEventErrorCode"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public static WarningEvent GetPlannerErrorCodeWarning(int plannerEventErrorCode, string message)
            {
                if (plannerEventErrorCode >= ushort.MinValue && plannerEventErrorCode <= ushort.MaxValue)
                    return new WarningEvent((ushort)(RangeStart + 200 + plannerEventErrorCode), message);

                return new WarningEvent(RangeStart + 249, string.Format("The Planner error code is invalid: {0} - {1}", plannerEventErrorCode, message));
            }


            #endregion Planner ECS errors
        }

        internal class DebugEvent : DebugEventIdBase
        {
            private DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            //internal static DebugEvent General(string message = "")
            //{
            //    return new DebugEvent(RangeStart + 501, string.Format("{0}", message));
            //}

            internal static DebugEvent ServiceRepositoryConstructed(string certificate, string endpoint)
            {
                return new DebugEvent(RangeStart + 502, string.Format("ServiceRepository constructed with the parameter, Certificate: {0}, WCF endpoint: {1}.", certificate, endpoint));
            }
        }
    }
}