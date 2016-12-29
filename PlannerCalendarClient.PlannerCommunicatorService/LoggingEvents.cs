using System;
using System.Linq;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.PlannerCommunicatorService;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            {
            }

            public static ErrorEvent ServiceStartException()
            {
                return new ErrorEvent(RangeStart + 902, "Unexpected exception occured during service start.");
            }

            internal static ErrorEvent CalendarSynchronizerException()
            {
                return new ErrorEvent(RangeStart + 903, "Unexpected exception thrown in PlannerCommunicator.CalendarSynchronizer"); 
            }

            internal static ErrorEvent CalendarUpdaterException()
            {
                return new ErrorEvent(RangeStart + 904, "Unexpected exception thrown in PlannerCommunicator.CalendarUpdater"); 
            }

            internal static ErrorEvent ResourceUpdaterException()
            {
                return new ErrorEvent(RangeStart + 905, "Unexpected exception thrown in PlannerCommunicator.ResourceUpdater");
            }

            internal static ErrorEvent ResourceUpdaterSaveDbException()
            {
                return new ErrorEvent(RangeStart + 906, "Unexpected exception thrown in PlannerCommunicator.ResourceUpdater when saving the resource change to the database");
            }

            internal static ErrorEvent ServiceBaseRunException()
            {
                return new ErrorEvent(RangeStart + 907, "Unexpected exception thrown in Servicebase.Run(PlannerCommunicatorService)");
            }

            internal static ErrorEvent ServiceCreateException()
            {
                return new ErrorEvent(RangeStart + 908, "Unexpected exception thrown when creating the Service");
            }

            internal static ErrorEvent ServiceAsConsoleRunException()
            {
                return new ErrorEvent(RangeStart + 909, "Unexpected exception thrown when run PlannerCommunicatorService in console mode");
            }

            internal static ErrorEvent UnknownSyncLogOperation(string operation)
            {
                return new ErrorEvent(RangeStart + 931, string.Format("The synclog operation '{0}' is unknown.", operation));
            }

            internal static ErrorEvent FailedToSynchronizeCalendarEvent(long eventId)
            {
                return new ErrorEvent(RangeStart + 932, string.Format("Synchronization failed for calendar event id '{0}'.", eventId));
            }

            internal static ErrorEvent FailedToSynchronizeCalendarEventMissingSyncLog(long eventId, string mailAccount, string iCalId)
            {
                return new ErrorEvent(RangeStart + 933, string.Format("Synchronization failed for calendar event db-id '{0}' and mail {1} / ICalId {2}. It has no synclogs.", eventId, mailAccount, iCalId));
            }

            internal static ErrorEvent UnknownServiceCommand(int command)
            {
                return new ErrorEvent(RangeStart + 934, string.Format("Unknown service command received: {0}", command));
            }
        
            internal static ErrorEvent ErrorRunningServiceCommand(int command, string commandName)
            {
                return new ErrorEvent(RangeStart + 935, string.Format("Error running the service command: {0} \"{1}\"", command, commandName));
            }

            internal static ErrorEvent CalendarSynchronizerForMailAccountsException(string[] mailBoxes)
            {
                var mails = string.Join(",", mailBoxes ?? new string[] {});
                return new ErrorEvent(RangeStart + 936, string.Format("Exception thrown in PlannerCommunicator.CalendarSynchronizer retrieving calendar event for the mailboxes: {0}", mails));
            }

            internal static ErrorEvent CalendarUpdaterForMailBoxException(string mailBox)
            {
                return new ErrorEvent(RangeStart + 937,string.Format("Unexpected exception thrown in PlannerCommunicator.CalendarUpdater for mailbox \"{0}\"", mailBox));
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            {
            }

            internal static WarningEvent DuplicateInstanceOfMailAddress(string mailAddress)
            {
                return new WarningEvent(RangeStart + 201, string.Format("Found duplicate instance of mailaddress ({0}) that can not be synchronized", mailAddress));
            }

            internal static WarningEvent NoResourcesFoundInPlanner()
            {
                return new WarningEvent(RangeStart + 202, "No Resources returned from Planner for Jobcenter"); 
            }
        }

        public class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static InfoEvent ServiceStart(string name, string version, string path, string releaseBuild)
            {
                return new InfoEvent(RangeStart + 101, string.Format("Service started. {0} version: {1} Executable path: '{2}' {3}", name, version, path, releaseBuild));
            }

            internal static InfoEvent ServiceStop(int exitCode)
            {
                return new InfoEvent(RangeStart + 103, string.Format("Service stopped. Exit code {0}", exitCode));
            }

            internal static InfoEvent ConfigurationInfo(string info)
            {
                return new InfoEvent(RangeStart + 104, string.Format("{0}", info));
            }

            internal static InfoEvent UpdatedResourcesFinished(int rowCount, TimeSpan duration)
            {
                return new InfoEvent(RangeStart + 105, string.Format("Updated list of Planner-resources ({0} rows affected) - duration: {1}", rowCount, duration));
            }

            internal static InfoEvent NoWhitelistFilterApplied()
            {
                return new InfoEvent(RangeStart + 106, "Whitelist in database is empty, so filter not applied"); 
            }

            internal static InfoEvent WhitelistFilterApplied(int resourceCount)
            {
                return new InfoEvent(RangeStart + 107, string.Format("Whitelist filter applied ({0} resources in list now)", resourceCount));
            }

            internal static InfoEvent UpdateResourcesStarting()
            {
                return new InfoEvent(RangeStart + 108, "Update list of Planner-resources starting");
            }

            internal static InfoEvent UpdateCalendarEventsCompleted(int affectedMailBoxes, TimeSpan duration)
            {
                return new InfoEvent(RangeStart + 131, string.Format("Updated calendar events in Planner for {0} mailboxes - duration: {1}", affectedMailBoxes, duration));
            }

            internal static InfoEvent SynchronizationOfCalendarEventsCompleted(int eventCount, TimeSpan duration)
            {
                return new InfoEvent(RangeStart + 151, string.Format("Synchronized calendar events with Planner ({0} events affected) - duration: {1}", eventCount, duration));
            }

            internal static InfoEvent RunServiceCommand(int command, string commandName)
            {
                return new InfoEvent(RangeStart + 152, string.Format("Running service command: {0} \"{1}\"", command, commandName));
            }
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

            internal static DebugEvent UpdatedResourcesEndServiceCall(int resourceCount, string jobcenterNumber)
            {
                return new DebugEvent(RangeStart + 503, string.Format("Returned list of resources from Planner-service ({0}) for the Jobcenter {1}", resourceCount, jobcenterNumber));
            }

            internal static DebugEvent RemovingDuplicates(int count)
            {
                return new DebugEvent(RangeStart + 504, string.Format("Removed duplicates from resource-list ({0})", count));
            }

            internal static DebugEvent IdentifyingNewResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 505, string.Format("Identified new resources in resource-list (count {0}: {1})", count, resources));
            }

            internal static DebugEvent RemovingResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 506, string.Format("Identified removed resources in resource-list (count: {0}: {1})", count, resources));
            }

            internal static DebugEvent LoadedWhitelistFromDatabase(int count)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Loaded list of whitelisted resources from database (count {0})", count));
            }

            internal static DebugEvent UpdatedResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 508, string.Format("Identified updated resources in resource-list (count {0}: {1})", count, resources));
            }

            internal static DebugEvent LoadedBlacklistFromDatabase(int count)
            {
                return new DebugEvent(RangeStart + 509, string.Format("Loaded list of blacklisted resources from database (count {0})", count));
            }

            internal static DebugEvent BlacklistedResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 510, string.Format("Did not look at blacklisted resources in resource-list (count {0}: {1})", count, resources));
            }

            internal static DebugEvent WhitelistedResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 511, string.Format("Blocked non-whitelisted resources in resource-list (count {0}: {1})", count, resources));
            }

            internal static DebugEvent IdentifyingNewNonWhitelistedResources(int count, string resources)
            {
                return new DebugEvent(RangeStart + 512, string.Format("Identified new non-whitelisted resources in resource-list (count {0}: {1})", count, resources));
            }

            internal static DebugEvent CreateCalendarEventForDeletion(long eventId, string mailAccount, string iCalId)
            {
                return new DebugEvent(RangeStart + 531, string.Format("Created CalendarEvent and SyncLog for deletion of calendar event db-id {0} and mail {1} / ICalId \"{2}\".", eventId, mailAccount, iCalId));
            }

            internal static DebugEvent CreateSyncLogForDeletion(long eventId, string mailAccount, string iCalId)
            {
                return new DebugEvent(RangeStart + 532, string.Format("Created SyncLog for deletion of calendar event db-id {0} and mail {1} / ICalId \"{2}\".", eventId, mailAccount, iCalId));
            }

            internal static DebugEvent EventUpToDate(long eventId, string mailAccount, string iCalId)
            {
                return new DebugEvent(RangeStart + 533, string.Format("Calendar event with db-id {0} and mail {1} / ICalId \"{2}\" is up-to-date.", eventId, mailAccount, iCalId));
            }

            internal static DebugEvent PendingSyncLog(long eventId, string mailAccount, string iCalId)
            {
                return new DebugEvent(RangeStart + 534, string.Format("Calendar event with db-id {0} and mail {1} / ICalId \"{2}\" has pending synclogs.", eventId, mailAccount, iCalId));
            }

            internal static DebugEvent CreateSyncLogForCreation(long eventId, string mailAccount, string iCalId)
            {
                return new DebugEvent(RangeStart + 535, string.Format("Created SyncLog for create of calendar event db-id {0} and mail {1} / ICalId \"{2}\".", eventId, mailAccount, iCalId));
            }

            internal static DebugEvent CalendarSynchronizerForMailAccounts(string[] mailBoxes, int noOfCalendars)
            {
                var mails = string.Join(",", mailBoxes ?? new string[] { });
                return new DebugEvent(RangeStart + 536, string.Format("PlannerCommunicator.CalendarSynchronizer retrieving calendar event for the mailboxes: {0}", mails));
            }
        }
    }
}