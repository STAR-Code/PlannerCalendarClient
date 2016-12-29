using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    /// <summary>
    /// Class for grouping SyncLogs into Create, Update, Delete groups. 
    /// There is a limitation on the service on events pr. call, hence the bucket max size.
    /// </summary>
    public class SyncLogBucket
    {
        public List<SyncLog> CreateEvents { get; private set; }
        public List<SyncLog> UpdateEvents { get; private set; }
        public List<SyncLog> DeleteEvents { get; private set; }

        private SyncLogBucket(List<SyncLog> createEvents, List<SyncLog> updateEvents, List<SyncLog> deleteEvents)
        {
            CreateEvents = createEvents;
            UpdateEvents = updateEvents;
            DeleteEvents = deleteEvents;
        }

        /// <summary>
        /// Gets three buckets of C,U,D SyncLogs. 
        /// Returns once one of the buckets reaches max size, in order to maintain chronological order
        /// </summary>
        /// <returns></returns>
        public static SyncLogBucket GetEventBucket(List<SyncLog> syncLogs, ILogger logger, int bucketMaxSize)
        {
            var groupedEvents = new List<List<SyncLog>> { new List<SyncLog>(), new List<SyncLog>(), new List<SyncLog>() };
            
            foreach (var e in syncLogs)
            {
                if (groupedEvents.Any(i => i.Count() == bucketMaxSize)) // If one of the groups has reached max we are done.
                    break;

                switch (e.Operation)
                {
                    case Constants.SyncLogOperationCREATE:
                        {
                            groupedEvents[0].Add(e);
                            break;
                        }
                    case Constants.SyncLogOperationUPDATE:
                        {
                            var now = DateTime.Now;
                            // Look for previous update SyncLog for the same calendar event. Set syncdate to now and remove.
                            var e1 = e;
                            var prevUpdates = groupedEvents[1].Where(q => q.CalendarEventId == e1.CalendarEventId).ToList();
                            prevUpdates.ForEach(w => w.SyncDate = now);
                            prevUpdates.ForEach(p => groupedEvents[1].Remove(p));

                            groupedEvents[1].Add(e);
                            break;
                        }
                    case Constants.SyncLogOperationDELETE:
                        {
                            groupedEvents[2].Add(e);
                            break;
                        }
                    default:
                        {
                            logger.LogError(LoggingEvents.ErrorEvent.UnknownSyncLogOperation(e.Operation));
                            break;
                        }
                }
            }

            return new SyncLogBucket(groupedEvents[0], groupedEvents[1], groupedEvents[2]);
        }
    }
}