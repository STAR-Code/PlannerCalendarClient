using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    public class CalendarEventSynchronizer
    {
        public static CalendarEventSyncResult SynchronizeCalendarEvent(CalendarEvent calEvent, CalendarEventItem calendarEventItem)
        {
            if (calEvent.HasPendingSyncLogs())
            {
                // Event exists in PCC - a SyncLog is pending to update Planner - do nothing
                return CalendarEventSyncResult.PendingSyncLogs;
            }

            //No pending synclogs
            if (calEvent.IsDeleted)
            {
                // Event exists in PCC, but is marked as deleted.
                // There is no pending synclog to delete the event from Planner - create it
                var newSyncLog = CreateNewSyncLogForDeletion(calendarEventItem);
                calEvent.SyncLogs.Add(newSyncLog);
                return CalendarEventSyncResult.Deleted;
            }

            SyncLog latestSync;
            if (!calEvent.TryGetLatestSyncLog(out latestSync))
            {
                // Synclog is missing for the calendar event, should never happen
                return CalendarEventSyncResult.MissingSyncLogs;
            }

            if (latestSync.IsPlannerOriginated)
            {
                // Event is originated from Planner - do nothing
                return CalendarEventSyncResult.UpToDate;
            }

            if (latestSync.IsMatchingTime(calendarEventItem.Start, calendarEventItem.End))
            {
                // Event exists in PCC - start and end times match Planner - do nothing
                return CalendarEventSyncResult.UpToDate;
            }

            // Event exists in PCC, but the start and/or end times do not match
            // There is no pending synclog to update the event in Planner - create it
            calEvent.SyncLogs.Add(latestSync.CopySyncLog());
            return CalendarEventSyncResult.Updated;
        }

        internal static CalendarEvent CreateNewCalendarEventForDeletion(CalendarEventItem calendarEventItem)
        {
            var newCalendarEvent = calendarEventItem.ToCalendarEvent();
            newCalendarEvent.IsDeleted = true;

            var newSyncLog = CreateNewSyncLogForDeletion(calendarEventItem);
            newCalendarEvent.SyncLogs.Add(newSyncLog);
            return newCalendarEvent;
        }

        private static SyncLog CreateNewSyncLogForDeletion(CalendarEventItem calendarEventItem)
        {
            var newSyncLog = calendarEventItem.ToSyncLog();
            newSyncLog.Operation = Constants.SyncLogOperationDELETE;
            return newSyncLog;
        }

        public enum CalendarEventSyncResult
        {
            /// <summary>
            /// There are pending synclogs for the event
            /// </summary>
            PendingSyncLogs,
            /// <summary>
            /// The event is up-to-date
            /// </summary>
            UpToDate,
            /// <summary>
            /// The event will be deleted
            /// </summary>
            Deleted,
            /// <summary>
            /// The event will updated
            /// </summary>
            Updated,
            /// <summary>
            /// The event doesn't have any synclog
            /// </summary>
            MissingSyncLogs
        }
    }
}