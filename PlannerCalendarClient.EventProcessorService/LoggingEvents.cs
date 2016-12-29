using PlannerCalendarClient.Logging;
using System;

namespace PlannerCalendarClient.EventProcessorService
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.EventProcessorService;

        public class ErrorEvent : ErrorEventIdBase
        {
            protected ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            public static ErrorEvent ExceptionThrown(string data = "")
            {
                return new ErrorEvent(RangeStart + 901, string.Format("Exception catch in: '{0}'", data));
            }

            public static ErrorEvent ServiceStartException(string data = "")
            {
                return new ErrorEvent(RangeStart + 902, string.Format("Exception occured during service start: {0}", data));
            }

            public static ErrorEvent ExceptionWhileRetrievingAppointment(string message = "")
            {
                return new ErrorEvent(RangeStart + 903, string.Format("Exception occured while retrieving appointment: {0}", message));
            }

            internal static ErrorEvent ExceptionWhileHandlingNotification(long notificationId, string ewsId)
            {
                return new ErrorEvent(RangeStart + 905, string.Format("Exception occured while handling notification. NotificationId = {0}; EwsId = {1}", notificationId, ewsId));
            }

            internal static ErrorEvent ExceptionWhileQualifyingOrUpdatingDatabase(string emailAddress, string iCalId)
            {
                return new ErrorEvent(RangeStart + 906, string.Format("Exception occured while qualifying or updating database hereof. EmailAddress = {0}; ICalId = {1}", emailAddress, iCalId));
            }

            internal static ErrorEvent ExceptionWhileGettingProvider(string serverUserEmailAccount)
            {
                return new ErrorEvent(RangeStart + 907, string.Format("Exception while constructing appointment provider! ServerUserEmailAccount = {0}", serverUserEmailAccount));
            }

            internal static ErrorEvent ExceptionWhileDeletingNotification(long notificationId, string ewsId)
            {
                return new ErrorEvent(RangeStart + 908, string.Format("Exception while removing the notification! Id = {0}; EwsId = {1}", notificationId, ewsId));
            }

            public static ErrorEvent ErrorExchangeMailboxNotFound(string ewsId, string mailbox)
            {
                return new ErrorEvent(RangeStart + 909, string.Format("Unable to resolve appointment mailbox for EwsId = {0}; ServiceUserMailbox = {1}", ewsId, mailbox));
            }

            public static ErrorEvent ExceptionWhileRetrievingMailboxAppointment(string mailbox)
            {
                return new ErrorEvent(RangeStart + 951, string.Format("Exception occured while retrieving appointments from mailbox '{0}'.", mailbox));
            }

            public static ErrorEvent ExceptionWhilePersistMailboxAppointment(string mailbox)
            {
                return new ErrorEvent(RangeStart + 952, string.Format("Exception occured while persisting appointments for mailbox {0}:", mailbox));
            }

            public static ErrorEvent ExceptionWhileDeletingMailboxAppointment(string mailbox)
            {
                return new ErrorEvent(RangeStart + 953, string.Format("Exception occured while deleting appointment as part of full pull. Mailbox: {0}:", mailbox));
            }

            public static ErrorEvent AppointmentOccurresUnfoldError(string mailbox)
            {
                return new ErrorEvent(RangeStart + 954, string.Format("Exception occured while unfold an master appointment. Mailbox: {0}:", mailbox));
            }

            internal static ErrorEvent UnknownServiceCommand(int command)
            {
                return new ErrorEvent(RangeStart + 956, string.Format("Unknown service command received: {0}", command));
            }

            internal static ErrorEvent ErrorRunningServiceCommand(int command, string commandName)
            {
                return new ErrorEvent(RangeStart + 957, string.Format("Error running the service command: {0} \"{1}\"", command, commandName));
            }

            internal static ErrorEvent ErrorExchangeAppointmentCalIdEmptyException(string ewsId)
            {
                return new ErrorEvent(RangeStart + 958, string.Format("There is no ICalId for appointment with exchange id = {0}", ewsId));
            }

            internal static ErrorEvent ErrorExchangeAppointmentCalRecurrenceIdEmptyException(string ewsId, string calId)
            {
                return new ErrorEvent(RangeStart + 959, string.Format("There is no ICalRecurrenceId for appointment with id = {0}, CalId {1}", ewsId, calId));
            }

            internal static ErrorEvent ErrorUpdateLastFullSyncForResource(string mailbox, Guid? resourceId)
            {
                return new ErrorEvent(RangeStart + 960, string.Format("Failed to update LastFullSync for mailbox = {0}, resource = {1}", mailbox, resourceId));
            }
        }

        public class InfoEvent : InfoEventIdBase
        {
            internal InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            public static InfoEvent ServiceStart(string name, string version, string path, string releaseBuild)
            {
                return new InfoEvent(RangeStart + 101, string.Format("Service started. {0} version: {1} Executable path: \"{2}\" {3}", name, version, path, releaseBuild));
            }

            public static InfoEvent ServiceStop(int code)
            {
                return new InfoEvent(RangeStart + 103, string.Format("Service stopped. Exit code {0}", code));
            }

            public static InfoEvent ConfigurationInfo(string message, params object[] parameters)
            {
                return new InfoEvent(RangeStart + 111, message.SafeFormat(parameters));
            }

            public static InfoEvent FullPullStart(DateTime startDate, DateTime endDate)
            {
                return new InfoEvent(RangeStart + 112, string.Format("Full pull sync starting: Interval from {0} to {1}", startDate, endDate));
            }

            internal static InfoEvent RunServiceCommand(int command, string commandName)
            {
                return new InfoEvent(RangeStart + 113, string.Format("Running service command: {0} \"{1}\"", command, commandName));
            }

            public static InfoEvent FullPullEnd(TimeSpan duration, int emailboxRetrievedTotal)
            {
                return new InfoEvent(RangeStart + 114, string.Format("Full pull sync ended: Duration {0}, mailboxes synchonized: {1}", duration, emailboxRetrievedTotal));
            }

            public static InfoEvent FullPullStartMailbox(string mailbox)
            {
                return new InfoEvent(RangeStart + 115, string.Format("Full pull sync starting for mailbox = {0}", mailbox));
            }

            public static InfoEvent FullPullEndMailbox(string mailbox, TimeSpan duration)
            {
                return new InfoEvent(RangeStart + 116, string.Format("Full pull sync ended for mailbox = {0}, Duration = {1}", mailbox, duration));
            }
        }

        public class WarningEvent : WarningEventIdBase
        {
            public WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            public static WarningEvent AppointmentCouldNotBeExtractedFromExchangeBecauseOfCalId(string mailBox, string id, DateTime start, DateTime end, Exception ex)
            {
                return new WarningEvent(RangeStart + 201, string.Format("Could not get CalId from Appointment: Mail={0}, Start={1}, End={2}, Id={3}, Exception={4}", mailBox, start, end, id, ex.Message));
            }
        }

        public class DebugEvent : DebugEventIdBase
        {
            public DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            public static DebugEvent General(string message, params object[] parameters)
            {
                return new DebugEvent(RangeStart + 501, message.SafeFormat(parameters));
            }

            public static DebugEvent UnfoldRecurringMasterAppointment(int index, string detail)
            {
                return new DebugEvent(RangeStart + 502, string.Format("UnfoldRecurringMaster {0}: {1}", index, detail));
            }

            public static DebugEvent IsMasterAppointment(string uniqueId)
            {
                return new DebugEvent(RangeStart + 503, string.Format("Is recurring master appointment {0}", uniqueId));
            }

            public static DebugEvent OccurrencePattern(string patternType, DateTime periodStartDate, DateTime periodEndDate)
            {
                return new DebugEvent(RangeStart + 504, string.Format("Occurres Pattern: {0}, Period start and end Date: {1} - {2}", patternType, periodStartDate, periodEndDate));
            }

            public static DebugEvent EmptyOccurresPeriod(int numberOfOccurrences, int startIndex)
            {
                return new DebugEvent(RangeStart + 505, string.Format("Occurres period is empty. period index {0} < Start Index {1}", numberOfOccurrences, startIndex));
            }

            public static DebugEvent AppointmentHasNoICalId(string icalId)
            {
                return new DebugEvent(RangeStart + 506, string.Format("Retrieve appointment has no ICalId! Using Exchange UniqueId instead. EwsId: {0}", icalId));
            }

            public static DebugEvent CallExchangeAppointmentBind(string place)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Call exchange Appointment.Bind() from: {0}", place));
            }

            public static DebugEvent CallExchangeFindAppointments(string place)
            {
                return new DebugEvent(RangeStart + 508, string.Format("Call exchange FindAppointments() from: {0}", place));
            }

            public static DebugEvent CallExchangeFolderBind(string place)
            {
                return new DebugEvent(RangeStart + 509, string.Format("Call exchange Folder.Bind() from: {0}", place));
            }

            public static DebugEvent CallExchangeConvertId(string place)
            {
                return new DebugEvent(RangeStart + 510, string.Format("Call exchange ConvertId() from: {0}", place));
            }

            public static DebugEvent CallExchangeCalendarFolderBind(string place)
            {
                return new DebugEvent(RangeStart + 511, string.Format("Call exchange Folder.Bind, place: {0}", place));
            }

            internal static DebugEvent OccurrenceFilterInfo(string message)
            {
                return new DebugEvent(RangeStart + 512, string.Format("Occurres Filter info: {0}", message));
            }

            public static DebugEvent CallExchangeAppointmentBindImpersonated(string place, string mailbox)
            {
                return new DebugEvent(RangeStart + 513, string.Format("Call exchange Appointment.Bind impersonating mailbox \"{0}\" from {1}", mailbox, place));
            }

            internal static DebugEventIdBase CallExchangeFindAppointmentsImpersonated(string place, string mailbox)
            {
                return new DebugEvent(RangeStart + 514, string.Format("Call exchange FindAppointments impersonating mailbox \"{0}\" from {1}", mailbox, place));
            }
        }
    }
}