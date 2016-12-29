using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    /// <summary>
    /// Class containing helper methods for calendar events
    /// </summary>
    public static class CalendarEventHelper
    {
        /// <summary>
        /// Creates a SyncLog from a CalendarEventItem
        /// </summary>
        /// <param name="calendarEventItem"></param>
        /// <returns></returns>
        public static SyncLog ToSyncLog(this CalendarEventItem calendarEventItem)
        {
            var syncLog = new SyncLog
            {
                CreatedDate = DateTime.Now,
                CalendarEnd = calendarEventItem.End,
                CalendarStart = calendarEventItem.Start,
            };

            return syncLog;
        }

        /// <summary>
        /// Creates a CalendarEvent from a CalendarEventItem
        /// </summary>
        /// <param name="calendarEventItem"></param>
        /// <returns></returns>
        public static CalendarEvent ToCalendarEvent(this CalendarEventItem calendarEventItem)
        {
            return new CalendarEvent
            {
                CalId = calendarEventItem.OriginId,
                MailAddress = calendarEventItem.OriginMailAddress,
                PlannerResourceId = calendarEventItem.PlannerResourceId,
                PlannerCalendarEventId = calendarEventItem.PlannerCalendarEventId,
                IsDeleted = calendarEventItem.HasBeenDeleted
            };
        }

        /// <summary>
        /// Gets the latest SyncLog for a CalendarEvent
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <param name="syncLog"></param>
        /// <returns></returns>
        public static bool TryGetLatestSyncLog(this CalendarEvent calendarEvent, out SyncLog syncLog)
        {
            syncLog = calendarEvent.SyncLogs.OrderByDescending(sl => sl.CreatedDate).FirstOrDefault();

            return syncLog != null;
        }

        /// <summary>
        /// Determines whether a CalendarEvent has pending SyncLogs
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public static bool HasPendingSyncLogs(this CalendarEvent calendarEvent)
        {
            return calendarEvent.SyncLogs.Any(sl => sl.SyncDate == null);
        }

        /// <summary>
        /// Determines whether a CalendarEvent matches the specified CalendarEventItem
        /// by comparing start and end times
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <param name="calendarEventItem"></param>
        /// <returns></returns>
        public static bool HasMatchingTimes(this CalendarEvent calendarEvent, CalendarEventItem calendarEventItem)
        {
            SyncLog latestSyncLog;
            return (calendarEvent.TryGetLatestSyncLog(out latestSyncLog) &&
                    latestSyncLog.CalendarStart == calendarEventItem.Start &&
                    latestSyncLog.CalendarEnd == calendarEventItem.End);
        }

        public static bool IsMatchingTime(this SyncLog syncLog, DateTime start, DateTime end)
        {
            return syncLog != null && syncLog.CalendarStart == start && syncLog.CalendarEnd == end;
        }
       
        /// <summary>
        /// Converts a list of SyncLogs to a list of CalendarEventItems
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public static IEnumerable<CalendarEventItem> ToCalendarEventItems(this IEnumerable<SyncLog> events)
        {
            var calendarEventItems = events.Select(ConvertToCalendarEventItem);
            return calendarEventItems;
        }

        private static CalendarEventItem ConvertToCalendarEventItem(this SyncLog syncLog)
        {
            var calendarEvent = syncLog.CalendarEvent;

            var calenderEventItem = new CalendarEventItem
            {
                End = syncLog.CalendarEnd,
                HasBeenDeleted = calendarEvent.IsDeleted,
                OriginId = calendarEvent.CalId,
                OriginMailAddress = calendarEvent.MailAddress,
                PlannerCalendarEventId = calendarEvent.PlannerCalendarEventId,
                PlannerResourceId = calendarEvent.PlannerResourceId,
                Start = syncLog.CalendarStart,
                // Do not set SyncLogItem; it is set from the service call
            };

            return calenderEventItem;
        }

        /// <summary>
        /// Creates a new ServiceCallReferenceLog from a ServiceCallReferenceItem
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="serviceCallReferenceItem"></param>
        /// <returns></returns>
        public static ServiceCallReferenceLog ToServiceCallReferenceItem(IECSClientExchangeDbEntities entities, ServiceCallReferenceItem serviceCallReferenceItem)
        {
            var newItem = entities.ServiceCallReferenceLogs.Create();
            newItem.CallEnded = serviceCallReferenceItem.CallEnded;
            newItem.CallStarted = serviceCallReferenceItem.CallStarted;
            newItem.Operation = serviceCallReferenceItem.OperationName.ToString().Substring(0, 1);
            newItem.ResponseText = serviceCallReferenceItem.ResponsText;
            newItem.Success = serviceCallReferenceItem.Success;
            newItem.ServiceCallResponseReferenceId = serviceCallReferenceItem.ServiceCallResponseReferenceId;
            entities.ServiceCallReferenceLogs.Add(newItem);
            return newItem;
        }

        /// <summary>
        /// Updates a SyncLog with data from SyncLogItem and ServiceCallReferenceItem after a call
        /// </summary>
        /// <param name="syncLog"></param>
        /// <param name="syncLogItem"></param>
        /// <param name="serviceCallReferenceItem"></param>
        public static void UpdateSyncLog(this SyncLog syncLog, SyncLogItem syncLogItem, ServiceCallReferenceLog serviceCallReferenceLog)
        {
            syncLog.PlannerEventErrorCode = syncLogItem.PlannerEventErrorCode;
            syncLog.PlannerSyncResponse = syncLogItem.PlannerSyncResponse;
            syncLog.PlannerSyncSuccess = syncLogItem.PlannerSyncSuccess;
            syncLog.SyncDate = syncLogItem.SyncDate;
            syncLog.ServiceCallReferenceLog = serviceCallReferenceLog;
        }

        /// <summary>
        /// Creates a copy of a SyncLog
        /// </summary>
        /// <param name="syncLog"></param>
        /// <returns></returns>
        public static SyncLog CopySyncLog(this SyncLog syncLog)
        {
            var syncLogCopy = new SyncLog
            {
                CreatedDate = DateTime.Now,
                CalendarEnd = syncLog.CalendarEnd,
                CalendarStart = syncLog.CalendarStart,
                Operation = syncLog.Operation
            };

            return syncLogCopy;
        }

        /// <summary>
        /// Chunks a list into smaller lists of specified size
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
